using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
// Note: Avoid UnityEngine.UI dependency; use UnityEngine.Canvas explicitly
using HMUI;
using System.Globalization;
using System.IO;
using MapMemo.UI.Menu;
using MapMemo.Core;

namespace MapMemo.UI.Edit
{
    public class MemoEditModalController : BSMLAutomaticViewController
    {
        // 設定値
        private static MapMemo.Core.MemoSettingsManager settings = MapMemo.Core.MemoSettingsManager.Load();
        [UIValue("historyMaxCount")] private int historyMaxCount = settings.HistoryMaxCount;
        [UIValue("historyShowCount")] private int historyShowCount = settings.HistoryShowCount;


        public static MemoEditModalController Instance;

        private string key;
        private string songName;
        private string songAuthor;
        // Shift 状態（true = 小文字モード）
        private bool isShift = false;

        // かなモード状態（true = カタカナ、false = ひらがな）
        private bool isKanaMode = false;

        [UIValue("memo")] private string memo = "";
        [UIComponent("modal")] private ModalView modal;
        [UIComponent("memoText")] public TextMeshProUGUI memoText;
        private string confirmedText = "";
        private string pendingText = "";
        [UIComponent("last-updated")] private TextMeshProUGUI lastUpdated;

        [UIComponent("suggestion-list")] private CustomListTableData suggestionList;
        private SuggestionListController suggestionController;

        // 辞書語リストは DictionaryManager が管理する

        //// ◆画面初期表示関連メソッド Start ◆////

        /// <summary>
        /// モーダルのインスタンスを取得または生成する
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="parent"></param>
        /// <param name="key"></param>
        /// <param name="songName"></param>
        /// <param name="songAuthor"></param>
        /// <returns></returns>
        public static MemoEditModalController GetInstance(
            MemoEntry entry,
            MemoPanelController parent,
            string key,
            string songName,
            string songAuthor)
        {
            if (ReferenceEquals(Instance, null))
            {
                Plugin.Log?.Info("MemoEditModal.GetInstance: creating new modal instance");
                Instance = BeatSaberUI.CreateViewController<MemoEditModalController>();

                Instance.ParseBSML(
                    Utilities.GetResourceContent(
                        typeof(MemoEditModalController).Assembly,
                        "MapMemo.Resources.MemoEdit.bsml"),
                        parent.HostGameObject);
                // 初回のみロード
                DictionaryManager.Load();
                InputHistoryManager.Instance.LoadHistory(Path.Combine("UserData", "MapMemo"), settings.HistoryMaxCount);
            }
            // Instance.parentPanel = parent;
            Instance.key = key;
            Instance.songName = songName;
            Instance.songAuthor = songAuthor;
            // Instance.memoText.maxVisibleLines = 5;
            Instance.memo = entry?.memo ?? "";
            Instance.lastUpdated.text = entry != null ? "Updated:" + MemoEditModalHelper.FormatLocal(entry.updatedAt) : "";
            if (Instance.memoText != null)
            {
                Instance.memoText.richText = true;
                Instance.UpdateMemoText(Instance.memo);
                Instance.confirmedText = Instance.memo;
                Instance.pendingText = "";
            }
            // A〜Z ボタンの見た目を整えるヘルパーを呼び出す
            MemoEditModalHelper.ApplyAlphaButtonCosmetics(Instance.modal, Instance.isShift);
            // サジェストリストを初期化する
            Instance.suggestionController.Clear();
            return Instance;
        }
        // ApplyAlphaButtonCosmetics moved to MemoEditModalHelper.ApplyAlphaButtonCosmetics
        /// <summary>
        /// モーダル表示
        /// </summary>
        /// <param name="parent">親パネルコントローラー</param>
        /// <param name="key">メモのキー</param>
        /// <param name="songName">曲名</param>
        /// <param name="songAuthor">曲の作者</param>
        public static void Show(
            MemoPanelController parent, string key, string songName, string songAuthor)
        {
            // 同期ロードを使って UI スレッドで確実に更新する
            var existing = MemoRepository.Load(key, songName, songAuthor);
            var modalCtrl = MemoEditModalController.GetInstance(existing, parent, key, songName, songAuthor);

            Plugin.Log?.Info("MemoEditModal.Show: reusing existing parsed modal instance");
            // 表示は既にバインド済みの modal を利用して行う
            try
            {
                var modalStatus = ReferenceEquals(modalCtrl.modal, null) ? "modal=null" : "modal!=null";
                var msg = $"MemoEditModal.Show: showing modal {modalStatus}";
                Plugin.Log?.Info(msg);
                modalCtrl.modal?.Show(true, true);
                // 画面の左側半分あたりに表示するように位置調整
                MemoEditModalHelper.RepositionModalToLeftHalf(modalCtrl.modal);
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.Warn($"MemoEditModal.Show: ModalView.Show failed: {ex.Message}; modal may not be visible");
            }
        }

