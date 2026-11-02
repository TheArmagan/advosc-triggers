// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using com.vrcfury.api;
using com.vrcfury.api.Components;

namespace ADVOSC.Triggers.Editor {
    internal sealed class SocketGenerator : ITriggerGenerator {
        public bool Handles(TriggerKind kind) {
            return kind == TriggerKind.SpsSocket;
        }

        public void Generate(AdvoscTrigger trigger, BuildContext context) {
            var socket = FuryComponents.CreateSocket(trigger.gameObject);
            socket.SetName(TriggerNames.Display(trigger));
            socket.SetMode(ToFuryMode(trigger.socketMode));

            // CreateSocket hands back a wrapper, so find the component it added. New ones go last.
            var added = trigger.GetComponents(VrcFuryCompat.SocketType);
            if (added.Length == 0) {
                throw new AdvoscBuildException($"VRCFury did not add a socket to {trigger.name}.");
            }

            using (var fields = new FuryFieldWriter(added[added.Length - 1])) {
                fields.SetString(VrcFuryCompat.FieldOscId, trigger.id);
                fields.Apply();
            }
        }

        private static FurySocket.Mode ToFuryMode(TriggerSocketMode mode) {
            switch (mode) {
                case TriggerSocketMode.Hole: return FurySocket.Mode.Hole;
                case TriggerSocketMode.Ring: return FurySocket.Mode.Ring;
                case TriggerSocketMode.RingOneWay: return FurySocket.Mode.RingOneWay;
                case TriggerSocketMode.None: return FurySocket.Mode.None;
                default: return FurySocket.Mode.Auto;
            }
        }
    }
}
