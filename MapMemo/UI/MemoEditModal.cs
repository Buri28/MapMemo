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

namespace MapMemo.UI
{
    public class MemoEditModal : BSMLAutomaticViewController
    {
        // この段階でインスタンスを作るとUnityの管理外のためバインド対象外となる。
        public static MemoEditModal Instance;

        // Show() から渡された親パネル参照を保持して、保存後に正しいパネルを更新できるようにする
        // private MemoPanelController parentPanel = null;
        private ISet<ClickableText> buttons = null;

        private string key;
        private string songName;
        private string songAuthor;
        // Shift 状態（true = 小文字モード）
        private bool isShift = false;
        [UIValue("memo")] private string memo = "";
        [UIComponent("modal")] private ModalView modal;
        [UIComponent("memoText")] private TextMeshProUGUI memoText;
        [UIComponent("scroll-view")] private ScrollView scrollView;
        private string confirmedText = "";
        private string pendingText = "";
        [UIComponent("last-updated")] private TextMeshProUGUI lastUpdated;

        [UIComponent("suggestion-list")] private CustomListTableData suggestionList;
        // [UIComponent("char-A")] private ClickableText charAButton;
        // [UIComponent("char-B")] private ClickableText charBBpickListutton;
        // [UIComponent("char-C")] private ClickableText charCButton;
        // [UIComponent("char-D")] private ClickableText charDButton;
        // [UIComponent("char-E")] private ClickableText charEButton;
        // [UIComponent("char-F")] private ClickableText charFButton;
        // [UIComponent("char-G")] private ClickableText charGButton;
        // [UIComponent("char-H")] private ClickableText charHButton;
        // [UIComponent("char-I")] private ClickableText charIButton;
        // [UIComponent("char-J")] private ClickableText charJButton;
        // [UIComponent("char-K")] private ClickableText charKButton;
        // [UIComponent("char-L")] private ClickableText charLButton;
        // [UIComponent("char-M")] private ClickableText charMButton;
        // [UIComponent("char-N")] private ClickableText charNButton;
        // [UIComponent("char-O")] private ClickableText charOButton;
        // [UIComponent("char-P")] private ClickableText charPButton;
        // [UIComponent("char-Q")] private ClickableText charQButton;
        // [UIComponent("char-R")] private ClickableText charRButton;
        // [UIComponent("char-S")] private ClickableText charSButton;
        // [UIComponent("char-T")] private ClickableText charTButton;
        // [UIComponent("char-U")] private ClickableText charUButton;
        // [UIComponent("char-V")] private ClickableText charVButton;
        // [UIComponent("char-W")] private ClickableText charWButton;
        // [UIComponent("char-X")] private ClickableText charXButton;
        // [UIComponent("char-Y")] private ClickableText charYButton;
        // [UIComponent("char-Z")] private ClickableText charZButton;
        // [UIComponent("char-0")] private ClickableText char0Button;
        // [UIComponent("char-1")] private ClickableText char1Button;
        // [UIComponent("char-2")] private ClickableText char2Button;
        // [UIComponent("char-3")] private ClickableText char3Button;
        // [UIComponent("char-4")] private ClickableText char4Button;
        // [UIComponent("char-5")] private ClickableText char5Button;
        // [UIComponent("char-6")] private ClickableText char6Button;
        // [UIComponent("char-7")] private ClickableText char7Button;
        // [UIComponent("char-8")] private ClickableText char8Button;
        // [UIComponent("char-9")] private ClickableText char9Button;
        // [UIComponent("char-comma")] private ClickableText charCommaButton;
        // [UIComponent("char-period")] private ClickableText charPeriodButton;
        // [UIComponent("char-exclam")] private ClickableText charExclamButton;
        // [UIComponent("char-question")] private ClickableText charQuestionButton;
        // [UIComponent("char-hyphen")] private ClickableText charHyphenButton;
        // [UIComponent("char-slash")] private ClickableText charSlashButton;
        // [UIComponent("char-colon")] private ClickableText charColonButton;
        // [UIComponent("char-semicolon")] private ClickableText charSemicolonButton;
        // [UIComponent("char-lparen")] private ClickableText charLParenButton;
        // [UIComponent("char-rparen")] private ClickableText charRParenButton;
        // [UIComponent("char-ampersand")] private ClickableText charAmpersandButton;
        // [UIComponent("char-at")] private ClickableText charAtButton;
        // [UIComponent("char-hash")] private ClickableText charHashButton;
        // [UIComponent("char-plus")] private ClickableText charPlusButton;
        // // ひらがなキー
        // [UIComponent("char-a")] private ClickableText charHiraAButton;
        // [UIComponent("char-i")] private ClickableText charHiraIButton;
        // [UIComponent("char-u")] private ClickableText charHiraUButton;
        // [UIComponent("char-e")] private ClickableText charHiraEButton;
        // [UIComponent("char-o")] private ClickableText charHiraOButton;

