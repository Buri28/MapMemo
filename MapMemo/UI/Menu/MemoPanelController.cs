using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using System.Linq;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using UnityEngine.UI;
using MapMemo.UI.Edit;
using MapMemo.Core;

namespace MapMemo.UI.Menu
{
    [HotReload]
    /// <summary>
    /// メモパネルのコントローラー。メニューのペンアイコン表示と更新を行います。
    /// </summary>
    public class MemoPanelController : BSMLAutomaticViewController
    {
        // この段階でインスタンスを作るとUnityの管理外のためバインド対象外となる。
        public static MemoPanelController instance;
        /// <summary>
        /// ホストとなる GameObject（バインド対象）
        /// </summary>
        public GameObject HostGameObject { get; set; }
        // 現在のレベルコンテキスト
        private LevelContext levelContext;
        // ペンアイコンテキスト
        [UIComponent("pen-text")] private ClickableText penText;
        // BSMLリソース名
        public string ResourceName => "MapMemo.Resources.MemoPanel.bsml";

        /// <summary>
        /// インスタンスが存在するかどうかを判定します。
        /// </summary>
        public static bool isInstance() => !ReferenceEquals(instance, null);

        /// <summary>
        /// 既存の LastInstance を使って表示を更新するユーティリティ
        /// </summary>
        public static MemoPanelController GetInstance(
            MonoBehaviour view, LevelContext levelContext)
        {
            if (!isInstance())
            {
                instance = BeatSaberUI.CreateViewController<MemoPanelController>();

                Plugin.Log?.Info($"instance.gameObject = {instance?.gameObject}");

                // 親に追加（ここでは view は既存の ViewController）
                instance.transform.SetParent(view.transform, false);

                var bsmlContent = Utilities.GetResourceContent(
                     typeof(MemoPanelController).Assembly,
                     "MapMemo.Resources.MemoPanel.bsml");
                instance.ParseBSML(bsmlContent, instance.gameObject);

                // 表示を確実にする
                instance.gameObject.SetActive(true);

                // 子の位置とサイズを親に合わせて調整
                var child = instance.transform.GetChild(0) as RectTransform;
                child.anchorMin = new Vector2(0f, 1f);
                child.anchorMax = new Vector2(1f, 1f);
                child.pivot = new Vector2(0.5f, 1f);
                child.anchoredPosition = Vector2.zero;
                child.sizeDelta = new Vector2(0f, 56f); // 親と同じ高さに

                // ペンパネルの位置調整
                //child.anchoredPosition = new Vector2(2f, -14f);　//　下の方
                //child.anchoredPosition = new Vector2(17f, 28f);　//　上の方
                child.anchoredPosition = new Vector2(14f, 13f); // 中央寄り

                var parentRt = view.transform as RectTransform;
                Plugin.Log?.Info($"Parent anchorMin: {parentRt.anchorMin}, anchorMax: {parentRt.anchorMax}, pivot: {parentRt.pivot}, sizeDelta: {parentRt.sizeDelta}");
                Plugin.Log?.Info("MemoPanelController.GetInstance: Created new instance:" + isInstance());
            }

            instance.levelContext = levelContext;
            instance.HostGameObject = view.gameObject;

            instance.Refresh();
            return instance;
        }

        /// <summary>
        /// BSMLを解析してホストにアタッチする
        /// </summary>
        public void ParseBSML(string bsml, GameObject host)
        {
            Plugin.Log?.Info("MemoPanelController: BSML parsed and attached to host '" + host.name + "'");
            BSMLParser.Instance.Parse(bsml, host, this);
        }

        /// <summary>
        /// 編集ボタン押下時の処理。エディットモーダルを表示します。
        /// </summary>
        [UIAction("on-edit-click")]
        public void OnEditClick()
        {
            MapMemo.Plugin.Log?.Info($"MemoPanel: Edit click key='{levelContext.GetLevelId()}' song='{levelContext.GetSongName()}' author='{levelContext.GetSongAuthor()}'");
            MemoEditModalController.Show(instance, levelContext);
        }

        /// <summary>
        /// 指定した GameObject にホバーヒントを設定します。
        /// 必要なら HoverHint コンポーネントを追加します。
        /// </summary>
        /// <param name="go">ホバーヒントを設定する対象の GameObject</param>
        /// <param name="hint">表示するホバーテキスト</param>
        public void SetHoverHint(GameObject go, string hint)
        {
            // HoverHint が無ければ追加
            var hover = go.GetComponent<HMUI.HoverHint>();
            if (hover == null)
                hover = go.AddComponent<HMUI.HoverHint>();

            hover.text = hint;
        }

        /// <summary>
        /// 表示内容を更新します。現在の LevelContext に紐づくメモを読み込み、ペンアイコンとツールチップを更新します。
        /// </summary>
        public Task Refresh()
        {
            Plugin.Log?.Info($"MemoPanel: Refresh called for key='{levelContext.GetLevelId()}' song='{levelContext.GetSongName()}' author='{levelContext.GetSongAuthor()}'");
            // 同期ロードを使って確実に現在の Key に紐づくデータを取得する
            var entry = MemoRepository.Load(levelContext.GetLevelId(), levelContext.GetSongName(), levelContext.GetSongAuthor());

            var parentLayout = penText.transform.parent.GetComponent<HorizontalLayoutGroup>();
            if (parentLayout != null)
            {
                parentLayout.childForceExpandWidth = false;
                parentLayout.childControlWidth = true;
            }

            var layout = penText.GetComponent<LayoutElement>();
            if (layout == null)
                layout = penText.gameObject.AddComponent<LayoutElement>();

            layout.preferredWidth = 10f; // 幅を制限
            layout.flexibleWidth = 0f;    // 自動伸縮を無効に
            if (entry == null)
            {
                MapMemo.Plugin.Log?.Info("MemoPanel: No memo entry found for key='" + levelContext.GetLevelId() + "'");
                penText.color = Color.cyan;
                penText.faceColor = Color.cyan;
                penText.HighlightColor = Color.green;
                penText.text = "　🖊";
                penText.fontStyle = FontStyles.Bold;
                SetHoverHint(penText.gameObject, "Add Memo");
            }
            else
            {
                MapMemo.Plugin.Log?.Info("MemoPanel: Memo entry found for key='" + levelContext.GetLevelId() + "'");

                penText.text = "　📝";
                penText.color = Color.yellow;
                penText.outlineColor = Color.white;
                penText.faceColor = Color.yellow;
                penText.HighlightColor = Color.green;
                penText.fontStyle = FontStyles.Bold;

                var button = penText.GetComponentInParent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    var colors = button.colors;
                    colors.normalColor = Color.yellow;
                    colors.highlightedColor = Color.yellow;
                    colors.pressedColor = Color.yellow;
                    colors.selectedColor = Color.yellow;
                    colors.disabledColor = Color.gray;
                    button.colors = colors;
                    button.transition = Selectable.Transition.None;
                }

                SetHoverHint(penText.gameObject, MakeTooltipLine(entry.memo, 30) + " (" + FormatLocal(entry.updatedAt) + ")");
            }

            return Task.CompletedTask;
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
        /// ツールチップ用のテキストを作成する
        /// </summary>
        private static string MakeTooltipLine(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var oneLine = text.Replace("\r", "").Replace("\n", " ");
            return oneLine.Length <= max ? oneLine : oneLine.Substring(0, max) + "…";
        }
    }
}