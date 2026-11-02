// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.BuildPipeline;

namespace ADVOSC.Triggers.Editor {
    internal sealed class AdvoscBuildException : Exception {
        public AdvoscBuildException(string message) : base(message) { }
    }

    /// <summary>
    /// Runs on the build copy before VRCFury (-10000), so VRCFury merges what this adds.
    /// It writes nothing to disk: no scripts, no refresh, and generated assets stay in memory.
    /// VRCFury reads a Full Controller's objects directly and copies them, so they need no asset file.
    /// </summary>
    internal sealed class AdvoscBuildHook : IVRCSDKPreprocessAvatarCallback {
        private const string DialogTitle = "ADVOSC Triggers";

        public int callbackOrder => -10500;

        private static readonly ITriggerGenerator[] Generators = {
            new SocketGenerator(),
            new PlugGenerator(),
            new TouchZoneGenerator(),
            new ContactGenerator()
        };

        public bool OnPreprocessAvatar(GameObject avatarObject) {
            if (avatarObject == null) return true;
            // A persistent object is a prefab on disk, not a build copy, so it must stay untouched.
            if (EditorUtility.IsPersistent(avatarObject)) return true;

            var triggers = TriggerValidator.FindTriggers(avatarObject);
            if (triggers.Length == 0) return true;

            try {
                var descriptor = avatarObject.GetComponent<VRCAvatarDescriptor>();
                if (descriptor == null) {
                    throw new AdvoscBuildException($"{avatarObject.name} has no VRC Avatar Descriptor on its root.");
                }

                var errors = TriggerValidator.ValidateAvatar(descriptor)
                    .Where(issue => issue.Severity == IssueSeverity.Error)
                    .Select(issue => issue.ToString())
                    .ToList();
                if (errors.Count > 0) {
                    throw new AdvoscBuildException("Fix these triggers, then build again:\n\n" + string.Join("\n\n", errors));
                }

                Generate(avatarObject, descriptor, triggers);
            } catch (Exception e) {
                ReportFailure(e);
                return false;
            }
            return true;
        }

        private static void Generate(GameObject avatarObject, VRCAvatarDescriptor descriptor, AdvoscTrigger[] triggers) {
            var context = new BuildContext(avatarObject, descriptor);
            foreach (var trigger in triggers) {
                FindGenerator(trigger.kind).Generate(trigger, context);
            }

            if (context.HasRoot) {
                ContactMergeGenerator.Generate(context);
                MasterToggleGenerator.Generate(context);
            }

            // If another tool runs the preprocess hooks twice, the second run finds nothing to do.
            foreach (var trigger in triggers) {
                UnityEngine.Object.DestroyImmediate(trigger);
            }
        }

        private static ITriggerGenerator FindGenerator(TriggerKind kind) {
            foreach (var generator in Generators) {
                if (generator.Handles(kind)) return generator;
            }
            throw new AdvoscBuildException($"Nothing can build a trigger of kind {kind}.");
        }

        private static void ReportFailure(Exception e) {
            var message = e is AdvoscBuildException ? e.Message : "Building the triggers failed:\n\n" + e;
            Debug.LogError("[ADVOSC Triggers] " + message);
            if (!Application.isBatchMode) {
                EditorUtility.DisplayDialog(DialogTitle, message, "OK");
            }
        }
    }

    internal sealed class BuildContext {
        public const string RootName = "ADVOSC Triggers";

        public readonly GameObject AvatarObject;
        public readonly VRCAvatarDescriptor Descriptor;
        public readonly List<ContactParameter> ContactParameters = new List<ContactParameter>();

        private GameObject root;

        public BuildContext(GameObject avatarObject, VRCAvatarDescriptor descriptor) {
            AvatarObject = avatarObject;
            Descriptor = descriptor;
        }

        public bool HasRoot => root != null;

        /// <summary>The parent of every generated contact. Made on first use.</summary>
        public GameObject Root {
            get {
                if (root != null) return root;
                root = new GameObject(RootName);
                root.transform.SetParent(AvatarObject.transform, false);
                return root;
            }
        }
    }

    internal struct ContactParameter {
        public string Name;
        public TriggerReceiverType Type;
    }

    internal interface ITriggerGenerator {
        bool Handles(TriggerKind kind);
        void Generate(AdvoscTrigger trigger, BuildContext context);
    }
}
