using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using io.github.ykysnk.utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace io.github.ykysnk.VRCFuryDisabler.Editor
{
    [InitializeOnLoad]
    internal static class VRCFuryPatches
    {
        private const string PatchId = "io.github.ykysnk.vrcfury-disabler.patches";
        private static readonly Type ThisType = typeof(VRCFuryPatches);
        private static readonly Type VRCFProgressWindowType = AccessTools.TypeByName("VF.VRCFProgressWindow");
        private static readonly Type VRCFuryBuilderType = AccessTools.TypeByName("VF.Builder.VRCFuryBuilder");
        private static readonly Type EditorWindowType = typeof(EditorWindow);

        private static readonly MethodInfo CreateMethod = AccessTools.Method(VRCFProgressWindowType, "Create");

        static VRCFuryPatches()
        {
            var harmony = new Harmony(PatchId);

            harmony.PatchAll(ThisType.Assembly);

            AssemblyReloadEvents.beforeAssemblyReload += () => harmony.UnpatchAll(PatchId);
            Utils.Log(nameof(VRCFuryPatches), "VRCFury Disabler Patches Initialized");
        }

        private static void ProgressReplace(object _, float current, string info)
        {
            var progressValue = Mathf.Clamp01(current);
            var percent = Math.Round(progressValue * 100);
            Utils.Log(nameof(VRCFuryPatches), $"Progress ({percent}%): {info}");
            EditorUtility.DisplayProgressBar($"VRCFury Building... {percent}%", info, progressValue);
        }

        private static void CloseReplace(object _)
        {
            EditorUtility.ClearProgressBar();
        }

        [HarmonyPatch]
        [PublicAPI]
        public static class VRCFuryBuilderRun
        {
            private static readonly MethodInfo Method = AccessTools.Method(VRCFuryBuilderType, "Run");

            private static readonly MethodInfo CloseMethod =
                AccessTools.Method(EditorWindowType, nameof(EditorWindow.Close));

            private static readonly MethodInfo CloseReplaceMethod = AccessTools.Method(ThisType, nameof(CloseReplace));

            private static MethodBase TargetMethod() => Method;

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var code in instructions)
                {
                    if (code.opcode == OpCodes.Call && (MethodInfo)code.operand == CreateMethod)
                    {
                        code.opcode = OpCodes.Ldnull;
                        code.operand = null;
                    }
                    else if (code.opcode == OpCodes.Callvirt && (MethodInfo)code.operand == CloseMethod)
                    {
                        code.opcode = OpCodes.Call;
                        code.operand = CloseReplaceMethod;
                    }

                    yield return code;
                }
            }
        }

        [HarmonyPatch]
        [PublicAPI]
        public static class VRCFuryBuilderApplyFuryConfigs
        {
            private static readonly MethodInfo Method = AccessTools.Method(VRCFuryBuilderType, "ApplyFuryConfigs");
            private static readonly MethodInfo ProgressMethod = AccessTools.Method(VRCFProgressWindowType, "Progress");

            private static readonly MethodInfo ProgressReplaceMethod =
                AccessTools.Method(ThisType, nameof(ProgressReplace));

            private static MethodBase TargetMethod() => Method;

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var code in instructions)
                {
                    if (code.opcode == OpCodes.Callvirt && (MethodInfo)code.operand == ProgressMethod)
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