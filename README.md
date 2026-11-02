# ADVOSC Triggers

A Unity package for VRChat avatars. It adds SPS sockets, plugs, Touch Zones and contacts to an avatar, and sends [ADVOSC](https://github.com/TheArmagan/advosc) the automation rules that react to them. The avatar side and the ADVOSC side come from one setup instead of being built by hand twice.

Package id: `rest.armagan.advosc-triggers`

Tested in VRChat with contacts and a small test plug and ring. Depth comes from VRCFury's contact layout, so check how it feels with your own plug.

New here? [Learn ADVOSC Triggers](LEARN_TRIGGERS.md) walks you through your first trigger, what each option does, and what to do when something doesn't work.

## Requirements

- Unity 2022.3
- VRChat Avatars SDK
- VRCFury
- ADVOSC 1.1.0 or newer, on Windows

## Install

### VPM listing (VCC or ALCOM)

1. Add this repository in VCC or ALCOM:

   ```
   https://thearmagan.github.io/advosc-triggers/index.json
   ```

2. Add VRCFury's repository too, or the dependency can't resolve:

   ```
   https://vcc.vrcfury.com
   ```

3. Add ADVOSC Triggers to your avatar project.

### .unitypackage

Install VRCFury and the VRChat Avatars SDK first, since a `.unitypackage` can't pull in dependencies. Then download the `.unitypackage` from [Releases](https://github.com/TheArmagan/advosc-triggers/releases) and import it. Files go to `Packages/rest.armagan.advosc-triggers/`.

## Usage

[LEARN_TRIGGERS.md](LEARN_TRIGGERS.md) has the full walkthrough. In short:

1. Add an `AdvoscTrigger` component to a GameObject under your avatar.
2. Pick the kind, a preset if there is one, and who can fire it: Self, Others or Both. The default is Others.
3. Pick a starter rule: empty, toy, variable or sound placeholder.
4. Use one of the export buttons:
   - **Open in ADVOSC** sends the rules straight to the app.
   - **Copy link** gives you a link to share.
   - **Export `.advosc.json`** writes a file. Use this if the bundle is over the 16 KB link limit.
5. Review the rules in ADVOSC, then turn on the ones you want.
6. Upload the avatar, or use Build and Test.

The component is non-destructive. Everything is generated on the build copy, so your source avatar isn't changed.

Generated contacts sit behind one toggle in an "ADVOSC Triggers" submenu in the avatar's menu. VRCFury's own socket toggles work as usual.

## Trigger kinds

Every parameter is local and unsynced, so triggers cost no network bits. Other players don't receive these values.

| Kind | Adds to the avatar | Rule source in ADVOSC |
|---|---|---|
| SPS socket | VRCFury socket | `/advosc/sps/socket/<id>/<metric>` |
| SPS plug | VRCFury plug | `/advosc/sps/plug/<id>/<metric>` |
| Touch Zone | VRCFury Touch Zone | `/avatar/parameters/VFH/Zone/Touch/<id>/Self` or `/Others` |
| Contact preset | Local contact receiver for headpat, boop, cheek, hand, shoulder, hug or foot | `/avatar/parameters/ADVOSC/<id>` |
| Custom contact | Local contact receiver with your own shape and tags | `/avatar/parameters/ADVOSC/<id>` |

SPS metrics are computed by ADVOSC from the raw `OGB/*` values:

| Metric | Value |
|---|---|
| `depth`, `depthSelf`, `depthOthers` | 0 to 1 |
| `active` | 0 or 1 |
| `speed` | Depth change per second, 0 or above |

Proximity contacts give a Float and Constant contacts give a Bool. Ids can use letters, digits, `_` and `-`, up to 48 characters.

## Links

A bundle of rules can travel in four forms:

| Form | Value |
|---|---|
| App link | `advosc://import?d=<payload>` |
| Web link | `https://thearmagan.github.io/advosc-triggers/link/#<payload>` |
| Paste code | `ADVOSCR1:<payload>` |
| File | `<name>.advosc.json` |

The web link exists because chat apps often won't make `advosc://` clickable. It previews the rules and has a button that opens ADVOSC.

The format is specified in [Documentation~/contract.md](Documentation~/contract.md).

### Safety behaviour

Anyone can write a link, so neither ADVOSC nor the web page trusts one.

In ADVOSC:

- Every rule is shown for review, and nothing is added until you accept.
- Rules always arrive turned off.
- Incoming rule ids are ignored and new ones are generated, so a link can't overwrite a rule you already have.
- A strict allowlist applies, and anything outside it rejects the whole bundle. Sources must be avatar parameters or `/advosc/sps/` values. Actions are limited to `setParameter`, `setVariable`, `osc` under `/avatar/parameters/`, an empty sound placeholder, and limited toy actions.
- `{{` and `[[` are rejected in every string, because those placeholders can make network reads. Values in a link are literal.
- Toy actions can't name a device and can't use stored patterns. Levels stay within 0 to 1, and durations are at most 60 seconds. The toy cap and panic hotkey in ADVOSC still apply.
- Links are limited to a 16 KB payload, 256 KB of JSON after inflating, and 64 rules.

On the web page:

- The payload stays in the URL fragment, which browsers don't send to servers. The page has no analytics, and its Content Security Policy blocks network requests.
- All payload content is treated as text, never as HTML.
- Rules with toy actions carry a plain warning.
- The page is a preview. ADVOSC does the real validation.

A rule with a contact source can be fired by anyone who can touch that contact on your avatar. Set the trigger to Self if you don't want that. ADVOSC also listens for OSC on all network interfaces, so a device on your LAN that can reach its ports can send fake parameter values.

## License

MPL-2.0. See [LICENSE](LICENSE).

VRCFury isn't bundled with this package. It's a separate dependency under its own license. This package uses VRCFury's public API and sets serialized fields the way its inspector does, and doesn't patch or ship any VRCFury code.
