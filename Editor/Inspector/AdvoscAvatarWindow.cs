// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace ADVOSC.Triggers.Editor {
    /// <summary>One place to see every trigger on an avatar and export them together.</summary>
    internal sealed class AdvoscAvatarWindow : EditorWindow {
        [SerializeField] private VRCAvatarDescriptor avatar;
        private Vector2 scroll;

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
            avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true);
            if (avatar == null) {
                EditorGUILayout.HelpBox("Pick an avatar in the scene.", MessageType.Info);
                return;
            }

            var triggers = TriggerValidator.FindTriggers(avatar.gameObject);
            if (triggers.Length == 0) {
                EditorGUILayout.HelpBox("This avatar has no triggers yet. Add an ADVOSC Trigger component to an object on it.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            scroll = EditorGUILayout.BeginScrollView(scroll);
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

        private static void DrawTrigger(AdvoscTrigger trigger) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(TriggerNames.Display(trigger), ObjectNames.NicifyVariableName(trigger.kind.ToString()));
            if (GUILayout.Button("Select", GUILayout.Width(60f))) {
                Selection.activeGameObject = trigger.gameObject;
                EditorGUIUtility.PingObject(trigger.gameObject);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
