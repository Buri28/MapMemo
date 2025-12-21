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
            var viewControllerType = FindType("StandardLevelDetailViewController");
            var beatmapLevelType = FindType("BeatmapLevel", "DataModels");
            var difficultyMaskType = FindType("BeatmapDifficultyMask");
            var beatmapCharacteristicType = FindType("BeatmapCharacteristicSO", "DataModels");
            if (beatmapCharacteristicType == null)
            {
                MapMemo.Plugin.Log?.Warn("ApplyPatches: BeatmapCharacteristicSO not found");
                return;
            }
            var characteristicArrayType = beatmapCharacteristicType.MakeArrayType();
            MapMemo.Plugin.Log?.Info($"ApplyPatches: Created BeatmapCharacteristicSO[] type: {characteristicArrayType.FullName}");

            if (viewControllerType == null || beatmapLevelType == null || difficultyMaskType == null || characteristicArrayType == null)
            {
                MapMemo.Plugin.Log?.Warn("ApplyPatches: type not found");
                return;
            }

            foreach (var method in viewControllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (method.Name == "SetData")
                {
                    //1.public void SetData(BeatmapLevel beatmapLevel, bool hidePracticeButton, string playButtonText, BeatmapDifficultyMask allowedBeatmapDifficultyMask, BeatmapCharacteristicSO[] notAllowedCharacteristics) 
                    //2.public void SetData(BeatmapLevelPack pack, BeatmapLevel beatmapLevel, bool hidePracticeButton, bool canBuyPack, string playButtonText, BeatmapDifficultyMask allowedBeatmapDifficultyMask, BeatmapCharacteristicSO[] notAllowedCharacteristics)
                    // 1から2が呼ばれるため、2のみパッチを当てる　1.39.1では1は呼ばれない
                    var parameters = method.GetParameters();
                    if (parameters.Length == 7 &&
                        parameters[0].ParameterType.Name == "BeatmapLevelPack" &&
                        parameters[1].ParameterType.Name == "BeatmapLevel")
                    {
                        MapMemo.Plugin.Log?.Info($"Patching SetData overload: {method}");

                        var postfix = new HarmonyMethod(typeof(StandardLevelDetailViewController_SetData_Patch)
                            .GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public));

                        harmony.Patch(method, postfix);
                    }
                }
            }
            MapMemo.Plugin.Log?.Info("ApplyPatches: end");
        }

        /// <summary>
        /// 指定した型名を、可能であれば指定アセンブリ内から検索して返します。
        /// AppDomain のすべてのアセンブリを走査し、最初に見つかった型を返します。
        /// </summary>
        /// <param name="typeName">検索する型名</param>
        /// <param name="preferredAssembly">優先して検索するアセンブリ名（省略可）</param>
        /// <returns>見つかった Type、見つからなければ null</returns>
        private static Type FindType(string typeName, string preferredAssembly = null)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (preferredAssembly != null && !asm.GetName().Name.Equals(preferredAssembly))
                    continue;

                var type = asm.GetTypes().FirstOrDefault(t => t.Name == typeName);
                if (type != null)
                {
                    MapMemo.Plugin.Log?.Info($"Found type '{typeName}' in assembly: {asm.FullName}");
                    return type;
                }
            }
            MapMemo.Plugin.Log?.Warn($"Type '{typeName}' not found");
            return null;
        }
    }
}