        // [UIComponent("char-ka")] private ClickableText charHiraKaButton;
        // [UIComponent("char-ki")] private ClickableText charHiraKiButton;
        // [UIComponent("char-ku")] private ClickableText charHiraKuButton;
        // [UIComponent("char-ke")] private ClickableText charHiraKeButton;
        // [UIComponent("char-ko")] private ClickableText charHiraKoButton;

        // [UIComponent("char-sa")] private ClickableText charHiraSaButton;
        // [UIComponent("char-shi")] private ClickableText charHiraShiButton;
        // [UIComponent("char-su")] private ClickableText charHiraSuButton;
        // [UIComponent("char-se")] private ClickableText charHiraSeButton;
        // [UIComponent("char-so")] private ClickableText charHiraSoButton;

        // [UIComponent("char-ta")] private ClickableText charHiraTaButton;
        // [UIComponent("char-chi")] private ClickableText charHiraChiButton;
        // [UIComponent("char-tsu")] private ClickableText charHiraTsuButton;
        // [UIComponent("char-te")] private ClickableText charHiraTeButton;
        // [UIComponent("char-to")] private ClickableText charHiraToButton;

        // [UIComponent("char-na")] private ClickableText charHiraNaButton;
        // [UIComponent("char-ni")] private ClickableText charHiraNiButton;
        // [UIComponent("char-nu")] private ClickableText charHiraNuButton;
        // [UIComponent("char-ne")] private ClickableText charHiraNeButton;
        // [UIComponent("char-no")] private ClickableText charHiraNoButton;

        // [UIComponent("char-ha")] private ClickableText charHiraHaButton;
        // [UIComponent("char-hi")] private ClickableText charHiraHiButton;
        // [UIComponent("char-fu")] private ClickableText charHiraFuButton;
        // [UIComponent("char-he")] private ClickableText charHiraHeButton;
        // [UIComponent("char-ho")] private ClickableText charHiraHoButton;

        // [UIComponent("char-ma")] private ClickableText charHiraMaButton;
        // [UIComponent("char-mi")] private ClickableText charHiraMiButton;
        // [UIComponent("char-mu")] private ClickableText charHiraMuButton;
        // [UIComponent("char-me")] private ClickableText charHiraMeButton;
        // [UIComponent("char-mo")] private ClickableText charHiraMoButton;

        // [UIComponent("char-ya")] private ClickableText charHiraYaButton;
        // [UIComponent("char-yu")] private ClickableText charHiraYuButton;
        // [UIComponent("char-yo")] private ClickableText charHiraYoButton;
        // [UIComponent("char-wa")] private ClickableText charHiraWaButton;
        // [UIComponent("char-wo")] private ClickableText charHiraWoButton;

        // [UIComponent("char-n")] private ClickableText charHiraNButton;
        // [UIComponent("char-long")] private ClickableText charHiraLongButton;
        // [UIComponent("char-cho")] private ClickableText charHiraChoButton;
        // [UIComponent("char-ka-cho")] private ClickableText charHiraKaChoButton;
        // [UIComponent("char-dot")] private ClickableText charHiraDotButton;

        // // カタカナキー
        // [UIComponent("char-ka-a")] private ClickableText charKataKaAButton;
        // [UIComponent("char-ka-i")] private ClickableText charKataKaIButton;
        // [UIComponent("char-ka-u")] private ClickableText charKataKaUButton;
        // [UIComponent("char-ka-e")] private ClickableText charKataKaEButton;
        // [UIComponent("char-ka-o")] private ClickableText charKataKaOButton;

        // [UIComponent("char-ka-ka")] private ClickableText charKataKaKaButton;
        // [UIComponent("char-ka-ki")] private ClickableText charKataKaKiButton;
        // [UIComponent("char-ka-ku")] private ClickableText charKataKaKuButton;
        // [UIComponent("char-ka-ke")] private ClickableText charKataKaKeButton;
        // [UIComponent("char-ka-ko")] private ClickableText charKataKaKoButton;

        // [UIComponent("char-ka-sa")] private ClickableText charKataKaSaButton;
        // [UIComponent("char-ka-shi")] private ClickableText charKataKaShiButton;
        // [UIComponent("char-ka-su")] private ClickableText charKataKaSuButton;
        // [UIComponent("char-ka-se")] private ClickableText charKataKaSeButton;
        // [UIComponent("char-ka-so")] private ClickableText charKataKaSoButton;

        // [UIComponent("char-ka-ta")] private ClickableText charKataKaTaButton;
        // [UIComponent("char-ka-chi")] private ClickableText charKataKaChiButton;
        // [UIComponent("char-ka-tsu")] private ClickableText charKataKaTsuButton;
        // [UIComponent("char-ka-te")] private ClickableText charKataKaTeButton;
        // [UIComponent("char-ka-to")] private ClickableText charKataKaToButton;

