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


            string songName = mapLevel.songName;
            string songAuthor = mapLevel.songAuthorName;
            string levelId = mapLevel.levelID;
            Plugin.Log?.Info($"SetData called with level: {mapLevel.songName} by {mapLevel.songAuthorName}, ID: {mapLevel.levelID}");

            // 詳細画面のViewを取得
            var field = typeof(StandardLevelDetailViewController)
                .GetField("_standardLevelDetailView", BindingFlags.NonPublic | BindingFlags.Instance);
            var view = field?.GetValue(__instance) as StandardLevelDetailView;

            if (!string.IsNullOrEmpty(songName) || !string.IsNullOrEmpty(songAuthor) || !string.IsNullOrEmpty(levelId))
            {
                SelectedLevelState.Update(
                    NormalizeUnknown(songName), NormalizeUnknown(songAuthor), NormalizeUnknown(levelId));
            }
            Plugin.Log?.Info($"MapMemo: Resolved song info name='{songName}' author='{songAuthor}' levelId='{levelId}'");
            string key = NormalizeUnknown(levelId);

            if (key.Equals("unknown", StringComparison.OrdinalIgnoreCase) || key.Equals("unknown|unknown", StringComparison.OrdinalIgnoreCase))
            {
                Plugin.Log?.Warn($"MapMemo: Suppressing SelectionHook due to non-meaningful key='{key}'");
                return;
            }
            SelectionHook.OnSongSelected(view, key, NormalizeUnknown(songName), NormalizeUnknown(songAuthor)).ConfigureAwait(false);
        }

        private static string NormalizeUnknown(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "unknown";
            var trimmed = s.Trim();
            if (trimmed.Equals("unknown", StringComparison.OrdinalIgnoreCase)) return "unknown";
            if (trimmed.Equals("!Not Defined!", StringComparison.OrdinalIgnoreCase)) return "unknown";
            return trimmed;
        }
    }
}
