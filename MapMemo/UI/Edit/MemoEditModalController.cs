using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Linq;
using UnityEngine;
using TMPro;
// Note: Avoid UnityEngine.UI dependency; use UnityEngine.Canvas explicitly
using HMUI;
using System.Globalization;
using System.IO;
using MapMemo.UI.Menu;
using MapMemo.Core;
using MapMemo.UI.Common;

namespace MapMemo.UI.Edit
{
    /// <summary>
    /// メモ編集用のモーダルコントローラー。BSML のバインドとユーザー入力処理、サジェスト表示を担当します。
    /// </summary>
    public class MemoEditModalController : BSMLAutomaticViewController
    {
        // 設定値
        private static MapMemo.Core.MemoSettingsManager settings = MapMemo.Core.MemoSettingsManager.Load();
        [UIValue("historyMaxCount")] private int historyMaxCount = settings.HistoryMaxCount;
        [UIValue("historyShowCount")] private int historyShowCount = settings.HistoryShowCount;

        // モーダルのシングルトンインスタンス
        public static MemoEditModalController Instance;

        // Shift 状態（true = 小文字モード）
        public bool isShift { get; private set; } = false;

        // かなモード状態（true = カタカナ、false = ひらがな）
        public bool isKanaMode { get; private set; } = false;

        [UIValue("memo")] private string memo = "";
        [UIComponent("modal")] private ModalView modal;
        [UIComponent("memoText")] public TextMeshProUGUI memoText;
        private string confirmedText = "";
        private string pendingText = "";
        [UIComponent("last-updated")] private TextMeshProUGUI lastUpdated;

        [UIComponent("suggestion-list")] private CustomListTableData suggestionList;
        private SuggestionListController suggestionController;
        private InputKeyController keyController;

        private LevelContext levelContext;

        [UIComponent("message")]
        private TextMeshProUGUI message;

        // 辞書語リストは DictionaryManager が管理する

        //// ◆画面初期表示関連メソッド Start ◆////

        /// <summary>
        /// モーダルのインスタンスを取得または生成する
        /// </summary>
        /// <param name="existingMemoInfo"></param>
        /// <param name="parent"></param>
        /// <param name="key"></param>
        /// <param name="songName"></param>
        /// <param name="songAuthor"></param>
        /// <returns></returns>
        public static MemoEditModalController GetInstance(
            MemoEntry existingMemoInfo,
            MemoPanelController parent,
            LevelContext levelContext)
        {
            if (ReferenceEquals(Instance, null))
            {
                Plugin.Log?.Info("MemoEditModal.GetInstance: creating new modal instance");
                // インスタンスを生成
                Instance = BeatSaberUI.CreateViewController<MemoEditModalController>();

                Instance.ParseBSML(
                    Utilities.GetResourceContent(
                        typeof(MemoEditModalController).Assembly,
                        "MapMemo.Resources.MemoEdit.bsml"),
                        parent.HostGameObject);
                // 初回のみ辞書ファイルと入力履歴ファイルを読み込み
                DictionaryManager.Instance.Load(Path.Combine("UserData", "MapMemo"));
                InputHistoryManager.Instance.LoadHistory(Path.Combine("UserData", "MapMemo"), settings.HistoryMaxCount);
                // キーバインド設定を読み込む (UserData に resource をコピーしてからロード)
                InputKeyManager.Instance.Load(Path.Combine("UserData", "MapMemo"));
            }
            // 必要なパラメータを設定 (LevelContext を使用)
            Instance.memo = existingMemoInfo?.memo ?? "";
            Instance.lastUpdated.text = existingMemoInfo != null ? "Updated:" + MemoEditModalHelper.FormatLocal(existingMemoInfo.updatedAt) : "";
            Instance.levelContext = levelContext;

            // メモ内容を初期化
            if (Instance.memoText != null)
            {
                Instance.memoText.richText = true;
                Instance.UpdateMemoText(Instance.memo);
                Instance.confirmedText = Instance.memo;
                Instance.pendingText = "";
            }

            // ボタンの見た目を整えるヘルパーを呼び出す
            Instance.keyController.InitializeAppearance(Instance.isShift);
            // サジェストリストを初期化する
            Instance.suggestionController.Clear();

            // 使える絵文字をログ出力
            // MemoEditModalHelper.WriteDebugLog("MemoEditModal.GetInstance: Available emojis:");

            return Instance;
        }

