// Builds the VPM listing from the GitHub releases. Each version's data comes from the package.json
// inside its released zip, and zipSHA256 from that zip's bytes, so the listing can't disagree with
// what people download. A failed download stops the build, which keeps the last good listing live.
//
//   node .github/scripts/build-listing.mjs --out _site/index.json
import { createHash } from "node:crypto";
import { writeFileSync } from "node:fs";
import { parseArgs } from "node:util";
import { readZipEntry } from "./lib/archive.mjs";

const PACKAGE_ID = "rest.armagan.advosc-triggers";

const listing = {
  name: "ADVOSC Triggers",
  id: "rest.armagan.advosc-triggers.listing",
  url: "https://thearmagan.github.io/advosc-triggers/index.json",
  author: "TheArmagan",
  packages: {},
};

async function fetchReleases(api, repository, token) {
  const releases = [];
  for (let page = 1; ; page++) {
    const response = await fetch(`${api}/repos/${repository}/releases?per_page=100&page=${page}`, {
      headers: {
        Accept: "application/vnd.github+json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });
    if (!response.ok) throw new Error(`Listing releases failed: HTTP ${response.status}`);
    const batch = await response.json();
    releases.push(...batch);
    if (batch.length < 100) return releases;
  }
}

async function versionEntry(release, asset) {
  const response = await fetch(asset.browser_download_url);
  if (!response.ok) throw new Error(`${asset.name}: HTTP ${response.status}`);
  const zip = Buffer.from(await response.arrayBuffer());

  const manifestBytes = readZipEntry(zip, "package.json");
  if (!manifestBytes) throw new Error(`${asset.name} has no package.json at its root`);
  const manifest = JSON.parse(manifestBytes.toString("utf8"));

  if (manifest.name !== PACKAGE_ID) throw new Error(`${asset.name} contains package ${manifest.name}`);
  if (`v${manifest.version}` !== release.tag_name) {
    throw new Error(`${asset.name} is version ${manifest.version}, released as ${release.tag_name}`);
  }

  return {
    ...manifest,
    url: asset.browser_download_url,
    zipSHA256: createHash("sha256").update(zip).digest("hex"),
  };
}

const { values } = parseArgs({ options: { out: { type: "string" } } });
if (!values.out) throw new Error("--out is required");

const repository = process.env.GITHUB_REPOSITORY ?? "TheArmagan/advosc-triggers";
const api = process.env.GITHUB_API_URL ?? "https://api.github.com";
const versions = {};

for (const release of await fetchReleases(api, repository, process.env.GITHUB_TOKEN)) {
  if (release.draft) continue;

  const asset = release.assets.find(
    ({ name }) => name.startsWith(`${PACKAGE_ID}-`) && name.endsWith(".zip"),
  );
  if (!asset) {
    console.warn(`Skipping ${release.tag_name}: no VPM zip attached`);
    continue;
  }

  const entry = await versionEntry(release, asset);
  versions[entry.version] = entry;
  console.log(`${entry.version}  ${entry.zipSHA256}`);
}

if (Object.keys(versions).length) listing.packages[PACKAGE_ID] = { versions };

writeFileSync(values.out, `${JSON.stringify(listing, null, 2)}\n`);
console.log(`Wrote ${values.out} with ${Object.keys(versions).length} version(s)`);
