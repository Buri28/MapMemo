using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using MapMemo.Core;
using MapMemo.UI.Common;
using MapMemo.UI.Edit;
using TMPro;
using UnityEngine;

namespace MapMemo.UI.Settings
{
    /// <summary>
    /// MapMemo の設定画面を提供するコントローラー。
    /// GameplaySetup のタブに BSML を追加します。
    /// </summary>
    public class MapMemoSettingsController : MonoBehaviour, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private static bool _tabAdded = false;

        [UIComponent("history-clear-message")]
        private TextMeshProUGUI historyClearMessage;

        /// <summary>
        /// プロパティ変更を通知します。
        /// </summary>
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 起動時に一度だけ呼ばれ、Mods タブに設定画面を追加します。
        /// </summary>
        private IEnumerator Start()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MapMemoSettingsViewController Start");
            if (_tabAdded) yield break;
            // ModsタブにMapMemo設定画面を追加
            yield return new WaitUntil(() => GameplaySetup.Instance != null);

            GameplaySetup.Instance.AddTab(
                "Map Memo",
                "MapMemo.Resources.MapMemoSettings.bsml",
                this,
                MenuType.Solo | MenuType.Online);
            _tabAdded = true;
        }

        // 整数フォーマッタ
        /// <summary>
        /// UI バインド用の整数フォーマッタ。
        /// </summary>
        [UIAction("FormatInt")] private string FormatInt(float value) => ((int)value).ToString();

        /// <summary>
        /// 履歴の最大保存件数（設定）。UI からの変更はここで保存されます。
        /// </summary>
        [UIValue("historyMaxCount")]
        public int HistoryMaxCount
        {
            get => MemoSettingsManager.Instance.HistoryMaxCount;
            set
            {
                Plugin.Log?.Info($"historyMaxCount: {value}");
                if (MemoSettingsManager.Instance.HistoryMaxCount == value) return;
                MemoSettingsManager.Instance.HistoryMaxCount = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// サジェストに表示する履歴件数（設定）。UI の変更はここで保存されます。
        /// </summary>
        [UIValue("historyShowCount")]
        public int HistoryShowCount
        {
            get => MemoSettingsManager.Instance.HistoryShowCount;
            set
            {
                Plugin.Log?.Info($"historyShowCount: {value}");
                if (MemoSettingsManager.Instance.HistoryShowCount == value) return;
                MemoSettingsManager.Instance.HistoryShowCount = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// 設定画面から履歴をクリアするアクション。
        /// </summary>
        [UIAction("on-clear-history")]
        private void OnClearHistory()
        {
            InputHistoryManager.DeleteHistory();
            UIHelper.Instance.ShowTemporaryMessage(historyClearMessage,
                "<color=#FF0000>History cleared.</color>");
        }

        /// <summary>
        /// 設定 UI で履歴最大件数が変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-history-max-count-changed")]
        private void OnHistoryMaxCountChange(float value)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"OnHistoryMaxCountChange: {value}");
            HistoryMaxCount = (int)value;
        }

        /// <summary>
        /// 設定 UI で履歴表示件数が変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-history-show-count-changed")]
        private void OnHistoryShowCountChange(float value)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"OnHistoryShowCountChange: {value}");
            HistoryShowCount = (int)value;
        }
    }
}
