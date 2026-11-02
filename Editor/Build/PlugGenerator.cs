// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace ADVOSC.Triggers.Editor {
    internal sealed class PlugGenerator : ITriggerGenerator {
        public bool Handles(TriggerKind kind) {
            return kind == TriggerKind.SpsPlug;
        }

        public void Generate(AdvoscTrigger trigger, BuildContext context) {
            var plug = trigger.gameObject.AddComponent(VrcFuryCompat.PlugType);
            using (var fields = new FuryFieldWriter(plug)) {
                // VRCFury uses a plug's name as its id in OGB/Pen/<id>.
                fields.SetString(VrcFuryCompat.FieldName, trigger.id);
                if (trigger.plugRenderer != null) {
                    fields.SetBool(VrcFuryCompat.FieldAutoRenderer, false);
                    fields.SetObjectList(VrcFuryCompat.FieldRenderers, trigger.plugRenderer);
                }
                fields.Apply();
            }
        }
    }
}
