using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using System.Linq;
using BeatSaberMarkupLanguage.Components;

namespace MapMemo.UI
{
    [HotReload]
    public class MemoPanelController : BSMLAutomaticViewController
    {
        // シングルトンインスタンス
        public static MemoPanelController instance { get; internal set; }
        // 現在のホストオブジェクト
        public GameObject HostGameObject { get; set; }
        public string Key { get; set; }
        public string SongName { get; set; }
        public string SongAuthor { get; set; }

        [UIComponent("pen-text")]
        private TMPro.TextMeshProUGUI penText;
        [UIValue("updated-local")] private string updatedLocal = "";

        public string ResourceName => "MapMemo.Resources.MemoPanel.bsml";

        /// <summary>
        /// 既存の LastInstance を使って表示を更新するユーティリティ
        /// </summary>
        public static MemoPanelController GetRefreshViewInstance(
            string key, string songName, string songAuthor)
        {
            instance.Key = key;
            instance.SongName = songName;
            instance.SongAuthor = songAuthor;

            instance.Refresh();
            return instance;
        }

        /// <summary>
        /// 初回表示時のセットアップ
        /// </summary>  
        protected override async void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (!firstActivation) return;

            MapMemo.Plugin.Log?.Info($"MemoPanelController.DidActivate: firstActivation={firstActivation} addedToHierarchy={addedToHierarchy} screenSystemEnabling={screenSystemEnabling}");
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
            var parentCtrl = this ?? transform?.GetComponentInParent<MemoPanelController>() ?? instance;
            if (parentCtrl == null)
            {
                MapMemo.Plugin.Log?.Warn("MemoPanel: OnEditClick parent controller is null; proceeding without parent");
            }
            MemoEditModal.Show(parentCtrl, Key ?? "unknown", SongName ?? "", SongAuthor ?? "");
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
                MapMemo.Plugin.Log?.Warn("MemoPanel: No memo entry found for key='" + Key + "'");
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
            NotifyPropertyChanged("pen-text");
            NotifyPropertyChanged("updated-local");
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

        // Save ボタン押下時に親パネルの表示上の更新日時を更新するための公開メソッド
        public void SetUpdatedLocal(DateTime utc)
        {
            try
            {
                updatedLocal = "Updated: " + FormatLocal(utc);
                NotifyPropertyChanged("updated-local");
            }
            catch { }
        }

        private static string MakeTooltipLine(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var oneLine = text.Replace("\r", "").Replace("\n", " ");
            return oneLine.Length <= max ? oneLine : oneLine.Substring(0, max) + "…";
        }
    }
}