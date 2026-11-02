// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;

namespace ADVOSC.Triggers.Editor {
    internal enum IssueSeverity {
        Warning,
        Error
    }

    internal sealed class TriggerIssue {
        public AdvoscTrigger Trigger;
        public IssueSeverity Severity;
        public string Message;

        /// <summary>This one would also break the exported bundle, not just the avatar build.</summary>
        public bool BlocksExport;

        public override string ToString() {
            return Trigger != null ? $"{Trigger.name}: {Message}" : Message;
        }
    }

    /// <summary>The checks shared by the inspector, the avatar window, the build hook and the exporter.</summary>
    internal static class TriggerValidator {
        private const float MaxContactRadius = 3f;

        // Leaves room for the " (others)" a Touch Zone set to Both adds to its rule names.
        private const int MaxDisplayNameLength = BundleExporter.MaxNameLength - 10;

        public static AdvoscTrigger[] FindTriggers(GameObject avatarObject) {
            var found = new List<AdvoscTrigger>();
            foreach (var trigger in avatarObject.GetComponentsInChildren<AdvoscTrigger>(true)) {
                if (!IsInsideEditorOnly(trigger.transform)) found.Add(trigger);
            }
            return found.ToArray();
        }

        public static List<TriggerIssue> ValidateAvatar(VRCAvatarDescriptor descriptor) {
            var issues = new List<TriggerIssue>();
            var triggers = FindTriggers(descriptor.gameObject);
            foreach (var trigger in triggers) {
                ValidateTrigger(trigger, triggers, issues);
            }
            return issues;
        }

        public static List<TriggerIssue> ValidateOne(AdvoscTrigger trigger) {
            var issues = new List<TriggerIssue>();
            var descriptor = trigger.GetComponentInParent<VRCAvatarDescriptor>(true);
            if (descriptor == null) {
                Add(issues, trigger, IssueSeverity.Error, "This trigger is not inside an avatar. Move it under the object with the VRC Avatar Descriptor.");
                ValidateTrigger(trigger, new[] { trigger }, issues);
                return issues;
            }
            if (IsInsideEditorOnly(trigger.transform)) {
                Add(issues, trigger, IssueSeverity.Warning, "This object is tagged EditorOnly, so this trigger is left out of builds and exports.");
            }
            ValidateTrigger(trigger, FindTriggers(descriptor.gameObject), issues);
            return issues;
        }

        private static void ValidateTrigger(AdvoscTrigger trigger, AdvoscTrigger[] allTriggers, List<TriggerIssue> issues) {
            CheckId(trigger, allTriggers, issues);
            CheckLiteral(trigger, trigger.displayName, "The display name", issues);
            if (trigger.displayName != null && trigger.displayName.Trim().Length > MaxDisplayNameLength) {
                AddExportBlocker(issues, trigger, $"The display name can't be over {MaxDisplayNameLength} characters.");
            }

            switch (trigger.kind) {
                case TriggerKind.SpsSocket:
                    CheckVrcFury(trigger, issues);
                    break;
                case TriggerKind.SpsPlug:
                    CheckVrcFury(trigger, issues);
                    CheckPlugRenderer(trigger, issues);
                    break;
                case TriggerKind.TouchZone:
                    CheckVrcFury(trigger, issues);
                    CheckRadius(trigger, issues);
                    break;
                case TriggerKind.ContactPreset:
                    CheckOwnBody(trigger, issues);
                    CheckManualTarget(trigger, issues);
                    CheckPlan(trigger, issues);
                    break;
                case TriggerKind.CustomContact:
                    CheckRadius(trigger, issues);
                    CheckTags(trigger, issues);
                    CheckPlan(trigger, issues);
                    break;
            }
        }

        private static void CheckId(AdvoscTrigger trigger, AdvoscTrigger[] allTriggers, List<TriggerIssue> issues) {
            if (!AdvoscTrigger.IsValidId(trigger.id)) {
                AddExportBlocker(issues, trigger, "The ID must be 1 to 48 characters using only letters, numbers, - and _.");
                return;
            }
            foreach (var other in allTriggers) {
                if (other != trigger && other.id == trigger.id) {
                    AddExportBlocker(issues, trigger, $"Another trigger on this avatar ({other.name}) already uses the ID '{trigger.id}'.");
                    return;
                }
            }
        }

        private static void CheckLiteral(AdvoscTrigger trigger, string text, string what, List<TriggerIssue> issues) {
            if (HasPlaceholder(text)) {
                AddExportBlocker(issues, trigger, $"{what} can't contain {{{{ or [[. ADVOSC rejects them in links.");
            }
        }

        public static bool HasPlaceholder(string text) {
            return !string.IsNullOrEmpty(text) && (text.Contains("{{") || text.Contains("[["));
        }

