using System.Threading.Tasks;
using UnityEngine;

namespace MapMemo.UI
{
    // 選曲変更時に呼び出される想定のフック土台
    // 実環境ではLevelSelection/DetailViewのイベントから呼び出す
    public static class SelectionHook
    {
        private static MemoPanelController currentPanel;

        // 楽曲選択時に呼び出す
        public static async Task OnSongSelected(Transform detailDescriptionParent, string key, string songName, string songAuthor)
        {
            MapMemo.Plugin.Log?.Info($"SelectionHook: OnSongSelected parent='{detailDescriptionParent?.name}' key='{key}' song='{songName}' author='{songAuthor}'");
            if (detailDescriptionParent == null) return;

            // 無意味なキー（unknownや空）の場合はパネルを取り付けない
            if (!IsMeaningful(key))
            {
                MapMemo.Plugin.Log?.Warn("SelectionHook: key is not meaningful; skipping panel attach/update");
                return;
            }

            if (currentPanel == null)
            {
                MapMemo.Plugin.Log?.Info("SelectionHook: Attaching new MemoPanelController");
                currentPanel = LevelDetailInjector.AttachTo(detailDescriptionParent, key, songName, songAuthor);
            }
            else
            {
                MapMemo.Plugin.Log?.Info("SelectionHook: Updating existing panel");
                currentPanel.Key = key;
                currentPanel.SongName = songName;
                currentPanel.SongAuthor = songAuthor;
            }

            if (currentPanel != null)
            {
                MapMemo.Plugin.Log?.Info("SelectionHook: RefreshAsync begin");
                await currentPanel.RefreshAsync();
                MapMemo.Plugin.Log?.Info("SelectionHook: RefreshAsync end");
            }
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