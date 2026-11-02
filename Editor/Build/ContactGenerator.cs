// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;

namespace ADVOSC.Triggers.Editor {
    /// <summary>
    /// Each receiver sits under the generated root so the master toggle covers all of them, and
    /// uses rootTransform to follow its bone or its trigger.
    /// </summary>
    internal sealed class ContactGenerator : ITriggerGenerator {
        public bool Handles(TriggerKind kind) {
            return kind == TriggerKind.ContactPreset || kind == TriggerKind.CustomContact;
        }

        public void Generate(AdvoscTrigger trigger, BuildContext context) {
            if (!ContactPlan.TryCreate(trigger, out var plan, out var error)) {
                throw new AdvoscBuildException($"{trigger.name}: {error}");
            }

            var parameter = TriggerNames.ContactParameter(trigger);
            var obj = new GameObject(trigger.id);
            obj.transform.SetParent(context.Root.transform, false);

            var receiver = obj.AddComponent<VRCContactReceiver>();
            receiver.rootTransform = plan.Anchor;
            receiver.position = plan.LocalPosition;
            receiver.rotation = plan.LocalRotation;
            receiver.radius = plan.LocalRadius;
            if (plan.Shape == TriggerShape.Capsule) {
                receiver.shapeType = ContactBase.ShapeType.Capsule;
                receiver.height = plan.LocalHeight;
            } else {
                receiver.shapeType = ContactBase.ShapeType.Sphere;
            }
            receiver.receiverType = plan.ReceiverType == TriggerReceiverType.Constant
                ? ContactReceiver.ReceiverType.Constant
                : ContactReceiver.ReceiverType.Proximity;
            receiver.parameter = parameter;
            receiver.localOnly = true;
            receiver.allowSelf = plan.AllowSelf;
            receiver.allowOthers = plan.AllowOthers;
            receiver.collisionTags = plan.Tags;

            context.ContactParameters.Add(new ContactParameter { Name = parameter, Type = plan.ReceiverType });
        }
    }
}
