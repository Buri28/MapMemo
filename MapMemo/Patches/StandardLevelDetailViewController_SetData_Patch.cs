using System.Reflection;
using MapMemo.Models;

namespace MapMemo.Patches
{

    /// <summary>
    /// StandardLevelDetailViewController.SetData のポストフィックスパッチを提供します。
    /// </summary>
    public class StandardLevelDetailViewController_SetData_Patch
    {
        /// <summary>
        /// SetData 呼び出し後に実行されるポストフィックス。
        /// 選択されたレベルから LevelContext を作成して処理を委譲します。
        /// </summary>
        /// <param name="__instance">パッチ対象のインスタンス（StandardLevelDetailViewController）</param>
        /// <param name="beatmapLevel">SetData に渡された BeatmapLevel オブジェクト</param>
        public static void Postfix(object __instance, object beatmapLevel)
        {
            var mapLevel = beatmapLevel as BeatmapLevel;

            LevelContext levelContext = new LevelContext(mapLevel);
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"SetData called with "
                    + $"level: {mapLevel.songName} by {mapLevel.songAuthorName}, ID: {mapLevel.levelID}");

            // 詳細画面のViewを取得
            var field = typeof(StandardLevelDetailViewController)
                .GetField("_standardLevelDetailView", BindingFlags.NonPublic | BindingFlags.Instance);
            var view = field?.GetValue(__instance) as StandardLevelDetailView;

            if (!levelContext.IsValid())
            {
                Plugin.Log?.Warn($"MapMemo: Suppressing SelectionHook due to non-meaningful "
                    + $"key='{levelContext.GetLevelId()}'");
                return;
            }
            SelectionHook.OnSongSelected(view, levelContext).ConfigureAwait(false);
        }

    }
}
