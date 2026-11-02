// Builds the VPM zip and the .unitypackage for a tag. Refuses to build when the tag and
// package.json disagree, or when any shipped file has no .meta with a guid.
//
//   node .github/scripts/build-release.mjs --tag v0.1.0 [--root .] [--out dist]
import { createHash } from "node:crypto";
import { appendFileSync, existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { parseArgs } from "node:util";
import { createTarGz, createZip } from "./lib/archive.mjs";

const PACKAGE_ID = "rest.armagan.advosc-triggers";
const ROOT_FILES = ["package.json", "LICENSE", "README.md", "CHANGELOG.md"];
const ROOT_FOLDERS = ["Runtime", "Editor"];
const REQUIRED_DEPENDENCIES = ["com.vrcfury.vrcfury", "com.vrchat.avatars"];
const SEMVER = /^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$/;

function readManifest(root, tag) {
  const manifest = JSON.parse(readFileSync(join(root, "package.json"), "utf8"));
  const problems = [];

  if (manifest.name !== PACKAGE_ID) problems.push(`name is "${manifest.name}", not ${PACKAGE_ID}`);
  if (!SEMVER.test(manifest.version ?? "")) problems.push(`version "${manifest.version}" isn't semver`);
  if (tag !== `v${manifest.version}`) problems.push(`tag ${tag} doesn't match version ${manifest.version}`);
  if (!manifest.displayName) problems.push("displayName is missing");
  if (!manifest.author?.name) problems.push("author.name is missing");
  for (const dependency of REQUIRED_DEPENDENCIES) {
    if (!manifest.vpmDependencies?.[dependency]) problems.push(`vpmDependencies has no ${dependency}`);
  }

  if (problems.length) throw new Error(`package.json: ${problems.join("; ")}`);
  return manifest;
}

/** Lists everything that ships, in stable order. Unity skips dot and `~` names, so this does too. */
function collectAssets(root) {
  const assets = [];

  for (const file of ROOT_FILES) {
    if (!existsSync(join(root, file))) throw new Error(`Missing ${file}`);
    assets.push({ path: file, isFolder: false });
  }

  const walk = (folder) => {
    assets.push({ path: folder, isFolder: true });
    const entries = readdirSync(join(root, folder), { withFileTypes: true }).sort((a, b) =>
      a.name < b.name ? -1 : 1,
    );
    for (const entry of entries) {
      if (entry.name.startsWith(".") || entry.name.endsWith("~") || entry.name.endsWith(".meta")) continue;
      const path = `${folder}/${entry.name}`;
      if (entry.isDirectory()) walk(path);
      else if (entry.isFile()) assets.push({ path, isFolder: false });
      else throw new Error(`Only files and folders can ship: ${path}`);
    }
  };

  for (const folder of ROOT_FOLDERS) {
    if (!existsSync(join(root, folder))) throw new Error(`Missing ${folder}/`);
    walk(folder);
  }
  return assets;
}

function attachMeta(root, assets) {
  const seen = new Map();
  const missing = [];

  for (const asset of assets) {
    const metaPath = join(root, `${asset.path}.meta`);
    if (!existsSync(metaPath)) {
      missing.push(`${asset.path}.meta`);
      continue;
    }
    asset.meta = readFileSync(metaPath);
    const guid = asset.meta.toString("utf8").match(/^guid:\s*([0-9a-f]{32})\s*$/m)?.[1];
    if (!guid) throw new Error(`No guid in ${asset.path}.meta`);
    if (seen.has(guid)) throw new Error(`${asset.path} and ${seen.get(guid)} share guid ${guid}`);
    seen.set(guid, asset.path);
    asset.guid = guid;
  }

  if (missing.length) {
    throw new Error(
      `Missing .meta files. Open the package in Unity so it writes them, then commit:\n  ${missing.join("\n  ")}`,
    );
  }
}

function vpmZip(root, assets) {
  const entries = [];
  for (const asset of assets) {
    if (!asset.isFolder) entries.push({ name: asset.path, data: readFileSync(join(root, asset.path)) });
    entries.push({ name: `${asset.path}.meta`, data: asset.meta });
  }
  return createZip(entries);
}

/** Each asset is a guid folder holding `asset`, `asset.meta` and `pathname`. Folders have no `asset`. */
function unityPackage(root, assets) {
  const entries = [];
  for (const asset of assets) {
    entries.push({ name: asset.guid });
    if (!asset.isFolder) {
      entries.push({ name: `${asset.guid}/asset`, data: readFileSync(join(root, asset.path)) });
    }
    entries.push({ name: `${asset.guid}/asset.meta`, data: asset.meta });
    entries.push({
      name: `${asset.guid}/pathname`,
      data: Buffer.from(`Packages/${PACKAGE_ID}/${asset.path}`, "utf8"),
    });
  }
  return createTarGz(entries);
}

function releaseNotes(root, version, sha256) {
  const changelog = readFileSync(join(root, "CHANGELOG.md"), "utf8").replace(/\r\n/g, "\n");
  const lines = changelog.split("\n");
  const start = lines.findIndex((line) => line.startsWith(`## ${version}`));
  let body = "";
  if (start !== -1) {
    const next = lines.findIndex((line, index) => index > start && line.startsWith("## "));
    body = lines.slice(start + 1, next === -1 ? undefined : next).join("\n").trim();
  }
  return `${body}\n\nVPM zip SHA-256: \`${sha256}\`\n`.trimStart();
}

const { values } = parseArgs({
  options: {
    tag: { type: "string" },
    root: { type: "string", default: "." },
    out: { type: "string", default: "dist" },
  },
});
if (!values.tag) throw new Error("--tag is required");

const manifest = readManifest(values.root, values.tag);
const assets = collectAssets(values.root);
attachMeta(values.root, assets);

mkdirSync(values.out, { recursive: true });
const zip = vpmZip(values.root, assets);
const zipPath = join(values.out, `${PACKAGE_ID}-${manifest.version}.zip`);
const unityPackagePath = join(values.out, `ADVOSC-Triggers-${manifest.version}.unitypackage`);
const notesPath = join(values.out, "notes.md");
const sha256 = createHash("sha256").update(zip).digest("hex");

writeFileSync(zipPath, zip);
writeFileSync(unityPackagePath, unityPackage(values.root, assets));
writeFileSync(notesPath, releaseNotes(values.root, manifest.version, sha256));

const outputs = {
  version: manifest.version,
  zip: zipPath,
  unitypackage: unityPackagePath,
  notes: notesPath,
  sha256,
  prerelease: String(manifest.version.includes("-")),
};
for (const [key, value] of Object.entries(outputs)) console.log(`${key}=${value}`);
if (process.env.GITHUB_OUTPUT) {
  appendFileSync(
    process.env.GITHUB_OUTPUT,
    Object.entries(outputs).map(([key, value]) => `${key}=${value}\n`).join(""),
  );
}
