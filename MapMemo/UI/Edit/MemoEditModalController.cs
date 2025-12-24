using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Linq;
using UnityEngine;
using TMPro;
using HMUI;
using System.Globalization;
using MapMemo.UI.Menu;
using MapMemo.Utilities;
using MapMemo.Services;
using MapMemo.UI.Common;
using MapMemo.Models;
using System.Net;

namespace MapMemo.UI.Edit
{
    /// <summary>
    /// メモ編集用のモーダルコントローラー。BSML のバインドとユーザー入力処理、サジェスト表示を担当します。
    /// </summary>
    public class MemoEditModalController : BSMLAutomaticViewController
    {
        // モーダルのシングルトンインスタンス
        public static MemoEditModalController Instance;

        // Shift 状態（true = 小文字モード）
        public bool isShift { get; private set; } = false;

        // かなモード状態（true = カタカナ、false = ひらがな）
        public bool isKanaMode { get; private set; } = false;
        // バインド対象のプロパティ
        // [UIValue("memo")] private string memo = "";
        // UI コンポーネント
        [UIComponent("modal")] private ModalView modal = null;
        // メモ編集用テキストコンポーネント
        [UIComponent("memoText")] public TextMeshProUGUI memoText;
        // 確定済みテキスト
        private string confirmedText = "";
        // 未確定テキスト
        private string pendingText = "";
        // 最終更新日時表示コンポーネント
        [UIComponent("last-updated")] private TextMeshProUGUI lastUpdated = null;
        // サジェストリストコンポーネント
        [UIComponent("suggestion-list")] private CustomListTableData suggestionList = null;
        // サジェストリストコントローラー
        private SuggestionListHandler suggestionHandler = null;
        // 入力キーコントローラー
        private InputKeyHandler keyHandler = null;
        // レベルコンテキスト(マップ情報)
        private LevelContext levelContext;
        // メッセージ表示コンポーネント
        [UIComponent("message")]
        private TextMeshProUGUI message = null;

        private MemoService memoService = MemoService.Instance;

        // 最大行数と1行あたりの最大文字数
        private static int MAX_LINES = 3;
        // 1行あたりの最大文字数
        private static int MAX_CHARS_PER_LINE = 20;

        //// ◆画面初期表示関連メソッド Start ◆////

        /// <summary>
        /// モーダルを表示します。指定の LevelContext に基づき該当メモをロードして表示します。
        /// </summary>
        /// <param name="parent">親パネルコントローラー</param>
        /// <param name="levelContext">メモのキー</param>
        public static void Show(
            MemoPanelController parent, LevelContext levelContext)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoEditModal.Show: called");
            // モーダルのインスタンスを取得する
            var modalCtrl = GetInstance(parent, levelContext);
            modalCtrl.Show();
        }