        private static void CheckVrcFury(AdvoscTrigger trigger, List<TriggerIssue> issues) {
            var problem = VrcFuryCompat.GetProblem(trigger.kind);
            if (problem != null) Add(issues, trigger, IssueSeverity.Error, problem);
        }

        private static void CheckRadius(AdvoscTrigger trigger, List<TriggerIssue> issues) {
            if (trigger.customRadius <= 0f) {
                Add(issues, trigger, IssueSeverity.Error, "The radius must be above 0.");
            } else if (trigger.customRadius > MaxContactRadius) {
                Add(issues, trigger, IssueSeverity.Error, $"The radius can't be over {MaxContactRadius} m.");
            }
        }

        private static void CheckTags(AdvoscTrigger trigger, List<TriggerIssue> issues) {
            var tags = ContactPlan.CleanTags(trigger.customTags);
            if (tags.Count == 0) {
                Add(issues, trigger, IssueSeverity.Error, "Add at least one collision tag, or nothing can touch this contact.");
            } else if (tags.Count > ContactBase.MAX_COLLISION_TAGS) {
                Add(issues, trigger, IssueSeverity.Error, $"A contact can have at most {ContactBase.MAX_COLLISION_TAGS} collision tags.");
            }
        }

        private static void CheckPlan(AdvoscTrigger trigger, List<TriggerIssue> issues) {
            if (!ContactPlan.TryCreate(trigger, out _, out var error)) {
                Add(issues, trigger, IssueSeverity.Error, error);
            }
        }

        private static void CheckOwnBody(AdvoscTrigger trigger, List<TriggerIssue> issues) {
            if (trigger.who == TriggerWho.Others || !ContactPresets.Get(trigger.preset).TouchedByOwnBody) return;
            Add(issues, trigger, IssueSeverity.Warning, "Your own body is always inside this spot, so it would stay on. Set Who can trigger it to Other people.");
        }

        private static void CheckManualTarget(AdvoscTrigger trigger, List<TriggerIssue> issues) {
            if (trigger.manualTarget == null) return;
            var descriptor = trigger.GetComponentInParent<VRCAvatarDescriptor>(true);
            if (descriptor != null && !trigger.manualTarget.IsChildOf(descriptor.transform)) {
                Add(issues, trigger, IssueSeverity.Error, "The manual target must be part of this avatar.");
            }
        }

        private static void CheckPlugRenderer(AdvoscTrigger trigger, List<TriggerIssue> issues) {
            var descriptor = trigger.GetComponentInParent<VRCAvatarDescriptor>(true);
            if (descriptor == null) return;

            if (trigger.plugRenderer != null) {
                if (!trigger.plugRenderer.transform.IsChildOf(descriptor.transform)) {
                    Add(issues, trigger, IssueSeverity.Error, "The plug mesh must be part of this avatar.");
                }
                return;
            }

            var message = FindAutoRendererProblem(trigger.transform, descriptor.transform);
            if (message != null) Add(issues, trigger, IssueSeverity.Error, message);
        }

        /// <summary>
        /// A rough version of VRCFury's mesh search, walking up from the trigger. It's only here to
        /// catch the plain "no mesh at all" case early, with a clearer message than the build gives.
        /// </summary>
        private static string FindAutoRendererProblem(Transform start, Transform avatarRoot) {
            var skins = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (var current = start; current != null; current = current.parent) {
                var onObject = current.GetComponents<Renderer>();
                if (onObject.Length == 1) return null;
                if (onObject.Length > 1) {
                    return $"{current.name} has more than one mesh on it, so VRCFury can't pick one. Set the plug mesh on this trigger.";
                }
                foreach (var skin in skins) {
                    if (IsWeightedTo(skin, current)) return null;
                }
                if (current == avatarRoot) break;
            }
            return "No mesh was found for this plug. Set the plug mesh on this trigger, or move the trigger onto the mesh or its bones.";
        }

        private static bool IsWeightedTo(SkinnedMeshRenderer skin, Transform bone) {
            if (skin.transform.IsChildOf(bone)) return false;
            foreach (var skinBone in skin.bones) {
                if (skinBone != null && skinBone.IsChildOf(bone)) return true;
            }
            return false;
        }

        public static bool IsInsideEditorOnly(Transform transform) {
            for (var current = transform; current != null; current = current.parent) {
                if (current.CompareTag("EditorOnly")) return true;
            }
            return false;
        }

        private static void Add(List<TriggerIssue> issues, AdvoscTrigger trigger, IssueSeverity severity, string message) {
            issues.Add(new TriggerIssue { Trigger = trigger, Severity = severity, Message = message });
        }

        private static void AddExportBlocker(List<TriggerIssue> issues, AdvoscTrigger trigger, string message) {
            issues.Add(new TriggerIssue { Trigger = trigger, Severity = IssueSeverity.Error, Message = message, BlocksExport = true });
        }
    }
}
