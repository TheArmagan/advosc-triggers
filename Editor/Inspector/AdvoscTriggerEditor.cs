// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace ADVOSC.Triggers.Editor {
    [CustomEditor(typeof(AdvoscTrigger))]
    [CanEditMultipleObjects]
    internal sealed class AdvoscTriggerEditor : UnityEditor.Editor {
        private static readonly GUIContent[] KindLabels = {
            new GUIContent("SPS Socket"),
            new GUIContent("SPS Plug"),
            new GUIContent("Touch Zone"),
            new GUIContent("Contact Preset"),
            new GUIContent("Custom Contact")
        };

        private static readonly GUIContent[] WhoLabels = {
            new GUIContent("Only me"),
            new GUIContent("Other people"),
            new GUIContent("Both")
        };

        private static readonly GUIContent[] SocketModeLabels = {
            new GUIContent("Auto"),
            new GUIContent("Hole"),
            new GUIContent("Ring"),
            new GUIContent("One-way ring"),
            new GUIContent("No light")
        };

        private static readonly GUIContent[] StarterLabels = {
            new GUIContent("Empty rule"),
            new GUIContent("Toy"),
            new GUIContent("Variable"),
            new GUIContent("Sound (pick it in ADVOSC)")
        };

        private static readonly GUIContent[] PresetLabels = Array.ConvertAll(
            ContactPresets.All,
            preset => new GUIContent(preset.Label)
        );

        private SerializedProperty kind;
        private SerializedProperty id;
        private SerializedProperty displayName;
        private SerializedProperty who;
        private SerializedProperty starterRule;
        private SerializedProperty socketMode;
        private SerializedProperty plugRenderer;
        private SerializedProperty preset;
        private SerializedProperty presetSide;
        private SerializedProperty presetRadiusScale;
        private SerializedProperty presetOffset;
        private SerializedProperty manualTarget;
        private SerializedProperty customShape;
        private SerializedProperty customRadius;
        private SerializedProperty customHeight;
        private SerializedProperty customReceiverType;
        private SerializedProperty customTags;

        private void OnEnable() {
            kind = serializedObject.FindProperty(nameof(AdvoscTrigger.kind));
            id = serializedObject.FindProperty(nameof(AdvoscTrigger.id));
            displayName = serializedObject.FindProperty(nameof(AdvoscTrigger.displayName));
            who = serializedObject.FindProperty(nameof(AdvoscTrigger.who));
            starterRule = serializedObject.FindProperty(nameof(AdvoscTrigger.starterRule));
            socketMode = serializedObject.FindProperty(nameof(AdvoscTrigger.socketMode));
            plugRenderer = serializedObject.FindProperty(nameof(AdvoscTrigger.plugRenderer));
            preset = serializedObject.FindProperty(nameof(AdvoscTrigger.preset));
            presetSide = serializedObject.FindProperty(nameof(AdvoscTrigger.presetSide));
            presetRadiusScale = serializedObject.FindProperty(nameof(AdvoscTrigger.presetRadiusScale));
            presetOffset = serializedObject.FindProperty(nameof(AdvoscTrigger.presetOffset));
            manualTarget = serializedObject.FindProperty(nameof(AdvoscTrigger.manualTarget));
            customShape = serializedObject.FindProperty(nameof(AdvoscTrigger.customShape));
            customRadius = serializedObject.FindProperty(nameof(AdvoscTrigger.customRadius));
            customHeight = serializedObject.FindProperty(nameof(AdvoscTrigger.customHeight));
            customReceiverType = serializedObject.FindProperty(nameof(AdvoscTrigger.customReceiverType));
            customTags = serializedObject.FindProperty(nameof(AdvoscTrigger.customTags));
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            Popup(kind, new GUIContent("Type", TriggerHelp.Type), KindLabels);
            EditorGUILayout.PropertyField(id, new GUIContent("ID", TriggerHelp.Id));
            EditorGUILayout.PropertyField(displayName, new GUIContent("Display name", TriggerHelp.DisplayName));
            Popup(who, new GUIContent("Who can trigger it", TriggerHelp.Who), WhoLabels);

            EditorGUILayout.Space();
            if (kind.hasMultipleDifferentValues) {
                EditorGUILayout.HelpBox("Pick objects of the same type to edit their other settings.", MessageType.Info);
            } else {
                DrawKindSettings((TriggerKind)kind.enumValueIndex);
            }

            EditorGUILayout.Space();
            Popup(starterRule, new GUIContent("Starter rule", TriggerHelp.StarterRule), StarterLabels);

            serializedObject.ApplyModifiedProperties();

            if (targets.Length == 1) {
                DrawSingleTargetFooter((AdvoscTrigger)target);
            }
        }

        private void DrawKindSettings(TriggerKind selected) {
            switch (selected) {
                case TriggerKind.SpsSocket:
                    Popup(socketMode, new GUIContent("Socket type", TriggerHelp.SocketMode), SocketModeLabels);
                    EditorGUILayout.HelpBox("The socket sits at this object and points along its blue axis. VRCFury adds its own menu toggle for it.", MessageType.None);
                    break;

                case TriggerKind.SpsPlug:
                    EditorGUILayout.PropertyField(plugRenderer, new GUIContent("Plug mesh", TriggerHelp.PlugMesh));
                    break;

                case TriggerKind.TouchZone:
                    EditorGUILayout.PropertyField(customRadius, new GUIContent("Radius (m)", TriggerHelp.ZoneRadius));
                    break;

                case TriggerKind.ContactPreset:
                    DrawPresetSettings();
                    break;

                case TriggerKind.CustomContact:
                    DrawCustomContactSettings();
                    break;
            }
        }

        private void DrawPresetSettings() {
            var current = Array.FindIndex(ContactPresets.All, entry => (int)entry.Id == preset.enumValueIndex);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = preset.hasMultipleDifferentValues;
            var label = new GUIContent("Preset", TriggerHelp.Preset);
            var picked = EditorGUILayout.Popup(label, Math.Max(current, 0), PresetLabels);
            EditorGUI.showMixedValue = false;
            HoverWholeRow(label.tooltip);
            if (EditorGUI.EndChangeCheck()) {
                preset.enumValueIndex = (int)ContactPresets.All[picked].Id;
            }

            if (ContactPresets.Get((ContactPresetId)preset.enumValueIndex).HasSides) {
                EditorGUILayout.PropertyField(presetSide, new GUIContent("Side", TriggerHelp.Side));
            }
            EditorGUILayout.PropertyField(presetRadiusScale, new GUIContent("Size", TriggerHelp.RadiusScale));
            EditorGUILayout.PropertyField(presetOffset, new GUIContent("Move by (m)", TriggerHelp.Offset));
            EditorGUILayout.PropertyField(manualTarget, new GUIContent("Manual target", TriggerHelp.ManualTarget));
        }

        private void DrawCustomContactSettings() {
            EditorGUILayout.PropertyField(customShape, new GUIContent("Shape", TriggerHelp.CustomShape));
            EditorGUILayout.PropertyField(customRadius, new GUIContent("Radius (m)", TriggerHelp.CustomRadius));
            if (customShape.enumValueIndex == (int)TriggerShape.Capsule) {
                EditorGUILayout.PropertyField(customHeight, new GUIContent("Height (m)", TriggerHelp.CustomHeight));
            }
            EditorGUILayout.PropertyField(customReceiverType, new GUIContent("Reports", TriggerHelp.CustomReports));
            EditorGUILayout.PropertyField(customTags, new GUIContent("Touched by", TriggerHelp.CustomTags), true);
            EditorGUILayout.HelpBox("Standard tags are Head, Torso, Hand, Foot and Finger.", MessageType.None);
        }

        private static void DrawSingleTargetFooter(AdvoscTrigger trigger) {
            var issues = TriggerValidator.ValidateOne(trigger);
            if (issues.Count > 0) EditorGUILayout.Space();
            foreach (var issue in issues) {
                var type = issue.Severity == IssueSeverity.Error ? MessageType.Error : MessageType.Warning;
                EditorGUILayout.HelpBox(issue.Message, type);
            }

            if (trigger.kind == TriggerKind.ContactPreset && ContactPlan.TryCreate(trigger, out var plan, out _)) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(new GUIContent("Attached to", TriggerHelp.AttachedTo), new GUIContent(plan.AnchorDescription, TriggerHelp.AttachedTo));
            }

            var descriptor = trigger.GetComponentInParent<VRCAvatarDescriptor>(true);
            if (descriptor == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("All triggers on " + descriptor.gameObject.name, EditorStyles.boldLabel);
            ExportButtons.Draw(descriptor);
        }

        private static void Popup(SerializedProperty property, GUIContent label, GUIContent[] options) {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            var picked = EditorGUILayout.Popup(label, property.enumValueIndex, options);
            EditorGUI.showMixedValue = false;
            HoverWholeRow(label.tooltip);
            if (EditorGUI.EndChangeCheck()) {
                property.enumValueIndex = picked;
            }
        }

        /// <summary>A dropdown only shows its tooltip over the label, so this covers the field too.</summary>
        private static void HoverWholeRow(string tooltip) {
            GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent(string.Empty, tooltip));
        }
    }
}