        // ApplyAlphaButtonCosmetics moved to MemoEditModalHelper.ApplyAlphaButtonCosmetics
        /// <summary>
        /// モーダルを表示します。指定の LevelContext に基づき該当メモをロードして表示します。
        /// </summary>
        /// <param name="parent">親パネルコントローラー</param>
        /// <param name="levelContext">メモのキー</param>
        public static void Show(
            MemoPanelController parent, LevelContext levelContext)
        {
            // 既存のメモを読み込む (LevelContext を使用してキー/曲情報を解決)
            var key = levelContext?.GetLevelId() ?? "unknown";
            var songName = levelContext?.GetSongName() ?? "unknown";
            var songAuthor = levelContext?.GetSongAuthor() ?? "unknown";

            var existingMemoInfo = MemoRepository.Load(key, songName, songAuthor);
            var modalCtrl = MemoEditModalController.GetInstance(
                existingMemoInfo, parent, levelContext);

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

            keyController = new InputKeyController(
                modal.gameObject.GetComponentsInChildren<ClickableText>(true),
                modal.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true)
            );

            suggestionController = new SuggestionListController(suggestionList, historyShowCount);
            suggestionController.SuggestionSelected += (value, subtext) =>
            {
                Plugin.Log?.Info($"選択されたのは: {value}");

                if (string.IsNullOrEmpty(value)) return;
                AppendSelectedString(value, subtext);
                suggestionController.Clear();
            };

            // ボタンのクリックリスナーを設定
            keyController.SetupKeyClickListeners();
        }

        /// <summary>
        /// モーダルが有効化されたときに呼ばれます。ボタンラベルの更新等を行います。
        /// </summary>
        private void OnEnable()
        {
            // モーダルが有効化されたときに呼ばれる
            Plugin.Log?.Info("MemoEditModal: OnEnable called");

            // ボタンのラベルを更新する
            keyController.UpdateAlphaButtonLabels(isShift);
        }
        // RepositionModalToLeftHalf moved to MemoEditModalHelper.RepositionModalToLeftHalf

        // Shift 切替時はラベルの差し替えだけ行う（スタイルは既に適用済みの前提）
        // UpdateAlphaButtonLabels moved to MemoEditModalHelper.UpdateAlphaButtonLabels
        //// ◆画面初期表示関連メソッド End ◆////

        /// 確定処理
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
                    key = levelContext.GetLevelId(),
                    songName = levelContext.GetSongName(),
                    songAuthor = levelContext.GetSongAuthor(),
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

                UIHelper.Instance.ShowTemporaryMessage(message,
                    "<color=#00FFFF>Memo Saved.</color>");
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
        /// <summary>
        /// キャンセル時の処理（モーダルを非表示にする）。
        /// </summary>
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

        /// <summary>
        /// モーダルを閉じる内部ユーティリティ。
        /// </summary>
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

        /// <summary>
        /// かなキーボードやサジェスト選択から受け取った文字列を未確定入力として追加します。
        /// </summary>
        private void AppendSelectedString(string s, string subText = null)
        {
            pendingText = "";
            memo = confirmedText;

            if (string.IsNullOrEmpty(s)) return;

            var iter = System.Globalization.StringInfo.GetTextElementEnumerator(s);
            while (iter.MoveNext())
            {
                Append(iter.GetTextElement(), false);
            }

            InputHistoryManager.Instance.AddHistory(s, subText);
            // 確定処理
            CommitMemo();
        }

        /// <summary>
        /// 指定した文字列を現在の未確定テキストに追加します。入力制限（行数/文字数）を超えないように制御します。
        /// </summary>
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

        /// <summary>
        /// サジェストリストをクリアします。
        /// </summary>
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

