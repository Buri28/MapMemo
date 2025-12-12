using System.Threading.Tasks;
using ModestTree;
using UnityEngine;

namespace MapMemo.UI
{
    // 選曲変更時に呼び出される想定のフック土台
    // 実環境ではLevelSelection/DetailViewのイベントから呼び出す
    public static class SelectionHook
    {
        //private static MemoPanelController currentPanel;
        public static async Task OnSongSelected(
            MonoBehaviour view, string key, string songName, string songAuthor)
        {
            // viewはStandardLevelDetailViewを想定
            Plugin.Log?.Info($"SelectionHook: OnSongSelected parent='{view?.name}' key='{key}' song='{songName}' author='{songAuthor}'");
            if (view == null) return;

            bool isInstance = MemoPanelController.isInstance();

            var ctrl = MemoPanelController.GetInstance(view, key, songName, songAuthor);
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