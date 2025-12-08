using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Tags;
using IPA.Logging;
using MapMemo.UI;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.ComponentModel;

namespace MapMemo.UI
{
    // シンプルな取り付けヘルパー：右側詳細パネルにMemoPanelを追加
    public static class LevelDetailInjector
    {
        public static GameObject LastHostGameObject { get; private set; }

        // ...existing code...

        // 一度だけ LevelDetail の適切な親を検索して AttachTo を呼ぶユーティリティ
        // 目的: 浮遊ボタンを廃止し、右側詳細パネルの安定したホストへ一度だけ付ける
        public static MemoPanelController AttachOnceToLevelDetailRoot()
        {
            try
            {
                // 既に LastHostGameObject が登録されていてコントローラが存在すれば再利用
                if (LastHostGameObject != null)
                {
                    try
                    {
                        var existingCtrl = LastHostGameObject.GetComponentInChildren<MemoPanelController>(true) ?? LastHostGameObject.GetComponent<MemoPanelController>();
                        if (existingCtrl != null)
                        {
                            MemoPanelController.LastInstance = existingCtrl;
                            return existingCtrl;
                        }
                    }
                    catch { }
                }

                // シーン内で右側詳細ビューっぽいオブジェクトを名前ベースで探索する
                var t = Resources.FindObjectsOfTypeAll<Transform>()
                    .FirstOrDefault(x => x.name.IndexOf("StandardLevelDetailView", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         x.name.IndexOf("LevelDetail", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         x.name.IndexOf("LevelDetails", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         x.name.IndexOf("LevelDetailView", System.StringComparison.OrdinalIgnoreCase) >= 0);

                if (t == null)
                {
                    // 広めのフォールバック: 説明や詳細を含むノードを探す
                    t = Resources.FindObjectsOfTypeAll<Transform>()
                        .FirstOrDefault(x => x.name.IndexOf("Description", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                             x.name.IndexOf("Details", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                             x.name.IndexOf("Info", System.StringComparison.OrdinalIgnoreCase) >= 0);
                }

                if (t == null)
                {
                    MapMemo.Plugin.Log?.Warn("AttachOnceToLevelDetailRoot: could not find LevelDetail-like transform in scene");
                    return null;
                }

                // 発見した Transform を親として AttachTo を呼ぶ（キー等は空で後から更新される想定）
                MapMemo.Plugin.Log?.Info($"AttachOnceToLevelDetailRoot: found candidate '{t.name}', attaching panel");
                var attached = AttachTo(t, key: string.Empty, songName: string.Empty, songAuthor: string.Empty);
                return attached;
            }
            catch (System.Exception e)
            {
                MapMemo.Plugin.Log?.Warn($"AttachOnceToLevelDetailRoot error: {e.Message}");
                return null;
            }
        }
        // フローティング表示は廃止：代わりに LevelDetail の親を探して一度だけ AttachTo を呼ぶ
        public static MemoPanelController AttachFloatingNearAnchor(Transform anchor, Vector2 screenOffset)
        {
            try
            {
                if (anchor == null) return null;
                string anchorName;
                try { anchorName = anchor.name; } catch { anchorName = "<destroyed>"; }
                MapMemo.Plugin.Log?.Info($"AttachFloatingNearAnchor: 浮動UIは無効。アンカー='{anchorName}' の親LevelDetailへのアタッチを試行します");

                // LevelDetail のルートに一度だけアタッチするようにする（アンカーごとではなくグローバルに一度）
                // 既に存在する添付があればそれを優先し、なければシーン内で適当な LevelDetail ルートを探して一度だけ取り付けます。
                try
                {
                    return AttachOnceToLevelDetailRoot();
                }
                catch (System.Exception ex)
                {
                    MapMemo.Plugin.Log?.Warn($"AttachFloatingNearAnchor: AttachOnceToLevelDetailRoot failed: {ex.Message}");
                    return null;
                }
            }
            catch (System.Exception e)
            {
                MapMemo.Plugin.Log?.Error($"LevelDetailInjector.AttachFloatingNearAnchor error: {e}");
                return null;
            }
        }

        // Floating positioning helpers removed to simplify the injector; AttachOnceToLevelDetailRoot/AttachTo
        // now rely on the parent layout for placement. Reintroduce helpers only if explicit floating
        // placement is required in future and implemented in a controlled manner.
        // ...existing code...

        // parent: 詳細パネルのTransform（説明文下が望ましい）
        public static MemoPanelController AttachTo(Transform parent,
            string key, string songName, string songAuthor)
        {
            try
            {
                var parentName = parent?.name ?? "<null>";
                MapMemo.Plugin.Log?.Info($"LevelDetailInjector: proceeding attach on parent '{parentName}' (minimal mode)");

                // LastIndstanceではだめ、インスタンスはキーで分けて管理しないといけない
                // 既に同じ親に取り付け済みなら何もしない（LastInstance があればそれを返す）
                if (LastHostGameObject == parent?.gameObject)
                {
                    MapMemo.Plugin.Log?.Info($"LevelDetailInjector: already attached to parent '{parentName}', skipping attach");
                    var instance = MemoPanelController.GetRefreshViewInstance(
                        key, songName, songAuthor);
                    return instance;
                }

                // 取り付け判定はHarmony側で十分に抑制しているため、この場では再チェックを最限にします
                // コントローラの新規インスタンスを作成して BSML パースに渡します
                var ctrl = new MemoPanelController();
                // 通常のパネル（説明文下）を生成
                var bsmlContent = Utilities.GetResourceContent(
                    typeof(MemoPanelController).Assembly, "MapMemo.Resources.MemoPanel.bsml");
                if (string.IsNullOrEmpty(bsmlContent))
                {
                    MapMemo.Plugin.Log?.Error("LevelDetailInjector: MemoPanel.bsml content not found");
                    return null;
                }
                BSMLParser.Instance.Parse(
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
                _ = ctrl.Refresh();
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