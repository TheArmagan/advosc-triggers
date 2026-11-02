# Learn ADVOSC Triggers

This guide takes you from nothing to "I touched my avatar and something happened". No OSC knowledge needed.

## The idea

There are two halves, and this package connects them:

1. **The avatar** feels something. A hand touches your head, or a plug goes into a socket.
2. **ADVOSC**, running on your PC, reacts. It plays a sound, drives a toy, or updates your chatbox.

You add a **trigger** in Unity. The trigger creates the avatar half, and also writes the **rule** for the ADVOSC half, so you don't build both by hand.

```
Someone touches you  ->  avatar parameter changes  ->  VRChat sends it to ADVOSC  ->  your rule runs
```

Only your own PC sees these values. They aren't sent to other players.

## What you need

- A Unity project with the VRChat Avatars SDK and VRCFury
- This package
- ADVOSC 1.1.0 or newer, because older versions can't open links
- OSC turned on in VRChat: Action Menu, Options, OSC, Enabled

## Your first trigger: a headpat

1. In Unity, right-click your avatar and create an empty child object. Name it `Headpat`.
2. Click **Add Component** and pick **ADVOSC Trigger**.
3. Set **Type** to **Contact Preset** and **Preset** to **Headpat**.
4. Set **Who can trigger it** to **Both**, so you can test it alone.
5. Set **Starter rule** to **Variable**.
6. Press **Open in ADVOSC**. ADVOSC shows you the rule. Add it, then turn it on in Automations.
7. Run **Build & Test** from the VRChat SDK panel, and wear the avatar in VRChat.
8. Pat your own head. The rule's value in ADVOSC moves from 0 toward 1.

That's the whole loop. Everything else is a variation on it.

Not sure what a setting does? Hover over it in Unity. The small book icon on the component opens this guide.

## Which trigger do I want?

| I want | Use | It reports |
|---|---|---|
| A touch spot that's ready to go | **Contact Preset** | How close, or on and off |
| A touch spot with my own shape and size | **Custom Contact** | How close, or on and off |
| A hole or ring for a plug | **SPS Socket** | How deep |
| A plug | **SPS Plug** | How deep |
| A touch area through VRCFury | **Touch Zone** | How close |

You can put as many on one avatar as you like. Each needs its own **ID**.

### Contact presets

| Preset | Where it sits | Reports |
|---|---|---|
| Headpat | Top of the head | 0 to 1 |
| Boop | Tip of the nose | On or off |
| Cheek | One cheek | 0 to 1 |
| Shoulder | One shoulder | 0 to 1 |
| Hand | One hand | 0 to 1 |
| Hug | Front of the chest | 0 to 1 |
| Foot | Front of one foot | 0 to 1 |

Presets size themselves to your avatar. Select the trigger and check the wire shape in the Scene view. If it's off, use **Size** and **Move by**.

Cheek, Shoulder, Hand and Foot cover one side. For both sides, add two triggers.

**Hand, Hug and Foot must use Other people.** Your own body is always inside them, so on Only me or Both they would never turn off. Unity warns you about this.

### Sockets and plugs

Sockets and plugs are made with VRCFury's SPS.

- A socket points along its object's blue arrow. Move and rotate the object to place it.
- A socket starts turned **off**. Turn it on in game from the **SPS** folder of your Expressions menu.
- A plug needs a mesh. Set **Plug mesh**, or leave it empty and VRCFury looks for one.
- SPS needs even scale, so the object and all its parents must have X, Y and Z equal.

## Who can trigger it?

| Option | Who counts |
|---|---|
| Only me | Your own hands or plug |
| Other people | Anyone else. This is the default |
| Both | Anyone, including you |

In a public instance, **Other people** and **Both** mean anyone near you can fire the trigger. Think about that before you connect one to a toy.

## Starter rules

This picks the rule a trigger adds to ADVOSC. You can change the rule in ADVOSC afterwards.

| Starter rule | What you get |
|---|---|
| Empty rule | The trigger with no action. Add your own in ADVOSC |
| Toy | Sockets and plugs make a toy follow depth. Other triggers give a 1 second pulse |
| Variable | The 0 to 1 value is stored under the trigger's display name |
| Sound | Plays a sound. Pick the file in ADVOSC. The rule shows "Pick a sound" until you do |

To show a variable in your chatbox, add it to your template. For a trigger named Headpat:

```
{{Var;Headpat;0}}
```

## Sending rules to ADVOSC

These three buttons are on every trigger, and in **Tools > ADVOSC Triggers**. They send every trigger on the avatar, not only the one you selected.

| Button | Use it when |
|---|---|
| **Open in ADVOSC** | ADVOSC is on this PC. This is the usual one |
| **Copy link** | You want to share the rules. The link opens a preview page first |
| **Export .advosc.json** | You're shipping rules with an avatar, or a link is too big |

To import a file or pasted link, use **Import rules** on the Automations page in ADVOSC.

ADVOSC always shows you the rules first, and they always arrive **turned off**. Nothing runs until you turn it on. A link can't overwrite your rules, and it can't read anything from the internet.

Sent the same rules twice? ADVOSC skips the ones you already have.

## In-game menu

You get two kinds of switch in your Expressions menu:

- **ADVOSC Triggers > Contacts** turns all contact triggers on or off together.
- **SPS** holds one toggle per socket. These come from VRCFury.

## When it doesn't work

| What you see | Try this |
|---|---|
| Nothing reaches ADVOSC | Turn OSC on in VRChat. If you changed the avatar, reset its OSC config from the OSC menu |
| Another OSC app is in use | Only one app can listen on port 9001. Close it, or forward through ADVOSC |
| Open in ADVOSC does nothing | Install ADVOSC 1.1.0 or newer and run it once |
| The rule says nothing was received | The parameter hasn't changed yet. Touch the trigger once |
| A socket reports nothing | Turn it on in the SPS menu. Haptics only work on your PC build of the avatar |
| A contact stays on | Your own body is inside it. Set it to Other people, or make it smaller |
| A rule stays idle | It may be locked to another avatar. Check the lock in the rule editor |
| A build stops with an SPS scale error | Make X, Y and Z scale equal on that object and its parents |
| VRCFury asks about Write Defaults | That's about your avatar, not this package. Pick what you'd normally pick |

## Words you'll see

| Word | Meaning |
|---|---|
| Parameter | A named value on your avatar, like `ADVOSC/headpat` |
| OSC | How VRChat sends parameters to other apps on your PC |
| Contact | VRChat's way of noticing that two things touch |
| SPS | VRCFury's system for sockets and plugs |
| Rule | One automation in ADVOSC: "when this happens, do that" |
| Starter rule | The rule a trigger brings with it |

## Good to know

This is an early release. Contacts and SPS have been tested in VRChat with a small test plug and ring. If depth feels off with your own plug, change the rule's range in ADVOSC.
