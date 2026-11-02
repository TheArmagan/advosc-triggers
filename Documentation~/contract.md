# ADVOSC Triggers contract

What the Unity package writes and what ADVOSC reads. The TypeScript source of truth is `src/shared/automation-bundle.ts` in the advosc repo. Change both together.

## Bundle JSON

```json
{
  "kind": "advosc.automation.bundle",
  "version": 1,
  "name": "My avatar triggers",
  "avatarName": "My avatar",
  "avatarIds": ["avtr_00000000-0000-0000-0000-000000000000"],
  "rules": [
    {
      "key": "headpat",
      "name": "Headpat",
      "source": { "kind": "parameter", "address": "/avatar/parameters/ADVOSC/headpat" },
      "match": "> 0.1",
      "releaseMatch": "<= 0.05",
      "mode": "edge",
      "cooldownMs": 1000,
      "actions": [{ "type": "setVariable", "name": "Headpat", "value": "1" }]
    }
  ]
}
```

- `avatarName` and `avatarIds` are optional. Leave `avatarIds` out when the avatar has no blueprint id.
- Rules have no `id` and no `enabled`. ADVOSC mints an id and imports every rule off.
- `key` is stable between exports. ADVOSC stores `<bundle name>/<key>` as `importKey`.
- `range` is `{ "inMin", "inMax", "outMin", "outMax" }` and is required on `live` rules.
- `mode` is `edge`, `while` or `live`. `match` is ignored by `live` but must be a string.

## Encoding

| Form | Value |
|---|---|
| Payload | base64url, no padding, of raw deflate of UTF-8 JSON. C# `DeflateStream` writes raw deflate, not zlib. |
| Link | `advosc://import?d=<payload>` |
| Web link | `https://thearmagan.github.io/advosc-triggers/link/#<payload>` |
| Paste code | `ADVOSCR1:<payload>` |
| File | The JSON above, as `<name>.advosc.json` |

Limits: payload 16 KB for links, 256 KB of JSON after inflating, 64 rules. A bundle over the link limit goes through a file.

## What a bundle may contain

Anything else makes ADVOSC reject the whole bundle.

- Strings must not contain `{{` or `[[`. Values are literal.
- `source.kind` is `parameter`, and the address starts with `/avatar/parameters/` or `/advosc/sps/`.
- Actions:

| `type` | Rules |
|---|---|
| `setParameter` | `address` under `/avatar/parameters/`, literal `value` |
| `osc` | `address` under `/avatar/parameters/`, `oscType` of `Float`, `Int` or `Bool`, literal `value` |
| `setVariable` | `name`, literal `value` |
| `sound` | Placeholder only: `soundIds` and `soundNames` must be empty. `loop` and `stopOnEnd` are booleans. |
| `toy` | `action.kind` of `link` (only on `live`), `pulse`, `preset` or `stop`. `target.toyIds` must be `[]`. Never `pattern`. |

Toy limits: `level` 0 to 1, `seconds` above 0 and at most 60, `preset` of `pulse`, `wave`, `fireworks` or `earthquake`. `target.features` must not be empty and come from `Vibrate`, `Rotate`, `Pump`, `Thrusting`, `Fingering`, `Suction`, `Depth`, `Stroke`, `Oscillate`.

Other limits:

- `key` matches `[A-Za-z0-9_-]{1,64}` and is unique within a bundle.
- Names are at most 120 characters, `match` and `releaseMatch` 128, values 512, addresses 256.
- Each rule has at most 32 actions, and `cooldownMs` is at most 24 hours.
- Leave optional fields out rather than sending them blank: an empty `avatarIds` or blank `releaseMatch` is rejected.
- Addresses contain no whitespace and none of `# * , ? [ ] { }`.

## Parameter names

Ids match `[A-Za-z0-9_-]{1,48}`.

| Trigger | Avatar parameter | OSC address |
|---|---|---|
| Contact | `ADVOSC/<id>` | `/avatar/parameters/ADVOSC/<id>` |
| Socket (raw) | `OGB/Orf/<id>/<suffix>` | `/avatar/parameters/OGB/Orf/<id>/<suffix>` |
| Plug (raw) | `OGB/Pen/<id>/<suffix>` | `/avatar/parameters/OGB/Pen/<id>/<suffix>` |
| Touch Zone | `VFH/Zone/Touch/<id>/Self` or `/Others` | `/avatar/parameters/VFH/Zone/Touch/<id>/Self` |

Contact parameters are declared in expression parameters with `networkSynced: false`, the same way VRCFury declares `OGB_ENABLED`. They cost no network bits, and receivers are `localOnly`.

Contact parameter types: Proximity receivers are Float, Constant receivers are Bool.

## Derived SPS sources

ADVOSC computes these from the raw `OGB/*` values. They are internal: packets from the network can't set them.

`/advosc/sps/<kind>/<id>/<metric>`

- `kind`: `socket` or `plug`
- `metric`: `depth`, `depthSelf`, `depthOthers` (0 to 1), `active` (0 or 1), `speed` (depth change per second, 0 or above)

## Unverified

Nobody has yet tested this in VRChat. Before relying on it, confirm that `OGB/*` floats arrive over OSC, and measure real plug values for the depth formula. That formula lives in one function in `src/engine/vrc-osc/sps.ts`.
