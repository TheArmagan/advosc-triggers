// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace ADVOSC.Triggers.Editor {
    /// <summary>
    /// Where a contact goes and how big it is. The inspector, gizmo and build hook all read this,
    /// so what the gizmo shows is what the build makes.
    /// </summary>
    internal sealed class ContactPlan {
        public Transform Anchor;

        // All local to Anchor, which is how a contact reads its fields when it has a root transform.
        public Vector3 LocalPosition;
        public Quaternion LocalRotation = Quaternion.identity;
        public float LocalRadius;
        public float LocalHeight;

        public TriggerShape Shape;
        public TriggerReceiverType ReceiverType;
        public List<string> Tags;
        public bool AllowSelf;
        public bool AllowOthers;
        public string AnchorDescription;

        public static bool TryCreate(AdvoscTrigger trigger, out ContactPlan plan, out string error) {
            plan = null;
            switch (trigger.kind) {
                case TriggerKind.ContactPreset:
                    return TryCreatePreset(trigger, out plan, out error);
                case TriggerKind.CustomContact:
                    return TryCreateCustom(trigger, out plan, out error);
                default:
                    error = "This trigger is not a contact.";
                    return false;
            }
        }

        private static bool TryCreatePreset(AdvoscTrigger trigger, out ContactPlan plan, out string error) {
            plan = null;
            var preset = ContactPresets.Get(trigger.preset);
            var descriptor = trigger.GetComponentInParent<VRCAvatarDescriptor>(true);
            if (descriptor == null) {
                error = "This trigger is not inside an avatar.";
                return false;
            }

            var useRight = preset.HasSides && trigger.presetSide == TriggerSide.Right;
            var anchor = trigger.manualTarget;
            var description = trigger.manualTarget != null ? "Manual target: " + trigger.manualTarget.name : null;
            if (anchor == null) {
                var bones = useRight ? preset.RightBones : preset.LeftBones;
                anchor = AvatarBones.FindFirst(descriptor, bones, out var usedBone);
                if (anchor != null) {
                    description = $"Humanoid bone {usedBone}: {anchor.name}";
                }
            }
            if (anchor == null) {
                error = $"The {preset.Label} preset needs the {DescribeBones(useRight ? preset.RightBones : preset.LeftBones)} bone, " +
                        "but this avatar's humanoid rig doesn't map it. Set a manual target on this trigger.";
                return false;
            }

            var anchorScale = LargestScale(anchor);
            if (anchorScale <= 0f) {
                error = $"{anchor.name} has zero scale, so a contact on it can't work.";
                return false;
            }

            var avatarScale = AvatarSizeScale(descriptor);
            var offset = preset.Offset;
            if (useRight) offset.x = -offset.x;
            var scaledOffset = offset * avatarScale + trigger.presetOffset;
            var worldPosition = anchor.position + descriptor.transform.rotation * scaledOffset;
            var radiusScale = trigger.presetRadiusScale > 0f ? trigger.presetRadiusScale : 1f;

            plan = new ContactPlan {
                Anchor = anchor,
                LocalPosition = anchor.InverseTransformPoint(worldPosition),
                LocalRadius = preset.Radius * avatarScale * radiusScale / anchorScale,
                Shape = TriggerShape.Sphere,
                ReceiverType = preset.Type,
                Tags = new List<string>(preset.Tags),
                AllowSelf = trigger.who != TriggerWho.Others,
                AllowOthers = trigger.who != TriggerWho.Self,
                AnchorDescription = description
            };
            error = null;
            return true;
        }

        private static bool TryCreateCustom(AdvoscTrigger trigger, out ContactPlan plan, out string error) {
            plan = null;
            var anchor = trigger.transform;
            var anchorScale = LargestScale(anchor);
            if (anchorScale <= 0f) {
                error = $"{anchor.name} has zero scale, so a contact on it can't work.";
                return false;
            }

            var isCapsule = trigger.customShape == TriggerShape.Capsule;
            var radius = trigger.customRadius;
            plan = new ContactPlan {
                Anchor = anchor,
                LocalPosition = Vector3.zero,
                LocalRadius = radius / anchorScale,
                // A capsule's height includes its round ends, so it can't be under twice the radius.
                LocalHeight = isCapsule ? Mathf.Max(trigger.customHeight, radius * 2f) / anchorScale : 0f,
                Shape = trigger.customShape,
                ReceiverType = trigger.customReceiverType,
                Tags = CleanTags(trigger.customTags),
                AllowSelf = trigger.who != TriggerWho.Others,
                AllowOthers = trigger.who != TriggerWho.Self,
                AnchorDescription = "This object: " + anchor.name
            };
            error = null;
            return true;
        }

        public static List<string> CleanTags(IEnumerable<string> tags) {
            var output = new List<string>();
            if (tags == null) return output;
            foreach (var tag in tags) {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                var trimmed = tag.Trim();
                if (!output.Contains(trimmed)) output.Add(trimmed);
            }
            return output;
        }

        public static float LargestScale(Transform transform) {
            var scale = transform.lossyScale;
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        /// <summary>How much bigger this avatar is than the size the preset table assumes.</summary>
        private static float AvatarSizeScale(VRCAvatarDescriptor descriptor) {
            var root = descriptor.transform;
            var eyeHeight = root.TransformVector(descriptor.ViewPosition).y;
            if (eyeHeight <= 0.01f) return 1f;
            return Mathf.Clamp(eyeHeight / ContactPresets.ReferenceEyeHeight, 0.1f, 10f);
        }

        private static string DescribeBones(HumanBodyBones[] bones) {
            return string.Join(" or ", Array.ConvertAll(bones, bone => bone.ToString()));
        }
    }

    internal static class AvatarBones {
        public static Transform FindFirst(VRCAvatarDescriptor descriptor, HumanBodyBones[] bones, out HumanBodyBones usedBone) {
            usedBone = HumanBodyBones.LastBone;
            var animator = descriptor.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return null;

            foreach (var bone in bones) {
                var found = Find(animator, bone);
                if (found == null) continue;
                usedBone = bone;
                return found;
            }
            return null;
        }

        private static Transform Find(Animator animator, HumanBodyBones bone) {
            Transform found = null;
            if (animator.isActiveAndEnabled) {
                found = animator.GetBoneTransform(bone);
            }
            return found != null ? found : FindInDescription(animator, bone);
        }

        /// <summary>GetBoneTransform can't be relied on while the avatar is inactive, so read the rig map.</summary>
        private static Transform FindInDescription(Animator animator, HumanBodyBones bone) {
            var boneName = bone.ToString();
            foreach (var mapped in animator.avatar.humanDescription.human) {
                var humanName = mapped.humanName.Replace(" ", "");
                if (!string.Equals(humanName, boneName, StringComparison.OrdinalIgnoreCase)) continue;
                return FindByName(animator.transform, mapped.boneName);
            }
            return null;
        }

        private static Transform FindByName(Transform root, string name) {
            var pending = new Queue<Transform>();
            pending.Enqueue(root);
            while (pending.Count > 0) {
                var current = pending.Dequeue();
                if (current.name == name) return current;
                foreach (Transform child in current) {
                    pending.Enqueue(child);
                }
            }
            return null;
        }
    }
}
