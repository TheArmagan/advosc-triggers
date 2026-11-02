// Everything in the payload is hostile: anyone can craft a link. Data reaches the page only through
// textContent, and the page's CSP blocks inline code and all network requests as a second layer.
// This is a preview. ADVOSC runs the real validation and review after the button is pressed.
"use strict";

// Limits from Documentation~/contract.md.
const MAX_PAYLOAD = 16 * 1024;
const MAX_JSON = 256 * 1024;
const MAX_RULES = 64;
const BUNDLE_KIND = "advosc.automation.bundle";
const BUNDLE_VERSION = 1;
const PAYLOAD_PATTERN = /^[A-Za-z0-9_-]+$/;

const MAX_NAME_SHOWN = 120;
const MAX_ADDRESS_SHOWN = 256;

// Control characters, zero-width characters and bidi overrides, which can disguise text.
const INVISIBLE = /[\p{Cc}\p{Cf}\p{Zl}\p{Zp}]+/gu;

const MODE_LABELS = {
  edge: "Once each time it matches",
  while: "For as long as it matches",
  live: "Follows the value continuously",
};

class LinkProblem extends Error {
  constructor(title, detail) {
    super(title);
    this.detail = detail;
  }
}

const BROKEN = new LinkProblem(
  "This link is broken",
  "It may have been cut off when it was copied. Ask whoever sent it for a new one.",
);

function clean(value, limit) {
  if (typeof value !== "string" && typeof value !== "number" && typeof value !== "boolean") return "";
  const characters = Array.from(String(value).replace(INVISIBLE, " ").trim());
  return characters.length > limit ? `${characters.slice(0, limit).join("")}...` : characters.join("");
}

function element(tag, className, text) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}

function isObject(value) {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function decodeBase64Url(payload) {
  const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
  const binary = atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, "="));
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return bytes;
}

/** Stops reading as soon as the output passes the cap, so a small payload can't expand without end. */
async function inflate(bytes) {
  const stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream("deflate-raw"));
  const reader = stream.getReader();
  const chunks = [];
  let total = 0;

  for (;;) {
    const { done, value } = await reader.read();
    if (done) break;
    total += value.length;
    if (total > MAX_JSON) {
      reader.cancel().catch(() => {});
      throw new LinkProblem(
        "This link is too large",
        "It unpacks to more than 256 KB. Ask whoever sent it for the .advosc.json file instead.",
      );
    }
    chunks.push(value);
  }

  const output = new Uint8Array(total);
  let offset = 0;
  for (const chunk of chunks) {
    output.set(chunk, offset);
    offset += chunk.length;
  }
  return output;
}

async function readBundle(payload) {
  let json;
  try {
    const bytes = await inflate(decodeBase64Url(payload));
    json = new TextDecoder("utf-8", { fatal: true }).decode(bytes);
  } catch (error) {
    throw error instanceof LinkProblem ? error : BROKEN;
  }

  let bundle;
  try {
    bundle = JSON.parse(json);
  } catch {
    throw BROKEN;
  }

  if (!isObject(bundle) || bundle.kind !== BUNDLE_KIND || !Array.isArray(bundle.rules)) {
    throw new LinkProblem("This isn't an ADVOSC rule link", "The data in it isn't a rule bundle.");
  }
  if (bundle.version !== BUNDLE_VERSION) {
    throw new LinkProblem(
      "This page can't read this link",
      "It uses a bundle version this page doesn't know.",
    );
  }
  if (bundle.rules.length === 0) {
    throw new LinkProblem("This link has no rules", "There's nothing in it to add.");
  }
  if (bundle.rules.length > MAX_RULES) {
    throw new LinkProblem("This link has too many rules", `The limit is ${MAX_RULES}.`);
  }
  if (!bundle.rules.every(isObject)) throw BROKEN;
  return bundle;
}

function describeToy(action) {
  const toy = isObject(action.action) ? action.action : {};
  const features = isObject(action.target) ? action.target : isObject(toy.target) ? toy.target : {};
  const names = Array.isArray(features.features) ? features.features.map((f) => clean(f, 32)) : [];
  const what = names.filter(Boolean).join(", ") || "connected toys";
  const seconds = clean(toy.seconds, 16);

  switch (toy.kind) {
    case "link":
      return `Toy: drive ${what} from the value`;
    case "pulse": {
      const level = typeof toy.level === "number" ? `${Math.round(toy.level * 100)}%` : "unknown level";
      return `Toy: pulse ${what} at ${level} for ${seconds} seconds`;
    }
    case "preset":
      return `Toy: play preset ${clean(toy.preset, 32)} on ${what} for ${seconds} seconds`;
    case "stop":
      return `Toy: stop ${what}`;
    default:
      return `Toy: unknown action ${clean(toy.kind, 32)}. ADVOSC will reject this bundle.`;
  }
}

