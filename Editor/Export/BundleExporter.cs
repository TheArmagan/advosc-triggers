// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using VRC.Core;
using VRC.SDK3.Avatars.Components;

namespace ADVOSC.Triggers.Editor {
    internal sealed class ExportResult {
        public readonly List<string> Errors = new List<string>();
        public int RuleCount;

        /// <summary>The file form, indented for reading.</summary>
        public string FileJson;

        public string Payload;
        public bool Ok => Errors.Count == 0;
        public bool FitsInLink => Payload != null && Payload.Length <= BundleExporter.MaxLinkPayload;
        public string AppLink => BundleExporter.AppLinkPrefix + Payload;
        public string WebLink => BundleExporter.WebLinkPrefix + Payload;
    }

    /// <summary>Writes the bundle described in Documentation~/contract.md.</summary>
    internal static class BundleExporter {
        public const int MaxLinkPayload = 16 * 1024;
        public const int MaxJsonBytes = 256 * 1024;
        public const int MaxRules = 64;
        public const string AppLinkPrefix = "advosc://import?d=";
        public const string WebLinkPrefix = "https://thearmagan.github.io/advosc-triggers/link/#";

        // ADVOSC's validator rejects a whole bundle over any one of these, so they're checked here
        // to give a useful message in Unity. They mirror src/shared/automation-bundle.ts.
        public const int MaxNameLength = 120;
        private const int MaxMatchLength = 128;
        private const int MaxAddressLength = 256;
        private const int MaxActions = 32;
        private const int MaxCooldownMs = 24 * 60 * 60 * 1000;

        private const string BundleKind = "advosc.automation.bundle";
        private const string BundleNameSuffix = " triggers";
        private const string FallbackAvatarName = "Avatar";
        private const int BundleVersion = 1;

        private static readonly Regex KeyPattern = new Regex(@"\A[A-Za-z0-9_-]{1,64}\z", RegexOptions.CultureInvariant);
        private static readonly Regex BlueprintIdPattern = new Regex(@"\Aavtr_[A-Za-z0-9_-]{1,64}\z", RegexOptions.CultureInvariant);
        private static readonly Regex AddressPattern = new Regex(@"\A[^\s\x00-\x1f\x7f#*,?\[\]{}]+\z", RegexOptions.CultureInvariant);

        public static ExportResult Export(VRCAvatarDescriptor descriptor) {
            var result = new ExportResult();

            foreach (var issue in TriggerValidator.ValidateAvatar(descriptor)) {
                if (issue.BlocksExport) result.Errors.Add(issue.ToString());
            }

            var rules = new List<BundleRule>();
            foreach (var trigger in TriggerValidator.FindTriggers(descriptor.gameObject)) {
                rules.AddRange(StarterRules.Build(trigger));
            }
            result.RuleCount = rules.Count;

            if (rules.Count == 0) {
                result.Errors.Add("This avatar has no triggers to export.");
            } else if (rules.Count > MaxRules) {
                result.Errors.Add($"This avatar has {rules.Count} rules, and a bundle can hold at most {MaxRules}.");
            }
            CheckRules(rules, result);
            if (!result.Ok) return result;

            var avatarName = CleanAvatarName(descriptor.gameObject.name);
            var blueprintId = FindBlueprintId(descriptor);

            var compact = new JsonWriter(false);
            WriteBundle(compact, avatarName, blueprintId, rules);
            foreach (var text in compact.Strings) {
                if (TriggerValidator.HasPlaceholder(text)) {
                    result.Errors.Add($"'{text}' contains {{{{ or [[, which ADVOSC rejects. Rename it and export again.");
                }
            }

            var compactBytes = new UTF8Encoding(false).GetBytes(compact.ToString());
            if (compactBytes.Length > MaxJsonBytes) {
                result.Errors.Add($"This bundle is {compactBytes.Length / 1024} KB, and ADVOSC reads at most {MaxJsonBytes / 1024} KB.");
            }
            if (!result.Ok) return result;

            var pretty = new JsonWriter(true);
            WriteBundle(pretty, avatarName, blueprintId, rules);
            result.FileJson = pretty.ToString();
            result.Payload = ToBase64Url(Deflate(compactBytes));
            return result;
        }

