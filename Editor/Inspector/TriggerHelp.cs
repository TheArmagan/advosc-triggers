// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace ADVOSC.Triggers.Editor {
    /// <summary>
    /// The text shown when hovering over a setting. Unity doesn't show tooltips on the entries of an
    /// open dropdown, so a dropdown's tooltip explains all of its options.
    /// </summary>
    internal static class TriggerHelp {
        public const string GuideUrl = "https://github.com/TheArmagan/advosc-triggers/blob/main/LEARN_TRIGGERS.md";

        public const string Type =
            "What this trigger is.\n\n" +
            "SPS Socket: a hole or ring that a plug goes into. Reports how deep.\n" +
            "SPS Plug: the part that goes into a socket. Reports how deep.\n" +
            "Touch Zone: an area on a plug or body part that reports touch through VRCFury.\n" +
            "Contact Preset: a ready-made touch spot, like a headpat.\n" +
            "Custom Contact: a touch spot where you pick the shape and size.";

        public const string Id =
            "A short name for this trigger, used in the parameter name ADVOSC listens to.\n\n" +
            "Letters, numbers, - and _ only. Each trigger on an avatar needs its own ID. " +
            "If you change it after adding rules, send the rules to ADVOSC again.";

        public const string DisplayName =
            "The name you see in ADVOSC, and on the socket's entry in the avatar menu. Optional: the ID is used if it's empty.";

        public const string Who =
            "Whose touch counts.\n\n" +
            "Only me: your own hands or plug.\n" +
            "Other people: anyone else. This is the default.\n" +
            "Both: anyone, including you.\n\n" +
            "In public instances, Other people and Both mean anyone nearby can fire this.";

        public const string StarterRule =
            "The rule this trigger adds in ADVOSC. Rules always arrive turned off, and you can edit them there.\n\n" +
            "Empty rule: the trigger with no action. You add the action in ADVOSC.\n" +
            "Toy: sockets and plugs follow depth, other triggers give a short pulse.\n" +
            "Variable: stores the 0 to 1 value under this trigger's name, for chatbox text.\n" +
            "Sound: plays a sound. You pick the file in ADVOSC.";

        public const string SocketMode =
            "How VRCFury sets up the socket. This decides how plugs line up with it.\n\n" +
            "Auto: VRCFury picks from the object's name.\n" +
            "Hole: for an opening with an end.\n" +
            "Ring: for a ring a plug can pass through, from either side.\n" +
            "One-way ring: a ring that only works from the front.\n" +
            "No light: no socket light, so plugs won't aim at it. Haptics still work.";

        public const string PlugMesh =
            "The mesh that is the plug. Leave it empty and VRCFury looks for one on this object or the bones around it.";

        public const string ZoneRadius =
            "How far the zone reaches from this object, in meters. 0.1 is about the width of a hand.";

        public const string Preset =
            "Which touch spot to add. The size and position fit themselves to your avatar's height.\n\n" +
            "Headpat: top of the head. Gives 0 to 1 for how close.\n" +
            "Boop: tip of the nose. On or off.\n" +
            "Cheek, Shoulder: gives 0 to 1 for how close.\n" +
            "Hand: on your hand, for holding hands.\n" +
            "Hug: front of the chest. Big, so a hug reaches it.\n" +
            "Foot: front of the foot, for stepping on.\n\n" +
            "Hand, Hug and Foot need Other people, because your own body is always inside them.";

        public const string Side = "Which side of the body. For both sides, add a second trigger with its own ID.";

        public const string RadiusScale = "1 is the preset's normal size for this avatar. 2 is twice as big, 0.5 is half.";

        public const string Offset =
            "Moves the spot from where the preset puts it, in meters. X is the avatar's right, Y is up, Z is forward. " +
            "Check the wire sphere in the Scene view while you change this.";

        public const string ManualTarget =
            "Attaches the spot to this object instead of the preset's bone. Use it when the avatar's rig doesn't have that bone mapped.";

        public const string AttachedTo = "The bone or object this spot follows.";

        public const string CustomShape = "Sphere: a ball around this object. Capsule: a long pill shape, good for arms and tails.";

        public const string CustomRadius = "How big the shape is, in meters from its center. 0.1 is about the width of a hand.";

        public const string CustomHeight =
            "The capsule's full length, including its round ends, in meters. It runs along this object's green axis.";

        public const string CustomReports =
            "What the parameter sends.\n\n" +
            "Proximity: 0 to 1 for how close the touch is. Use it for smooth effects.\n" +
            "Constant: on or off. Use it for a simple tap.";

        public const string CustomTags =
            "What can touch this. VRChat's standard tags are Head, Torso, Hand, Foot and Finger.";

        public const string WindowAvatar = "The avatar to work on. Selecting anything on an avatar picks it here too.";

        public const string SelectButton = "Selects this trigger's object in the Hierarchy.";

        public const string OpenInApp =
            "Sends every trigger's rule on this avatar to ADVOSC. ADVOSC shows them for review first, and they arrive turned off. " +
            "Needs a version of ADVOSC with link support.";

        public const string CopyLink =
            "Copies a web link to these rules. Paste it in Discord or on a product page: the page previews the rules and opens ADVOSC.";

        public const string ExportFile =
            "Saves these rules as a .advosc.json file. Use it when there are too many rules for a link, or to ship rules with an avatar. " +
            "In ADVOSC, use Import rules on the Automations page.";
    }
}
