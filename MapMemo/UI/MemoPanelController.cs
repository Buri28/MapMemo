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

namespace MapMemo.UI
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

        [UIComponent("pen-text")] private TMPro.TextMeshProUGUI penText;

        public string ResourceName => "MapMemo.Resources.MemoPanel.bsml";

        public static bool isInstance() => !ReferenceEquals(instance, null);

        /// <summary>
        /// 既存の LastInstance を使って表示を更新するユーティリティ
        /// </summary>
        public static MemoPanelController GetInstance(
            StandardLevelDetailView view, string key, string songName, string songAuthor)
        {
            if (!isInstance())
            {
                instance = BeatSaberUI.CreateViewController<MemoPanelController>();
                var bsmlContent = Utilities.GetResourceContent(
                    typeof(MemoPanelController).Assembly,
                    "MapMemo.Resources.MemoPanel.bsml");
                instance.ParseBSML(bsmlContent, view.gameObject);

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

        /// <summary>
        /// 初回表示時のセットアップ
        /// </summary>  
        protected override async void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            // TODO:本来ここが呼ばれるべきだが呼ばれていない(インスタンスを直接newしているため)
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (!firstActivation) return;

            MapMemo.Plugin.Log?.Info($"MemoPanelController.DidActivate: firstActivation={firstActivation} addedToHierarchy={addedToHierarchy} screenSystemEnabling={screenSystemEnabling}");
            // アクティベートされたインスタンスを設定する
            instance = this;
            if (HostGameObject == null)
            {
                HostGameObject = this.transform != null ? this.transform.gameObject : null;
            }
            await Refresh();
        }

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
            // 同期ロードを使って確実に現在の Key に紐づくデータを取得する
            var entry = MemoRepository.Load(Key, SongName, SongAuthor);

            if (entry == null)
            {
                MapMemo.Plugin.Log?.Info("MemoPanel: No memo entry found for key='" + Key + "'");
                penText.color = Color.white;
                penText.text = " 🖊　";
                penText.alpha = 0.5f;

                SetHoverHint(penText.gameObject, "メモを追加");

            }
            else
            {
                MapMemo.Plugin.Log?.Info("MemoPanel: Memo entry found for key='" + Key + "'");

                penText.text = " 📝　";
                penText.color = Color.yellow;
                penText.fontStyle = FontStyles.Bold;

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