// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;

namespace ADVOSC.Triggers.Editor {
    /// <summary>Builds the rules a trigger starts with in ADVOSC.</summary>
    internal static class StarterRules {
        private const string FloatMatch = "> 0.1";
        private const string FloatReleaseMatch = "<= 0.05";
        private const string BoolMatch = "1";
        private const int EdgeCooldownMs = 1000;
        private const float PulseLevel = 0.5f;
        private const float PulseSeconds = 1f;

        private sealed class Source {
            public string Key;
            public string Name;
            public string Address;
            public bool IsBool;
            public bool IsSpsDepth;
        }

        public static List<BundleRule> Build(AdvoscTrigger trigger) {
            var rules = new List<BundleRule>();
            foreach (var source in FindSources(trigger)) {
                rules.Add(BuildRule(trigger.starterRule, source));
            }
            return rules;
        }

        private static List<Source> FindSources(AdvoscTrigger trigger) {
            var name = TriggerNames.Display(trigger);
            var sources = new List<Source>();

            switch (trigger.kind) {
                case TriggerKind.SpsSocket:
                case TriggerKind.SpsPlug:
                    sources.Add(new Source {
                        Key = trigger.id,
                        Name = name,
                        Address = TriggerNames.SpsDepthAddress(trigger),
                        IsSpsDepth = true
                    });
                    break;

                case TriggerKind.TouchZone:
                    // A zone has one parameter per side, so Both needs a rule for each.
                    if (trigger.who == TriggerWho.Both) {
                        sources.Add(TouchSource(trigger, $"{trigger.id}-self", $"{name} (self)", TriggerWho.Self));
                        sources.Add(TouchSource(trigger, $"{trigger.id}-others", $"{name} (others)", TriggerWho.Others));
                    } else {
                        sources.Add(TouchSource(trigger, trigger.id, name, trigger.who));
                    }
                    break;

                case TriggerKind.Existing:
                    AddExistingSources(trigger, name, sources);
                    break;

                default:
                    sources.Add(new Source {
                        Key = trigger.id,
                        Name = name,
                        Address = TriggerNames.ContactAddress(trigger),
                        IsBool = IsConstantContact(trigger)
                    });
                    break;
            }
            return sources;
        }

        private static Source TouchSource(AdvoscTrigger trigger, string key, string name, TriggerWho side) {
            return new Source {
                Key = key,
                Name = name,
                Address = TriggerNames.TouchZoneAddress(trigger, side)
            };
        }

        private static void AddExistingSources(AdvoscTrigger trigger, string name, List<Source> sources) {
            var target = trigger.existingComponent;
            var targetName = ExistingTargets.NameOf(trigger);

            switch (ExistingTargets.TypeOf(target)) {
                case ExistingType.Contact:
                    sources.Add(new Source {
                        Key = trigger.id,
                        Name = name,
                        Address = TriggerNames.ParameterAddress(ExistingTargets.ToOscName(targetName)),
                        IsBool = ExistingTargets.IsConstant(target)
                    });
                    break;

                case ExistingType.Socket:
                case ExistingType.Plug:
                    var kind = ExistingTargets.TypeOf(target) == ExistingType.Plug ? "plug" : "socket";
                    sources.Add(new Source {
                        Key = trigger.id,
                        Name = name,
                        Address = TriggerNames.SpsDepthAddress(kind, ExistingTargets.ToOscId(targetName), trigger.who),
                        IsSpsDepth = true
                    });
                    break;

                case ExistingType.TouchZone:
                    var zone = ExistingTargets.ToOscId(targetName);
                    if (trigger.who == TriggerWho.Both) {
                        sources.Add(ExistingZoneSource($"{trigger.id}-self", $"{name} (self)", zone, TriggerWho.Self));
                        sources.Add(ExistingZoneSource($"{trigger.id}-others", $"{name} (others)", zone, TriggerWho.Others));
                    } else {
                        sources.Add(ExistingZoneSource(trigger.id, name, zone, trigger.who));
                    }
                    break;
            }
        }

        private static Source ExistingZoneSource(string key, string name, string zone, TriggerWho side) {
            return new Source { Key = key, Name = name, Address = TriggerNames.TouchZoneAddress(zone, side) };
        }

        private static bool IsConstantContact(AdvoscTrigger trigger) {
            var type = trigger.kind == TriggerKind.ContactPreset
                ? ContactPresets.Get(trigger.preset).Type
                : trigger.customReceiverType;
            return type == TriggerReceiverType.Constant;
        }

        private static BundleRule BuildRule(StarterRuleChoice choice, Source source) {
            switch (choice) {
                case StarterRuleChoice.Toy:
                    if (source.IsSpsDepth) {
                        return AddAction(LiveRule(source), new ToyLinkAction());
                    }
                    return AddAction(EdgeRule(source), new ToyPulseAction { Level = PulseLevel, Seconds = PulseSeconds });

                case StarterRuleChoice.Variable:
                    // ADVOSC ignores this value on a live rule and sends the mapped number instead.
                    return AddAction(LiveRule(source), new SetVariableAction { Name = VariableName(source), Value = "0" });

                case StarterRuleChoice.Counter:
                    return AddAction(EdgeRule(source), new ChangeVariableAction { Name = VariableName(source), Value = "1" });

                case StarterRuleChoice.SoundPlaceholder:
                    return AddAction(EdgeRule(source), new SoundPlaceholderAction());

                default:
                    return EdgeRule(source);
            }
        }

        private static BundleRule AddAction(BundleRule rule, BundleAction action) {
            rule.Actions.Add(action);
            return rule;
        }

        private static BundleRule EdgeRule(Source source) {
            return new BundleRule {
                Key = source.Key,
                Name = source.Name,
                Address = source.Address,
                Match = source.IsBool ? BoolMatch : FloatMatch,
                ReleaseMatch = source.IsBool ? null : FloatReleaseMatch,
                Mode = BundleRule.ModeEdge,
                CooldownMs = EdgeCooldownMs
            };
        }

        private static BundleRule LiveRule(Source source) {
            return new BundleRule {
                Key = source.Key,
                Name = source.Name,
                Address = source.Address,
                Mode = BundleRule.ModeLive,
                Range = new BundleRange { InMin = 0f, InMax = 1f, OutMin = 0f, OutMax = 1f }
            };
        }

        /// <summary>ADVOSC placeholders split on ;, so a name with one couldn't be read back.</summary>
        private static string VariableName(Source source) {
            var name = source.Name.Replace(";", "").Trim();
            return name.Length > 0 ? name : source.Key;
        }
    }
}
