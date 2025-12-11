using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;

namespace MapMemo.UI.Patches
{
    /// <summary>
    /// Harmonyパッチ適用ユーティリティ
    /// </summary>
    public static class MapMemoPatcher
    {
        /// <summary>
        /// Harmonyパッチを適用します
        /// </summary>
        public static void ApplyPatches(Harmony harmony)
        {
            MapMemo.Plugin.Log?.Info("HarmonyPatches: TryApplyPatches start");
            Type viewControllerType = typeof(StandardLevelDetailViewController);

            Plugin.Log?.Info("HarmonyPatches: Found StandardLevelDetailViewController type");
            var methodInfo = viewControllerType.GetMethod(
               "SetData",
               BindingFlags.Instance | BindingFlags.Public,
               null,
               new[] {
                    typeof(BeatmapLevel),
                    typeof(bool),
                    typeof(string),
                    typeof(BeatmapDifficultyMask),
                    typeof(BeatmapCharacteristicSO[])
               },
               null
           );
            if (methodInfo == null)
            {
                MapMemo.Plugin.Log?.Warn("HarmonyPatches: SetData method not found");
                return;
            }
            Plugin.Log?.Info("HarmonyPatches: Found SetData method");
            var postfix = new HarmonyMethod(
                typeof(StandardLevelDetailViewController_SetData_Patch)
                .GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public));
            harmony.Patch(methodInfo, postfix);
            Plugin.Log?.Info("HarmonyPatches: TryApplyPatches end");
        }
    }
}
