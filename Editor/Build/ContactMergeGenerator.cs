// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using com.vrcfury.api;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ADVOSC.Triggers.Editor {
    /// <summary>
    /// Declares contact parameters in expression parameters as unsynced and unsaved. They cost no
    /// network bits, and this way nothing depends on animator-only parameters reaching OSC.
    /// Both generated objects stay in memory. VRCFury clones them, so no file is left in Assets.
    /// </summary>
    internal static class ContactMergeGenerator {
        public static void Generate(BuildContext context) {
            var fullController = FuryComponents.CreateFullController(context.Root);
            fullController.AddController(CreateController(context.ContactParameters));
            fullController.AddParams(CreateParameters(context.ContactParameters));
            foreach (var parameter in context.ContactParameters) {
                // Global, so VRCFury keeps ADVOSC/<id> as it is and doesn't add a prefix.
                fullController.AddGlobalParam(parameter.Name);
            }
        }

        private static AnimatorController CreateController(List<ContactParameter> parameters) {
            var entries = new List<AnimatorControllerParameter>();
            foreach (var parameter in parameters) {
                entries.Add(new AnimatorControllerParameter {
                    name = parameter.Name,
                    type = parameter.Type == TriggerReceiverType.Constant
                        ? AnimatorControllerParameterType.Bool
                        : AnimatorControllerParameterType.Float
                });
            }
            return new AnimatorController {
                name = "ADVOSC Triggers",
                parameters = entries.ToArray()
            };
        }

        private static VRCExpressionParameters CreateParameters(List<ContactParameter> parameters) {
            var entries = new List<VRCExpressionParameters.Parameter>();
            foreach (var parameter in parameters) {
                entries.Add(new VRCExpressionParameters.Parameter {
                    name = parameter.Name,
                    valueType = parameter.Type == TriggerReceiverType.Constant
                        ? VRCExpressionParameters.ValueType.Bool
                        : VRCExpressionParameters.ValueType.Float,
                    defaultValue = 0f,
                    saved = false,
                    networkSynced = false
                });
            }
            var asset = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            asset.name = "ADVOSC Triggers";
            asset.parameters = entries.ToArray();
            return asset;
        }
    }
}
