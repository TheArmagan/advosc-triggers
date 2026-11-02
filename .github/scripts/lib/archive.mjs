// Tar and zip without third-party packages. Output is deterministic: the same files give the same
// bytes on any run, so re-building a tag can't change a published zipSHA256.
import { crc32, deflateRawSync, gzipSync, inflateRawSync } from "node:zlib";

const ZIP_DOS_DATE = 0x0021; // 1980-01-01, the earliest date a zip can hold.
const ZIP_UTF8_NAMES = 0x0800;
const ZIP_DEFLATE = 8;

function tarHeader(name, size, type) {
  const nameBytes = Buffer.from(name, "utf8");
  if (nameBytes.length > 100) throw new Error(`Tar entry name is too long: ${name}`);

  const header = Buffer.alloc(512);
  nameBytes.copy(header, 0);
  const octal = (value, width, offset) =>
    header.write(`${value.toString(8).padStart(width - 1, "0")}\0`, offset, "ascii");

  octal(type === "5" ? 0o755 : 0o644, 8, 100);
  octal(0, 8, 108);
  octal(0, 8, 116);
  octal(size, 12, 124);
  octal(0, 12, 136);
  header.write(" ".repeat(8), 148, "ascii");
  header.write(type, 156, "ascii");
  header.write("ustar\0" + "00", 257, "ascii");

  let sum = 0;
  for (const byte of header) sum += byte;
  header.write(`${sum.toString(8).padStart(6, "0")}\0 `, 148, "ascii");
  return header;
}

/** `entries` are `{ name, data }`. Leave out `data` for a folder entry. */
export function createTarGz(entries) {
  const blocks = [];
  for (const { name, data } of entries) {
    if (data === undefined) {
      blocks.push(tarHeader(name.endsWith("/") ? name : `${name}/`, 0, "5"));
      continue;
    }
    blocks.push(tarHeader(name, data.length, "0"), data);
    const padding = (512 - (data.length % 512)) % 512;
    if (padding) blocks.push(Buffer.alloc(padding));
  }
  blocks.push(Buffer.alloc(1024));
  return gzipSync(Buffer.concat(blocks), { level: 9 });
}

/** `entries` are `{ name, data }`, and names use forward slashes. */
export function createZip(entries) {
  const locals = [];
  const centrals = [];
  let offset = 0;

  for (const { name, data } of entries) {
    const nameBytes = Buffer.from(name, "utf8");
    const packed = deflateRawSync(data, { level: 9 });
    const checksum = crc32(data);

    const local = Buffer.alloc(30);
    local.writeUInt32LE(0x04034b50, 0);
    local.writeUInt16LE(20, 4);
    local.writeUInt16LE(ZIP_UTF8_NAMES, 6);
    local.writeUInt16LE(ZIP_DEFLATE, 8);
    local.writeUInt16LE(0, 10);
    local.writeUInt16LE(ZIP_DOS_DATE, 12);
    local.writeUInt32LE(checksum, 14);
    local.writeUInt32LE(packed.length, 18);
    local.writeUInt32LE(data.length, 22);
    local.writeUInt16LE(nameBytes.length, 26);
    locals.push(local, nameBytes, packed);

    const central = Buffer.alloc(46);
    central.writeUInt32LE(0x02014b50, 0);
    central.writeUInt16LE(20, 4);
    central.writeUInt16LE(20, 6);
    central.writeUInt16LE(ZIP_UTF8_NAMES, 8);
    central.writeUInt16LE(ZIP_DEFLATE, 10);
    central.writeUInt16LE(0, 12);
    central.writeUInt16LE(ZIP_DOS_DATE, 14);
    central.writeUInt32LE(checksum, 16);
    central.writeUInt32LE(packed.length, 20);
    central.writeUInt32LE(data.length, 24);
    central.writeUInt16LE(nameBytes.length, 28);
    central.writeUInt32LE(offset, 42);
    centrals.push(central, nameBytes);

    offset += local.length + nameBytes.length + packed.length;
  }

  const directory = Buffer.concat(centrals);
  const end = Buffer.alloc(22);
  end.writeUInt32LE(0x06054b50, 0);
  end.writeUInt16LE(entries.length, 8);
  end.writeUInt16LE(entries.length, 10);
  end.writeUInt32LE(directory.length, 12);
  end.writeUInt32LE(offset, 16);
  return Buffer.concat([...locals, directory, end]);
}

/** Reads one file out of a zip through its central directory. Returns null if it isn't there. */
export function readZipEntry(zip, wanted) {
  let end = zip.length - 22;
  while (end >= 0 && zip.readUInt32LE(end) !== 0x06054b50) end--;
  if (end < 0) throw new Error("Not a zip file");

  const count = zip.readUInt16LE(end + 10);
  let cursor = zip.readUInt32LE(end + 16);

  for (let i = 0; i < count; i++) {
    if (zip.readUInt32LE(cursor) !== 0x02014b50) throw new Error("Bad zip central directory");
    const method = zip.readUInt16LE(cursor + 10);
    const packedSize = zip.readUInt32LE(cursor + 20);
    const nameLength = zip.readUInt16LE(cursor + 28);
    const extraLength = zip.readUInt16LE(cursor + 30);
    const commentLength = zip.readUInt16LE(cursor + 32);
    const localOffset = zip.readUInt32LE(cursor + 42);
    const name = zip.toString("utf8", cursor + 46, cursor + 46 + nameLength);

    if (name === wanted) {
      const start =
        localOffset + 30 + zip.readUInt16LE(localOffset + 26) + zip.readUInt16LE(localOffset + 28);
      const packed = zip.subarray(start, start + packedSize);
      if (method === 0) return Buffer.from(packed);
      if (method === ZIP_DEFLATE) return inflateRawSync(packed);
      throw new Error(`Unsupported zip compression method ${method} for ${name}`);
    }
    cursor += 46 + nameLength + extraLength + commentLength;
  }
  return null;
}