        /// <summary>
        /// モーダルを表示します。
        /// </summary>
        public void Show()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoEditModal.Show: called");
            try
            {
                var modalStatus = ReferenceEquals(modal, null) ? "modal=null" : "modal!=null";
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal.Show: showing modal {modalStatus}");
                modal?.Show(true, true);
                // 画面の左側半分あたりに表示するように位置調整
                RepositionModalToLeftHalf(modal);
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.Warn($"MemoEditModal.Show: ModalView.Show failed: {ex.Message}; modal may not be visible");
            }
        }
        /// <summary>
        /// モーダルを画面左半分に移動して表示位置を調整します。
        /// </summary>
        private void RepositionModalToLeftHalf(ModalView modal)
        {
            if (modal == null) return;
            try
            {
                var rt = modal.gameObject.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float offsetX = 0f;
                    var parentCanvas = modal.gameObject.GetComponentInParent<Canvas>();
                    if (parentCanvas != null)
                    {
                        var canvasRt = parentCanvas.GetComponent<RectTransform>();
                        if (canvasRt != null)
                        {
                            offsetX = -1f * (canvasRt.rect.width * 0.5f);
                        }
                    }
                    if (offsetX == 0f)
                    {
                        offsetX = -1f * (UnityEngine.Screen.width * 0.5f);
                    }
                    var current = rt.anchoredPosition;
                    rt.anchoredPosition = new Vector2(current.x + offsetX, current.y);
                    if (MapMemo.Plugin.VerboseLogs) MapMemo.Plugin.Log?.Info($"MemoEditModal.RepositionModalToLeftHalf: shifted modal anchoredPosition by {offsetX} (newX={rt.anchoredPosition.x})");
                }
            }
            catch (Exception ex)
            {
                MapMemo.Plugin.Log?.Warn($"MemoEditModal.RepositionModalToLeftHalf: exception {ex}");
            }
        }

        /// <summary>
        /// モーダルのインスタンスを取得または生成する
        /// </summary>
        /// <param name="existingMemoInfo"></param>
        /// <param name="parent"></param>
        /// <param name="key"></param>
        /// <param name="songName"></param>
        /// <param name="songAuthor"></param>
        /// <returns></returns>
        private static MemoEditModalController GetInstance(
            MemoPanelController parent,
            LevelContext levelContext)
        {
            if (ReferenceEquals(Instance, null))
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoEditModal.GetInstance: creating new modal instance");
                // インスタンスを生成
                Instance = BeatSaberUI.CreateViewController<MemoEditModalController>();
                // 親パネルにアタッチ
                Instance.InitializeModal(parent);
            }
            // 必要なパラメータを設定
            Instance.InitializeParameters(levelContext);
            return Instance;
        }
        /// <summary>
        /// モーダルの初期化処理。BSMLパース、辞書/履歴/キーバインドの読み込み、キー表示の初期化を行います。
        /// </summary>
        /// <param name="parent">親パネルコントローラー</param>
        public void InitializeModal(MemoPanelController parent)
        {
            // BSML をパースしてモーダルにアタッチする
            this.ParseBSML(
                BeatSaberMarkupLanguage.Utilities.GetResourceContent(
                    typeof(MemoEditModalController).Assembly,
                    "MapMemo.Resources.MemoEdit.bsml"),
                    parent.HostGameObject);
            // リソースのロード
            memoService.LoadResources();

            // ボタンの見た目を整えるヘルパーを呼び出す
            keyHandler.InitializeAppearance(isShift);
        }

        /// <summary>
        /// モーダルのパラメータを初期化します。レベルコンテキストと既存メモ情報を設定します。
        /// </summary>
        /// <param name="levelContext">レベルコンテキスト</param>
        public void InitializeParameters(LevelContext levelContext)
        {
            // 既存のメモを読み込む (LevelContext を使用してキー/曲情報を解決)
            var existingMemoInfo = memoService.LoadMemo(levelContext);

            var memo = existingMemoInfo?.memo ?? "";
            this.lastUpdated.text = existingMemoInfo != null ?
                "Updated:" + this.memoService.FormatLocal(existingMemoInfo.updatedAt) : "";
            this.levelContext = levelContext;

            // メモ内容を初期化
            if (this.memoText != null)
            {
                this.memoText.richText = true;
                this.confirmedText = memo;
                this.pendingText = "";
                this.UpdateMemoText();
            }
            // サジェストリストを初期化
            this.suggestionHandler.Clear();
        }

        /// <summary>
        /// BSML をパースしてモーダルにアタッチします。
        /// </summary>
        /// <param name="bsml"></param>
        /// <param name="host"></param>
        private void ParseBSML(string bsml, GameObject host)
        {
            BSMLParser.Instance.Parse(bsml, host, this);
            var hostName = host?.name ?? "(null)";
            var modalStatus = ReferenceEquals(modal, null) ? "modal=null" : "modal!=null";
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal: BSML parsed and attached to host '{hostName}' {modalStatus}");
        }

        /// <summary>
        /// BSML パース後の初期化処理。キーコントローラーとサジェストリストコントローラーをセットアップします。
        /// </summary>
        [UIAction("#post-parse")]
        private void OnPostParse()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoEditModal: OnPostParse called — setting up pick list");

            keyHandler = new InputKeyHandler(
                modal.gameObject.GetComponentsInChildren<ClickableText>(true),
                modal.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true)
            );

            suggestionHandler = new SuggestionListHandler(suggestionList);
            suggestionHandler.SuggestionSelected += (value, subtext) =>
            {
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"SuggestList selected: {value}");

                if (string.IsNullOrEmpty(value)) return;
                AppendSelectedString(value, subtext);
                suggestionHandler.Clear();
            };

            // ボタンのクリックリスナーを設定
            keyHandler.SetupKeyClickListeners();
        }

        /// <summary>
        /// モーダルが有効化されたときに呼ばれます。ボタンラベルの更新等を行います。
        /// </summary>
        private void OnEnable()
        {
            // モーダルが有効化されたときに呼ばれる
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoEditModal: OnEnable called");

            // ボタンのラベルを更新する このタイミングではNullReferenceがでる
            // keyController.UpdateAlphaButtonLabels(isShift);
        }

        //// ◆画面初期表示関連メソッド End ◆////

        /// <summary>
        /// 未確定テキストを確定します。
        /// </summary>
        private void CommitMemo()
        {
            // 確定処理
            confirmedText += pendingText;

            pendingText = "";
            //memo = confirmedText;
            // NotifyPropertyChanged("memo");
            if (memoText != null)
            {
                UpdateMemoText();
            }
        }
        private static readonly string UserNewlinePlaceholder = "\uF000";


        private string EscapeBsmlTag(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string safe = s.Replace("<", "\u27E8").Replace(">", "\u27E9");
            return safe;
        }
        private string EscapeForDisplayRaw(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\r", "").Replace("\n", UserNewlinePlaceholder);
        }
        /// <summary>
        /// メモ表示テキストを更新します。
        /// </summary>
        private void UpdateMemoText()
        {
            var confirmedEsc = EscapeForDisplayRaw(confirmedText);
            var pendingEsc = EscapeForDisplayRaw(GetPendingText());
            var raw = confirmedEsc + pendingEsc;


            // 表示上の折り返しをシミュレーション（InsertLineBreaks は '\n' を挿入する）
            var wrapped = StringHelper.InsertLineBreaks(raw, MAX_CHARS_PER_LINE);
            var display = wrapped;

            // プレースホルダをユーザー改行表示に戻す
            display = display.Replace(UserNewlinePlaceholder, "↲\n");

            Plugin.Log?.Debug($"MemoEditModal.UpdateMemoText: confirmedText: {confirmedEsc} pendingText: '{pendingEsc}'");
            memoText.text = display;

            memoText.ForceMeshUpdate();
        }
        /// <summary>
        /// 保存ボタン押下時の処理。メモを保存して親パネルを更新します。
        /// </summary>
        [UIAction("on-save")]
        public async void OnSave()
        {
            try
            {
                var text = confirmedText + pendingText;
                // メモを保存
                await memoService.SaveMemoAsync(levelContext, text);
                // 最終更新日時の表示を更新
                lastUpdated.text = memoService.FormatLocal(DateTime.UtcNow);
                // 親パネルの反映
                var parentPanelLocal = MemoPanelController.instance;
                //  入力履歴に追加
                MemoService.Instance.AddHistory(pendingText);
                // 確定状態にする
                CommitMemo();

                // 保存完了メッセージを表示
                UIHelper.Instance.ShowTemporaryMessage(
                    message, "<color=#00FF00>Memo Saved.</color>");
                // 親パネルを更新
                await parentPanelLocal.Refresh();
            }
            catch (System.Exception ex)
            {
                MapMemo.Plugin.Log?.Error($"MemoEditModal.OnSave: exception {ex}");
            }
            finally
            {
                // モーダルは閉じずに編集を継続できるようにする
                if (Plugin.VerboseLogs) MapMemo.Plugin.Log?.Info("MemoEditModal.OnSave: keeping modal open after save");
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
            // memo = confirmedText;

            if (string.IsNullOrEmpty(s)) return;

            var iter = System.Globalization.StringInfo.GetTextElementEnumerator(s);
            while (iter.MoveNext())
            {
                Append(iter.GetTextElement(), false);
            }
            // 履歴に追加
            memoService.AddHistory(s, subText);
            // 確定処理
            CommitMemo();
        }

        /// <summary>
        /// 指定した文字列を現在の未確定テキストに追加します。
        /// 入力制限（行数/文字数）を超えないように制御します。
        /// </summary>
        public bool Append(string s, bool isSuggestUpdate = true)
        {
            if (string.IsNullOrEmpty(s)) return false;
            // if (memo == null) memo = "";
            // if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal.Append: add='{s}' len-before={memo.Length}");

            // 未確定文字を削除して確定文字に設定
            var pendingTextWithTag = GetPendingText();
            var confirmedText = this.confirmedText;

            // if (!string.IsNullOrEmpty(pendingTextWithTag))
            // {
            //     // this.confirmedText = memo.Replace(pendingTextWithTag, "");
            //     // 末尾に装飾済み未確定テキストがあればそこだけ切り取る
            //     if (!string.IsNullOrEmpty(memo) && memo.EndsWith(pendingTextWithTag))
            //     {
            //         this.confirmedText = memo.Substring(0, memo.Length - pendingTextWithTag.Length);
            //     }
            // }

            // ここで重みづけを考慮した文字数制限を行う
            // 3行、かつ各行20文字（ASCIIアルファベットは0.5文字換算）を超えないようにする
            // 改行／非改行に関わらず、追加後に表示上の行数制限を満たすかをチェック
            if (!StringHelper.FitsWithinLines(
                confirmedText + pendingText + s, MAX_LINES, MAX_CHARS_PER_LINE))
            {
                return false;
            }

            // ここで自動改行するのをやめる
            // if (GetLastLineLength(confirmedText + pendingText + s) > maxCharsPerLine)
            // {
            //     // 3行超過する場合、改行も追加もしない
            //     if (isOverMaxLine(confirmedText + pendingText + s, maxLines))
            //     {
            //         return false;
            //     }
            //     // 最大文字数を超過する場合は強制改行を挿入
            //     s = "\n" + s;
            // }

            // 未確定文字列に追加
            this.pendingText += EscapeBsmlTag(s);

            // 表示用テキストを更新
            // this.memo = confirmedText + GetPendingText();

            // サジェストリストを更新
            if (isSuggestUpdate)
            {
                UpdateSuggestions();
            }
            // プロパティ変更通知
            // NotifyPropertyChanged("memo");

            // テキストコンポーネントを更新
            if (memoText != null)
            {
                UpdateMemoText();
            }
            // if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal.Append: len-after={memo.Length}");
            return true;
        }

        /// <summary>
        /// サジェストリストをクリアします。
        /// </summary>
        private void ClearSuggestions()
        {
            if (suggestionHandler != null)
            {
                suggestionHandler.Clear();
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
            if (suggestionHandler != null)
            {
                suggestionHandler.UpdateSuggestions(pendingText);
                return;
            }

            // Fallback: clear list
            suggestionList.Data.Clear();
            suggestionList.TableView.ReloadData();
        }


        // /// <summary>
        // /// 指定したテキストの最終行の長さをテキスト要素単位で計算して返します（ASCII 分割ルール適用）。
        // /// </summary>
        // private double GetLastLineLength(string text)
        // {
        //     if (string.IsNullOrEmpty(text)) return 0;

        //     var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        //     var lastLine = lines.LastOrDefault() ?? "";

        //     var enumerator = StringInfo.GetTextElementEnumerator(lastLine);
        //     double length = 0.0;
        //     while (enumerator.MoveNext())
        //     {
        //         var elem = enumerator.GetTextElement();
        //         // ASCII のアルファベットのみを半分扱いにする
        //         length += StringHelper.IsAsciiAlphabet(elem) ? 0.5 : 1.0;
        //     }

        //     return length;
        // }

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
            // 未確定テキストが空の場合は空文字を返す
            if (string.IsNullOrEmpty(pendingText)) return "";
            // 未確定テキストを黄色の下線付きで装飾して返す
            return "<color=#FFFF00><u>" + pendingText + "</u></color>";
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
                this.pendingText = RemoveLastTextElement(pendingText);
                // memo = confirmedText + GetPendingText();
                // NotifyPropertyChanged("memo");
                UpdateMemoText();
                UpdateSuggestions();
                return;
            }

            if (string.IsNullOrEmpty(confirmedText))
            {
                return;
            }

            confirmedText = RemoveLastTextElement(confirmedText);
            // memo = confirmedText;
            //NotifyPropertyChanged("memo");
            UpdateMemoText();
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
            keyHandler.UpdateAlphaButtonLabels(isShift);
        }

        /// <summary>
        /// かなモード切替処理（ひらがな/カタカナの切替）。
        /// </summary>
        [UIAction("on-char-toggle-kana")]
        private void OnCharToggleKana()
        {
            isKanaMode = !isKanaMode;
            keyHandler.UpdateKanaModeButtonLabel(isKanaMode);
        }

        /// <summary>
        /// 確定ボタン押下時の処理
        /// </summary>
        [UIAction("on-char-enter")]
        private void OnCharEnter()
        {
            if (pendingText.Length > 0)
            {
                // 入力履歴に追加
                if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal.OnCharEnter: adding to history: '{pendingText}'");
                memoService.AddHistory(pendingText);
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