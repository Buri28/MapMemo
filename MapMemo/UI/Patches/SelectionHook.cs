using System.Threading.Tasks;
using MapMemo.Core;
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
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"SelectionHook: OnSongSelected parent='{view?.name}' key='{levelContext.GetLevelId()}' song='{levelContext.GetSongName()}' author='{levelContext.GetSongAuthor()}'");
            if (view == null) return;

            bool isInstance = MemoPanelController.isInstance();

            var ctrl = MemoPanelController.GetInstance(view, levelContext);
            await ctrl.Refresh();
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