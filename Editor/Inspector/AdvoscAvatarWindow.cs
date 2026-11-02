// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace ADVOSC.Triggers.Editor {
    /// <summary>One place to see every trigger on an avatar and export them together.</summary>
    internal sealed class AdvoscAvatarWindow : EditorWindow {
        [SerializeField] private VRCAvatarDescriptor avatar;
        private Vector2 scroll;

        // Components can't survive a domain reload here, so a reload just means scanning again.
        private List<Component> scanned;
        private VRCAvatarDescriptor scannedAvatar;

        [MenuItem("Tools/ADVOSC Triggers")]
        private static void Open() {
            GetWindow<AdvoscAvatarWindow>("ADVOSC Triggers").Show();
        }

        private void OnEnable() {
            PickFromSelection();
        }

        private void OnSelectionChange() {
            PickFromSelection();
            Repaint();
        }

        private void PickFromSelection() {
            var selected = Selection.activeGameObject;
            if (selected == null) return;
            var found = selected.GetComponentInParent<VRCAvatarDescriptor>(true);
            if (found != null) avatar = found;
        }

        private void OnGUI() {
            avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(new GUIContent("Avatar", TriggerHelp.WindowAvatar), avatar, typeof(VRCAvatarDescriptor), true);
            if (avatar == null) {
                EditorGUILayout.HelpBox("Pick an avatar in the scene.", MessageType.Info);
                return;
            }

            var triggers = TriggerValidator.FindTriggers(avatar.gameObject);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawScan();

            if (triggers.Length == 0) {
                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox("This avatar has no triggers yet. Add an ADVOSC Trigger component to an object on it, or scan for what it already has.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            var issues = TriggerValidator.ValidateAvatar(avatar);
            foreach (var trigger in triggers) {
                DrawTrigger(trigger);
                foreach (var issue in issues) {
                    if (issue.Trigger != trigger) continue;
                    EditorGUILayout.HelpBox(issue.Message, issue.Severity == IssueSeverity.Error ? MessageType.Error : MessageType.Warning);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            ExportButtons.Draw(avatar);
        }

        private void DrawScan() {
            if (GUILayout.Button(new GUIContent("Scan avatar for existing contacts", TriggerHelp.Scan))) {
                scanned = ExistingTargets.Scan(avatar);
                scannedAvatar = avatar;
            }

            if (scanned == null || scannedAvatar != avatar) return;
            scanned.RemoveAll(component => component == null);

            if (scanned.Count == 0) {
                EditorGUILayout.HelpBox("Nothing new found on this avatar.", MessageType.Info);
                return;
            }

            Component added = null;
            foreach (var component in scanned) {
                EditorGUILayout.BeginHorizontal();
                var name = ExistingTargets.ReadName(component);
                var label = string.IsNullOrWhiteSpace(name) ? component.gameObject.name + " (no name set)" : name;
                EditorGUILayout.LabelField(new GUIContent(label, label), new GUIContent(ExistingTargets.Label(ExistingTargets.TypeOf(component))));
                if (GUILayout.Button(new GUIContent("Add", TriggerHelp.ScanAdd), GUILayout.Width(60f))) {
                    added = component;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (added != null) {
                ExistingTargets.Add(added, avatar);
                scanned.Remove(added);
            }

            if (GUILayout.Button(new GUIContent($"Add all {scanned.Count}", TriggerHelp.ScanAddAll))) {
                Undo.SetCurrentGroupName("Add ADVOSC Triggers");
                var group = Undo.GetCurrentGroup();
                foreach (var component in scanned) {
                    ExistingTargets.Add(component, avatar);
                }
                Undo.CollapseUndoOperations(group);
                scanned.Clear();
            }
        }

        private static void DrawTrigger(AdvoscTrigger trigger) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(TriggerNames.Display(trigger), ObjectNames.NicifyVariableName(trigger.kind.ToString()));
            if (GUILayout.Button(new GUIContent("Select", TriggerHelp.SelectButton), GUILayout.Width(60f))) {
                Selection.activeGameObject = trigger.gameObject;
                EditorGUIUtility.PingObject(trigger.gameObject);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