        private static void WriteBundle(JsonWriter json, string avatarName, string blueprintId, List<BundleRule> rules) {
            json.BeginObject();
            json.Key("kind");
            json.Value(BundleKind);
            json.Key("version");
            json.Value(BundleVersion);
            json.Key("name");
            json.Value(BundleName(avatarName));
            json.Key("avatarName");
            json.Value(Shorten(avatarName, MaxNameLength));
            if (blueprintId != null) {
                json.Key("avatarIds");
                json.BeginArray();
                json.Value(blueprintId);
                json.EndArray();
            }
            json.Key("rules");
            json.BeginArray();
            foreach (var rule in rules) {
                rule.Write(json);
            }
            json.EndArray();
            json.EndObject();
        }

        private static string FindBlueprintId(VRCAvatarDescriptor descriptor) {
            var pipeline = descriptor.GetComponent<PipelineManager>();
            if (pipeline == null || string.IsNullOrWhiteSpace(pipeline.blueprintId)) return null;
            // A malformed id would reject the whole bundle, and a rule without one still works.
            return BlueprintIdPattern.IsMatch(pipeline.blueprintId) ? pipeline.blueprintId : null;
        }

        private static void CheckRules(List<BundleRule> rules, ExportResult result) {
            var keys = new HashSet<string>();
            foreach (var rule in rules) {
                if (!KeyPattern.IsMatch(rule.Key)) {
                    result.Errors.Add($"The rule key '{rule.Key}' isn't valid.");
                } else if (!keys.Add(rule.Key)) {
                    result.Errors.Add($"Two rules would use the key '{rule.Key}'. A Touch Zone set to Both adds -self and -others to its ID, so change the ID that clashes.");
                }
                if (string.IsNullOrWhiteSpace(rule.Name) || rule.Name.Length > MaxNameLength) {
                    result.Errors.Add($"The rule name for '{rule.Key}' must be 1 to {MaxNameLength} characters.");
                }
                if (rule.Address.Length > MaxAddressLength || !AddressPattern.IsMatch(rule.Address)) {
                    result.Errors.Add($"The address for '{rule.Key}' isn't valid: {rule.Address}");
                }
                if (rule.Match.Length > MaxMatchLength) {
                    result.Errors.Add($"The match for '{rule.Key}' is over {MaxMatchLength} characters.");
                }
                if (rule.ReleaseMatch != null && (rule.ReleaseMatch.Trim().Length == 0 || rule.ReleaseMatch.Length > MaxMatchLength)) {
                    result.Errors.Add($"The release match for '{rule.Key}' must be 1 to {MaxMatchLength} characters.");
                }
                if (rule.Actions.Count > MaxActions) {
                    result.Errors.Add($"The rule '{rule.Key}' has more than {MaxActions} actions.");
                }
                if (rule.CooldownMs.HasValue && (rule.CooldownMs.Value < 0 || rule.CooldownMs.Value > MaxCooldownMs)) {
                    result.Errors.Add($"The cooldown on '{rule.Key}' must be from 0 to 24 hours.");
                }
            }
        }

        private static string CleanAvatarName(string name) {
            var trimmed = name.Trim();
            return trimmed.Length > 0 ? trimmed : FallbackAvatarName;
        }

        /// <summary>
        /// ADVOSC stores this in importKey, so it must come out the same on every export. It depends
        /// only on the avatar's name, and shortening always cuts at the same place.
        /// </summary>
        private static string BundleName(string avatarName) {
            return Shorten(avatarName, MaxNameLength - BundleNameSuffix.Length) + BundleNameSuffix;
        }

        private static string Shorten(string text, int maxLength) {
            if (text.Length <= maxLength) return text;
            var end = char.IsHighSurrogate(text[maxLength - 1]) ? maxLength - 1 : maxLength;
            return text.Substring(0, end);
        }

        /// <summary>DeflateStream writes raw deflate with no zlib header, which is what the contract asks for.</summary>
        private static byte[] Deflate(byte[] data) {
            using (var output = new MemoryStream()) {
                using (var deflate = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal, true)) {
                    deflate.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        private static string ToBase64Url(byte[] data) {
            return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
