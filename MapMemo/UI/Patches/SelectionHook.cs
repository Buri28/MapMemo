using System.Threading.Tasks;
using MapMemo.Core;
using MapMemo.UI.Menu;
using ModestTree;
using UnityEngine;

namespace MapMemo.Patches
{
    // 選曲変更時に呼び出される想定のフック土台
    // 実環境ではLevelSelection/DetailViewのイベントから呼び出す
    public static class SelectionHook
    {
        //private static MemoPanelController currentPanel;
        public static async Task OnSongSelected(
            MonoBehaviour view, LevelContext levelContext)
        {
            // viewはStandardLevelDetailViewを想定
            Plugin.Log?.Info($"SelectionHook: OnSongSelected parent='{view?.name}' key='{levelContext.GetLevelId()}' song='{levelContext.GetSongName()}' author='{levelContext.GetSongAuthor()}'");
            if (view == null) return;

            bool isInstance = MemoPanelController.isInstance();

            var ctrl = MemoPanelController.GetInstance(view, levelContext);
            await ctrl.Refresh();
        }

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