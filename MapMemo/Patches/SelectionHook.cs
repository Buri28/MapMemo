using System.Threading.Tasks;
using MapMemo.Domain;
using MapMemo.Models;
using MapMemo.Services;
using MapMemo.UI.Menu;
using UnityEngine;

namespace MapMemo.Patches
{
    /// <summary>
    /// 選曲イベントなどから呼び出されるフックの土台ユーティリティ。
    /// </summary>
    // 実環境ではLevelSelection/DetailViewのイベントから呼び出す
    public static class SelectionHook
    {
        //private static MemoPanelController currentPanel;
        /// <summary>
        /// 曲が選択されたときに呼ばれるハンドラ。MemoPanelController を取得して表示を更新します。
        /// </summary>
        public static async Task OnSongSelected(
            MonoBehaviour view, LevelContext levelContext)
        {
            // viewはStandardLevelDetailViewを想定
            if (Plugin.VerboseLogs) Plugin.Log?.Info(
                    $"SelectionHook: OnSongSelected "
                    + $"parent='{view?.name}' key='{levelContext.GetLevelId()}' \n"
                    + $"song='{levelContext.GetSongName()}' author='{levelContext.GetSongAuthor()}'"
                    + $" levelAuthor='{levelContext.GetLevelAuthor()}'");
            if (view == null) return;
            // インスタンス取得タイミングはUpdateBeatSaverDataAsyncより前
            var ctrl = MemoPanelController.GetInstance(view, levelContext);
            if (MemoSettingsManager.Instance.BeatSaverAccessMode == "Auto")
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info("SelectionHook: BeatSaverAccessMode is 'Auto', fetching BeatSaver data.");
                // BeatSaverからデータを取得してMemoPanelを更新
                var levelId = levelContext.GetLevelId();
                var levelHash = Utilities.BeatSaberUtils.GetLevelHash(levelId);
                // BeatSaverからデータを取得してMemoPanelを更新
                MemoService.Instance.UpdateBeatSaverDataAsync(levelHash, map =>
                {
                    if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal.InitializeParameters: "
                    + $"Using cached BeatSaver map info: id='{map.id}' for hash '{levelHash}'");
                    var ctrl = MemoPanelController.GetInstance(view, levelContext);
                    ctrl.Refresh();
                },
                error =>
                {
                    Plugin.Log?.Warn("Failed to fetch BeatSaver data: " + error);
                });
            }
            else
            {
                await ctrl.Refresh();
            }
        }

        /// <summary>
        /// 文字列が有意な値かどうかを判定します（unknown やプレースホルダを除外）。
        /// </summary>
        private static bool IsMeaningful(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var t = s.Trim();
            if (t.Equals("unknown", System.StringComparison.OrdinalIgnoreCase)) return false;
            if (t.Equals("unknown|unknown", System.StringComparison.OrdinalIgnoreCase)) return false;
            if (t.Equals("!Not Defined!", System.StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
    }
}