// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ADVOSC.Triggers.Editor {
    /// <summary>
    /// VRCFury has no public API for plugs and Touch Zones, and none for a socket's OSC id. Those
    /// components are found by name and their serialized fields set with SerializedObject, which is
    /// what VRCFury's own inspector does. Nothing here patches VRCFury or touches its non-serialized code.
    /// </summary>
    internal static class VrcFuryCompat {
        private const string SocketTypeName = "VF.Component.VRCFuryHapticSocket";
        private const string PlugTypeName = "VF.Component.VRCFuryHapticPlug";
        private const string TouchZoneTypeName = "VF.Component.VRCFuryHapticTouchReceiver";

        public const string FieldOscId = "oscId";
        public const string FieldName = "name";
        public const string FieldRadius = "radius";
        public const string FieldAutoRenderer = "autoRenderer";
        public const string FieldRenderers = "configureTpsMesh";

        private static readonly Dictionary<TriggerKind, string> ProblemCache = new Dictionary<TriggerKind, string>();

        public static Type SocketType => FindType(SocketTypeName);
        public static Type PlugType => FindType(PlugTypeName);
        public static Type TouchZoneType => FindType(TouchZoneTypeName);

        /// <summary>Why this kind can't be built with the installed VRCFury, or null if it can.</summary>
        public static string GetProblem(TriggerKind kind) {
            if (ProblemCache.TryGetValue(kind, out var cached)) return cached;
            var problem = FindProblem(kind);
            ProblemCache[kind] = problem;
            return problem;
        }

        private static string FindProblem(TriggerKind kind) {
            switch (kind) {
                case TriggerKind.SpsSocket:
                    return CheckFields(SocketType, SocketTypeName, FieldOscId);
                case TriggerKind.SpsPlug:
                    return CheckFields(PlugType, PlugTypeName, FieldName, FieldAutoRenderer, FieldRenderers);
                case TriggerKind.TouchZone:
                    return CheckFields(TouchZoneType, TouchZoneTypeName, FieldName, FieldRadius);
                default:
                    return null;
            }
        }

        private static Type FindType(string fullName) {
            var direct = Type.GetType(fullName + ", VRCFury");
            if (direct != null) return direct;
            foreach (var candidate in TypeCache.GetTypesDerivedFrom<MonoBehaviour>()) {
                if (candidate.FullName == fullName) return candidate;
            }
            return null;
        }

        /// <summary>Probes a hidden temporary component, because fields can only be read from an instance.</summary>
        private static string CheckFields(Type type, string typeName, params string[] fields) {
            if (type == null) {
                return $"VRCFury's {ShortName(typeName)} component was not found. Check that VRCFury is installed and up to date.";
            }

            var probe = EditorUtility.CreateGameObjectWithHideFlags("ADVOSC Triggers probe", HideFlags.HideAndDontSave);
            try {
                var component = probe.AddComponent(type);
                if (component == null) {
                    return $"VRCFury's {ShortName(typeName)} component could not be added.";
                }
                using (var serialized = new SerializedObject(component)) {
                    foreach (var field in fields) {
                        if (serialized.FindProperty(field) == null) {
                            return MissingFieldMessage(typeName, field);
                        }
                    }
                }
            } finally {
                UnityEngine.Object.DestroyImmediate(probe);
            }
            return null;
        }

        public static string MissingFieldMessage(string componentName, string field) {
            return $"VRCFury's {ShortName(componentName)} has no usable '{field}' field. " +
                   "This VRCFury version doesn't match what ADVOSC Triggers expects, so update ADVOSC Triggers.";
        }

        private static string ShortName(string typeName) {
            var dot = typeName.LastIndexOf('.');
            return dot >= 0 ? typeName.Substring(dot + 1) : typeName;
        }
    }

    /// <summary>Sets serialized fields, and throws rather than building something half configured.</summary>
    internal sealed class FuryFieldWriter : IDisposable {
        private readonly SerializedObject serialized;
        private readonly string componentName;

        public FuryFieldWriter(Component component) {
            serialized = new SerializedObject(component);
            componentName = component.GetType().Name;
        }

        public void SetString(string field, string value) {
            Find(field, SerializedPropertyType.String).stringValue = value;
        }

        public void SetBool(string field, bool value) {
            Find(field, SerializedPropertyType.Boolean).boolValue = value;
        }

        public void SetFloat(string field, float value) {
            Find(field, SerializedPropertyType.Float).floatValue = value;
        }

        public void SetObjectList(string field, params UnityEngine.Object[] values) {
            var list = serialized.FindProperty(field);
            if (list == null || !list.isArray) {
                throw new AdvoscBuildException(VrcFuryCompat.MissingFieldMessage(componentName, field));
            }
            list.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) {
                var element = list.GetArrayElementAtIndex(i);
                if (element.propertyType != SerializedPropertyType.ObjectReference) {
                    throw new AdvoscBuildException(VrcFuryCompat.MissingFieldMessage(componentName, field));
                }
                element.objectReferenceValue = values[i];
            }
        }

        private SerializedProperty Find(string field, SerializedPropertyType type) {
            var property = serialized.FindProperty(field);
            if (property == null || property.propertyType != type) {
                throw new AdvoscBuildException(VrcFuryCompat.MissingFieldMessage(componentName, field));
            }
            return property;
        }

        public void Apply() {
            // The build copy is thrown away after the build, so this should leave no undo step behind.
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Dispose() {
            serialized.Dispose();
        }
    }
}