        // 辞書の読み込みと検索は DictionaryManager に委譲

        /// BSMLをパースする
        public void ParseBSML(string bsml, GameObject host)
        {
            BSMLParser.Instance.Parse(bsml, host, this);
            var hostName = host?.name ?? "(null)";
            var modalStatus = ReferenceEquals(modal, null) ? "modal=null" : "modal!=null";
            Plugin.Log?.Info($"MemoEditModal: BSML parsed and attached to host '{hostName}' {modalStatus}");
        }

        [UIAction("#post-parse")]
        private void OnPostParse()
        {
            Plugin.Log?.Info("MemoEditModal: OnPostParse called — setting up pick list");
            suggestionController = new SuggestionListController(suggestionList, historyShowCount);
            suggestionController.SuggestionSelected += (value, subtext) =>
            {
                Plugin.Log?.Info($"選択されたのは: {value}");
                // ログ出力用
                MemoEditModalHelper.IsEmoji(value);

                if (string.IsNullOrEmpty(value)) return;
                AppendSelectedString(value, subtext);
                suggestionController.Clear();
            };

            // Attach emoji click listeners: extracted to a helper for readability
            MemoEditModalHelper.SetupKeyClickListeners(this.modal);
        }

        private void OnEnable()
        {
            // モーダルが有効化されたときに呼ばれる
            Plugin.Log?.Info("MemoEditModal: OnEnable called");

            // A〜Z ボタンのラベルを更新する
            MemoEditModalHelper.UpdateAlphaButtonLabels(this.modal, this.isShift);
        }
        // RepositionModalToLeftHalf moved to MemoEditModalHelper.RepositionModalToLeftHalf

        // Shift 切替時はラベルの差し替えだけ行う（スタイルは既に適用済みの前提）
        // UpdateAlphaButtonLabels moved to MemoEditModalHelper.UpdateAlphaButtonLabels
        //// ◆画面初期表示関連メソッド End ◆////


        private void CommitMemo()
        {
            // 確定処理
            confirmedText += pendingText;

            pendingText = "";
            memo = confirmedText;
            NotifyPropertyChanged("memo");
            if (memoText != null)
            {
                UpdateMemoText(memo);
            }
        }

        private void UpdateMemoText(string memoValue)
        {
            memoText.text = memoValue.Replace("\n", "↲\n");
            memoText.ForceMeshUpdate();
        }

