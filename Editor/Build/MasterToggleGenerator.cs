// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using com.vrcfury.api;

namespace ADVOSC.Triggers.Editor {
    /// <summary>One menu toggle for all contacts. Sockets keep VRCFury's own per-socket toggles.</summary>
    internal static class MasterToggleGenerator {
        private const string MenuPath = "ADVOSC Triggers/Contacts";

        public static void Generate(BuildContext context) {
            var toggle = FuryComponents.CreateToggle(context.Root);
            toggle.SetMenuPath(MenuPath);
            toggle.SetDefaultOn();
            toggle.GetActions().AddTurnOn(context.Root);
        }
    }
}
