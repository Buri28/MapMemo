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

        // ...existing code...

        // 一度だけ LevelDetail の適切な親を検索して AttachTo を呼ぶユーティリティ
        // 目的: 浮遊ボタンを廃止し、右側詳細パネルの安定したホストへ一度だけ付ける
        public static MemoPanelController AttachOnceToLevelDetailRoot()
        {
            try
            {
                // 既にインスタンスが存在すれば再利用
                if (MemoPanelController.LastInstance != null) return MemoPanelController.LastInstance;

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

        // anchor の World 位置（またはアンカーの RectTransform をスクリーン変換）から
        // Canvas ローカル座標へ変換して ctrl の RectTransform を配置するユーティリティ
        private static void PositionFloating(MemoPanelController ctrl, Transform anchor, Vector2 screenOffset)
        {
            try
            {
                if (ctrl == null || anchor == null) return;
                GameObject ctrlGo = null;
                try { ctrlGo = ctrl.gameObject; } catch { ctrlGo = null; }
                if (ctrlGo == null) return;
                var canvas = ctrlGo.GetComponentInParent<Canvas>();
                if (canvas == null) return;
                var canvasRt = canvas.transform as RectTransform;
                if (canvasRt == null) return;
                MapMemo.Plugin.Log?.Info($"PositionFloating: canvas='{canvas.name}' worldCamera={(canvas.worldCamera == null ? "<null>" : canvas.worldCamera.name)}");

                // World -> Screen
                // 注意: ScreenSpaceOverlay の Canvas の場合、RectTransformUtility はカメラに null を期待します
                Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : (canvas.worldCamera ?? Camera.main);
                Vector3 worldPos = anchor.position;
                // If the anchor is a RectTransform, nudge the worldPos to the top of the rect so the floating panel doesn't land centered on the anchor
                var anchorRt = anchor as RectTransform;
                if (anchorRt != null)
                {
                    try
                    {
                        var up = anchorRt.up;
                        var heightWorld = (anchorRt.rect.height * anchorRt.lossyScale.y) * 0.5f;
                        worldPos = anchorRt.position + up * heightWorld;
                    }
                    catch { }
                }
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

                // スクリーン空間でオフセットを適用
                screenPoint += screenOffset;

                // スクリーン座標 -> Canvas のローカル座標へ変換
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPoint, cam, out localPoint);
                MapMemo.Plugin.Log?.Info($"PositionFloating: worldPos={worldPos} screenPoint={screenPoint} localPoint={localPoint}");

                // コントローラの RectTransform にアンカー位置をセット
                var ctrlRt = ctrlGo.GetComponent<RectTransform>();
                if (ctrlRt == null)
                {
                    MapMemo.Plugin.Log?.Warn("PositionFloating: ctrl has no RectTransform");
                    return;
                }
                ctrlRt.SetParent(canvasRt, worldPositionStays: false);
                ctrlRt.anchorMin = ctrlRt.anchorMax = ctrlRt.pivot = new Vector2(0.5f, 0.5f);
                ctrlRt.anchoredPosition = localPoint;
                MapMemo.Plugin.Log?.Info($"PositionFloating: ctrlRt.size={ctrlRt.rect.size} anchoredPosition={ctrlRt.anchoredPosition}");
            }
            catch (System.Exception e)
            {
                MapMemo.Plugin.Log?.Warn($"PositionFloating error: {e.Message}");
            }
        }

        // 安全版: 事前に計算したワールド座標から配置する。anchor が破棄される可能性があるときに使用する。
        private static void PositionFloatingByWorldPos(MemoPanelController ctrl, Vector3 worldPos, Vector2 screenOffset)
        {
            try
            {
                if (ctrl == null) return;
                GameObject ctrlGo = null;
                try { ctrlGo = ctrl.gameObject; } catch { ctrlGo = null; }
                if (ctrlGo == null)
                {
                    MapMemo.Plugin.Log?.Warn("PositionFloatingByWorldPos: ctrl.gameObject is null");
                    return;
                }
                var canvas = ctrlGo.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    MapMemo.Plugin.Log?.Warn("PositionFloatingByWorldPos: no Canvas found for ctrl");
                    return;
                }
                var canvasRt = canvas.transform as RectTransform;
                if (canvasRt == null) return;
                MapMemo.Plugin.Log?.Info($"PositionFloatingByWorldPos: canvas='{canvas.name}' worldCamera={(canvas.worldCamera == null ? "<null>" : canvas.worldCamera.name)}");

                // 注意: ScreenSpaceOverlay の Canvas では Camera を null にする
                Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : (canvas.worldCamera ?? Camera.main);
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
                screenPoint += screenOffset;
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPoint, cam, out localPoint);
                MapMemo.Plugin.Log?.Info($"PositionFloatingByWorldPos: worldPos={worldPos} screenPoint={screenPoint} localPoint={localPoint}");

                var ctrlRt = ctrlGo.GetComponent<RectTransform>();
                if (ctrlRt == null)
                {
                    MapMemo.Plugin.Log?.Warn("PositionFloatingByWorldPos: ctrl has no RectTransform");
                    return;
                }
                ctrlRt.SetParent(canvasRt, worldPositionStays: false);
                ctrlRt.anchorMin = ctrlRt.anchorMax = ctrlRt.pivot = new Vector2(0.5f, 0.5f);
                ctrlRt.anchoredPosition = localPoint;
                MapMemo.Plugin.Log?.Info($"PositionFloatingByWorldPos: ctrlRt.size={ctrlRt.rect.size} anchoredPosition={ctrlRt.anchoredPosition}");
            }
            catch (System.Exception e)
            {
                MapMemo.Plugin.Log?.Warn($"PositionFloatingByWorldPos error: {e.Message}");
            }
        }
        // ...existing code...

        // parent: 詳細パネルのTransform（説明文下が望ましい）
        public static MemoPanelController AttachTo(Transform parent,
            string key, string songName, string songAuthor)
        {
            try
            {
                var parentName = parent?.name ?? "<null>";
                MapMemo.Plugin.Log?.Info($"LevelDetailInjector: proceeding attach on parent '{parentName}' (minimal mode)");

                // 既に同じ親に取り付け済みなら何もしない（LastInstance があればそれを返す）
                if (LastHostGameObject == parent?.gameObject)
                {
                    MapMemo.Plugin.Log?.Info($"LevelDetailInjector: already attached to parent '{parentName}', skipping attach");
                    return MemoPanelController.LastInstance;
                }

                // 取り付け判定はHarmony側で十分に抑制しているため、この場では再チェックを最小限にします
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
