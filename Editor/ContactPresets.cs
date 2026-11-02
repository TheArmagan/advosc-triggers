// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using UnityEngine;

namespace ADVOSC.Triggers.Editor {
    internal sealed class ContactPreset {
        public ContactPresetId Id;
        public string Label;

        /// <summary>Tried in order, so a rig without an optional bone uses the next one.</summary>
        public HumanBodyBones[] LeftBones;
        public HumanBodyBones[] RightBones;

        /// <summary>Whether the side picker applies. Right flips the sign of Offset.x.</summary>
        public bool HasSides;

        /// <summary>From the bone, in meters on the avatar's axes: X right, Y up, Z forward.</summary>
        public Vector3 Offset;

        public float Radius;
        public string[] Tags;
        public TriggerReceiverType Type;
    }

    /// <summary>
    /// The one table of presets. Sizes are for an avatar with eyes 1.3 m above the ground and are
    /// scaled to the avatar at build time. Nothing here relies on a Jaw bone, since many rigs have none.
    /// </summary>
    internal static class ContactPresets {
        public const float ReferenceEyeHeight = 1.3f;

        public static readonly ContactPreset[] All = {
            new ContactPreset {
                Id = ContactPresetId.Headpat,
                Label = "Headpat",
                LeftBones = new[] { HumanBodyBones.Head },
                RightBones = new[] { HumanBodyBones.Head },
                Offset = new Vector3(0f, 0.18f, 0f),
                Radius = 0.12f,
                Tags = new[] { "Hand", "Finger" },
                Type = TriggerReceiverType.Proximity
            },
            new ContactPreset {
                Id = ContactPresetId.Boop,
                Label = "Boop",
                LeftBones = new[] { HumanBodyBones.Head },
                RightBones = new[] { HumanBodyBones.Head },
                Offset = new Vector3(0f, 0.05f, 0.11f),
                Radius = 0.035f,
                Tags = new[] { "Finger", "Hand" },
                Type = TriggerReceiverType.Constant
            },
            new ContactPreset {
                Id = ContactPresetId.Cheek,
                Label = "Cheek",
                LeftBones = new[] { HumanBodyBones.Head },
                RightBones = new[] { HumanBodyBones.Head },
                HasSides = true,
                Offset = new Vector3(-0.07f, 0.03f, 0.08f),
                Radius = 0.05f,
                Tags = new[] { "Hand", "Finger" },
                Type = TriggerReceiverType.Proximity
            },
            new ContactPreset {
                Id = ContactPresetId.Hand,
                Label = "Hand",
                LeftBones = new[] { HumanBodyBones.LeftHand },
                RightBones = new[] { HumanBodyBones.RightHand },
                HasSides = true,
                Offset = Vector3.zero,
                Radius = 0.09f,
                Tags = new[] { "Hand", "Finger" },
                Type = TriggerReceiverType.Proximity
            },
            new ContactPreset {
                Id = ContactPresetId.Shoulder,
                Label = "Shoulder",
                LeftBones = new[] { HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm },
                RightBones = new[] { HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm },
                HasSides = true,
                Offset = new Vector3(0f, 0.04f, 0f),
                Radius = 0.08f,
                Tags = new[] { "Hand", "Finger" },
                Type = TriggerReceiverType.Proximity
            },
            new ContactPreset {
                Id = ContactPresetId.Hug,
                Label = "Hug",
                LeftBones = new[] { HumanBodyBones.UpperChest, HumanBodyBones.Chest, HumanBodyBones.Spine },
                RightBones = new[] { HumanBodyBones.UpperChest, HumanBodyBones.Chest, HumanBodyBones.Spine },
                Offset = new Vector3(0f, 0f, 0.12f),
                Radius = 0.22f,
                Tags = new[] { "Torso", "Hand" },
                Type = TriggerReceiverType.Proximity
            },
            new ContactPreset {
                Id = ContactPresetId.Foot,
                Label = "Foot",
                LeftBones = new[] { HumanBodyBones.LeftFoot },
                RightBones = new[] { HumanBodyBones.RightFoot },
                HasSides = true,
                Offset = new Vector3(0f, 0f, 0.06f),
                Radius = 0.09f,
                Tags = new[] { "Foot", "Hand" },
                Type = TriggerReceiverType.Proximity
            }
        };

        public static ContactPreset Get(ContactPresetId id) {
            foreach (var preset in All) {
                if (preset.Id == id) return preset;
            }
            return All[0];
        }
    }
}
