using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
// Note: Avoid UnityEngine.UI dependency; use UnityEngine.Canvas explicitly
using HMUI;
using IPA.Config.Data;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using IPA.Utilities;
using System.IO;
using MapMemo.UI;
using UnityEngine.Rendering;
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
        [UIComponent("memoText")] private TextMeshProUGUI memoText;
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
                Plugin.Log?.Info("MemoEditModal.Show: showing modal" + (ReferenceEquals(modalCtrl.modal, null) ? " modal=null" : " modal!=null"));
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
            Plugin.Log?.Info("MemoEditModal: BSML parsed and attached to host '" + host.name + "'" + (ReferenceEquals(modal, null) ? " modal=null" : " modal!=null"));
        }

        [UIAction("#post-parse")]
        private void OnPostParse()
        {
            Plugin.Log?.Info("MemoEditModal: OnPostParse called — setting up pick list");
            try
            {
                suggestionController = new SuggestionListController(suggestionList, historyShowCount);
                suggestionController.SuggestionSelected += (value, subtext) =>
                {
                    Plugin.Log?.Info($"選択されたのは: {value}");
                    if (string.IsNullOrEmpty(value)) return;
                    AppendSelectedString(value, subtext);
                    suggestionController.Clear();
                };
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"MemoEditModal.OnPostParse: failed to initialize SuggestionListController: {ex.Message}");
            }

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

        private void Append(string s, bool isSuggestUpdate = true)
        {
            if (string.IsNullOrEmpty(s)) return;
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
                    return;
                }
            }

            if (GetLastLineLength(confirmedText + pendingText + s) > maxCharsPerLine)
            {
                // 3文字超過する場合、改行も追加もしない
                if (isOverMaxLine(confirmedText + pendingText + s, maxLines))
                {
                    return;
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
        }

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

        [UIAction("on-char-long")] private void OnCharLong() => Append("ー");
        // BSML 上では長音記号ボタンに複数の on-click 名が使われているため
        // 互換のためにエイリアスを用意する
        [UIAction("on-char-cho")] private void OnCharCho() => Append("ー");
        [UIAction("on-char-ka-cho")] private void OnCharKaCho() => Append("ー");
        [UIAction("on-char-dot")] private void OnCharDot() => Append("・");
        [UIAction("on-char-space")] private void OnCharSpace() => Append(" ");
        // [UIAction("on-char-newline")] private void OnCharNewline() => Append("\n");
        [UIAction("on-char-backspace")]
        private void OnCharBackspace()
        {
            if (pendingText.Length > 0)
            {
                // 未確定文字列から削除
                pendingText = pendingText.Substring(0, pendingText.Length - 1);
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

            confirmedText = confirmedText.Substring(0, confirmedText.Length - 1);
            memo = confirmedText;
            NotifyPropertyChanged("memo");
            UpdateMemoText(memo);
            UpdateSuggestions();
        }

        // 英数字・記号入力
        [UIAction("on-char-A")] private void OnCharA_Eng() => Append(isShift ? "a" : "A");
        [UIAction("on-char-B")] private void OnCharB() => Append(isShift ? "b" : "B");
        [UIAction("on-char-C")] private void OnCharC() => Append(isShift ? "c" : "C");
        [UIAction("on-char-D")] private void OnCharD() => Append(isShift ? "d" : "D");
        [UIAction("on-char-E")] private void OnCharE_Eng() => Append(isShift ? "e" : "E");
        [UIAction("on-char-F")] private void OnCharF() => Append(isShift ? "f" : "F");
        [UIAction("on-char-G")] private void OnCharG() => Append(isShift ? "g" : "G");
        [UIAction("on-char-H")] private void OnCharH() => Append(isShift ? "h" : "H");
        [UIAction("on-char-I")] private void OnCharI_Eng() => Append(isShift ? "i" : "I");
        [UIAction("on-char-J")] private void OnCharJ() => Append(isShift ? "j" : "J");
        [UIAction("on-char-K")] private void OnCharK() => Append(isShift ? "k" : "K");
        [UIAction("on-char-L")] private void OnCharL() => Append(isShift ? "l" : "L");
        [UIAction("on-char-M")] private void OnCharM() => Append(isShift ? "m" : "M");
        [UIAction("on-char-N")] private void OnCharN_Eng() => Append(isShift ? "n" : "N");
        [UIAction("on-char-O")] private void OnCharO_Eng() => Append(isShift ? "o" : "O");
        [UIAction("on-char-P")] private void OnCharP() => Append(isShift ? "p" : "P");
        [UIAction("on-char-Q")] private void OnCharQ() => Append(isShift ? "q" : "Q");
        [UIAction("on-char-R")] private void OnCharR() => Append(isShift ? "r" : "R");
        [UIAction("on-char-S")] private void OnCharS() => Append(isShift ? "s" : "S");
        [UIAction("on-char-T")] private void OnCharT() => Append(isShift ? "t" : "T");
        [UIAction("on-char-U")] private void OnCharU_Eng() => Append(isShift ? "u" : "U");
        [UIAction("on-char-V")] private void OnCharV() => Append(isShift ? "v" : "V");
        [UIAction("on-char-W")] private void OnCharW() => Append(isShift ? "w" : "W");
        [UIAction("on-char-X")] private void OnCharX() => Append(isShift ? "x" : "X");
        [UIAction("on-char-Y")] private void OnCharY() => Append(isShift ? "y" : "Y");
        [UIAction("on-char-Z")] private void OnCharZ() => Append(isShift ? "z" : "Z");

        [UIAction("on-char-0")] private void OnChar0() => Append("0");
        [UIAction("on-char-1")] private void OnChar1() => Append("1");
        [UIAction("on-char-2")] private void OnChar2() => Append("2");
        [UIAction("on-char-3")] private void OnChar3() => Append("3");
        [UIAction("on-char-4")] private void OnChar4() => Append("4");
        [UIAction("on-char-5")] private void OnChar5() => Append("5");
        [UIAction("on-char-6")] private void OnChar6() => Append("6");
        [UIAction("on-char-7")] private void OnChar7() => Append("7");
        [UIAction("on-char-8")] private void OnChar8() => Append("8");
        [UIAction("on-char-9")] private void OnChar9() => Append("9");

        [UIAction("on-char-comma")] private void OnCharComma() => Append(",");
        [UIAction("on-char-period")] private void OnCharPeriod() => Append(".");
        [UIAction("on-char-exclam")] private void OnCharExclam() => Append("!");
        [UIAction("on-char-question")] private void OnCharQuestion() => Append("?");

        // 追加記号
        [UIAction("on-char-hyphen")] private void OnCharHyphen() => Append("-");
        [UIAction("on-char-slash")] private void OnCharSlash() => Append("/");
        [UIAction("on-char-colon")] private void OnCharColon() => Append(":");
        [UIAction("on-char-semicolon")] private void OnCharSemicolon() => Append(";");
        [UIAction("on-char-lparen")] private void OnCharLParen() => Append("(");
        [UIAction("on-char-rparen")] private void OnCharRParen() => Append(")");
        [UIAction("on-char-ampersand")] private void OnCharAmpersand() => Append("&");
        [UIAction("on-char-at")] private void OnCharAt() => Append("@");
        [UIAction("on-char-hash")] private void OnCharHash() => Append("#");
        [UIAction("on-char-plus")] private void OnCharPlus() => Append("+");

        // 英字キーボード追加記号
        [UIAction("on-char-underscore")] private void OnCharUnderscore() => Append("_");
        [UIAction("on-char-equals")] private void OnCharEquals() => Append("=");
        [UIAction("on-char-lbracket")] private void OnCharLBracket() => Append("[");
        [UIAction("on-char-rbracket")] private void OnCharRBracket() => Append("]");
        [UIAction("on-char-lbrace")] private void OnCharLBrace() => Append("{");
        [UIAction("on-char-rbrace")] private void OnCharRBrace() => Append("}");
        [UIAction("on-char-apostrophe")] private void OnCharApostrophe() => Append("'");
        [UIAction("on-char-quote")] private void OnCharQuote() => Append("\"");
        [UIAction("on-char-less")] private void OnCharLess() => Append("<");
        [UIAction("on-char-greater")] private void OnCharGreater() => Append(">");
        [UIAction("on-char-pipe")] private void OnCharPipe() => Append("|");
        [UIAction("on-char-backslash")] private void OnCharBackslash() => Append("\\");
        [UIAction("on-char-caret")] private void OnCharCaret() => Append("^");
        [UIAction("on-char-tilde")] private void OnCharTilde() => Append("~");
        [UIAction("on-char-backtick")] private void OnCharBacktick() => Append("`");
        [UIAction("on-char-asterisk")] private void OnCharAsterisk() => Append("*");
        [UIAction("on-char-percent")] private void OnCharPercent() => Append("%");
        [UIAction("on-char-dollar")] private void OnCharDollar() => Append("$");

        // ら行
        [UIAction("on-char-ra")] private void OnCharRa() => Append(isKanaMode ? "ラ" : "ら");
        [UIAction("on-char-ri")] private void OnCharRi() => Append(isKanaMode ? "リ" : "り");
        [UIAction("on-char-ru")] private void OnCharRu() => Append(isKanaMode ? "ル" : "る");
        [UIAction("on-char-re")] private void OnCharRe() => Append(isKanaMode ? "レ" : "れ");
        [UIAction("on-char-ro")] private void OnCharRo() => Append(isKanaMode ? "ロ" : "ろ");

        [UIAction("on-char-a")] private void OnCharA() => Append(isKanaMode ? "ア" : "あ");
        [UIAction("on-char-i")] private void OnCharI() => Append(isKanaMode ? "イ" : "い");
        [UIAction("on-char-u")] private void OnCharU() => Append(isKanaMode ? "ウ" : "う");
        [UIAction("on-char-e")] private void OnCharE() => Append(isKanaMode ? "エ" : "え");
        [UIAction("on-char-o")] private void OnCharO() => Append(isKanaMode ? "オ" : "お");
        [UIAction("on-char-ka")] private void OnCharKa() => Append(isKanaMode ? "カ" : "か");
        [UIAction("on-char-ki")] private void OnCharKi() => Append(isKanaMode ? "キ" : "き");
        [UIAction("on-char-ku")] private void OnCharKu() => Append(isKanaMode ? "ク" : "く");
        [UIAction("on-char-ke")] private void OnCharKe() => Append(isKanaMode ? "ケ" : "け");
        [UIAction("on-char-ko")] private void OnCharKo() => Append(isKanaMode ? "コ" : "こ");
        [UIAction("on-char-sa")] private void OnCharSa() => Append(isKanaMode ? "サ" : "さ");
        [UIAction("on-char-shi")] private void OnCharShi() => Append(isKanaMode ? "シ" : "し");
        [UIAction("on-char-su")] private void OnCharSu() => Append(isKanaMode ? "ス" : "す");
        [UIAction("on-char-se")] private void OnCharSe() => Append(isKanaMode ? "セ" : "せ");
        [UIAction("on-char-so")] private void OnCharSo() => Append(isKanaMode ? "ソ" : "そ");

        [UIAction("on-char-ta")] private void OnCharTa() => Append(isKanaMode ? "タ" : "た");
        [UIAction("on-char-chi")] private void OnCharChi() => Append(isKanaMode ? "チ" : "ち");
        [UIAction("on-char-tsu")] private void OnCharTsu() => Append(isKanaMode ? "ツ" : "つ");
        [UIAction("on-char-te")] private void OnCharTe() => Append(isKanaMode ? "テ" : "て");
        [UIAction("on-char-to")] private void OnCharTo() => Append(isKanaMode ? "ト" : "と");

        [UIAction("on-char-na")] private void OnCharNa() => Append(isKanaMode ? "ナ" : "な");
        [UIAction("on-char-ni")] private void OnCharNi() => Append(isKanaMode ? "ニ" : "に");
        [UIAction("on-char-nu")] private void OnCharNu() => Append(isKanaMode ? "ヌ" : "ぬ");
        [UIAction("on-char-ne")] private void OnCharNe() => Append(isKanaMode ? "ネ" : "ね");
        [UIAction("on-char-no")] private void OnCharNo() => Append(isKanaMode ? "ノ" : "の");

        [UIAction("on-char-ha")] private void OnCharHa() => Append(isKanaMode ? "ハ" : "は");
        [UIAction("on-char-hi")] private void OnCharHi() => Append(isKanaMode ? "ヒ" : "ひ");
        [UIAction("on-char-fu")] private void OnCharFu() => Append(isKanaMode ? "フ" : "ふ");
        [UIAction("on-char-he")] private void OnCharHe() => Append(isKanaMode ? "ヘ" : "へ");
        [UIAction("on-char-ho")] private void OnCharHo() => Append(isKanaMode ? "ホ" : "ほ");

        [UIAction("on-char-ma")] private void OnCharMa() => Append(isKanaMode ? "マ" : "ま");
        [UIAction("on-char-mi")] private void OnCharMi() => Append(isKanaMode ? "ミ" : "み");
        [UIAction("on-char-mu")] private void OnCharMu() => Append(isKanaMode ? "ム" : "む");
        [UIAction("on-char-me")] private void OnCharMe() => Append(isKanaMode ? "メ" : "め");
        [UIAction("on-char-mo")] private void OnCharMo() => Append(isKanaMode ? "モ" : "も");
        [UIAction("on-char-ya")] private void OnCharYa() => Append(isKanaMode ? "ヤ" : "や");
        [UIAction("on-char-yu")] private void OnCharYu() => Append(isKanaMode ? "ユ" : "ゆ");
        [UIAction("on-char-yo")] private void OnCharYo() => Append(isKanaMode ? "ヨ" : "よ");
        [UIAction("on-char-wa")] private void OnCharWa() => Append(isKanaMode ? "ワ" : "わ");
        [UIAction("on-char-wo")] private void OnCharWo() => Append(isKanaMode ? "ヲ" : "を");
        [UIAction("on-char-n")] private void OnCharN() => Append(isKanaMode ? "ン" : "ん");

        // 濁点
        [UIAction("on-char-ga")] private void OnCharGa() => Append(isKanaMode ? "ガ" : "が");
        [UIAction("on-char-gi")] private void OnCharGi() => Append(isKanaMode ? "ギ" : "ぎ");
        [UIAction("on-char-gu")] private void OnCharGu() => Append(isKanaMode ? "グ" : "ぐ");
        [UIAction("on-char-ge")] private void OnCharGe() => Append(isKanaMode ? "ゲ" : "げ");
        [UIAction("on-char-go")] private void OnCharGo() => Append(isKanaMode ? "ゴ" : "ご");
        [UIAction("on-char-za")] private void OnCharZa() => Append(isKanaMode ? "ザ" : "ざ");
        [UIAction("on-char-ji")] private void OnCharJi() => Append(isKanaMode ? "ジ" : "じ");
        [UIAction("on-char-zu")] private void OnCharZu() => Append(isKanaMode ? "ズ" : "ず");
        [UIAction("on-char-ze")] private void OnCharZe() => Append(isKanaMode ? "ゼ" : "ぜ");
        [UIAction("on-char-zo")] private void OnCharZo() => Append(isKanaMode ? "ゾ" : "ぞ");
        [UIAction("on-char-da")] private void OnCharDa() => Append(isKanaMode ? "ダ" : "だ");
        [UIAction("on-char-di")] private void OnCharDi() => Append(isKanaMode ? "ヂ" : "ぢ");
        [UIAction("on-char-du")] private void OnCharDu() => Append(isKanaMode ? "ヅ" : "づ");
        [UIAction("on-char-de")] private void OnCharDe() => Append(isKanaMode ? "デ" : "で");
        [UIAction("on-char-do")] private void OnCharDo() => Append(isKanaMode ? "ド" : "ど");
        [UIAction("on-char-ba")] private void OnCharBa() => Append(isKanaMode ? "バ" : "ば");
        [UIAction("on-char-bi")] private void OnCharBi() => Append(isKanaMode ? "ビ" : "び");
        [UIAction("on-char-bu")] private void OnCharBu() => Append(isKanaMode ? "ブ" : "ぶ");
        [UIAction("on-char-be")] private void OnCharBe() => Append(isKanaMode ? "ベ" : "べ");
        [UIAction("on-char-bo")] private void OnCharBo() => Append(isKanaMode ? "ボ" : "ぼ");

        // 半濁点
        [UIAction("on-char-pa")] private void OnCharPa() => Append(isKanaMode ? "パ" : "ぱ");
        [UIAction("on-char-pi")] private void OnCharPi() => Append(isKanaMode ? "ピ" : "ぴ");
        [UIAction("on-char-pu")] private void OnCharPu() => Append(isKanaMode ? "プ" : "ぷ");
        [UIAction("on-char-pe")] private void OnCharPe() => Append(isKanaMode ? "ペ" : "ぺ");
        [UIAction("on-char-po")] private void OnCharPo() => Append(isKanaMode ? "ポ" : "ぽ");

        // 小文字（ひらがな）
        [UIAction("on-char-xtu")] private void OnCharXtu() => Append(isKanaMode ? "ッ" : "っ");
        [UIAction("on-char-xya")] private void OnCharXya() => Append(isKanaMode ? "ャ" : "ゃ");
        [UIAction("on-char-xyu")] private void OnCharXyu() => Append(isKanaMode ? "ュ" : "ゅ");
        [UIAction("on-char-xyo")] private void OnCharXyo() => Append(isKanaMode ? "ョ" : "ょ");

        // カタカナ基本
        [UIAction("on-char-ka-a")] private void OnCharKaA() => Append(isKanaMode ? "ア" : "あ");
        [UIAction("on-char-ka-i")] private void OnCharKaI() => Append(isKanaMode ? "イ" : "い");
        [UIAction("on-char-ka-u")] private void OnCharKaU() => Append(isKanaMode ? "ウ" : "う");
        [UIAction("on-char-ka-e")] private void OnCharKaE() => Append(isKanaMode ? "エ" : "え");
        [UIAction("on-char-ka-o")] private void OnCharKaO() => Append(isKanaMode ? "オ" : "お");
        [UIAction("on-char-ka-ka")] private void OnCharKaKa() => Append(isKanaMode ? "カ" : "か");
        [UIAction("on-char-ka-ki")] private void OnCharKaKi() => Append(isKanaMode ? "キ" : "き");
        [UIAction("on-char-ka-ku")] private void OnCharKaKu() => Append(isKanaMode ? "ク" : "く");
        [UIAction("on-char-ka-ke")] private void OnCharKaKe() => Append(isKanaMode ? "ケ" : "け");
        [UIAction("on-char-ka-ko")] private void OnCharKaKo() => Append(isKanaMode ? "コ" : "こ");
        [UIAction("on-char-ka-sa")] private void OnCharKaSa() => Append(isKanaMode ? "サ" : "さ");
        [UIAction("on-char-ka-shi")] private void OnCharKaShi() => Append(isKanaMode ? "シ" : "し");
        [UIAction("on-char-ka-su")] private void OnCharKaSu() => Append(isKanaMode ? "ス" : "す");
        [UIAction("on-char-ka-se")] private void OnCharKaSe() => Append(isKanaMode ? "セ" : "せ");
        [UIAction("on-char-ka-so")] private void OnCharKaSo() => Append(isKanaMode ? "ソ" : "そ");
        [UIAction("on-char-ka-ta")] private void OnCharKaTa() => Append(isKanaMode ? "タ" : "た");
        [UIAction("on-char-ka-chi")] private void OnCharKaChi() => Append(isKanaMode ? "チ" : "ち");
        [UIAction("on-char-ka-tsu")] private void OnCharKaTsu() => Append(isKanaMode ? "ツ" : "つ");
        [UIAction("on-char-ka-te")] private void OnCharKaTe() => Append(isKanaMode ? "テ" : "て");
        [UIAction("on-char-ka-to")] private void OnCharKaTo() => Append(isKanaMode ? "ト" : "と");
        [UIAction("on-char-ka-na")] private void OnCharKaNa() => Append(isKanaMode ? "ナ" : "な");
        [UIAction("on-char-ka-ni")] private void OnCharKaNi() => Append(isKanaMode ? "ニ" : "に");
        [UIAction("on-char-ka-nu")] private void OnCharKaNu() => Append(isKanaMode ? "ヌ" : "ぬ");
        [UIAction("on-char-ka-ne")] private void OnCharKaNe() => Append(isKanaMode ? "ネ" : "ね");
        [UIAction("on-char-ka-no")] private void OnCharKaNo() => Append(isKanaMode ? "ノ" : "の");
        [UIAction("on-char-ka-ha")] private void OnCharKaHa() => Append(isKanaMode ? "ハ" : "は");
        [UIAction("on-char-ka-hi")] private void OnCharKaHi() => Append(isKanaMode ? "ヒ" : "ひ");
        [UIAction("on-char-ka-fu")] private void OnCharKaFu() => Append(isKanaMode ? "フ" : "ふ");
        [UIAction("on-char-ka-he")] private void OnCharKaHe() => Append(isKanaMode ? "ヘ" : "へ");
        [UIAction("on-char-ka-ho")] private void OnCharKaHo() => Append(isKanaMode ? "ホ" : "ほ");
        [UIAction("on-char-ka-ma")] private void OnCharKaMa() => Append(isKanaMode ? "マ" : "ま");
        [UIAction("on-char-ka-mi")] private void OnCharKaMi() => Append(isKanaMode ? "ミ" : "み");
        [UIAction("on-char-ka-mu")] private void OnCharKaMu() => Append(isKanaMode ? "ム" : "む");
        [UIAction("on-char-ka-me")] private void OnCharKaMe() => Append(isKanaMode ? "メ" : "め");
        [UIAction("on-char-ka-mo")] private void OnCharKaMo() => Append(isKanaMode ? "モ" : "も");
        [UIAction("on-char-ka-ya")] private void OnCharKaYa() => Append(isKanaMode ? "ヤ" : "や");
        [UIAction("on-char-ka-yu")] private void OnCharKaYu() => Append(isKanaMode ? "ユ" : "ゆ");
        [UIAction("on-char-ka-yo")] private void OnCharKaYo() => Append(isKanaMode ? "ヨ" : "よ");
        [UIAction("on-char-ka-wa")] private void OnCharKaWa() => Append(isKanaMode ? "ワ" : "わ");
        [UIAction("on-char-ka-wo")] private void OnCharKaWo() => Append(isKanaMode ? "ヲ" : "を");
        [UIAction("on-char-ka-ra")] private void OnCharKaRa() => Append(isKanaMode ? "ラ" : "ら");
        [UIAction("on-char-ka-ri")] private void OnCharKaRi() => Append(isKanaMode ? "リ" : "り");
        [UIAction("on-char-ka-ru")] private void OnCharKaRu() => Append(isKanaMode ? "ル" : "る");
        [UIAction("on-char-ka-re")] private void OnCharKaRe() => Append(isKanaMode ? "レ" : "れ");
        [UIAction("on-char-ka-ro")] private void OnCharKaRo() => Append(isKanaMode ? "ロ" : "ろ");
        [UIAction("on-char-ka-n")] private void OnCharKaN() => Append(isKanaMode ? "ン" : "ん");

        // カタカナ濁点
        [UIAction("on-char-ka-ga")] private void OnCharKaGa() => Append("ガ");
        [UIAction("on-char-ka-gi")] private void OnCharKaGi() => Append("ギ");
        [UIAction("on-char-ka-gu")] private void OnCharKaGu() => Append("グ");
        [UIAction("on-char-ka-ge")] private void OnCharKaGe() => Append("ゲ");
        [UIAction("on-char-ka-go")] private void OnCharKaGo() => Append("ゴ");
        [UIAction("on-char-ka-za")] private void OnCharKaZa() => Append("ザ");
        [UIAction("on-char-ka-ji")] private void OnCharKaJi() => Append("ジ");
        [UIAction("on-char-ka-zu")] private void OnCharKaZu() => Append("ズ");
        [UIAction("on-char-ka-ze")] private void OnCharKaZe() => Append("ゼ");
        [UIAction("on-char-ka-zo")] private void OnCharKaZo() => Append("ゾ");
        [UIAction("on-char-ka-da")] private void OnCharKaDa() => Append("ダ");
        [UIAction("on-char-ka-di")] private void OnCharKaDi() => Append("ヂ");
        [UIAction("on-char-ka-du")] private void OnCharKaDu() => Append("ヅ");
        [UIAction("on-char-ka-de")] private void OnCharKaDe() => Append("デ");
        [UIAction("on-char-ka-do")] private void OnCharKaDo() => Append("ド");
        [UIAction("on-char-ka-ba")] private void OnCharKaBa() => Append("バ");
        [UIAction("on-char-ka-bi")] private void OnCharKaBi() => Append("ビ");
        [UIAction("on-char-ka-bu")] private void OnCharKaBu() => Append("ブ");
        [UIAction("on-char-ka-be")] private void OnCharKaBe() => Append("ベ");
        [UIAction("on-char-ka-bo")] private void OnCharKaBo() => Append("ボ");

        // カタカナ半濁点
        [UIAction("on-char-ka-pa")] private void OnCharKaPa() => Append("パ");
        [UIAction("on-char-ka-pi")] private void OnCharKaPi() => Append("ピ");
        [UIAction("on-char-ka-pu")] private void OnCharKaPu() => Append("プ");
        [UIAction("on-char-ka-pe")] private void OnCharKaPe() => Append("ペ");
        [UIAction("on-char-ka-po")] private void OnCharKaPo() => Append("ポ");

        // カタカナ小文字
        [UIAction("on-char-ka-xtu")] private void OnCharKaXtu() => Append("ッ");
        [UIAction("on-char-ka-xya")] private void OnCharKaXya() => Append("ャ");
        [UIAction("on-char-ka-xyu")] private void OnCharKaXyu() => Append("ュ");
        [UIAction("on-char-ka-xyo")] private void OnCharKaXyo() => Append("ョ");

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