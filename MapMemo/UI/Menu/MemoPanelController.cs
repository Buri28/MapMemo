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
    public class MemoPanelController : BSMLAutomaticViewController
    {
        // この段階でインスタンスを作るとUnityの管理外のためバインド対象外となる。
        public static MemoPanelController instance;
        // 現在のホストオブジェクト
        public GameObject HostGameObject { get; set; }

        private string Key { get; set; }
        private string SongName { get; set; }
        private string SongAuthor { get; set; }

        [UIComponent("pen-text")] private ClickableText penText;

        public string ResourceName => "MapMemo.Resources.MemoPanel.bsml";

        public static bool isInstance() => !ReferenceEquals(instance, null);


        /// <summary>
        /// 既存の LastInstance を使って表示を更新するユーティリティ
        /// </summary>
        public static MemoPanelController GetInstance(
            MonoBehaviour view, string key, string songName, string songAuthor)
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
                //child.anchoredPosition = new Vector2(2f, -14f);
                //child.anchoredPosition = new Vector2(17f, 28f);
                child.anchoredPosition = new Vector2(14f, 13f);

                var parentRt = view.transform as RectTransform;
                Plugin.Log?.Info($"Parent anchorMin: {parentRt.anchorMin}, anchorMax: {parentRt.anchorMax}, pivot: {parentRt.pivot}, sizeDelta: {parentRt.sizeDelta}");
                Plugin.Log?.Info("MemoPanelController.GetInstance: Created new instance:" + isInstance());
            }

            instance.Key = key;
            instance.SongName = songName;
            instance.SongAuthor = songAuthor;
            instance.HostGameObject = view.gameObject;

            instance.Refresh();
            return instance;
        }
        /// BSMLをパースする
        public void ParseBSML(string bsml, GameObject host)
        {
            Plugin.Log?.Info("MemoPanelController: BSML parsed and attached to host '" + host.name + "'");
            BSMLParser.Instance.Parse(bsml, host, this);
        }
        // public class MyFlowCoordinator : FlowCoordinator
        // {
        //     private MemoPanelController _memoPanel;

        //     protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        //     {
        //         if (!firstActivation) return;



        //         SetTitle("Memo Panel"); // ← これが重要！

        //         _memoPanel = BeatSaberUI.CreateViewController<MemoPanelController>();

        //         var bsmlContent = Utilities.GetResourceContent(
        //             typeof(MemoPanelController).Assembly,
        //             "MapMemo.Resources.MemoPanel.bsml");

        //         _memoPanel.ParseBSML(bsmlContent, _memoPanel.gameObject);
        //         Plugin.Log?.Info("Logging hierarchy after BSML parse:");
        //         LogHierarchy(_memoPanel.transform);
        //         _memoPanel.gameObject.SetActive(true);
        //         var image = _memoPanel.gameObject.AddComponent<UnityEngine.UI.Image>();

        //         var rt = _memoPanel.transform as RectTransform;
        //         rt.anchorMin = new Vector2(0.5f, 0.5f);
        //         rt.anchorMax = new Vector2(0.5f, 0.5f);
        //         rt.pivot = new Vector2(0.5f, 0.5f);
        //         rt.anchoredPosition = new Vector2(0f, -100f);
        //         rt.sizeDelta = new Vector2(300f, 150f);

        //         image.color = UnityEngine.Color.red;

        //         Plugin.Log?.Info($"Child count: {_memoPanel.transform.childCount}");
        //         for (int i = 0; i < _memoPanel.transform.childCount; i++)
        //         {
        //             var child = _memoPanel.transform.GetChild(i);
        //             Plugin.Log?.Info($"Child[{i}] = {child.name}, active={child.gameObject.activeSelf}");
        //         }

        //         ProvideInitialViewControllers(_memoPanel);
        //         // _memoPanel.transform.SetParent(BeatSaberUI.MainFlowCoordinator.transform, false);
        //     }
        //     void LogHierarchy(Transform t, string indent = "")
        //     {
        //         Plugin.Log?.Info($"{indent}- {t.name} (active={t.gameObject.activeSelf})");
        //         for (int i = 0; i < t.childCount; i++)
        //         {
        //             LogHierarchy(t.GetChild(i), indent + "  ");
        //         }
        //     }
        // }


        // /// <summary>
        // /// 初回表示時のセットアップ
        // /// </summary>  
        // protected override async void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        // {
        //     // TODO:本来ここが呼ばれるべきだが呼ばれていない(インスタンスを直接newしているため)
        //     base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        //     if (!firstActivation) return;

        //     MapMemo.Plugin.Log?.Info($"MemoPanelController.DidActivate: firstActivation={firstActivation} addedToHierarchy={addedToHierarchy} screenSystemEnabling={screenSystemEnabling}");
        //     // アクティベートされたインスタンスを設定する
        //     instance = this;
        //     if (HostGameObject == null)
        //     {
        //         HostGameObject = this.transform != null ? this.transform.gameObject : null;
        //     }
        //     await Refresh();
        // }

        /// <summary>
        /// 編集ボタン押下時
        /// </summary>
        [UIAction("on-edit-click")]
        public void OnEditClick()
        {
            MapMemo.Plugin.Log?.Info($"MemoPanel: Edit click key='{Key}' song='{SongName}' author='{SongAuthor}'");
            MemoEditModal.Show(instance, Key ?? "unknown", SongName ?? "", SongAuthor ?? "");
        }

        /// <summary>
        /// ホバーヒント設定ユーティリティ
        /// </summary>
        /// <param name="go"></param>
        /// <param name="hint"></param>
        public void SetHoverHint(GameObject go, string hint)
        {
            // HoverHint が無ければ追加
            var hover = go.GetComponent<HMUI.HoverHint>();
            if (hover == null)
                hover = go.AddComponent<HMUI.HoverHint>();

            hover.text = hint;
        }
        /// <summary>
        /// 表示内容の更新
        /// </summary>
        public Task Refresh()
        {
            Plugin.Log?.Info($"MemoPanel: Refresh called for key='{Key}' song='{SongName}' author='{SongAuthor}'");
            // 同期ロードを使って確実に現在の Key に紐づくデータを取得する
            var entry = MemoRepository.Load(Key, SongName, SongAuthor);

            var parentLayout = penText.transform.parent.GetComponent<HorizontalLayoutGroup>();
            if (parentLayout != null)
            {
                parentLayout.childForceExpandWidth = false;
                parentLayout.childControlWidth = true;
            }

            var layout = penText.GetComponent<LayoutElement>();
            if (layout == null)
                layout = penText.gameObject.AddComponent<LayoutElement>();

            layout.preferredWidth = 10f; // 幅を250に制限
            layout.flexibleWidth = 0f;    // 自動伸縮を無効に
            if (entry == null)
            {
                MapMemo.Plugin.Log?.Info("MemoPanel: No memo entry found for key='" + Key + "'");
                penText.color = Color.cyan;
                penText.faceColor = Color.cyan;
                penText.HighlightColor = Color.green;
                penText.text = "　🖊";
                penText.fontStyle = FontStyles.Bold;
                SetHoverHint(penText.gameObject, "Add Memo");
            }
            else
            {
                MapMemo.Plugin.Log?.Info("MemoPanel: Memo entry found for key='" + Key + "'");

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
                    colors.disabledColor = Color.gray; // お好みで
                    button.colors = colors;
                    button.transition = Selectable.Transition.None;
                }

                SetHoverHint(penText.gameObject, MakeTooltipLine(entry.memo, 30) + " (" + FormatLocal(entry.updatedAt) + ")");
            }

            return Task.CompletedTask;
        }

        private static string MakeSummary(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Replace("\n", " ");
            return text.Length <= max ? text : text.Substring(0, max) + "…";
        }

        private static string FormatLocal(DateTime utc)
        {
            var local = utc.ToLocalTime();
            return $"{local:yyyy/MM/dd HH:mm}";
        }

        private static string MakeTooltipLine(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var oneLine = text.Replace("\r", "").Replace("\n", " ");
            return oneLine.Length <= max ? oneLine : oneLine.Substring(0, max) + "…";
        }
    }
}