function describeAction(action) {
  if (!isObject(action)) return "Unknown action. ADVOSC will reject this bundle.";
  const address = clean(action.address, MAX_ADDRESS_SHOWN);
  const value = clean(action.value, MAX_NAME_SHOWN);

  switch (action.type) {
    case "setParameter":
      return `Set ${address} to ${value}`;
    case "osc":
      return `Send ${clean(action.oscType, 16)} ${value} to ${address}`;
    case "setVariable":
      return `Set variable ${clean(action.name, MAX_NAME_SHOWN)} to ${value}`;
    case "sound":
      return "Play a sound, which you pick in ADVOSC";
    case "toy":
      return describeToy(action);
    default:
      return `Unknown action ${clean(action.type, 32)}. ADVOSC will reject this bundle.`;
  }
}

function renderRule(rule) {
  const actions = Array.isArray(rule.actions) ? rule.actions : [];
  const source = isObject(rule.source) ? clean(rule.source.address, MAX_ADDRESS_SHOWN) : "";
  // The typeof check comes first: using an object as a key would call its hostile toString.
  const modeLabel =
    typeof rule.mode === "string" && Object.hasOwn(MODE_LABELS, rule.mode)
      ? MODE_LABELS[rule.mode]
      : `Unknown (${clean(rule.mode, 32)})`;

  const item = element("li", "rule");
  item.append(element("h3", "", clean(rule.name, MAX_NAME_SHOWN) || "Unnamed rule"));

  const details = element("dl");
  details.append(element("dt", "", "Source"), element("dd", "address", source || "None given"));
  details.append(element("dt", "", "Runs"), element("dd", "", modeLabel));

  const list = element("ul");
  for (const action of actions) list.append(element("li", "", describeAction(action)));
  if (actions.length === 0) list.append(element("li", "", "None"));
  details.append(element("dt", "", "Does"), element("dd"));
  details.lastChild.append(list);
  item.append(details);

  if (actions.some((action) => isObject(action) && action.type === "toy")) {
    item.append(
      element(
        "p",
        "warning",
        "This rule controls connected toys. Once you turn it on, anything that changes its source can run them.",
      ),
    );
  }
  return item;
}

function openSection(payload) {
  const section = element("section");
  const button = element("a", "button", "Open in ADVOSC");
  button.href = `advosc://import?d=${payload}`;
  section.append(button);
  section.append(
    element(
      "p",
      "",
      "ADVOSC shows every rule for review before anything is added, and added rules start turned off.",
    ),
  );
  section.append(
    element(
      "p",
      "muted",
      "If nothing happens, ADVOSC isn't installed, or hasn't been opened since it was installed.",
    ),
  );
  return section;
}

function renderMessage(title, detail) {
  const box = element("div", "message");
  box.append(element("h2", "", title), element("p", "", detail));
  return box;
}

function setHeading(title, subtitle) {
  document.getElementById("title").textContent = title;
  document.getElementById("subtitle").textContent = subtitle;
}

function ruleCount(count) {
  return count === 1 ? "1 rule" : `${count} rules`;
}

function fail(title, detail) {
  setHeading("ADVOSC rule link", "");
  document.getElementById("content").replaceChildren(renderMessage(title, detail));
}

async function render() {
  const content = document.getElementById("content");
  const payload = location.hash.slice(1);

  if (!payload) {
    fail("This link is empty", "It has no rules in it. Ask whoever sent it for a new one.");
    return;
  }
  if (payload.length > MAX_PAYLOAD) {
    fail("This link is too large", "Ask whoever sent it for the .advosc.json file instead.");
    return;
  }
  if (!PAYLOAD_PATTERN.test(payload)) {
    fail(BROKEN.message, BROKEN.detail);
    return;
  }

  if (typeof DecompressionStream === "undefined") {
    setHeading("ADVOSC rule link", "");
    content.replaceChildren(
      renderMessage(
        "This browser can't preview the rules",
        "You can still open the link, and ADVOSC will show every rule to you.",
      ),
      openSection(payload),
    );
    return;
  }

  let bundle;
  try {
    bundle = await readBundle(payload);
  } catch (error) {
    if (!(error instanceof LinkProblem)) throw error;
    fail(error.message, error.detail);
    return;
  }

  // A hash change during the await means a newer render owns the page.
  if (location.hash.slice(1) !== payload) return;

  const avatar = clean(bundle.avatarName, MAX_NAME_SHOWN);
  const toyRules = bundle.rules.filter(
    (rule) => Array.isArray(rule.actions) && rule.actions.some((a) => isObject(a) && a.type === "toy"),
  ).length;

  setHeading(
    clean(bundle.name, MAX_NAME_SHOWN) || "Unnamed bundle",
    avatar ? `Rules for ${avatar}` : "Rules for any avatar",
  );

  const summary = `${ruleCount(bundle.rules.length)}${toyRules ? `, ${toyRules} with toy actions` : ""}`;
  const rules = element("ol", "rules");
  for (const rule of bundle.rules) rules.append(renderRule(rule));
  content.replaceChildren(openSection(payload), element("h2", "", summary), rules);
}

// Anything unexpected ends as the broken-link message, with no button, not a page stuck on loading.
function show() {
  render().catch(() => fail(BROKEN.message, BROKEN.detail));
}

window.addEventListener("hashchange", show);
show();
