// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace ADVOSC.Triggers.Editor {
    /// <summary>The three export buttons, shared by the inspector and the avatar window.</summary>
    internal static class ExportButtons {
        private const string DialogTitle = "ADVOSC Triggers";
        private const string FileExtension = ".advosc.json";

        public static void Draw(VRCAvatarDescriptor descriptor) {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Open in ADVOSC", "Sends this avatar's rules to ADVOSC for review. They arrive turned off."))) {
                OpenInApp(descriptor);
            }
            if (GUILayout.Button(new GUIContent("Copy link", "Copies a web link you can share."))) {
                CopyLink(descriptor);
            }
            if (GUILayout.Button(new GUIContent("Export .advosc.json", "Saves the rules as a file."))) {
                ExportFile(descriptor);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void OpenInApp(VRCAvatarDescriptor descriptor) {
            var result = ExportForLink(descriptor);
            if (result != null) Application.OpenURL(result.AppLink);
        }

        private static void CopyLink(VRCAvatarDescriptor descriptor) {
            var result = ExportForLink(descriptor);
            if (result == null) return;
            EditorGUIUtility.systemCopyBuffer = result.WebLink;
            Debug.Log("[ADVOSC Triggers] Link copied.");
        }

        private static ExportResult ExportForLink(VRCAvatarDescriptor descriptor) {
            var result = Export(descriptor);
            if (result == null) return null;
            if (!result.FitsInLink) {
                EditorUtility.DisplayDialog(
                    DialogTitle,
                    $"These rules are too big for a link ({result.Payload.Length / 1024} KB, and a link holds " +
                    $"{BundleExporter.MaxLinkPayload / 1024} KB). Use Export .advosc.json and open the file in ADVOSC instead.",
                    "OK"
                );
                return null;
            }
            return result;
        }

        private static void ExportFile(VRCAvatarDescriptor descriptor) {
            var result = Export(descriptor);
            if (result == null) return;

            var path = EditorUtility.SaveFilePanel(
                "Export ADVOSC rules",
                "",
                SafeFileName(descriptor.gameObject.name) + FileExtension,
                "json"
            );
            if (string.IsNullOrEmpty(path)) return;

            if (!path.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase)) {
                path = Path.ChangeExtension(path, null) + FileExtension;
            }
            File.WriteAllText(path, result.FileJson, new UTF8Encoding(false));
            Debug.Log($"[ADVOSC Triggers] Exported {result.RuleCount} rules to {path}");
        }

        private static ExportResult Export(VRCAvatarDescriptor descriptor) {
            var result = BundleExporter.Export(descriptor);
            if (result.Ok) return result;
            EditorUtility.DisplayDialog(DialogTitle, string.Join("\n\n", result.Errors), "OK");
            return null;
        }

        private static string SafeFileName(string name) {
            foreach (var invalid in Path.GetInvalidFileNameChars()) {
                name = name.Replace(invalid, '_');
            }
            return name;
        }
    }
}