        /// <summary>
        /// サジェスト候補を更新します。
        /// </summary>
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


        /// <summary>
        /// 指定したテキストの最終行の長さをテキスト要素単位で計算して返します（ASCII 分割ルール適用）。
        /// </summary>
        private double GetLastLineLength(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var lastLine = lines.LastOrDefault() ?? "";

            var enumerator = StringInfo.GetTextElementEnumerator(lastLine);
            double length = 0.0;
            while (enumerator.MoveNext())
            {
                var elem = enumerator.GetTextElement();
                // ASCII のアルファベットのみを半分扱いにする
                length += IsAsciiAlphabet(elem) ? 0.5 : 1.0;
            }

            return length;
        }

        /// <summary>
        /// 文字要素が ASCII アルファベットか判定します。
        /// </summary>
        private static bool IsAsciiAlphabet(string textElement)
        {
            if (string.IsNullOrEmpty(textElement)) return false;
            if (textElement.Length == 1)
            {
                var c = textElement[0];
                return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
            }
            // 複数文字（結合文字など）はアルファベット扱いしない
            return false;
        }

        /// <summary>
        /// テキストが最大行数を超えるかどうかを判定します。
        /// </summary>
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

        /// <summary>
        /// 現在の未確定テキストを装飾して返します。
        /// </summary>
        private string GetPendingText()
        {
            return "<color=#FFFF00><u>" + pendingText + "</u></color>";
        }

        // FormatLocal and EditLabel moved to MemoEditModalHelper
        /// <summary>
        /// 履歴の最大件数を UI から変更されたときに呼ばれます。
        /// 設定を保存し、InputHistoryManager に反映します。
        /// </summary>
        [UIAction("on-history-max-count-change")]
        private void OnHistoryMaxCountChange(int value)
        {
            historyMaxCount = value;
            settings.HistoryMaxCount = value;
            settings.Save();
            InputHistoryManager.Instance.SetMaxHistoryCount(value);
            UpdateSuggestions();
        }

        /// <summary>
        /// 表示する履歴件数（候補数）を UI から変更されたときに呼ばれます。
        /// </summary>
        [UIAction("on-history-show-count-change")]
        private void OnHistoryShowCountChange(int value)
        {
            historyShowCount = value;
            settings.HistoryShowCount = value;
            settings.Save();
            UpdateSuggestions();
        }

        /// <summary>
        /// 履歴をクリアする UI 操作のハンドラ。
        /// </summary>
        [UIAction("on-clear-history")]
        private void OnClearHistory()
        {
            InputHistoryManager.Instance.ClearHistory();
            UpdateSuggestions();
        }

        /// <summary>
        /// スペース入力ハンドラ（BSML action）。スペースを追加します。
        /// </summary>
        [UIAction("on-char-space")] private void OnCharSpace() => Append(" ");

        /// <summary>
        /// バックスペース操作の処理。未確定文字を優先的に削除し、必要なら確定文字を削除します。
        /// </summary>
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

        /// <summary>
        /// 文字列の最後のテキスト要素（結合文字を考慮）を削除して返します。
        /// </summary>
        private static string RemoveLastTextElement(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var si = new StringInfo(text);
            int count = si.LengthInTextElements;

            if (count <= 1) return string.Empty;

            return si.SubstringByTextElements(0, count - 1);
        }

        /// <summary>
        /// Shift ボタンのトグル処理（A〜Z ラベルの切替を行う）。
        /// </summary>
        [UIAction("on-char-shift")]
        private void OnCharShift()
        {
            // Shift をトグルして A〜Z ボタン表示を切替
            isShift = !isShift;
            keyController.UpdateAlphaButtonLabels(isShift);
        }

        /// <summary>
        /// かなモード切替処理（ひらがな/カタカナの切替）。
        /// </summary>
        [UIAction("on-char-toggle-kana")]
        private void OnCharToggleKana()
        {
            isKanaMode = !isKanaMode;
            keyController.UpdateKanaModeButtonLabel(isKanaMode);
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