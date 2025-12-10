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

        // 楽曲選択時に呼び出す
        public static async Task OnSongSelected(
            Transform detailDescriptionParent, string key, string songName, string songAuthor)
        {
            MapMemo.Plugin.Log?.Info($"SelectionHook: OnSongSelected parent='{detailDescriptionParent?.name}' key='{key}' song='{songName}' author='{songAuthor}'");
            if (detailDescriptionParent == null) return;

            // 無意味なキー（unknownや空）の場合はパネルを取り付けない
            if (!IsMeaningful(key))
            {
                MapMemo.Plugin.Log?.Warn("SelectionHook: key is not meaningful; skipping panel attach/update");
                return;
            }
            bool isInstance = MemoPanelController.isInstance();

            var ctrl = MemoPanelController.GetInstance(
                    detailDescriptionParent, key, songName, songAuthor);
            if (!isInstance)
            {
                LevelDetailInjector.AttachTo(detailDescriptionParent, ctrl, key, songName, songAuthor);
            }

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