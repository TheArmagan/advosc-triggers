// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace ADVOSC.Triggers.Editor {
    internal sealed class TouchZoneGenerator : ITriggerGenerator {
        public bool Handles(TriggerKind kind) {
            return kind == TriggerKind.TouchZone;
        }

        public void Generate(AdvoscTrigger trigger, BuildContext context) {
            var scale = ContactPlan.LargestScale(trigger.transform);
            if (scale <= 0f) {
                throw new AdvoscBuildException($"{trigger.name} has zero scale, so its Touch Zone can't work.");
            }

            var zone = trigger.gameObject.AddComponent(VrcFuryCompat.TouchZoneType);
            using (var fields = new FuryFieldWriter(zone)) {
                // VRCFury uses this as <id> in VFH/Zone/Touch/<id>/Self and /Others.
                fields.SetString(VrcFuryCompat.FieldName, trigger.id);
                // VRCFury reads this radius in the object's local units.
                fields.SetFloat(VrcFuryCompat.FieldRadius, trigger.customRadius / scale);
                fields.Apply();
            }
        }
    }
}