        // [UIComponent("char-ka-na")] private ClickableText charKataKaNaButton;
        // [UIComponent("char-ka-ni")] private ClickableText charKataKaNiButton;
        // [UIComponent("char-ka-nu")] private ClickableText charKataKaNuButton;
        // [UIComponent("char-ka-ne")] private ClickableText charKataKaNeButton;
        // [UIComponent("char-ka-no")] private ClickableText charKataKaNoButton;

        // [UIComponent("char-ka-ha")] private ClickableText charKataKaHaButton;
        // [UIComponent("char-ka-hi")] private ClickableText charKataKaHiButton;
        // [UIComponent("char-ka-fu")] private ClickableText charKataKaFuButton;
        // [UIComponent("char-ka-he")] private ClickableText charKataKaHeButton;
        // [UIComponent("char-ka-ho")] private ClickableText charKataKaHoButton;

        // [UIComponent("char-ka-ma")] private ClickableText charKataKaMaButton;
        // [UIComponent("char-ka-mi")] private ClickableText charKataKaMiButton;
        // [UIComponent("char-ka-mu")] private ClickableText charKataKaMuButton;
        // [UIComponent("char-ka-me")] private ClickableText charKataKaMeButton;
        // [UIComponent("char-ka-mo")] private ClickableText charKataKaMoButton;

        // [UIComponent("char-ka-ya")] private ClickableText charKataKaYaButton;
        // [UIComponent("char-ka-yu")] private ClickableText charKataKaYuButton;
        // [UIComponent("char-ka-yo")] private ClickableText charKataKaYoButton;
        // [UIComponent("char-ka-wa")] private ClickableText charKataKaWaButton;
        // [UIComponent("char-ka-wo")] private ClickableText charKataKaWoButton;

        // [UIComponent("char-ka-ra")] private ClickableText charKataKaRaButton;
        // [UIComponent("char-ka-ri")] private ClickableText charKataKaRiButton;
        // [UIComponent("char-ka-ru")] private ClickableText charKataKaRuButton;
        // [UIComponent("char-ka-re")] private ClickableText charKataKaReButton;
        // [UIComponent("char-ka-ro")] private ClickableText charKataKaRoButton;

        // [UIComponent("char-ka-n")] private ClickableText charKataKaNButton;

        // // カタカナ濁点・半濁点
        // [UIComponent("char-ka-ga")] private ClickableText charKataKaGaButton;
        // [UIComponent("char-ka-gi")] private ClickableText charKataKaGiButton;
        // [UIComponent("char-ka-gu")] private ClickableText charKataKaGuButton;
        // [UIComponent("char-ka-ge")] private ClickableText charKataKaGeButton;
        // [UIComponent("char-ka-go")] private ClickableText charKataKaGoButton;
        // [UIComponent("char-ka-za")] private ClickableText charKataKaZaButton;
        // [UIComponent("char-ka-ji")] private ClickableText charKataKaJiButton;
        // [UIComponent("char-ka-zu")] private ClickableText charKataKaZuButton;
        // [UIComponent("char-ka-ze")] private ClickableText charKataKaZeButton;
        // [UIComponent("char-ka-zo")] private ClickableText charKataKaZoButton;
        // [UIComponent("char-ka-da")] private ClickableText charKataKaDaButton;
        // [UIComponent("char-ka-di")] private ClickableText charKataKaDiButton;
        // [UIComponent("char-ka-du")] private ClickableText charKataKaDuButton;
        // [UIComponent("char-ka-de")] private ClickableText charKataKaDeButton;
        // [UIComponent("char-ka-do")] private ClickableText charKataKaDoButton;
        // [UIComponent("char-ka-ba")] private ClickableText charKataKaBaButton;
        // [UIComponent("char-ka-bi")] private ClickableText charKataKaBiButton;
        // [UIComponent("char-ka-bu")] private ClickableText charKataKaBuButton;
        // [UIComponent("char-ka-be")] private ClickableText charKataKaBeButton;
        // [UIComponent("char-ka-bo")] private ClickableText charKataKaBoButton;

        // [UIComponent("char-ka-pa")] private ClickableText charKataKaPaButton;
        // [UIComponent("char-ka-pi")] private ClickableText charKataKaPiButton;
        // [UIComponent("char-ka-pu")] private ClickableText charKataKaPuButton;
        // [UIComponent("char-ka-pe")] private ClickableText charKataKaPeButton;
        // [UIComponent("char-ka-po")] private ClickableText charKataKaPoButton;

        // [UIComponent("char-ka-xtu")] private ClickableText charKataKaXtuButton;
        // [UIComponent("char-ka-xya")] private ClickableText charKataKaXyaButton;
        // [UIComponent("char-ka-xyu")] private ClickableText charKataKaXyuButton;
        // [UIComponent("char-ka-xyo")] private ClickableText charKataKaXyoButton;


