using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using io.github.ykysnk.utils;
using io.github.ykysnk.utils.Editor.Patches;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace io.github.ykysnk.VRCFuryDisabler.Editor.Patches
{
    internal class VRCFuryBuilderPatch : Patch<VRCFuryBuilderPatch>
    {
        private static readonly Type PatchType = ThisType;
        private static readonly Type VRCFuryBuilderType = AccessTools.TypeByName("VF.Builder.VRCFuryBuilder");
        private static readonly Type VRCFProgressWindowType = AccessTools.TypeByName("VF.VRCFProgressWindow");
        private static readonly Type EditorWindowType = typeof(EditorWindow);

        private static readonly MethodInfo CreateMethod = AccessTools.Method(VRCFProgressWindowType, "Create");

        protected override void Execute(Harmony harmony)
        {
        }

        private static void ProgressReplace(object _, float current, string info)
        {
            var progressValue = Mathf.Clamp01(current);
            var percent = Math.Round(progressValue * 100);
            Utils.Log("VRCFury", $"Progress ({percent}%): {info}");
            EditorUtility.DisplayProgressBar($"VRCFury Building... {percent}%", info, progressValue);
        }

        private static void CloseReplace(object _)
        {
            EditorUtility.ClearProgressBar();
        }

        [UsedImplicitly]
        private class Run : PatchMethod<Run>
        {
            private static readonly MethodInfo CloseMethod =
                AccessTools.Method(EditorWindowType, nameof(EditorWindow.Close));

            private static readonly MethodInfo CloseReplaceMethod = AccessTools.Method(PatchType, nameof(CloseReplace));
            public override MethodInfo TargetMethod { get; } = AccessTools.Method(VRCFuryBuilderType, nameof(Run));
            public override string TranspilerMethod => nameof(Transpiler);

            protected override bool Prepare(MethodInfo? original, Harmony harmony)
            {
#if VRCF_DISABLER_AVATARS
                return true;
#else
                return false;
#endif
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var code in instructions)
                {
                    if (code.opcode == OpCodes.Call && (MethodInfo?)code.operand == CreateMethod)
                    {
                        code.opcode = OpCodes.Ldnull;
                        code.operand = null;
                    }
                    else if (code.opcode == OpCodes.Callvirt && (MethodInfo?)code.operand == CloseMethod)
                    {
                        code.opcode = OpCodes.Call;
                        code.operand = CloseReplaceMethod;
                    }

                    yield return code;
                }
            }
        }

        [UsedImplicitly]
        private class ApplyFuryConfigs : PatchMethod<ApplyFuryConfigs>
        {
            private static readonly MethodInfo ProgressMethod = AccessTools.Method(VRCFProgressWindowType, "Progress");

            private static readonly MethodInfo ProgressReplaceMethod =
                AccessTools.Method(PatchType, nameof(ProgressReplace));

            public override MethodInfo TargetMethod { get; } =
                AccessTools.Method(VRCFuryBuilderType, nameof(ApplyFuryConfigs));

            public override string TranspilerMethod => nameof(Transpiler);

            protected override bool Prepare(MethodInfo? original, Harmony harmony)
            {
#if VRCF_DISABLER_AVATARS
                return true;
#else
                return false;
#endif
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var code in instructions)
                {
                    if (code.opcode == OpCodes.Callvirt && (MethodInfo?)code.operand == ProgressMethod)
                    {
                        code.opcode = OpCodes.Call;
                        code.operand = ProgressReplaceMethod;
                    }

                    yield return code;
                }
            }
        }
    }
}