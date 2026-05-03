using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using BeatSaberMarkupLanguage.Components;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MapMemo.UI.Edit;
using MapMemo.Models;
using MapMemo.Services;
using Mapmemo.Models;
using MapMemo.Domain;
using MapMemo.UI.Common;
using HMUI;
using System.Text.RegularExpressions;

namespace MapMemo.UI.Menu
{


    [HotReload]
    /// <summary>
    /// メモパネルのコントローラー。メニューのペンアイコン表示と更新を行います。
    /// </summary>
    public class MemoPanelController : BSMLAutomaticViewController
    {
        private static readonly Version PenDecorationMinVersion = new Version(1, 42, 0);
        private const int PenTextLeftPadding = 4;
        private const int PenTextTopPadding = 2;
        // ペンアイコンに重ねる装飾マーカーの設定 〇○□❏▱■▪
        private const string PenDecorationMarkerText = "■";
        private const float PenDecorationMarkerFontSize = 8.0f;
        // 右に移動するには正の値を指定する必要があるが、テキストのサイズやフォントによって見た目の位置が変わるため、微調整が必要
        private const float PenDecorationMarkerX = -0.1f;
        // テキストの下に配置するために負の値を指定。こちらも微調整が必要。
        private const float PenDecorationMarkerY = 0.6f;

        private enum EventThemeType
        {
            None,
            Halloween,
            AprilFool,
            Christmas,
            NewYear
        }

        // この段階でインスタンスを作るとUnityの管理外のためバインド対象外となる。
        public static MemoPanelController Instance = null;
        /// <summary>
        /// ホストとなる GameObject（バインド対象）
        /// </summary>
        public GameObject HostGameObject { get; set; }
        private MonoBehaviour hostView;
        /// <summary> 現在のレベルコンテキスト</summary>
        private LevelContext levelContext;
        /// <summary> ペンアイコンテキスト</summary>
        [UIComponent("pen-text")] private ClickableText penText = null;
        private TextMeshProUGUI penDecorationMarker;
        private Color penDecorationDefaultColor = Color.white;
        private Color penDecorationHighlightColor = Color.white;
        /// <summary> HotReload 用リソース名</summary>
        public string ResourceName => "MapMemo.Resources.MemoPanel.bsml";
        /// <summary> メモサービスのインスタンス。</summary>
        private MemoService memoService = MemoService.Instance;

        /// <summary>
        /// インスタンスが存在するかどうかを判定します。
        /// </summary>
        public static bool isInstance() => !ReferenceEquals(Instance, null);

        /// <summary>
        /// BSML 解析後の初期化処理。
        /// </summary>
        [UIAction("#post-parse")]
        private void OnPostParse()
        {
            EnsurePenDecoration();
            EnsurePenDecorationHoverRelay();
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanelController: OnPostParse called");
        }

        /// <summary>
        /// 毎フレーム、補助マーカーをペンアイコンの描画位置へ追従させます。
        /// </summary>
        private void LateUpdate()
        {
            if (!IsPenDecorationEnabled())
            {
                return;
            }

            SyncPenDecorationTransform();
        }

        /// <summary>
        /// 既存の LastInstance を使って表示を更新するユーティリティ
        /// </summary>
        /// <param name="view">ホストとなる ViewController の MonoBehaviour</param>
        /// <param name="levelContext">現在の LevelContext</param>
        /// <returns>MemoPanelController のインスタンス</returns>
        public static MemoPanelController GetInstance(
            MonoBehaviour view, LevelContext levelContext)
        {
            if (!isInstance())
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanelController.GetInstance: "
                    + $"instance is null, creating new one");
                Instance = BeatSaberUI.CreateViewController<MemoPanelController>();

                if (Plugin.VerboseLogs) Plugin.Log?.Info($"instance.gameObject = {Instance?.gameObject}");

                // 親に追加（ここでは view は既存の ViewController）
                Instance.transform.SetParent(view.transform, false);

                var bsmlContent = BeatSaberMarkupLanguage.Utilities.GetResourceContent(
                     typeof(MemoPanelController).Assembly,
                     "MapMemo.Resources.MemoPanel.bsml");
                Instance.ParseBSML(bsmlContent, Instance.gameObject);

                // 表示を確実にする
                Instance.gameObject.SetActive(true);

                // 子の位置とサイズを親に合わせて調整
                var child = Instance.transform.GetChild(0) as RectTransform;
                child.anchorMin = new Vector2(0f, 1f);
                child.anchorMax = new Vector2(1f, 1f);
                child.pivot = new Vector2(0.5f, 1f);
                child.anchoredPosition = Vector2.zero;
                child.sizeDelta = new Vector2(0f, 56f); // 親と同じ高さに

