// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using VRC.SDKBase;

namespace ADVOSC.Triggers {
    public enum TriggerKind {
        SpsSocket,
        SpsPlug,
        TouchZone,
        ContactPreset,
        CustomContact,
        Existing
    }

    public enum TriggerWho {
        Self,
        Others,
        Both
    }

    public enum TriggerSocketMode {
        Auto,
        Hole,
        Ring,
        RingOneWay,
        None
    }

    public enum ContactPresetId {
        Headpat,
        Boop,
        Cheek,
        Hand,
        Shoulder,
        Hug,
        Foot
    }

    public enum TriggerSide {
        Left,
        Right
    }

    public enum TriggerShape {
        Sphere,
        Capsule
    }

    public enum TriggerReceiverType {
        Proximity,
        Constant
    }

    public enum StarterRuleChoice {
        Empty,
        Toy,
        Variable,
        SoundPlaceholder
    }

    /// <summary>
    /// Describes one trigger. The build hook turns it into real components on the build copy,
    /// so the avatar in the scene never changes. It is removed from uploads as an IEditorOnly.
    /// </summary>
    [AddComponentMenu("ADVOSC/ADVOSC Trigger")]
    [HelpURL("https://github.com/TheArmagan/advosc-triggers/blob/main/LEARN_TRIGGERS.md")]
    public class AdvoscTrigger : MonoBehaviour, IEditorOnly {
        public const int MaxIdLength = 48;

        private static readonly Regex IdPattern = new Regex(@"\A[A-Za-z0-9_-]{1,48}\z", RegexOptions.CultureInvariant);

        public TriggerKind kind = TriggerKind.ContactPreset;
        public string id = "";
        public string displayName = "";
        public TriggerWho who = TriggerWho.Others;
        public StarterRuleChoice starterRule = StarterRuleChoice.Variable;

        public TriggerSocketMode socketMode = TriggerSocketMode.Auto;

        /// <summary>Leave empty to let VRCFury find the mesh for a plug.</summary>
        public Renderer plugRenderer;

        public ContactPresetId preset = ContactPresetId.Headpat;
        public TriggerSide presetSide = TriggerSide.Left;
        public float presetRadiusScale = 1f;

        /// <summary>Meters, along the avatar's own axes: X is its right, Y up, Z forward.</summary>
        public Vector3 presetOffset = Vector3.zero;

        /// <summary>Used instead of the humanoid bone, for bones the rig doesn't map.</summary>
        public Transform manualTarget;

        public TriggerShape customShape = TriggerShape.Sphere;

        /// <summary>Meters. Also the Touch Zone radius.</summary>
        public float customRadius = 0.1f;

        public float customHeight = 0.3f;
        public TriggerReceiverType customReceiverType = TriggerReceiverType.Proximity;
        public List<string> customTags = new List<string> { "Hand", "Finger" };

        /// <summary>The contact or VRCFury component an Existing trigger reads. Nothing is built for it.</summary>
        public Component existingComponent;

        /// <summary>Replaces the name read from it, for when a tool renames it during the build.</summary>
        public string existingName = "";

        public static bool IsValidId(string value) {
            return value != null && IdPattern.IsMatch(value);
        }

        /// <summary>Turns any text into a valid id, or an empty string if nothing can be kept.</summary>
        public static string SanitizeId(string value) {
            if (string.IsNullOrEmpty(value)) return "";
            var output = new StringBuilder();
            foreach (var c in value) {
                if (output.Length >= MaxIdLength) break;
                var keep = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
                if (keep) {
                    output.Append(c);
                } else if (char.IsWhiteSpace(c)) {
                    output.Append('_');
                }
            }
            return output.ToString();
        }

        private void Reset() {
            id = SanitizeId(gameObject.name);
        }
    }
}
