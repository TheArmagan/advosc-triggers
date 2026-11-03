# Changelog

## 0.3.0 (2026-11-03)

### Added

- Counter starter rule: adds 1 to a variable each time the trigger fires. Needs ADVOSC 1.2.0 or newer.
- `changeVariable` in the bundle format, and the link page describes it.

## 0.2.0 (2026-11-02)

### Added

- Existing trigger kind: reads a contact, VRCFury socket, plug or Touch Zone already on the avatar, without adding anything to it.
- Scan in **Tools > ADVOSC Triggers** to list what an avatar already has and add triggers for it.
- Name override for parameters that a tool renames during the build.
- Warnings when a contact's name isn't on the avatar, and when other people can fire a toy rule on an existing contact.

## 0.1.0 (2026-11-02)

First version. Tested in VRChat with contacts and a small test plug and ring.

### Added

- `AdvoscTrigger` component with SPS socket, SPS plug, Touch Zone, contact preset and custom contact kinds.
- Contact presets: headpat, boop, cheek, hand, shoulder, hug and foot.
- Per-trigger Self, Others or Both.
- Starter rules: empty, toy, variable and sound placeholder.
- Export through Open in ADVOSC, Copy link and `.advosc.json`.
- One master toggle in an "ADVOSC Triggers" submenu.
- Web page for `advosc://` links, which previews the rules and opens ADVOSC.
- VPM listing and `.unitypackage` releases.
