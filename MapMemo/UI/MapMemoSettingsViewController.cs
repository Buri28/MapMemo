using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using UnityEngine;

namespace MapMemo.UI
{
    public class MapMemoSettingsViewController : MonoBehaviour, INotifyPropertyChanged
    {
        private SettingsManager settings = null;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static bool _tabAdded = false;
        private IEnumerator Start()
        {
            Plugin.Log?.Info("MapMemoSettingsViewController Start");
            if (_tabAdded) yield break;
            // ModsタブにMapMemo設定画面を追加
            yield return new WaitUntil(() => GameplaySetup.Instance != null);

            GameplaySetup.Instance.AddTab(
                "Map Memo",
                "MapMemo.Resources.MapMemoSettings.bsml",
                this,
                MenuType.Solo | MenuType.Online);
            settings = SettingsManager.Load();
            _tabAdded = true;
        }

        // 整数フォーマッタ
        [UIAction("FormatInt")] private string FormatInt(float value) => ((int)value).ToString();

        [UIValue("historyMaxCount")]
        public int HistoryMaxCount
        {
            get => settings.HistoryMaxCount;
            set
            {
                Plugin.Log?.Info($"historyMaxCount: {value}");
                if (settings.HistoryMaxCount == value) return;
                settings.HistoryMaxCount = value;
                settings.Save();
                NotifyPropertyChanged();
            }
        }

        [UIValue("historyShowCount")]
        public int HistoryShowCount
        {
            get => settings.HistoryShowCount;
            set
            {
                Plugin.Log?.Info($"historyShowCount: {value}");
                if (settings.HistoryShowCount == value) return;
                settings.HistoryShowCount = value;
                settings.Save();
                NotifyPropertyChanged();
            }
        }

        [UIAction("on-clear-history")]
        private void OnClearHistory()
        {
            InputHistoryManager.ClearHistoryStatic();
        }

        [UIAction("on-history-max-count-changed")]
        private void OnHistoryMaxCountChange(float value)
        {
            Plugin.Log?.Info($"OnHistoryMaxCountChange: {value}");
            HistoryMaxCount = (int)value;
        }

        [UIAction("on-history-show-count-changed")]
        private void OnHistoryShowCountChange(float value)
        {
            Plugin.Log?.Info($"OnHistoryShowCountChange: {value}");
            HistoryShowCount = (int)value;
        }
    }
}
