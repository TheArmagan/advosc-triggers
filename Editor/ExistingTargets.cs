// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ADVOSC.Triggers.Editor {
    internal enum ExistingType {
        None,
        Contact,
        Socket,
        Plug,
        TouchZone
    }

    /// <summary>
    /// Reads contacts and VRCFury components that are already on an avatar. Nothing here changes
    /// them: an Existing trigger only needs to know the name its rule should listen to.
    /// </summary>
    internal static class ExistingTargets {
        private static readonly Regex OscUnsafe = new Regex(@"[\s#*,?\[\]{}]", RegexOptions.CultureInvariant);

        public static ExistingType TypeOf(Component component) {
            if (component == null) return ExistingType.None;
            if (component is ContactReceiver) return ExistingType.Contact;
            var type = component.GetType();
            if (type == VrcFuryCompat.SocketType) return ExistingType.Socket;
            if (type == VrcFuryCompat.PlugType) return ExistingType.Plug;
            if (type == VrcFuryCompat.TouchZoneType) return ExistingType.TouchZone;
            return ExistingType.None;
        }

        public static string Label(ExistingType type) {
            switch (type) {
                case ExistingType.Contact: return "Contact";
                case ExistingType.Socket: return "SPS socket";
                case ExistingType.Plug: return "SPS plug";
                case ExistingType.TouchZone: return "Touch Zone";
                default: return "Unknown";
            }
        }

        /// <summary>The first usable component on an object, for when a whole object gets dragged in.</summary>
        public static Component FindOn(GameObject gameObject) {
            if (gameObject == null) return null;
            foreach (var component in gameObject.GetComponents<Component>()) {
                if (TypeOf(component) != ExistingType.None) return component;
            }
            return null;
        }

        /// <summary>
        /// The name set on the component, or empty. VRCFury can also build a name from the object's
        /// name through private rules that change between versions, so that isn't guessed at here.
        /// </summary>
        public static string ReadName(Component component) {
            switch (TypeOf(component)) {
                case ExistingType.Contact:
                    return ((ContactReceiver)component).parameter ?? "";
                case ExistingType.Socket:
                    var oscId = ReadString(component, VrcFuryCompat.FieldOscId);
                    return string.IsNullOrWhiteSpace(oscId) ? ReadString(component, VrcFuryCompat.FieldName) : oscId;
                case ExistingType.Plug:
                case ExistingType.TouchZone:
                    return ReadString(component, VrcFuryCompat.FieldName);
                default:
                    return "";
            }
        }

        private static string ReadString(Component component, string field) {
            using (var serialized = new SerializedObject(component)) {
                var property = serialized.FindProperty(field);
                return property != null && property.propertyType == SerializedPropertyType.String
                    ? property.stringValue ?? ""
                    : "";
            }
        }

        /// <summary>The override if there is one, or else what the component says.</summary>
        public static string NameOf(AdvoscTrigger trigger) {
            var typed = trigger.existingName?.Trim();
            return string.IsNullOrEmpty(typed) ? ReadName(trigger.existingComponent)?.Trim() ?? "" : typed;
        }

        /// <summary>VRChat swaps these characters for _ in OSC addresses.</summary>
        public static string ToOscName(string name) {
            return OscUnsafe.Replace(name ?? "", "_");
        }

        /// <summary>VRCFury also swaps / in an id, since it's one segment of the parameter name.</summary>
        public static string ToOscId(string name) {
            return ToOscName(name).Replace('/', '_');
        }

        public static bool IsConstant(Component component) {
            return component is ContactReceiver receiver && receiver.receiverType != ContactReceiver.ReceiverType.Proximity;
        }

        /// <summary>
        /// False means the name isn't on the avatar as it stands now. A tool may still add or rename
        /// it during the build, so this can only ever be a warning.
        /// </summary>
        public static bool IsOnAvatar(VRCAvatarDescriptor descriptor, string parameter) {
            var expressions = descriptor.expressionParameters as VRCExpressionParameters;
            if (expressions != null && expressions.parameters != null) {
                foreach (var entry in expressions.parameters) {
                    if (entry != null && entry.name == parameter) return true;
                }
            }

            foreach (var layer in descriptor.baseAnimationLayers) {
                var controller = layer.animatorController as UnityEditor.Animations.AnimatorController;
                if (controller == null) continue;
                foreach (var entry in controller.parameters) {
                    if (entry.name == parameter) return true;
                }
            }
            return false;
        }

        /// <summary>Everything on an avatar that could get an Existing trigger and hasn't got one yet.</summary>
        public static List<Component> Scan(VRCAvatarDescriptor descriptor) {
            var root = descriptor.gameObject;
            var triggers = TriggerValidator.FindTriggers(root);

            var taken = new HashSet<Component>();
            var names = new HashSet<string>();
            foreach (var trigger in triggers) {
                if (trigger.kind != TriggerKind.Existing || trigger.existingComponent == null) continue;
                taken.Add(trigger.existingComponent);
                if (TypeOf(trigger.existingComponent) == ExistingType.Contact) names.Add(NameOf(trigger));
            }

            var found = new List<Component>();
            foreach (var receiver in root.GetComponentsInChildren<ContactReceiver>(true)) {
                // Several contacts on one parameter are all one source as far as a rule is concerned.
                if (string.IsNullOrWhiteSpace(receiver.parameter) || !names.Add(receiver.parameter)) continue;
                if (!taken.Contains(receiver) && !TriggerValidator.IsInsideEditorOnly(receiver.transform)) found.Add(receiver);
            }

            foreach (var type in new[] { VrcFuryCompat.SocketType, VrcFuryCompat.PlugType, VrcFuryCompat.TouchZoneType }) {
                if (type == null) continue;
                foreach (var component in root.GetComponentsInChildren(type, true)) {
                    if (!taken.Contains(component) && !TriggerValidator.IsInsideEditorOnly(component.transform)) found.Add(component);
                }
            }
            return found;
        }

        /// <summary>Adds a trigger next to the component, so moving the object keeps them together.</summary>
        public static AdvoscTrigger Add(Component component, VRCAvatarDescriptor descriptor) {
            var trigger = Undo.AddComponent<AdvoscTrigger>(component.gameObject);
            var name = ReadName(component);
            var label = string.IsNullOrWhiteSpace(name) ? component.gameObject.name : name;

            trigger.kind = TriggerKind.Existing;
            trigger.existingComponent = component;
            trigger.id = UniqueId(label, descriptor, trigger);
            trigger.displayName = label.Length > AdvoscTrigger.MaxIdLength ? trigger.id : label;

            EditorUtility.SetDirty(trigger);
            PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
            return trigger;
        }

        private static string UniqueId(string label, VRCAvatarDescriptor descriptor, AdvoscTrigger self) {
            var baseId = AdvoscTrigger.SanitizeId(label);
            if (baseId.Length == 0) baseId = "existing";

            var used = new HashSet<string>();
            foreach (var trigger in TriggerValidator.FindTriggers(descriptor.gameObject)) {
                if (trigger != self) used.Add(trigger.id);
            }

            var id = baseId;
            for (var i = 2; used.Contains(id); i++) {
                var suffix = "_" + i;
                id = baseId.Substring(0, Math.Min(baseId.Length, AdvoscTrigger.MaxIdLength - suffix.Length)) + suffix;
            }
            return id;
        }
    }
}
