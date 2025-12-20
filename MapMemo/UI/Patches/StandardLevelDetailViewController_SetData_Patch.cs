using HarmonyLib;
using MapMemo.Core;
using MapMemo.Patches;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MapMemo.UI.Patches
{

    public class StandardLevelDetailViewController_SetData_Patch
    {
        /// <summary>
        /// SetData メソッドのポストフィックス
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="beatmapLevel"></param>
        public static void Postfix(object __instance, object beatmapLevel)
        {
            //var viewController = __instance as StandardLevelDetailViewController;
            var mapLevel = beatmapLevel as BeatmapLevel;

            LevelContext levelContext = new LevelContext(mapLevel);
            Plugin.Log?.Info($"SetData called with level: {mapLevel.songName} by {mapLevel.songAuthorName}, ID: {mapLevel.levelID}");

            // 詳細画面のViewを取得
            var field = typeof(StandardLevelDetailViewController)
                .GetField("_standardLevelDetailView", BindingFlags.NonPublic | BindingFlags.Instance);
            var view = field?.GetValue(__instance) as StandardLevelDetailView;

            if (!levelContext.IsValid())
            {
                Plugin.Log?.Warn($"MapMemo: Suppressing SelectionHook due to non-meaningful key='{levelContext.GetLevelId()}'");
                return;
            }
            SelectionHook.OnSongSelected(view, levelContext).ConfigureAwait(false);
        }

    }
}
