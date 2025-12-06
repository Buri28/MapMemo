using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Tags;
using IPA.Logging;
using MapMemo.UI;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace MapMemo.UI
{
    // シンプルな取り付けヘルパー：右側詳細パネルにMemoPanelを追加
    public static class LevelDetailInjector
    {
        public static GameObject LastHostGameObject { get; private set; }
        // parent: 詳細パネルのTransform（説明文下が望ましい）
        public static MemoPanelController AttachTo(Transform parent,
            string key, string songName, string songAuthor)
        {
            try
            {
                // 取り付け判定はHarmony側で十分に抑制しているため、ここでは親チェックを行わず取り付ける
                var parentName = parent?.name ?? "<null>";
                MapMemo.Plugin.Log?.Info($"LevelDetailInjector: proceeding attach on parent '{parentName}'");

                var ctrl = new MemoPanelController();
                // 通常のパネル（説明文下）を生成
                var bsmlContent = Utilities.GetResourceContent(
                    typeof(MemoPanelController).Assembly, "MapMemo.Resources.MemoPanel.bsml");
                if (string.IsNullOrEmpty(bsmlContent))
                {
                    MapMemo.Plugin.Log?.Error("LevelDetailInjector: MemoPanel.bsml content not found");
                    return null;
                }
                var parserParams = BSMLParser.Instance.Parse(
                    bsmlContent,
                    parent.gameObject,
                    ctrl
                );
                MemoPanelController.LastInstance = ctrl;
                MapMemo.Plugin.Log?.Info($"LevelDetailInjector: parent='{parent.name}', before children={((RectTransform)parent).childCount}");
                var parentRt = parent as RectTransform;
                if (parentRt != null && parentRt.childCount > 0)
                {
                    var last = parentRt.GetChild(parentRt.childCount - 1) as RectTransform;
                    // レイアウトは親の垂直レイアウトに従わせる（末尾へ）
                    last?.SetAsLastSibling();
                    if (last != null) last.gameObject.SetActive(true);
                    if (MapMemo.Plugin.VerboseLogs) MapMemo.Plugin.Log?.Info($"LevelDetailInjector: added child='{last?.name}', after children={parentRt.childCount}");
                    // 子一覧をデバッグ出力（冗長なため VerboseLogs 有効時のみ）
                    if (MapMemo.Plugin.VerboseLogs)
                    {
                        for (int i = 0; i < parentRt.childCount; i++)
                        {
                            var c = parentRt.GetChild(i);
                            MapMemo.Plugin.Log?.Info($"LevelDetailInjector: child[{i}]='{c.name}' active={c.gameObject.activeSelf}");
                        }
                    }
                }
                // 親が『NoAllowedBeatmapInfoText』『NoDataInfoContainer』等の情報テキストの場合、表示が制御されることがあるため一段上へ退避
                var pn = parent.name;
                if (pn == "NoAllowedBeatmapInfoText" || pn == "NoDataInfoContainer")
                {
                    var higher = parent.parent as RectTransform;
                    if (higher != null && parentRt != null && parentRt.childCount > 0)
                    {
                        var last = parentRt.GetChild(parentRt.childCount - 1) as RectTransform;
                        if (last != null)
                        {
                            // worldPositionStays: true により位置維持、falseにすると透明な当たり判定が背後を覆ってしまう
                            last.SetParent(higher, worldPositionStays: true);
                            last.SetAsLastSibling();
                            // 一段上に付け替えた後も末尾へ配置（レイアウト任せ）
                            MapMemo.Plugin.Log?.Info($"LevelDetailInjector: reparented last child '{last.name}' to '{higher.name}' due to parent='{pn}'");
                        }
                    }
                }
                ctrl.HostGameObject = parent.gameObject;
                LastHostGameObject = parent.gameObject;
                ctrl.Key = key;
                ctrl.SongName = songName;
                ctrl.SongAuthor = songAuthor;
                // 直ちに表示更新（DidActivate待ちの間のプレースホルダ回避）
                _ = ctrl.RefreshAsync();
                return ctrl;
            }
            catch (System.Exception e)
            {
                MapMemo.Plugin.Log?.Error($"LevelDetailInjector.AttachTo error: {e}");
                return null;
            }
        }
    }
}
