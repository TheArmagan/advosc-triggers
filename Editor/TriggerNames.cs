// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace ADVOSC.Triggers.Editor {
    /// <summary>The names and addresses from the contract, kept in one place.</summary>
    internal static class TriggerNames {
        private const string AvatarParameterPrefix = "/avatar/parameters/";

        public static string Display(AdvoscTrigger trigger) {
            if (!string.IsNullOrWhiteSpace(trigger.displayName)) return trigger.displayName.Trim();
            if (trigger.kind == TriggerKind.ContactPreset) return ContactPresets.Get(trigger.preset).Label;
            return trigger.id;
        }

        public static string ContactParameter(AdvoscTrigger trigger) {
            return "ADVOSC/" + trigger.id;
        }

        public static string ContactAddress(AdvoscTrigger trigger) {
            return AvatarParameterPrefix + ContactParameter(trigger);
        }

        public static string ParameterAddress(string parameter) {
            return AvatarParameterPrefix + parameter;
        }

        public static string TouchZoneAddress(AdvoscTrigger trigger, TriggerWho side) {
            return TouchZoneAddress(trigger.id, side);
        }

        public static string TouchZoneAddress(string id, TriggerWho side) {
            return $"{AvatarParameterPrefix}VFH/Zone/Touch/{id}/{(side == TriggerWho.Self ? "Self" : "Others")}";
        }

        /// <summary>The derived depth for this trigger's Self, Others or Both setting.</summary>
        public static string SpsDepthAddress(AdvoscTrigger trigger) {
            return SpsDepthAddress(trigger.kind == TriggerKind.SpsPlug ? "plug" : "socket", trigger.id, trigger.who);
        }

        public static string SpsDepthAddress(string kind, string id, TriggerWho who) {
            string metric;
            switch (who) {
                case TriggerWho.Self:
                    metric = "depthSelf";
                    break;
                case TriggerWho.Others:
                    metric = "depthOthers";
                    break;
                default:
                    metric = "depth";
                    break;
            }
            return $"/advosc/sps/{kind}/{id}/{metric}";
        }
    }
}
