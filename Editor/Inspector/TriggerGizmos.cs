// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using UnityEditor;
using UnityEngine;

namespace ADVOSC.Triggers.Editor {
    internal static class TriggerGizmos {
        private static readonly Color ZoneColor = new Color(0.25f, 0.8f, 1f, 0.9f);
        private const float SpsArrowLength = 0.1f;

        [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.InSelectionHierarchy)]
        private static void Draw(AdvoscTrigger trigger, GizmoType gizmoType) {
            var previousColor = Gizmos.color;
            var previousMatrix = Gizmos.matrix;
            Gizmos.color = ZoneColor;

            switch (trigger.kind) {
                case TriggerKind.SpsSocket:
                case TriggerKind.SpsPlug:
                    DrawSps(trigger);
                    break;
                case TriggerKind.TouchZone:
                    if (trigger.customRadius > 0f) {
                        Gizmos.DrawWireSphere(trigger.transform.position, trigger.customRadius);
                    }
                    break;
                case TriggerKind.Existing:
                    break;
                default:
                    if (ContactPlan.TryCreate(trigger, out var plan, out _)) {
                        DrawContact(plan);
                    }
                    break;
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        private static void DrawSps(AdvoscTrigger trigger) {
            var transform = trigger.transform;
            var tip = transform.position + transform.forward * SpsArrowLength;
            Gizmos.DrawLine(transform.position, tip);
            Gizmos.DrawWireSphere(transform.position, SpsArrowLength * 0.15f);
        }

        private static void DrawContact(ContactPlan plan) {
            var scale = ContactPlan.LargestScale(plan.Anchor);
            var center = plan.Anchor.TransformPoint(plan.LocalPosition);
            var radius = plan.LocalRadius * scale;

            if (plan.Shape == TriggerShape.Sphere) {
                Gizmos.DrawWireSphere(center, radius);
                return;
            }

            var rotation = plan.Anchor.rotation * plan.LocalRotation;
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            var endDistance = Mathf.Max(plan.LocalHeight * scale * 0.5f - radius, 0f);
            var top = Vector3.up * endDistance;
            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawWireSphere(-top, radius);
            foreach (var side in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back }) {
                Gizmos.DrawLine(top + side * radius, -top + side * radius);
            }
        }
    }
}