        public static MemoEditModal GetInstance(
            MemoEntry entry,
            MemoPanelController parent,
            string key,
            string songName,
            string songAuthor)
        {
            if (ReferenceEquals(Instance, null))
            {
                Plugin.Log?.Info("MemoEditModal.GetInstance: creating new modal instance");
                Instance = BeatSaberUI.CreateViewController<MemoEditModal>();

                Instance.ParseBSML(
                    Utilities.GetResourceContent(
                        typeof(MemoEditModal).Assembly,
                        "MapMemo.Resources.MemoEdit.bsml"),
                        parent.HostGameObject);
            }
            // Instance.parentPanel = parent;
            Instance.key = key;
            Instance.songName = songName;
            Instance.songAuthor = songAuthor;
            // Instance.memoText.maxVisibleLines = 5;
            Instance.memo = entry?.memo ?? "";
            Instance.lastUpdated.text = entry != null ? "Updated:" + FormatLocal(entry.updatedAt) : "";
            if (Instance.memoText != null)
            {
                Instance.memoText.text = Instance.memo;
                Instance.confirmedText = Instance.memo;
                Instance.pendingText = "";
                Instance.memoText.richText = true;
                Instance.memoText.ForceMeshUpdate();
            }
            // A〜Z ボタンの見た目を整えるヘルパーを呼び出す
            ApplyAlphaButtonCosmetics(Instance);
            return Instance;
        }

        [UIAction("#post-parse")]
         private void OnPostParse()
        {
            Plugin.Log?.Info("MemoEditModal: OnPostParse called — setting up pick list");
            suggestionList.TableView.didSelectCellWithIdxEvent += OnCellSelected;
            suggestionList.CellSizeValue = 6f;
            suggestionList.ExpandCell = true;

            suggestionList.Data.Clear();
            suggestionList.Data.Add(new CustomListTableData.CustomCellInfo("あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよわをん"));
            suggestionList.Data.Add(new CustomListTableData.CustomCellInfo("🦊 きつね"));
            suggestionList.Data.Add(new CustomListTableData.CustomCellInfo("💧 しずく"));
            suggestionList.Data.Add(new CustomListTableData.CustomCellInfo("💧 しずく"));
            suggestionList.Data.Add(new CustomListTableData.CustomCellInfo("💧 しずく"));
            suggestionList.Data.Add(new CustomListTableData.CustomCellInfo("💧 しずく"));
            suggestionList.Data.Add(new CustomListTableData.CustomCellInfo("💧 しずく"));
            suggestionList.Data.Add(new CustomListTableData.CustomCellInfo("💧 しずく"));
            suggestionList.Data.Add(new CustomListTableData.CustomCellInfo("💧 しずく"));
            suggestionList.TableView.ReloadData();
            
        }
        private void OnCellSelected(TableView tableView, int index)
        {
            var selected = suggestionList.Data[index];
            Plugin.Log?.Info($"選択されたのは: {selected.Text.ToString()}");

            // ここに処理を書く！
            AppendSelectedString(selected.Text.ToString());
        }
// private bool _shouldSetupScroll = false;

// [UIAction("#post-parse")]
// private void OnPostParse()
// {
//     Plugin.Log?.Info("🍄 OnPostParse called — deferring setup to OnEnable");
//     _shouldSetupScroll = true;

//     // GameObjectがすでにアクティブなら、OnEnableはもう呼ばれないのでここで呼ぶ！
//     if (gameObject.activeInHierarchy)
//     {
//         Plugin.Log?.Info("🌿 OnEnable already happened — starting coroutine now");
//         StartCoroutine(WaitAndSetupScroll());
//         _shouldSetupScroll = false;
//     }
// }



// private void OnEnable()
// {
//     Plugin.Log?.Info("🍄 OnEnable called");

//     if (_shouldSetupScroll)
//     {
//         Plugin.Log?.Info("🌿 Deferred scroll setup — starting now");
//         StartCoroutine(WaitAndSetupScroll());
//         _shouldSetupScroll = false;
//     }
// }
        

        private void CommitMemo()
        {
            // 確定処理
            confirmedText += pendingText;
            pendingText = "";
            memo = confirmedText;
            NotifyPropertyChanged("memo");
            if (memoText != null)
            {
                memoText.text = memo;
                memoText.ForceMeshUpdate();
            }
        }

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
            var modalCtrl = MemoEditModal.GetInstance(existing, parent, key, songName, songAuthor);

            Plugin.Log?.Info("MemoEditModal.Show: reusing existing parsed modal instance");
            // 表示は既にバインド済みの modal を利用して行う
            try
            {
                Plugin.Log?.Info("MemoEditModal.Show: showing modal" + (ReferenceEquals(modalCtrl.modal, null) ? " modal=null" : " modal!=null"));
                modalCtrl.modal?.Show(true, true);
                // 画面の左側半分あたりに表示するように位置調整
                RepositionModalToLeftHalf(modalCtrl.modal);
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.Warn($"MemoEditModal.Show: ModalView.Show failed: {ex.Message}; modal may not be visible");
            }
        }

