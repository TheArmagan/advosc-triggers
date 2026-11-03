// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;

namespace ADVOSC.Triggers.Editor {
    internal sealed class BundleRange {
        public float InMin;
        public float InMax;
        public float OutMin;
        public float OutMax;
    }

    internal sealed class BundleRule {
        public const string ModeEdge = "edge";
        public const string ModeLive = "live";

        public string Key;
        public string Name;
        public string Address;
        public string Match = "";
        public string ReleaseMatch;
        public string Mode;
        public BundleRange Range;
        public int? CooldownMs;
        public List<BundleAction> Actions = new List<BundleAction>();

        public void Write(JsonWriter json) {
            json.BeginObject();
            json.Key("key");
            json.Value(Key);
            json.Key("name");
            json.Value(Name);
            json.Key("source");
            json.BeginObject();
            json.Key("kind");
            json.Value("parameter");
            json.Key("address");
            json.Value(Address);
            json.EndObject();
            json.Key("match");
            json.Value(Match);
            if (ReleaseMatch != null) {
                json.Key("releaseMatch");
                json.Value(ReleaseMatch);
            }
            json.Key("mode");
            json.Value(Mode);
            if (Range != null) {
                json.Key("range");
                json.BeginObject();
                json.Key("inMin");
                json.Value(Range.InMin);
                json.Key("inMax");
                json.Value(Range.InMax);
                json.Key("outMin");
                json.Value(Range.OutMin);
                json.Key("outMax");
                json.Value(Range.OutMax);
                json.EndObject();
            }
            if (CooldownMs.HasValue) {
                json.Key("cooldownMs");
                json.Value(CooldownMs.Value);
            }
            json.Key("actions");
            json.BeginArray();
            foreach (var action in Actions) {
                action.Write(json);
            }
            json.EndArray();
            json.EndObject();
        }
    }

    internal abstract class BundleAction {
        public abstract void Write(JsonWriter json);
    }

    internal sealed class SetVariableAction : BundleAction {
        public string Name;
        public string Value;

        public override void Write(JsonWriter json) {
            json.BeginObject();
            json.Key("type");
            json.Value("setVariable");
            json.Key("name");
            json.Value(Name);
            json.Key("value");
            json.Value(Value);
            json.EndObject();
        }
    }

    /// <summary>Does math on a variable's current value. An unset variable counts as 0.</summary>
    internal sealed class ChangeVariableAction : BundleAction {
        public const string OpAdd = "add";

        public string Name;
        public string Op = OpAdd;
        /// <summary>A number for every op except append, or ADVOSC rejects the bundle.</summary>
        public string Value;

        public override void Write(JsonWriter json) {
            json.BeginObject();
            json.Key("type");
            json.Value("changeVariable");
            json.Key("name");
            json.Value(Name);
            json.Key("op");
            json.Value(Op);
            json.Key("value");
            json.Value(Value);
            json.EndObject();
        }
    }

    /// <summary>Carries no sound. ADVOSC shows "Pick a sound" until the user sets one.</summary>
    internal sealed class SoundPlaceholderAction : BundleAction {
        public override void Write(JsonWriter json) {
            json.BeginObject();
            json.Key("type");
            json.Value("sound");
            json.Key("soundIds");
            json.BeginArray();
            json.EndArray();
            json.Key("soundNames");
            json.BeginArray();
            json.EndArray();
            json.Key("loop");
            json.Value(false);
            json.Key("stopOnEnd");
            json.Value(false);
            json.EndObject();
        }
    }

    internal abstract class ToyAction : BundleAction {
        private const string VibrateFeature = "Vibrate";

        protected abstract string Kind { get; }

        protected virtual void WriteDetails(JsonWriter json) { }

        public override void Write(JsonWriter json) {
            json.BeginObject();
            json.Key("type");
            json.Value("toy");
            json.Key("action");
            json.BeginObject();
            json.Key("kind");
            json.Value(Kind);
            json.Key("target");
            json.BeginObject();
            json.Key("toyIds");
            json.BeginArray();
            json.EndArray();
            json.Key("features");
            json.BeginArray();
            json.Value(VibrateFeature);
            json.EndArray();
            json.EndObject();
            WriteDetails(json);
            json.EndObject();
            json.EndObject();
        }
    }

    internal sealed class ToyLinkAction : ToyAction {
        protected override string Kind => "link";
    }

    internal sealed class ToyPulseAction : ToyAction {
        public float Level;
        public float Seconds;

        protected override string Kind => "pulse";

        protected override void WriteDetails(JsonWriter json) {
            json.Key("level");
            json.Value(Level);
            json.Key("seconds");
            json.Value(Seconds);
        }
    }
}
