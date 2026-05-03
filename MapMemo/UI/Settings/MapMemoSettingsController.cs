using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using MapMemo.Services;
using MapMemo.UI.Common;
using MapMemo.UI.Menu;
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
        /// デバッグ用イベント設定 UI の表示フラグ。true の場合、イベント関連の設定項目を表示します。
        private const bool ShowEventDebugControls = false;

        [UIValue("beatsaverAccessModeOptions")]
        public List<object> BeatSaverAccessModeOptions = new List<object> { "Manual", "Semi-Auto", "Auto" };

        [UIValue("eventThemeOptions")]
        public List<object> EventThemeOptions = new List<object>
        {
            "0: Auto",
            "1: Halloween",
            "2: April Fool",
            "3: Christmas",
            "4: New Year"
        };

        /// <summary> プロパティ変更通知イベント。</summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary> 設定タブ追加済みフラグ。</summary>
        private static bool _tabAdded = false;
        /// <summary> 履歴クリア完了メッセージ表示用の TextMeshProUGUI。</summary>
        [UIComponent("history-clear-message")]
        private TextMeshProUGUI historyClearMessage = null;
        /// <summary> デバッグ用イベント設定コンテナ。</summary>
        [UIComponent("event-debug-controls")]
        private RectTransform eventDebugControls = null;
        /// <summary> メモサービスのインスタンス。</summary>
        private MemoService memoService = MemoService.Instance;

        /// <summary>
        /// MonoBehaviour の初期化時に呼ばれます。
        /// </summary>
        private void Awake()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MapMemoSettingsViewController Awake");
        }

        /// <summary>
        /// BSML 解析後にデバッグ用 UI の表示状態を調整します。
        /// </summary>
        [UIAction("#post-parse")]
        private void OnPostParse()
        {
            ApplyEventDebugControlsVisibility();
        }

        /// <summary>
        /// プロパティ変更を通知します。
        /// </summary>
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// デバッグ用イベント設定 UI の表示状態を反映します。
        /// </summary>
        private void ApplyEventDebugControlsVisibility()
        {
            if (eventDebugControls == null)
            {
                return;
            }

            eventDebugControls.gameObject.SetActive(ShowEventDebugControls);
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
            get => memoService.GetHistoryMaxCount();
            set
            {
                Plugin.Log?.Info($"historyMaxCount: {value}");
                if (memoService.GetHistoryMaxCount() == value) return;

                memoService.SaveHistoryMaxCount(value);
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// サジェストに表示する履歴件数（設定）。UI の変更はここで保存されます。
        /// </summary>
        [UIValue("historyShowCount")]
        public int HistoryShowCount
        {
            get => memoService.GetHistoryShowCount();
            set
            {
                Plugin.Log?.Info($"historyShowCount: {value}");
                if (memoService.GetHistoryShowCount() == value) return;
                memoService.SaveHistoryShowCount(value);
                NotifyPropertyChanged();
            }
        }
        /// <summary>
        /// ツールチップに BSR を表示するか（設定）。UI の変更はここで保存されます。
        /// </summary>
        [UIValue("tooltipShowBsr")]
        public bool TooltipShowBsr
        {
            get => memoService.GetTooltipShowBsr();
            set
            {
                Plugin.Log?.Info($"tooltipShowBsr: {value}");
                if (memoService.GetTooltipShowBsr() == value) return;
                memoService.SaveTooltipShowBsr(value);
                NotifyPropertyChanged();
                MemoPanelController.Instance.Refresh();
            }
        }
        /// <summary>
        /// ツールチップに Rating を表示するか（設定）。UI の変更はここで保存されます。
        /// </summary>
        [UIValue("tooltipShowRating")]
        public bool TooltipShowRating
        {
            get => memoService.GetTooltipShowRating();
            set
            {
                Plugin.Log?.Info($"tooltipShowRating: {value}");
                if (memoService.GetTooltipShowRating() == value) return;
                memoService.SaveTooltipShowRating(value);
                NotifyPropertyChanged();
                MemoPanelController.Instance.Refresh();
            }
        }

        /// <summary>
        /// 設定 UI でカバー画像のホバーヒントに説明文を表示するかが変更されたときに呼ばれます。
        /// </summary>
        [UIValue("coverHoverHint")]
        public bool CoverHoverHint
        {
            get => memoService.GetCoverHoverHint();
            set
            {
                Plugin.Log?.Info($"coverHoverHint: {value}");
                if (memoService.GetCoverHoverHint() == value) return;
                memoService.SaveCoverHoverHint(value);
                NotifyPropertyChanged();
                MemoPanelController.Instance.Refresh();
            }
        }

        /// <summary>
        /// カバー画像ホバーヒントの最大文字数（合計）（設定）。UI の変更はここで保存されます。
        /// </summary>
        [UIValue("coverHoverMaxChars")]
        public int CoverHoverMaxChars
        {
            get => memoService.GetCoverHoverMaxChars();
            set
            {
                if (memoService.GetCoverHoverMaxChars() == value) return;
                memoService.SaveCoverHoverMaxChars(value);
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// プレイ後に空のメモを自動作成するか（設定）。UI の変更はここで保存されます。
        /// </summary>
        [UIValue("autoCreateEmptyMemo")]
        public bool AutoCreateEmptyMemo
        {
            get => memoService.IsAutoCreateEmptyMemo();
            set
            {
                Plugin.Log?.Info($"autoCreateEmptyMemo: {value}");
                if (memoService.IsAutoCreateEmptyMemo() == value) return;
                memoService.SaveAutoCreateEmptyMemo(value);
                NotifyPropertyChanged();
            }
        }
        /// <summary>
        /// BeatSaver へのアクセスモード（設定）。UI の変更はここで保存されます。
        /// </summary>
        [UIValue("beatsaverAccessMode")]
        public string BeatsaverAccessMode
        {
            get => memoService.GetBeatSaverAccessMode();
            set
            {
                Plugin.Log?.Info($"beatsaverAccessMode: {value}");
                if (memoService.GetBeatSaverAccessMode() == value) return;
                memoService.SaveBeatSaverAccessMode(value);
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// イベント表示を有効にするか（設定）。UI の変更はここで保存されます。
        /// </summary>
        [UIValue("eventModeEnabled")]
        public bool EventModeEnabled
        {
            get => memoService.GetEventModeEnabled();
            set
            {
                Plugin.Log?.Info($"eventModeEnabled: {value}");
                if (memoService.GetEventModeEnabled() == value) return;
                memoService.SaveEventModeEnabled(value);
                NotifyPropertyChanged();
                if (MemoPanelController.isInstance()) MemoPanelController.Instance.Refresh();
            }
        }

        /// <summary>
        /// テスト用のイベント上書きを有効にするか（設定）。UI の変更はここで保存されます。
        /// </summary>
        [UIValue("eventDebugOverrideEnabled")]
        public bool EventDebugOverrideEnabled
        {
            get => memoService.GetEventDebugOverrideEnabled();
            set
            {
                Plugin.Log?.Info($"eventDebugOverrideEnabled: {value}");
                if (memoService.GetEventDebugOverrideEnabled() == value) return;
                memoService.SaveEventDebugOverrideEnabled(value);
                NotifyPropertyChanged();
                if (MemoPanelController.isInstance()) MemoPanelController.Instance.Refresh();
            }
        }

        /// <summary>
        /// 選択中のイベント番号（設定）。UI の変更はここで保存されます。
        /// </summary>
        [UIValue("eventTheme")]
        public string EventTheme
        {
            get => memoService.GetEventTheme();
            set
            {
                Plugin.Log?.Info($"eventTheme: {value}");
                if (memoService.GetEventTheme() == value) return;
                memoService.SaveEventTheme(value);
                NotifyPropertyChanged();
                if (MemoPanelController.isInstance()) MemoPanelController.Instance.Refresh();
            }
        }

        /// <summary>
        /// 設定画面から履歴をクリアするアクション。
        /// </summary>
        [UIAction("on-clear-history")]
        private void OnClearHistory()
        {
            memoService.DeleteHistory();
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

        /// <summary>
        /// 設定 UI でツールチップに BSR を表示するかが変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-tooltip-show-bsr-changed")]
        private void OnTooltipShowBsrChanged(bool value)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"OnTooltipShowBsrChanged: {value}");
            TooltipShowBsr = value;
        }

        /// <summary>
        /// 設定 UI でツールチップに Rating を表示するかが変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-tooltip-show-rating-changed")]
        private void OnTooltipShowRatingChanged(bool value)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"OnTooltipShowRatingChanged: {value}");
            TooltipShowRating = value;
        }

        /// <summary>
        /// 設定 UI でカバー画像のホバーヒントに説明文を表示するかが変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-cover-hover-hint-changed")]
        private void OnCoverHoverHintChanged(bool value)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"OnCoverHoverHintChanged: {value}");
            CoverHoverHint = value;
        }

        /// <summary>
        /// 設定 UI でプレイ後に空のメモを作成するかが変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-auto-create-empty-memo-changed")]
        private void OnAutoCreateEmptyMemoChanged(bool value)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"OnAutoCreateEmptyMemoChanged: {value}");
            AutoCreateEmptyMemo = value;
        }
        /// <summary>
        /// 設定 UI で BeatSaver へのアクセスモードが変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-beatsaver-access-mode-changed")]
        private void OnBeatsaverAccessModeChanged(object value)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"OnBeatsaverAccessModeChanged: {value}");
            BeatsaverAccessMode = (string)value;
        }

        /// <summary>
        /// 設定 UI でイベント表示有効フラグが変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-event-mode-enabled-changed")]
        private void OnEventModeEnabledChanged(bool value)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"OnEventModeEnabledChanged: {value}");
            EventModeEnabled = value;
        }

        /// <summary>
        /// 設定 UI でテスト用イベント上書きフラグが変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-event-debug-override-enabled-changed")]
        private void OnEventDebugOverrideEnabledChanged(bool value)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"OnEventDebugOverrideEnabledChanged: {value}");
            EventDebugOverrideEnabled = value;
        }

        /// <summary>
        /// 設定 UI でイベント番号が変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-event-theme-changed")]
        private void OnEventThemeChanged(object value)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"OnEventThemeChanged: {value}");
            EventTheme = (string)value;
        }

        /// <summary>
        /// 設定 UI でカバー画像ホバーヒントの最大文字数が変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-cover-hover-max-chars-changed")]
        private void OnCoverHoverMaxCharsChanged(float value)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"OnCoverHoverMaxCharsChanged: {value}");
            CoverHoverMaxChars = (int)value;
        }
    }
}