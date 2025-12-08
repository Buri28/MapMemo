using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MapMemo.UI
{
    [HotReload]
    public class MemoPanelController : BSMLAutomaticViewController
    {
        public static MemoPanelController LastInstance { get; internal set; }
        public GameObject HostGameObject { get; set; }
        public string Key { get; set; }
        public string SongName { get; set; }
        public string SongAuthor { get; set; }

        [UIValue("memo-summary")] private string memoSummary = "";
        [UIValue("updated-local")] private string updatedLocal = "";
        [UIValue("tooltip-line")] private string tooltipLine = "";

        public string ResourceName => "MapMemo.Resources.MemoPanel.bsml";



        protected override async void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            MapMemo.Plugin.Log?.Info($"MemoPanelController.DidActivate: firstActivation={firstActivation} addedToHierarchy={addedToHierarchy} screenSystemEnabling={screenSystemEnabling}");
            LastInstance = this;
            if (HostGameObject == null)
            {
                HostGameObject = this.transform != null ? this.transform.gameObject : null;
            }

            await RefreshAsync();
            MapMemo.Plugin.Log?.Info($"MemoPanelController.DidActivate: RefreshAsync completed; memoSummary='{(memoSummary ?? "")}' updatedLocal='{(updatedLocal ?? "")}'");
        }

        [UIAction("on-edit-click")]
        public void OnEditClick()
        {
            MapMemo.Plugin.Log?.Info($"MemoPanel: Edit click key='{Key}' song='{SongName}' author='{SongAuthor}'");
            var parentCtrl = this ?? transform?.GetComponentInParent<MemoPanelController>() ?? LastInstance;
            if (parentCtrl == null)
            {
                MapMemo.Plugin.Log?.Warn("MemoPanel: OnEditClick parent controller is null; proceeding without parent");
            }
            MemoEditModal.Show(parentCtrl, Key ?? "unknown", SongName ?? "", SongAuthor ?? "");
        }

        public async Task RefreshAsync()
        {
            // 同期ロードを使って確実に現在の Key に紐づくデータを取得する
            var entry = MemoRepository.Load(Key, SongName, SongAuthor);

            if (entry == null)
            {
                memoSummary = "メモなし";
                updatedLocal = "";
                tooltipLine = "";
            }
            else
            {
                memoSummary = MakeSummary(entry.memo, 30);
                updatedLocal = FormatLocal(entry.updatedAt);
                tooltipLine = MakeTooltipLine(entry.memo, 30);
            }

            NotifyPropertyChanged("memo-summary");
            NotifyPropertyChanged("updated-local");
            NotifyPropertyChanged("tooltip-line");
            await Task.CompletedTask;
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