        /// BSMLをパースする
        public void ParseBSML(string bsml, GameObject host)
        {
            BSMLParser.Instance.Parse(bsml, host, this);
            Plugin.Log?.Info("MemoEditModal: BSML parsed and attached to host '" + host.name + "'" + (ReferenceEquals(modal, null) ? " modal=null" : " modal!=null"));
        }

        [UIAction("on-save")]
        public async void OnSave()
        {
            try
            {
                var text = confirmedText + pendingText ?? "";
                //if (text.Length > 256) text = text.Substring(0, 256);
                var entry = new MemoEntry { 
                    key = key ?? "unknown", 
                    songName = songName ?? "unknown", 
                    songAuthor = songAuthor ?? "unknown", 
                    memo = text };
                Plugin.Log?.Info($"MemoEditModal.OnSave: key='{entry.key}' song='{entry.songName}' author='{entry.songAuthor}' len={text.Length}");
                lastUpdated.text = FormatLocal(DateTime.UtcNow);

                await MemoRepository.SaveAsync(entry);
                // 表示更新（transform未参照で安全にフォールバック）
                // var parentPanelLocal = this.parentPanel ?? MemoPanelController.instance;

                // 親パネルの反映
                var parentPanelLocal = MemoPanelController.instance;
                // 確定状態にする
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
        private void AppendSelectedString(string s)
        {
            foreach (var ch in s)
            {
                Append(ch.ToString());
            }
        }

        private void Append(string s)
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

            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal.Append: len-after={memo.Length}");
            NotifyPropertyChanged("memo");
            if (memoText != null)
            {
                memoText.text = memo;
                memoText.ForceMeshUpdate();
            }
        }

        int GetLastLineLength(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var lastLine = lines.LastOrDefault() ?? "";
            return lastLine.Length;
        }

        private bool isOverMaxLine(string text, int maxLines)
        {
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            return lines.Length + 1 > maxLines;
        }

        private string GetPendingText()
        {
            return "<color=#FFFF00><u>" + pendingText + "</u></color>";
        }

        private static string FormatLocal(DateTime utc)
        {
            var local = utc.ToLocalTime();
            return $"{local:yyyy/MM/dd HH:mm:ss}";
        }
        // private static bool IsHalfWidth(string s)
        // {
        //     return System.Text.RegularExpressions.Regex.IsMatch(s, @"^[\u0020-\u007E]+$");
        // }
        private static void RepositionModalToLeftHalf(HMUI.ModalView modal)
        {
            if (ReferenceEquals(modal, null)) return;
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
                    MapMemo.Plugin.Log?.Info($"MemoEditModal.RepositionModalToLeftHalf: shifted modal anchoredPosition by {offsetX} (newX={rt.anchoredPosition.x})");
                }
            }
            catch (System.Exception ex)
            {
                MapMemo.Plugin.Log?.Warn($"MemoEditModal.RepositionModalToLeftHalf: exception {ex}");
            }
        }

        // Shift 切替時はラベルの差し替えだけ行う（スタイルは既に適用済みの前提）
        private void UpdateAlphaButtonLabels(MemoEditModal ctrl)
        {
            try
            {
                if (ctrl.modal == null)
                {
                    MapMemo.Plugin.Log?.Warn("MemoEditModal.UpdateAlphaButtonLabels: modal is null, cannot collect buttons");
                }
                if (ctrl.modal != null && ctrl.modal.gameObject == null)
                {
                    MapMemo.Plugin.Log?.Warn("MemoEditModal.UpdateAlphaButtonLabels: modal.gameObject is null, cannot collect buttons");
                }  
                Plugin.Log?.Info("MemoEditModal.UpdateAlphaButtonLabels: " 
                    + ctrl.modal.gameObject.GetComponentsInChildren<ClickableText>(true).Count() 
                    + " ClickableText components found under modal");
                foreach (var btn in ctrl.modal.gameObject.GetComponentsInChildren<ClickableText>(true))
                {

                    var stored = btn.text.Trim().Replace("　", ""); // 全角スペースを取り除く   

                    // if (btn == null || string.IsNullOrEmpty(stored)) continue;
                    // var ch = stored.FirstOrDefault();
                    // if (ch == default) continue;
                    var label = isShift ? stored.ToLowerInvariant() : stored.ToUpperInvariant();
                    btn.text = EditLabel(label);
                }
            }
            catch { }
        }

        private static string EditLabel(string label)
        {
            return "  " + label + "  "; 
        }

        // 一括で A〜Z ボタンにスタイルを適用するヘルパー
        // Reflection を使って private フィールド `charAButton`..`charZButton` を取得し、見た目を整えます。
        private static void ApplyAlphaButtonCosmetics(MemoEditModal ctrl)
        {
            if (ReferenceEquals(ctrl, null)) return;
            try
            {
                var collected = new System.Collections.Generic.HashSet<ClickableText>();
                if (ctrl.modal == null)
                {
                    MapMemo.Plugin.Log?.Warn("MemoEditModal.ApplyAlphaButtonCosmetics: modal is null, cannot collect buttons");
                }
                if (ctrl.modal != null && ctrl.modal.gameObject == null)
                {
                    MapMemo.Plugin.Log?.Warn("MemoEditModal.ApplyAlphaButtonCosmetics: modal.gameObject is null, cannot collect buttons");
                }  
                Plugin.Log?.Info("MemoEditModal.ApplyAlphaButtonCosmetics: " 
                    + ctrl.modal.gameObject.GetComponentsInChildren<ClickableText>(true).Count() 
                    + " ClickableText components found under modal");
                // 2) BSML バインド済みの modal 配下（あれば）
                // if (ctrl.modal != null && ctrl.modal.gameObject != null)
                // {
                //     foreach (var b in ctrl.modal.gameObject.GetComponentsInChildren<ClickableText>(true))
                //     {
                //         collected.Add(b);
                //     } 
                // }
                            
                // 収集したボタンに一括でスタイルを適用
                foreach (var btn in ctrl.modal.gameObject.GetComponentsInChildren<ClickableText>(true))
                {
                    try
                    {
                        if (btn == null) continue;
                        btn.fontSize = 3.8f;
                        btn.fontStyle = FontStyles.Italic | FontStyles.Underline;
                        btn.alignment = TextAlignmentOptions.Center;
                        // 特定の色以外も設定できるようにするため、ScriptableObject由来の色設定は無効化
                        
                        //btn.useScriptableObjectColors = true;
                        btn.color = Color.cyan;
                        //btn.faceColorはHighlightColorに影響するため設定しない
                        btn.DefaultColor = Color.cyan;
                        btn.HighlightColor = new Color(1f, 0.3f, 0f, 1f); // RGB: (255, 77, 0)// new Color(1f, 0f, 0f, 1f);
                        btn.outlineColor = Color.yellow;
                        btn.outlineWidth = 0.3f;

                        // グロー(これではうまくいかないので一旦コメントアウト)
                        // btn.fontMaterial.EnableKeyword("GLOW_ON");
                        // btn.fontMaterial.SetColor("_GlowColor", new Color(1f, 0.3f, 0f));
                        // btn.fontMaterial.SetFloat("_GlowPower", 0.5f);

                        // リッチテキストが有効になるのは初回だけのためコメントアウト
                        //btn.richText = true;

                        // フォントを設定すると座標がずれる問題があるため一旦コメントアウト
                        // var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                        // var font = fonts.FirstOrDefault(f => f.name.Contains("Assistant SDF")); // 使いたいフォント名に合わせて

                        // if (font != null)
                        // {
                        //     btn.font = font;
                        // }

                        var layout = btn.gameObject.GetComponent<LayoutElement>();
                        if (layout == null)
                            layout = btn.gameObject.AddComponent<LayoutElement>();
                        layout.preferredWidth = 5f;
                        layout.minWidth = 5f;

                        var label = btn.text.Trim().Replace(" ", "");;
                        label = ctrl.isShift ? label.ToLowerInvariant() : label.ToUpperInvariant();

                        // if(IsHalfWidth(label))
                        // {
                        label = EditLabel(label); // 全角スペースで囲む
                        // }
                        btn.text = label;
                    }
                    catch { /* 個別のボタン処理失敗は無視して続行 */ }
                }
            }
            catch { /* 全体取得に失敗しても崩壊させない */ }
        }

        [UIAction("on-char-a")] private void OnCharA() => Append("あ");
        [UIAction("on-char-i")] private void OnCharI() => Append("い");
        [UIAction("on-char-u")] private void OnCharU() => Append("う");
        [UIAction("on-char-e")] private void OnCharE() => Append("え");
        [UIAction("on-char-o")] private void OnCharO() => Append("お");

        [UIAction("on-char-ka")] private void OnCharKa() => Append("か");
        [UIAction("on-char-ki")] private void OnCharKi() => Append("き");
        [UIAction("on-char-ku")] private void OnCharKu() => Append("く");
        [UIAction("on-char-ke")] private void OnCharKe() => Append("け");
        [UIAction("on-char-ko")] private void OnCharKo() => Append("こ");

        [UIAction("on-char-sa")] private void OnCharSa() => Append("さ");
        [UIAction("on-char-shi")] private void OnCharShi() => Append("し");
        [UIAction("on-char-su")] private void OnCharSu() => Append("す");
        [UIAction("on-char-se")] private void OnCharSe() => Append("せ");
        [UIAction("on-char-so")] private void OnCharSo() => Append("そ");

        [UIAction("on-char-ta")] private void OnCharTa() => Append("た");
        [UIAction("on-char-chi")] private void OnCharChi() => Append("ち");
        [UIAction("on-char-tsu")] private void OnCharTsu() => Append("つ");
        [UIAction("on-char-te")] private void OnCharTe() => Append("て");
        [UIAction("on-char-to")] private void OnCharTo() => Append("と");

        [UIAction("on-char-na")] private void OnCharNa() => Append("な");
        [UIAction("on-char-ni")] private void OnCharNi() => Append("に");
        [UIAction("on-char-nu")] private void OnCharNu() => Append("ぬ");
        [UIAction("on-char-ne")] private void OnCharNe() => Append("ね");
        [UIAction("on-char-no")] private void OnCharNo() => Append("の");

        [UIAction("on-char-ha")] private void OnCharHa() => Append("は");
        [UIAction("on-char-hi")] private void OnCharHi() => Append("ひ");
        [UIAction("on-char-fu")] private void OnCharFu() => Append("ふ");
        [UIAction("on-char-he")] private void OnCharHe() => Append("へ");
        [UIAction("on-char-ho")] private void OnCharHo() => Append("ほ");

        [UIAction("on-char-ma")] private void OnCharMa() => Append("ま");
        [UIAction("on-char-mi")] private void OnCharMi() => Append("み");
        [UIAction("on-char-mu")] private void OnCharMu() => Append("む");
        [UIAction("on-char-me")] private void OnCharMe() => Append("め");
        [UIAction("on-char-mo")] private void OnCharMo() => Append("も");

        [UIAction("on-char-ya")] private void OnCharYa() => Append("や");
        [UIAction("on-char-yu")] private void OnCharYu() => Append("ゆ");
        [UIAction("on-char-yo")] private void OnCharYo() => Append("よ");
        [UIAction("on-char-wa")] private void OnCharWa() => Append("わ");
        [UIAction("on-char-wo")] private void OnCharWo() => Append("を");

        [UIAction("on-char-n")] private void OnCharN() => Append("ん");
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
                if (memoText != null)
                {
                    memoText.text = memo;
                    memoText.ForceMeshUpdate();
                }
                return;
            }
            
            if (string.IsNullOrEmpty(confirmedText)) return;
            confirmedText = confirmedText.Substring(0, confirmedText.Length - 1);
            memo = confirmedText;
            NotifyPropertyChanged("memo");
            if (memoText != null)
            {
                memoText.text = memo;
                memoText.ForceMeshUpdate();
            }
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

        // ら行
        [UIAction("on-char-ra")] private void OnCharRa() => Append("ら");
        [UIAction("on-char-ri")] private void OnCharRi() => Append("り");
        [UIAction("on-char-ru")] private void OnCharRu() => Append("る");
        [UIAction("on-char-re")] private void OnCharRe() => Append("れ");
        [UIAction("on-char-ro")] private void OnCharRo() => Append("ろ");

        // 濁点
        [UIAction("on-char-ga")] private void OnCharGa() => Append("が");
        [UIAction("on-char-gi")] private void OnCharGi() => Append("ぎ");
        [UIAction("on-char-gu")] private void OnCharGu() => Append("ぐ");
        [UIAction("on-char-ge")] private void OnCharGe() => Append("げ");
        [UIAction("on-char-go")] private void OnCharGo() => Append("ご");
        [UIAction("on-char-za")] private void OnCharZa() => Append("ざ");
        [UIAction("on-char-ji")] private void OnCharJi() => Append("じ");
        [UIAction("on-char-zu")] private void OnCharZu() => Append("ず");
        [UIAction("on-char-ze")] private void OnCharZe() => Append("ぜ");
        [UIAction("on-char-zo")] private void OnCharZo() => Append("ぞ");
        [UIAction("on-char-da")] private void OnCharDa() => Append("だ");
        [UIAction("on-char-di")] private void OnCharDi() => Append("ぢ");
        [UIAction("on-char-du")] private void OnCharDu() => Append("づ");
        [UIAction("on-char-de")] private void OnCharDe() => Append("で");
        [UIAction("on-char-do")] private void OnCharDo() => Append("ど");
        [UIAction("on-char-ba")] private void OnCharBa() => Append("ば");
        [UIAction("on-char-bi")] private void OnCharBi() => Append("び");
        [UIAction("on-char-bu")] private void OnCharBu() => Append("ぶ");
        [UIAction("on-char-be")] private void OnCharBe() => Append("べ");
        [UIAction("on-char-bo")] private void OnCharBo() => Append("ぼ");

        // 半濁点
        [UIAction("on-char-pa")] private void OnCharPa() => Append("ぱ");
        [UIAction("on-char-pi")] private void OnCharPi() => Append("ぴ");
        [UIAction("on-char-pu")] private void OnCharPu() => Append("ぷ");
        [UIAction("on-char-pe")] private void OnCharPe() => Append("ぺ");
        [UIAction("on-char-po")] private void OnCharPo() => Append("ぽ");

        // 小文字（ひらがな）
        [UIAction("on-char-xtu")] private void OnCharXtu() => Append("っ");
        [UIAction("on-char-xya")] private void OnCharXya() => Append("ゃ");
        [UIAction("on-char-xyu")] private void OnCharXyu() => Append("ゅ");
        [UIAction("on-char-xyo")] private void OnCharXyo() => Append("ょ");

        // カタカナ基本
        [UIAction("on-char-ka-a")] private void OnCharKaA() => Append("ア");
        [UIAction("on-char-ka-i")] private void OnCharKaI() => Append("イ");
        [UIAction("on-char-ka-u")] private void OnCharKaU() => Append("ウ");
        [UIAction("on-char-ka-e")] private void OnCharKaE() => Append("エ");
        [UIAction("on-char-ka-o")] private void OnCharKaO() => Append("オ");
        [UIAction("on-char-ka-ka")] private void OnCharKaKa() => Append("カ");
        [UIAction("on-char-ka-ki")] private void OnCharKaKi() => Append("キ");
        [UIAction("on-char-ka-ku")] private void OnCharKaKu() => Append("ク");
        [UIAction("on-char-ka-ke")] private void OnCharKaKe() => Append("ケ");
        [UIAction("on-char-ka-ko")] private void OnCharKaKo() => Append("コ");
        [UIAction("on-char-ka-sa")] private void OnCharKaSa() => Append("サ");
        [UIAction("on-char-ka-shi")] private void OnCharKaShi() => Append("シ");
        [UIAction("on-char-ka-su")] private void OnCharKaSu() => Append("ス");
        [UIAction("on-char-ka-se")] private void OnCharKaSe() => Append("セ");
        [UIAction("on-char-ka-so")] private void OnCharKaSo() => Append("ソ");
        [UIAction("on-char-ka-ta")] private void OnCharKaTa() => Append("タ");
        [UIAction("on-char-ka-chi")] private void OnCharKaChi() => Append("チ");
        [UIAction("on-char-ka-tsu")] private void OnCharKaTsu() => Append("ツ");
        [UIAction("on-char-ka-te")] private void OnCharKaTe() => Append("テ");
        [UIAction("on-char-ka-to")] private void OnCharKaTo() => Append("ト");
        [UIAction("on-char-ka-na")] private void OnCharKaNa() => Append("ナ");
        [UIAction("on-char-ka-ni")] private void OnCharKaNi() => Append("ニ");
        [UIAction("on-char-ka-nu")] private void OnCharKaNu() => Append("ヌ");
        [UIAction("on-char-ka-ne")] private void OnCharKaNe() => Append("ネ");
        [UIAction("on-char-ka-no")] private void OnCharKaNo() => Append("ノ");
        [UIAction("on-char-ka-ha")] private void OnCharKaHa() => Append("ハ");
        [UIAction("on-char-ka-hi")] private void OnCharKaHi() => Append("ヒ");
        [UIAction("on-char-ka-fu")] private void OnCharKaFu() => Append("フ");
        [UIAction("on-char-ka-he")] private void OnCharKaHe() => Append("ヘ");
        [UIAction("on-char-ka-ho")] private void OnCharKaHo() => Append("ホ");
        [UIAction("on-char-ka-ma")] private void OnCharKaMa() => Append("マ");
        [UIAction("on-char-ka-mi")] private void OnCharKaMi() => Append("ミ");
        [UIAction("on-char-ka-mu")] private void OnCharKaMu() => Append("ム");
        [UIAction("on-char-ka-me")] private void OnCharKaMe() => Append("メ");
        [UIAction("on-char-ka-mo")] private void OnCharKaMo() => Append("モ");
        [UIAction("on-char-ka-ya")] private void OnCharKaYa() => Append("ヤ");
        [UIAction("on-char-ka-yu")] private void OnCharKaYu() => Append("ユ");
        [UIAction("on-char-ka-yo")] private void OnCharKaYo() => Append("ヨ");
        [UIAction("on-char-ka-wa")] private void OnCharKaWa() => Append("ワ");
        [UIAction("on-char-ka-wo")] private void OnCharKaWo() => Append("ヲ");
        [UIAction("on-char-ka-ra")] private void OnCharKaRa() => Append("ラ");
        [UIAction("on-char-ka-ri")] private void OnCharKaRi() => Append("リ");
        [UIAction("on-char-ka-ru")] private void OnCharKaRu() => Append("ル");
        [UIAction("on-char-ka-re")] private void OnCharKaRe() => Append("レ");
        [UIAction("on-char-ka-ro")] private void OnCharKaRo() => Append("ロ");
        [UIAction("on-char-ka-n")] private void OnCharKaN() => Append("ン");

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
            try
            {
                UpdateAlphaButtonLabels(this);
            }
            catch (Exception e)
            {
                Plugin.Log?.Warn($"MemoEditModal.OnCharShift: UpdateAlphaButtonLabels failed: {e.Message}");
            }
        }
        [UIAction("on-char-enter")]
        private void OnCharEnter()
        {
            if(pendingText.Length > 0)
            {
                // 未確定文字を確定文字にする
                CommitMemo();
            }
            else
            {
                Append("\n");
            }
            
        }
    }
}