        [UIAction("on-save")]
        public async void OnSave()
        {
            try
            {
                var text = confirmedText + pendingText ?? "";
                //if (text.Length > 256) text = text.Substring(0, 256);
                var entry = new MemoEntry
                {
                    key = key ?? "unknown",
                    songName = songName ?? "unknown",
                    songAuthor = songAuthor ?? "unknown",
                    memo = text
                };
                Plugin.Log?.Info($"MemoEditModal.OnSave: key='{entry.key}' song='{entry.songName}' author='{entry.songAuthor}' len={text.Length}");
                lastUpdated.text = MemoEditModalHelper.FormatLocal(DateTime.UtcNow);

                await MemoRepository.SaveAsync(entry);
                // 表示更新（transform未参照で安全にフォールバック）
                // var parentPanelLocal = this.parentPanel ?? MemoPanelController.instance;

                // 親パネルの反映
                var parentPanelLocal = MemoPanelController.instance;
                // 確定状態にする
                InputHistoryManager.Instance.AddHistory(pendingText);
                CommitMemo();
                await parentPanelLocal.Refresh();
                MapMemo.Plugin.Log?.Info("MemoEditModal.OnSave: refreshing MemoPanelController");

            }
            catch (System.Exception ex)
            {
                MapMemo.Plugin.Log?.Error($"MemoEditModal.OnSave: exception {ex}");
            }
            finally
            {
                // モーダルは閉じずに編集を継続できるようにする
                MapMemo.Plugin.Log?.Info("MemoEditModal.OnSave: keeping modal open after save");
            }
        }

        [UIAction("on-cancel")]
        public void OnCancel()
        {
            if (modal != null)
            {
                modal.Hide(true);
            }
            else
            {
                DismissModal();
            }
        }

        private void DismissModal()
        {
            // 呼び出し側で明示的に閉じたい場合のみ使用。既定では閉じない。
            if (modal != null)
            {
                modal.Hide(true);
            }
            else if (gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }

        // かなキーボードの入力処理
        private void AppendSelectedString(string s, string subText = null)
        {
            pendingText = "";
            memo = confirmedText;
            foreach (var ch in s)
            {
                Append(ch.ToString(), false);
            }
            InputHistoryManager.Instance.AddHistory(s, subText);
            // 確定処理
            CommitMemo();
        }

        public bool Append(string s, bool isSuggestUpdate = true)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (memo == null) memo = "";
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal.Append: add='{s}' len-before={memo.Length}");

            // 未確定文字を削除して確定文字に設定
            confirmedText = memo.Replace(GetPendingText(), "");

            int maxLines = 3;
            int maxCharsPerLine = 20;
            if (s == "\n")
            {
                // 改行の場合、3行を超過しない
                if (isOverMaxLine(confirmedText + pendingText + s, maxLines))
                {
                    return false;
                }
            }

            if (GetLastLineLength(confirmedText + pendingText + s) > maxCharsPerLine)
            {
                // 3文字超過する場合、改行も追加もしない
                if (isOverMaxLine(confirmedText + pendingText + s, maxLines))
                {
                    return false;
                }
                // 最大文字数を超過する場合は強制改行を挿入
                s = "\n" + s;
            }
            // 未確定文字列に追加
            pendingText += s;

            memo = confirmedText + GetPendingText();

            if (isSuggestUpdate)
            {
                UpdateSuggestions();
            }

            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal.Append: len-after={memo.Length}");
            NotifyPropertyChanged("memo");
            if (memoText != null)
            {
                UpdateMemoText(memo);
            }
            return true;
        }

        // Append and immediately commit (used for emoji clicks so they become confirmed instantly)
        // Modified: only auto-commit if there was no pending text before the append
        // public void AppendEmoji(string s)
        // {
        //     if (string.IsNullOrEmpty(s)) return;
        //     // Determine whether there were pending (unconfirmed) characters before appending

        //     var hadPending = !string.IsNullOrEmpty(pendingText);

        //     if (Append(s, false))
        //     {
        //         InputHistoryManager.Instance.AddHistory(s);
        //         if (!hadPending)
        //         {
        //             CommitMemo();
        //             UpdateSuggestions();
        //             if (Plugin.VerboseLogs) Plugin.Log?.Info($"AppendEmoji: auto-committed '{s}'");
        //         }
        //         else
        //         {
        //             UpdateSuggestions();
        //             if (Plugin.VerboseLogs) Plugin.Log?.Info($"AppendEmoji: appended '{s}' without commit (pending present)");
        //         }
        //     }
        // }

