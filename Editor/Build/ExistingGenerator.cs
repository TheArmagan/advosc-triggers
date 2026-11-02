// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace ADVOSC.Triggers.Editor {
    /// <summary>What it points at is already on the avatar, so there's nothing to build.</summary>
    internal sealed class ExistingGenerator : ITriggerGenerator {
        public bool Handles(TriggerKind kind) {
            return kind == TriggerKind.Existing;
        }

        public void Generate(AdvoscTrigger trigger, BuildContext context) {
        }
    }
}