                // ペンパネルの位置調整
                child.anchoredPosition = new Vector2(14f, 13f); // 中央寄り

                var parentRt = view.transform as RectTransform;

                if (Plugin.VerboseLogs) Plugin.Log?.Info(
                    $"Parent anchorMin: {parentRt.anchorMin}, "
                    + $"anchorMax: {parentRt.anchorMax}, "
                    + $"pivot: {parentRt.pivot}, "
                    + $"sizeDelta: {parentRt.sizeDelta}");
                if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanelController.GetInstance: "
                    + $"Created new instance:" + isInstance());

                // リソースのロード
                MemoService.Instance.LoadResources();
            }
            Instance.levelContext = levelContext;
            Instance.HostGameObject = view.gameObject;
            Instance.hostView = view;

            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanelController.GetInstance: Refreshing instance");
            Instance.Refresh();

            return Instance;
        }

        /// <summary>
        /// BSMLを解析してホストにアタッチする
        /// </summary>
        /// <param name="bsml">BSML文字列</param>
        /// <param name="host">ホストのGameObject</param>
        public void ParseBSML(string bsml, GameObject host)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanelController: "
                + $"BSML parsed and attached to host '" + host.name + "'");
            BSMLParser.Instance.Parse(bsml, host, this);
        }

        /// <summary>
        /// 編集ボタン押下時の処理。エディットモーダルを表示します。
        /// </summary>
        [UIAction("on-edit-click")]
        public void OnEditClick()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: "
                + $"Edit click key='{levelContext.GetLevelId()}' "
                + $"song='{levelContext.GetSongName()}' author='{levelContext.GetSongAuthor()}'"
                + $" levelAuthor='{levelContext.GetLevelAuthor()}'");
            MemoEditModalController.Show(Instance, levelContext);
        }

        /// <summary>
        /// 指定した GameObject にホバーヒントを設定します。
        /// 必要なら HoverHint コンポーネントを追加します。
        /// </summary>
        /// <param name="go">ホバーヒントを設定する対象の GameObject</param>
        /// <param name="hint">表示するホバーテキスト</param>
        public void SetHoverHint(GameObject go, string hint)
        {
            if (go == null)
            {
                Plugin.Log?.Warn("MemoPanel: GameObject is null, cannot set hover hint");
                return;
            }

            var normalizedHint = string.IsNullOrWhiteSpace(hint) ? "" : hint.Trim();
            var hover = go.GetComponent<HMUI.HoverHint>();

            if (string.IsNullOrEmpty(normalizedHint))
            {
                if (hover != null)
                {
                    if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: Clearing hover hint because hint is empty");
                    hover.text = "";
                    hover.enabled = false;
                }
                return;
            }

            // HoverHint が無ければ追加
            if (hover == null)
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: Adding HoverHint component");
                hover = go.AddComponent<HMUI.HoverHint>();
            }

            // AddComponent で追加した HoverHint は _hoverHintController が null のまま
            // (DI で注入されないため) なので、既存の動いている HoverHint から Controller を拝借する
            InjectHoverHintController(hover);

            hover.text = normalizedHint;
            hover.enabled = true;
        }

        /// <summary>
        /// HMUI.HoverHint の非公開フィールド <c>_hoverHintController</c> へのリフレクション参照。
        /// AddComponent で生成した HoverHint にコントローラーを注入するために使用します。
        /// </summary>
        private static readonly FieldInfo _hoverHintControllerField =
            typeof(HMUI.HoverHint).GetField("_hoverHintController",
                BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// 指定した <see cref="HMUI.HoverHint"/> に <see cref="HMUI.HoverHintController"/> を注入します。
        /// <para>
        /// AddComponent で追加した HoverHint は DI が行われないため <c>_hoverHintController</c> が null のままです。
        /// 既存の動作済み HoverHint（ペンアイコン）またはシーン全体から Controller を取得して注入します。
        /// </para>
        /// </summary>
        /// <param name="target">Controller を注入する対象の HoverHint</param>
        private void InjectHoverHintController(HMUI.HoverHint target)
        {
            if (target == null)
            {
                Plugin.Log?.Warn("MemoPanel: HoverHint target is null, cannot inject controller");
                return;
            }
            if (_hoverHintControllerField == null)
            {
                Plugin.Log?.Warn("MemoPanel: HoverHintController field is null, cannot inject controller");
                return;
            }

            // 既にセット済みなら何もしない
            if (_hoverHintControllerField.GetValue(target) != null)
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: HoverHintController already set, skipping injection");
                return;
            }
            HMUI.HoverHintController controller = null;

            // 既存の HoverHintController をシーン全体から探して流用する
            if (controller == null)
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: HoverHintController searching in scene");
                controller = UnityEngine.Object.FindObjectOfType<HMUI.HoverHintController>();
            }

            if (controller != null)
            {
                _hoverHintControllerField.SetValue(target, controller);
                if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: injected HoverHintController into HoverHint");
            }
            else
            {
                Plugin.Log?.Warn("MemoPanel: HoverHintController not found, tooltip may not show");
            }
        }

        /// <summary>
        /// 表示内容を更新します。現在の LevelContext に紐づくメモを読み込み、ペンアイコンとツールチップを更新します。
        /// </summary>
        public Task Refresh()
        {
            try
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Debug("MemoPanel: Refresh called");
                if (penText == null)
                {
                    if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: penText is null, cannot refresh");
                    return Task.CompletedTask;
                }

                if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: "
                    + $"Refresh called for key='{levelContext.GetLevelId()}' "
                    + $"song='{levelContext.GetSongName()}' author='{levelContext.GetSongAuthor()}'"
                    + $" levelAuthor='{levelContext.GetLevelAuthor()}'"
                    + $" hash='{levelContext.GetLevelHash()}'");

                // 同期ロードを使って確実に現在の Key に紐づくデータを取得する
                var entry = memoService.LoadMemo(levelContext);
                var beatSaverMap = BeatSaverManager.Instance.TryGetCache(
                                        levelContext.GetLevelHash());
                var noEntryColor = Color.cyan;
                var noEntryHighlight = Color.green;
                var entryColor = Color.yellow;
                var entryHighlight = Color.green;
                var activeEventTheme = GetActiveEventTheme();
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: "
                    + $"Trying to get BeatSaverMap from cache for hash='{levelContext.GetLevelHash()}'");
                if (beatSaverMap != null)
                {
                    if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: "
                        + $"Found BeatSaverMap in cache for hash='{levelContext.GetLevelHash()}'");
                    float score = (float)(beatSaverMap.stats.score * 100);
                    // BeatSaver情報がある場合は色を変える
                    var colorStr = UIHelper.Instance.GetMultiGradientColor(score);
                    var highlightStr = UIHelper.Instance.GetHighlightColor(colorStr);
                    noEntryColor = UIHelper.Instance.ToColor(colorStr);
                    noEntryHighlight = UIHelper.Instance.ToColor(highlightStr);
                    entryColor = UIHelper.Instance.ToColor(colorStr);
                    entryHighlight = UIHelper.Instance.ToColor(highlightStr);
                    if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: "
                        + $"No entry color for score={score} is '{colorStr}' highlight '{highlightStr}'");
                }

                if (activeEventTheme != EventThemeType.None)
                {
                    noEntryColor = GetEventBaseColor(activeEventTheme, noEntryColor);
                    noEntryHighlight = GetEventHighlightColor(activeEventTheme, noEntryHighlight);
                    entryColor = GetEventBaseColor(activeEventTheme, entryColor);
                    entryHighlight = GetEventHighlightColor(activeEventTheme, entryHighlight);
                }

                // カバー画像のホバーヒントを設定（BeatSaver 情報があれば説明文を、なければ空にする）
                if (memoService.GetCoverHoverHint())
                {
                    SetCoverHoverHint(beatSaverMap);
                }
                else
                {
                    // 設定OFFのときは既存のヒントを無効化し raycastTarget も戻す
                    ClearCoverHoverHint();
                }

                var parentLayout = penText.transform.parent.GetComponent<HorizontalLayoutGroup>();
                if (parentLayout != null)
                {
                    parentLayout.childForceExpandWidth = false;
                    parentLayout.childControlWidth = true;
                    parentLayout.padding.left = PenTextLeftPadding;
                    parentLayout.padding.top = PenTextTopPadding;
                }
                var layout = penText.GetComponent<LayoutElement>();
                if (layout == null)
                    layout = penText.gameObject.AddComponent<LayoutElement>();

                layout.preferredWidth = 10f; // 幅を制限
                layout.flexibleWidth = 0f;    // 自動伸縮を無効に
                EnsurePenDecoration();
                EnsurePenDecorationHoverRelay();
                if (entry == null)
                {
                    if (Plugin.VerboseLogs) MapMemo.Plugin.Log?.Info("MemoPanel: "
                        + $"No memo entry found for key='" + levelContext.GetLevelId() + "'");
                    penText.text = GetStyledEventIcon(activeEventTheme, entry);
                    penText.fontStyle = FontStyles.Bold;
                    penText.color = noEntryColor;
                    penText.DefaultColor = noEntryColor;
                    penText.HighlightColor = noEntryHighlight;
                    SetPenDecorationColors(noEntryColor, noEntryHighlight);
                    SetHoverHint(penText.gameObject,
                        MakeTooltipLine(entry, beatSaverMap, memoService, activeEventTheme, 40));
                }
                else
                {
                    if (Plugin.VerboseLogs) MapMemo.Plugin.Log?.Info("MemoPanel: "
                        + $"Memo entry found for key='" + levelContext.GetLevelId() + "'");

                    penText.text = GetStyledEventIcon(activeEventTheme, entry);
                    penText.color = entryColor;
                    penText.DefaultColor = entryColor;
                    penText.HighlightColor = entryHighlight;
                    SetPenDecorationColors(entryColor, entryHighlight);
                    penText.fontStyle = FontStyles.Bold;

                    var button = penText.GetComponentInParent<UnityEngine.UI.Button>();
                    if (button != null)
                    {
                        var colors = button.colors;
                        colors.normalColor = entryColor;
                        colors.highlightedColor = entryHighlight;
                        colors.pressedColor = entryHighlight;
                        colors.selectedColor = entryHighlight;
                        colors.disabledColor = Color.gray;
                        button.colors = colors;
                        button.transition = Selectable.Transition.None;
                    }
                    if (Plugin.VerboseLogs) MapMemo.Plugin.Log?.Info("MemoPanel: "
                        + $"Setting hover hint for memo entry key='" + levelContext.GetLevelId() + "'");


                    SetHoverHint(penText.gameObject,
                        MakeTooltipLine(entry, beatSaverMap, memoService, activeEventTheme, 40));
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"MemoPanel.Refresh: {ex}");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// ペンアイコン用の装飾マーカー文字を生成します。
        /// レイアウトには参加させず、penText の子として重ねます。
        /// </summary>
        private void EnsurePenDecoration()
        {
            if (!IsPenDecorationEnabled())
            {
                if (penDecorationMarker != null)
                {
                    penDecorationMarker.gameObject.SetActive(false);
                }
                return;
            }

            if (penText == null || penDecorationMarker != null)
            {
                return;
            }

            var penRect = penText.rectTransform;
            var parentTransform = penRect.parent;
            if (parentTransform == null)
            {
                return;
            }

            var decorationObject = new GameObject("PenDecorationMarker",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            decorationObject.transform.SetParent(parentTransform, false);

            var decorationRect = decorationObject.GetComponent<RectTransform>();
            decorationRect.anchorMin = penRect.anchorMin;
            decorationRect.anchorMax = penRect.anchorMax;
            decorationRect.pivot = penRect.pivot;
            decorationRect.sizeDelta = penRect.sizeDelta;
            decorationRect.localScale = Vector3.one;

            penDecorationMarker = decorationObject.GetComponent<TextMeshProUGUI>();
            penDecorationMarker.text = PenDecorationMarkerText;
            penDecorationMarker.font = penText.font;
            penDecorationMarker.fontSize = PenDecorationMarkerFontSize;
            penDecorationMarker.alignment = TextAlignmentOptions.Center;
            penDecorationMarker.enableWordWrapping = false;
            penDecorationMarker.richText = true;
            penDecorationMarker.raycastTarget = false;
            penDecorationMarker.fontStyle = FontStyles.Italic;
            penDecorationMarker.color = penDecorationDefaultColor;

            var decorationLayout = decorationObject.GetComponent<LayoutElement>();
            decorationLayout.ignoreLayout = true;
            SyncPenDecorationTransform();

            var penSiblingIndex = penText.transform.GetSiblingIndex();
            decorationObject.transform.SetSiblingIndex(Mathf.Max(0, penSiblingIndex));
            decorationObject.SetActive(true);
        }

        /// <summary>
        /// ペン装飾マーカー用の通常色とホバー色を更新します。
        /// </summary>
        private void SetPenDecorationColors(Color normalColor, Color highlightColor)
        {
            if (!IsPenDecorationEnabled() || penDecorationMarker == null)
            {
                return;
            }

            penDecorationDefaultColor = normalColor;
            penDecorationHighlightColor = highlightColor;
            ApplyPenDecorationColor(penDecorationDefaultColor);
        }

        /// <summary>
        /// ペンアイコンにホバー連動の装飾更新を追加します。
        /// </summary>
        private void EnsurePenDecorationHoverRelay()
        {
            if (!IsPenDecorationEnabled() || penText == null)
            {
                return;
            }

            var relay = penText.gameObject.GetComponent<PenDecorationHoverRelay>();
            if (relay == null)
            {
                relay = penText.gameObject.AddComponent<PenDecorationHoverRelay>();
            }

            relay.Owner = this;
        }

        /// <summary>
        /// ペンアイコンへのポインター進入時に、補助マーカーをハイライト色へ切り替えます。
        /// </summary>
        private void OnPenPointerEnter()
        {
            if (!IsPenDecorationEnabled())
            {
                return;
            }

            ApplyPenDecorationColor(penDecorationHighlightColor);
        }

        /// <summary>
        /// ペンアイコンからポインターが外れたときに、補助マーカーを通常色へ戻します。
        /// </summary>
        private void OnPenPointerExit()
        {
            if (!IsPenDecorationEnabled())
            {
                return;
            }

            ApplyPenDecorationColor(penDecorationDefaultColor);
        }

        /// <summary>
        /// 補助マーカー文字列へ現在色を埋め込み、表示状態を更新します。
        /// </summary>
        /// <param name="color">マーカーに適用する色</param>
        private void ApplyPenDecorationColor(Color color)
        {
            if (!IsPenDecorationEnabled() || penDecorationMarker == null)
            {
                return;
            }

            SyncPenDecorationTransform();
            penDecorationMarker.text = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{PenDecorationMarkerText}</color>";
            penDecorationMarker.color = Color.white;
            penDecorationMarker.raycastTarget = false;
            penDecorationMarker.gameObject.SetActive(true);
        }

        /// <summary>
        /// ペンアイコンの実描画位置を基準に、補助マーカーの RectTransform を同期します。
        /// </summary>
        private void SyncPenDecorationTransform()
        {
            if (!IsPenDecorationEnabled() || penDecorationMarker == null || penText == null)
            {
                return;
            }

            var penRect = penText.rectTransform;
            var decorationRect = penDecorationMarker.rectTransform;
            penText.ForceMeshUpdate();
            var textBounds = penText.textBounds;
            var renderedCenter = (Vector2)textBounds.center;
            decorationRect.anchorMin = penRect.anchorMin;
            decorationRect.anchorMax = penRect.anchorMax;
            decorationRect.pivot = penRect.pivot;
            decorationRect.sizeDelta = penRect.sizeDelta;
            decorationRect.anchoredPosition = penRect.anchoredPosition
                + renderedCenter
                + new Vector2(PenDecorationMarkerX, PenDecorationMarkerY);
        }

        /// <summary>
        /// Beat Saber 1.42 以降でのみ補助マーカーを有効にします。
        /// </summary>
        private static bool IsPenDecorationEnabled()
        {
            return TryParseBeatSaberVersion(Application.version, out var currentVersion)
                && currentVersion >= PenDecorationMinVersion;
        }

        /// <summary>
        /// 実行時のバージョン文字列から数値部分を抽出し、比較用の Version へ正規化します。
        /// </summary>
        /// <param name="versionText">Unity から取得したバージョン文字列</param>
        /// <param name="version">正規化後の Version</param>
        /// <returns>Version へ変換できた場合は true</returns>
        private static bool TryParseBeatSaberVersion(string versionText, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(versionText))
            {
                return false;
            }

            var match = Regex.Match(versionText, @"\d+(?:\.\d+){0,3}");
            if (!match.Success)
            {
                return false;
            }

            var parts = match.Value.Split('.');
            if (parts.Length == 1)
            {
                return Version.TryParse($"{parts[0]}.0.0", out version);
            }

            if (parts.Length == 2)
            {
                return Version.TryParse($"{parts[0]}.{parts[1]}.0", out version);
            }

            return Version.TryParse(match.Value, out version);
        }

        /// <summary>
        /// ペンアイコンのポインターイベントを横取りして、補助マーカーの色だけを追従させます。
        /// </summary>
        private class PenDecorationHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public MemoPanelController Owner { get; set; }

            public void OnPointerEnter(PointerEventData eventData)
            {
                Owner?.OnPenPointerEnter();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                Owner?.OnPenPointerExit();
            }
        }

        /// <summary>
        /// カバー画像のホバーヒントを無効化し、raycastTarget を false に戻します。
        /// 設定 OFF 時に既存の HoverHint コンポーネントが残り続けるのを防ぎます。
        /// </summary>
        private void ClearCoverHoverHint()
        {
            var coverObject = FindCoverHoverTarget();
            if (coverObject == null)
            {
                Plugin.Log?.Warn("MemoPanel: cover hover target not found, cannot clear hover hint");
                return;
            }

            // HoverHint コンポーネントが存在する場合のみ無効化・raycastTarget 復元を行う
            var hover = coverObject.GetComponent<HMUI.HoverHint>();
            if (hover == null)
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: No HoverHint component found on cover object, skipping clear");
                return;
            }

            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: Clearing cover hover hint");
            hover.text = "";
            hover.enabled = false;

            var imageView = coverObject.GetComponent<ImageView>();
            if (imageView != null)
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: Disabling raycastTarget on ImageView '{coverObject.name}'");
                imageView.raycastTarget = false;
            }
        }

        /// <summary>
        /// カバー画像の GameObject を特定し、BeatSaver 説明文のホバーヒントを設定します。
        /// BeatSaver データが未取得の場合はヒントをクリアします。
        /// </summary>
        /// <param name="beatSaverMap">BeatSaver から取得したマップ情報（null 可）</param>
        private void SetCoverHoverHint(BeatSaverMap beatSaverMap)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: Setting cover hover hint");
            var coverObject = FindCoverHoverTarget();
            if (coverObject == null)
            {
                Plugin.Log?.Warn("MemoPanel: cover hover target not found");
                return;
            }

            EnsureHoverRaycastTarget(coverObject);
            SetHoverHint(coverObject, MakeCoverTooltipText(beatSaverMap));

            // パッチ側にカバーの HoverHint を登録（パッチ適用対象の絞り込みに使用）
            // ツールチップの大きさは結局制御できなかったためパッチは削除したが、念のため HoverHintController は登録しておく
            // var coverHover = coverObject.GetComponent<HMUI.HoverHint>();
            // MapMemo.Patches.HoverHintController_ShowHint_Patch.CoverHoverHint = coverHover;
        }

        /// <summary>
        /// カバー画像の <see cref="ImageView"/> / <see cref="Image"/> に対して
        /// raycastTarget を有効化し、VR ポインターのヒット判定を受け取れるようにします。
        /// </summary>
        /// <param name="coverObject">対象の GameObject</param>
        private static void EnsureHoverRaycastTarget(GameObject coverObject)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: Ensuring raycastTarget on cover object '{coverObject.name}'");
            if (coverObject == null)
            {
                Plugin.Log?.Warn("MemoPanel: coverObject is null, cannot ensure raycastTarget");
                return;
            }
            var imageView = coverObject.GetComponent<ImageView>();
            if (imageView != null)
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: Enabling raycastTarget on ImageView '{coverObject.name}'");
                imageView.raycastTarget = true;
            }
        }

        /// <summary>
        /// カバー画像の GameObject を探して返します。
        /// まずリフレクションで <c>StandardLevelDetailView._levelBar._songArtworkImageView</c> を辿り、
        /// 取得できない場合はオブジェクト名のヒューリスティック探索にフォールバックします。
        /// </summary>
        /// <returns>カバー画像の GameObject。見つからない場合は null。</returns>
        private GameObject FindCoverHoverTarget()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: Finding cover hover target");
            var reflectedTarget = FindCoverHoverTargetByReflection();
            if (reflectedTarget != null)
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: Found cover hover target by reflection: '{reflectedTarget.name}'");
                return reflectedTarget;
            }
            Plugin.Log?.Warn("MemoPanel: Reflection method failed, trying heuristic search");
            return null;
        }

        /// <summary>
        /// リフレクションを使って <c>StandardLevelDetailView._levelBar._songArtworkImageView</c> を取得します。
        /// Beat Saber のバージョンによってフィールドが存在しない場合は null を返します。
        /// </summary>
        /// <returns>曲アートワークの <see cref="ImageView"/> の GameObject。取得できない場合は null。</returns>
        private GameObject FindCoverHoverTargetByReflection()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: Finding cover hover target by reflection");
            if (hostView == null)
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: hostView is null, cannot find cover hover target by reflection");
                return null;
            }

            var levelBarField = hostView.GetType().GetField("_levelBar",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var levelBar = levelBarField?.GetValue(hostView) as MonoBehaviour;
            if (levelBar == null)
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: _levelBar field not found or null in hostView");
                return null;
            }
            var artworkField = levelBar.GetType().GetField("_songArtworkImageView",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var artwork = artworkField?.GetValue(levelBar) as ImageView;
            if (artwork == null)
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel: _songArtworkImageView field not found or null in _levelBar");
                return null;
            }
            return artwork.gameObject;
        }

        /// <summary>
        /// UTC日時をローカル日時に変換してフォーマットする。
        /// </summary>
        /// <param name="utc">UTC の日時</param>
        /// <returns>ローカル時刻をフォーマットした文字列（yyyy/MM/dd HH:mm）</returns>
        private static string FormatLocal(DateTime utc)
        {
            var local = utc.ToLocalTime();
            return $"{local:yyyy/MM/dd HH:mm}";
        }

        /// <summary>
        /// 現在有効なイベントテーマを返します。
        /// </summary>
        private EventThemeType GetActiveEventTheme()
        {
            if (!memoService.GetEventModeEnabled())
            {
                return EventThemeType.None;
            }

            if (memoService.GetEventDebugOverrideEnabled())
            {
                var selectedTheme = memoService.GetEventTheme();
                var overriddenTheme = ParseEventTheme(selectedTheme);
                if (selectedTheme != "0: Auto")
                {
                    return overriddenTheme;
                }
            }

            return ResolveEventTheme(DateTime.Now);
        }

        /// <summary>
        /// 設定値からイベントテーマを解決します。
        /// </summary>
        private static EventThemeType ParseEventTheme(string value)
        {
            switch (value)
            {
                case "1: Halloween":
                    return EventThemeType.Halloween;
                case "2: April Fool":
                    return EventThemeType.AprilFool;
                case "3: Christmas":
                    return EventThemeType.Christmas;
                case "4: New Year":
                    return EventThemeType.NewYear;
                default:
                    return EventThemeType.None;
            }
        }

        /// <summary>
        /// 現在日付からイベントテーマを解決します。
        /// </summary>
        private static EventThemeType ResolveEventTheme(DateTime now)
        {
            if (now.Month == 10 && now.Day == 31)
            {
                return EventThemeType.Halloween;
            }

            if (now.Month == 4 && now.Day == 1)
            {
                return EventThemeType.AprilFool;
            }

            if (now.Month == 12 && (now.Day == 24 || now.Day == 25))
            {
                return EventThemeType.Christmas;
            }

            if (now.Month == 1 && now.Day >= 1 && now.Day <= 3)
            {
                return EventThemeType.NewYear;
            }

            return EventThemeType.None;
        }

        /// <summary>
        /// イベント用のアイコンを返します。
        /// </summary>
        private static string GetEventIcon(EventThemeType eventTheme, MemoEntry entry)
        {
            switch (eventTheme)
            {
                case EventThemeType.Halloween:
                    if (entry == null)
                    {
                        return "🎃";
                    }
                    return "🦇";
                case EventThemeType.AprilFool:
                    if (entry == null)
                    {
                        return "☠☠";
                    }
                    return "👽👽";
                case EventThemeType.Christmas:
                    if (entry == null)
                    {
                        return "🎄";
                    }
                    return "⛄";
                case EventThemeType.NewYear:
                    if (entry == null)
                    {
                        return "🎍";
                    }
                    return "🎊";
                default:
                    if (entry == null)
                    {
                        return "🖊";
                    }

                    return entry.autoCreateEmptyMemo ? "📑" : "📝";
            }
        }

        /// <summary>
        /// アイコン文字列を返します。
        /// 絵文字本体の色変化が見えにくいため、補助記号を含む文字列をそのまま表示します。
        /// </summary>
        private static string GetStyledEventIcon(EventThemeType eventTheme, MemoEntry entry)
        {
            return GetEventIcon(eventTheme, entry);
        }

        /// <summary>
        /// イベント用の基本色を返します。
        /// </summary>
        private static Color GetEventBaseColor(EventThemeType eventTheme, Color fallback)
        {
            switch (eventTheme)
            {
                case EventThemeType.Halloween:
                    return new Color(0.45f, 0.08f, 0.08f);
                case EventThemeType.AprilFool:
                    return new Color(0.45f, 0.85f, 0.18f);
                case EventThemeType.Christmas:
                    return new Color(0.10f, 0.60f, 0.20f);
                case EventThemeType.NewYear:
                    return new Color(0.85f, 0.70f, 0.20f);
                default:
                    return fallback;
            }
        }

        /// <summary>
        /// イベント用のハイライト色を返します。
        /// </summary>
        private static Color GetEventHighlightColor(EventThemeType eventTheme, Color fallback)
        {
            switch (eventTheme)
            {
                case EventThemeType.Halloween:
                    return new Color(0.65f, 0.18f, 0.18f);
                case EventThemeType.AprilFool:
                    return new Color(0.65f, 1.00f, 0.32f);
                case EventThemeType.Christmas:
                    return new Color(0.75f, 0.15f, 0.15f);
                case EventThemeType.NewYear:
                    return new Color(1.00f, 0.85f, 0.35f);
                default:
                    return fallback;
            }
        }

        /// <summary>
        /// イベント用の既定ツールチップ文言を返します。
        /// </summary>
        private static string GetEventTooltipText(EventThemeType eventTheme)
        {
            switch (eventTheme)
            {
                case EventThemeType.Halloween:
                    return "Happy Halloween👻";
                case EventThemeType.AprilFool:
                    return "Cursed memo?";
                case EventThemeType.Christmas:
                    return "Merry Christmas🎅";
                case EventThemeType.NewYear:
                    return "Happy New Year🌅";
                default:
                    return "Add memo";
            }
        }

        /// <summary>
        /// ツールチップ用のテキストを作成する
        /// </summary>
        /// <param name="entry">メモエントリ</param>
        /// <param name="max">最大文字数</param>
        /// <returns>ツールチップ用のテキスト</returns>
        private static string MakeTooltipLine(
            MemoEntry entry,
            BeatSaverMap beatSaverMap,
            MemoService memoService,
            EventThemeType eventTheme,
            int max)
        {
            var toolTipStr = "";
            double weightedLength = 0;
            if (entry == null)
            {
                toolTipStr += GetEventTooltipText(eventTheme);
            }
            else
            {
                if (string.IsNullOrEmpty(entry.memo))
                {
                    toolTipStr += GetEventTooltipText(eventTheme);
                }
                else
                {
                    var oneLine = entry.memo.Replace("\r", "").Replace("\n", " ");

                    var (cutString, isComplete, pWeightedLength) = MemoService.Instance.GetWeightedCutString(oneLine, max);
                    weightedLength = pWeightedLength;
                    if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoPanel.MakeTooltipLine: "
                        + $"original='{oneLine}' cutString='{cutString}' "
                        + $"isComplete={isComplete} weightedLength={weightedLength}");
                    toolTipStr = isComplete ? cutString : cutString + "…";
                }
                // 日時が大体10文字分で1行全角18文字と仮定すると8文字までは日時が同じ行に入る
                if (weightedLength % 18 <= 8)
                {
                    toolTipStr += " (" + FormatLocal(entry.updatedAt) + ")";
                }
                else
                {
                    toolTipStr += "\n(" + FormatLocal(entry.updatedAt) + ")";
                }
            }
            if (beatSaverMap != null)
            {
                var beatSaverLine = "\n";
                if (memoService.GetTooltipShowBsr() && !memoService.GetTooltipShowRating())
                {
                    beatSaverLine += $"[{beatSaverMap.id}]";
                }
                if (memoService.GetTooltipShowRating() && !memoService.GetTooltipShowBsr())
                {
                    beatSaverLine += "["
                        + $"{beatSaverMap.stats.score * 100:0.0}%"
                        + $"⬆{beatSaverMap.stats.upvotes}⬇{beatSaverMap.stats.downvotes}]";
                }
                if (memoService.GetTooltipShowBsr() && memoService.GetTooltipShowRating())
                {
                    beatSaverLine += $"[{beatSaverMap.id} | "
                        + $"{beatSaverMap.stats.score * 100:0.0}%"
                        + $"⬆{beatSaverMap.stats.upvotes}⬇{beatSaverMap.stats.downvotes}]";
                }
                if (memoService.GetTooltipShowBsr() || memoService.GetTooltipShowRating())
                {
                    toolTipStr += beatSaverLine;
                }
            }
            return toolTipStr;
        }

        /// <summary>
        /// カバー画像ホバー時に表示するツールチップ文字列を生成します。
        /// BeatSaver の説明文を最大 <c>maxLines</c> 行・1 行 <c>maxCharsPerLine</c> 文字に切り詰めて返します。
        /// 説明文がない場合や BeatSaver 情報が未取得の場合は空文字を返します。
        /// </summary>
        /// <param name="beatSaverMap">BeatSaver から取得したマップ情報（null 可）</param>
        /// <returns>ツールチップに表示する文字列</returns>
        private static string MakeCoverTooltipText(BeatSaverMap beatSaverMap)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoPanel: Making cover tooltip text");
            if (beatSaverMap == null)
            {
                // BeatSaberの公式マップはBeatSaverに存在しないため、beatSaverMapがnullになる
                if (Plugin.VerboseLogs) Plugin.Log?.Info("MakeCoverTooltipText: beatSaverMap is null");
                return "";
            }

            if (string.IsNullOrWhiteSpace(beatSaverMap.description))
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info("MakeCoverTooltipText: beatSaverMap.description is null or whitespace");
                return "";
            }

            int maxChars = MemoService.Instance.GetCoverHoverMaxChars();

            var text = beatSaverMap.description.Trim();
            if (text.Length <= maxChars)
                return text;

            // maxChars を超える場合は切り詰めて … を付ける
            return text.Substring(0, maxChars) + "…";
        }
    }
}