        private void ClearSuggestions()
        {
            if (suggestionController != null)
            {
                suggestionController.Clear();
                return;
            }
            suggestionList.Data.Clear();
            suggestionList.TableView.ClearSelection();
            suggestionList.TableView.ReloadData();
        }

        private void UpdateSuggestions()
        {
            if (suggestionController != null)
            {
                suggestionController.UpdateSuggestions(pendingText);
                return;
            }

            // Fallback: clear list
            suggestionList.Data.Clear();
            suggestionList.TableView.ReloadData();
        }


        private int GetLastLineLength(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var lastLine = lines.LastOrDefault() ?? "";
            return lastLine.Length;
        }

        private bool isOverMaxLine(string text, int maxLines)
        {
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.LastOrDefault() == "")
            {
                // 最後が改行で終わっている場合は行数を-1する
                return lines.Length > maxLines;
            }
            return lines.Length + 1 > maxLines;
        }

        private string GetPendingText()
        {
            return "<color=#FFFF00><u>" + pendingText + "</u></color>";
        }

        // FormatLocal and EditLabel moved to MemoEditModalHelper
        [UIAction("on-history-max-count-change")]
        private void OnHistoryMaxCountChange(int value)
        {
            historyMaxCount = value;
            settings.HistoryMaxCount = value;
            settings.Save();
            InputHistoryManager.Instance.SetMaxHistoryCount(value);
            UpdateSuggestions();
        }

        [UIAction("on-history-show-count-change")]
        private void OnHistoryShowCountChange(int value)
        {
            historyShowCount = value;
            settings.HistoryShowCount = value;
            settings.Save();
            UpdateSuggestions();
        }

        [UIAction("on-clear-history")]
        private void OnClearHistory()
        {
            InputHistoryManager.Instance.ClearHistory();
            UpdateSuggestions();
        }

        [UIAction("on-char-space")] private void OnCharSpace() => Append(" ");
        [UIAction("on-char-backspace")]
        private void OnCharBackspace()
        {
            if (pendingText.Length > 0)
            {
                pendingText = RemoveLastTextElement(pendingText);
                memo = confirmedText + GetPendingText();
                NotifyPropertyChanged("memo");
                UpdateMemoText(memo);
                UpdateSuggestions();
                return;
            }

            if (string.IsNullOrEmpty(confirmedText))
            {
                return;
            }

            confirmedText = RemoveLastTextElement(confirmedText);
            memo = confirmedText;
            NotifyPropertyChanged("memo");
            UpdateMemoText(memo);
            UpdateSuggestions();
        }

        private static string RemoveLastTextElement(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var si = new StringInfo(text);
            int count = si.LengthInTextElements;

            if (count <= 1) return string.Empty;

            return si.SubstringByTextElements(0, count - 1);
        }

        [UIAction("on-char-shift")]
        private void OnCharShift()
        {
            // Shift をトグルして A〜Z ボタン表示を切替
            isShift = !isShift;
            MemoEditModalHelper.UpdateAlphaButtonLabels(this.modal, this.isShift);
        }
        [UIAction("on-char-toggle-kana")]
        private void OnCharToggleKana()
        {
            isKanaMode = !isKanaMode;
            MemoEditModalHelper.UpdateKanaModeButtonLabel(this.modal, this.isKanaMode);

        }

        /// <summary>
        /// 確定ボタン押下時の処理
        /// </summary>
        [UIAction("on-char-enter")]
        private void OnCharEnter()
        {
            if (pendingText.Length > 0)
            {
                InputHistoryManager.Instance.AddHistory(pendingText);
                // 未確定文字を確定文字にする
                CommitMemo();
                UpdateSuggestions();
            }
            else
            {
                Append("\n");
                CommitMemo();
            }

        }
    }
}