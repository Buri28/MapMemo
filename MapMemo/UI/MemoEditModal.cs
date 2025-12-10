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

namespace MapMemo.UI
{
    public class MemoEditModal : BSMLAutomaticViewController
    {
        // 再利用可能なシングルトンインスタンス
        public static MemoEditModal Instance = new MemoEditModal();

        // Show() から渡された親パネル参照を保持して、保存後に正しいパネルを更新できるようにする
        private MemoPanelController parentPanel = null;
        private bool isParsed = false;
        private string key;
        private string songName;
        private string songAuthor;
        // Shift 状態（true = 小文字モード）
        private bool isShift = false;
        [UIValue("memo")] private string memo = "";
        [UIComponent("modal")] private ModalView modal;
        [UIComponent("memoText")] private TextMeshProUGUI memoText;
        [UIComponent("last-updated")] private TextMeshProUGUI lastUpdated;
        [UIComponent("char-A")] private ClickableText charAButton;
        [UIComponent("char-B")] private ClickableText charBButton;
        [UIComponent("char-C")] private ClickableText charCButton;
        [UIComponent("char-D")] private ClickableText charDButton;
        [UIComponent("char-E")] private ClickableText charEButton;
        [UIComponent("char-F")] private ClickableText charFButton;
        [UIComponent("char-G")] private ClickableText charGButton;
        [UIComponent("char-H")] private ClickableText charHButton;
        [UIComponent("char-I")] private ClickableText charIButton;
        [UIComponent("char-J")] private ClickableText charJButton;
        [UIComponent("char-K")] private ClickableText charKButton;
        [UIComponent("char-L")] private ClickableText charLButton;
        [UIComponent("char-M")] private ClickableText charMButton;
        [UIComponent("char-N")] private ClickableText charNButton;
        [UIComponent("char-O")] private ClickableText charOButton;
        [UIComponent("char-P")] private ClickableText charPButton;
        [UIComponent("char-Q")] private ClickableText charQButton;
        [UIComponent("char-R")] private ClickableText charRButton;
        [UIComponent("char-S")] private ClickableText charSButton;
        [UIComponent("char-T")] private ClickableText charTButton;
        [UIComponent("char-U")] private ClickableText charUButton;
        [UIComponent("char-V")] private ClickableText charVButton;
        [UIComponent("char-W")] private ClickableText charWButton;
        [UIComponent("char-X")] private ClickableText charXButton;
        [UIComponent("char-Y")] private ClickableText charYButton;
        [UIComponent("char-Z")] private ClickableText charZButton;

        public static MemoEditModal GetInstance(
            MemoEntry entry,
            MemoPanelController parent,
            string key,
            string songName,
            string songAuthor)
        {
            if (!Instance.isParsed)
            {
                // BSMLをパース指定なければパースする
                Instance.ParseBSML(
                    Utilities.GetResourceContent(
                        typeof(MemoEditModal).Assembly,
                        "MapMemo.Resources.MemoEdit.bsml"),
                    ResolveHost(parent));
                Instance.isParsed = true;
            }
            Instance.parentPanel = parent;
            Instance.key = key;
            Instance.songName = songName;
            Instance.songAuthor = songAuthor;
            Instance.memo = entry?.memo ?? "";
            Instance.lastUpdated.text = entry != null ? "Updated:" + FormatLocal(entry.updatedAt) : "";
            if (Instance.memoText != null)
            {
                Instance.memoText.text = Instance.memo;
                Instance.memoText.ForceMeshUpdate();
            }
            // A〜Z ボタンの見た目を整えるヘルパーを呼び出す
            ApplyAlphaButtonCosmetics(Instance);
            return Instance;
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

            MapMemo.Plugin.Log?.Info("MemoEditModal.Show: reusing existing parsed modal instance");
            // 表示は既にバインド済みの modal を利用して行う
            try
            {
                modalCtrl.modal?.Show(true, true);
                // 画面の左側半分あたりに表示するように位置調整
                RepositionModalToLeftHalf(modalCtrl.modal);
            }
            catch (System.Exception ex)
            {
                MapMemo.Plugin.Log?.Warn($"MemoEditModal.Show: ModalView.Show failed: {ex.Message}; modal may not be visible");
            }
        }
        /// BSMLをパースする
        public void ParseBSML(string bsml, GameObject host)
        {
            BSMLParser.Instance.Parse(bsml, host, this);
        }

        [UIAction("on-save")]
        public async void OnSave()
        {
            try
            {
                var text = memo ?? "";
                //if (text.Length > 256) text = text.Substring(0, 256);
                var entry = new MemoEntry { key = key ?? "unknown", songName = songName ?? "unknown", songAuthor = songAuthor ?? "unknown", memo = text };
                MapMemo.Plugin.Log?.Info($"MemoEditModal.OnSave: key='{entry.key}' song='{entry.songName}' author='{entry.songAuthor}' len={text.Length}");
                // UI は Unity のメインスレッドで描画されるため、保存待ちの await によって
                // 続きがスレッドプールで実行されると NotifyPropertyChanged が効かない場合があります。
                // そのため、まず表示上の更新日時を先に反映してからファイル保存を行います。
                // TODO　: Saveボタンを押しても画面上で更新されない 
                lastUpdated.text = FormatLocal(DateTime.UtcNow);
                NotifyPropertyChanged("last-updated");
                await MemoRepository.SaveAsync(entry);
                // 表示更新（transform未参照で安全にフォールバック）
                var parentPanelLocal = this.parentPanel ?? MemoPanelController.instance;
                if (parentPanelLocal != null)
                {
                    // Save ボタンで反映すべきは親パネルの表示上の更新日時のみ。
                    try
                    {
                        parentPanelLocal.SetUpdatedLocal(DateTime.UtcNow);
                    }
                    catch { }
                    MapMemo.Plugin.Log?.Info("MemoEditModal.OnSave: refreshing MemoPanelController");
                    await parentPanelLocal.Refresh();
                }
                else
                {
                    MapMemo.Plugin.Log?.Warn("MemoEditModal.OnSave: MemoPanelController not found for refresh");
                }
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
        private void Append(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            if (memo == null) memo = "";
            if (MapMemo.Plugin.VerboseLogs) MapMemo.Plugin.Log?.Info($"MemoEditModal.Append: add='{s}' code={(int)s[0]} len-before={memo.Length}");
            var appended = memo + s;
            if (appended.Length > 256) appended = appended.Substring(0, 256);
            memo = appended;
            if (MapMemo.Plugin.VerboseLogs) MapMemo.Plugin.Log?.Info($"MemoEditModal.Append: len-after={memo.Length}");
            NotifyPropertyChanged("memo");
            if (memoText != null)
            {
                memoText.text = memo;
                memoText.ForceMeshUpdate();
            }
        }

        private static string FormatLocal(DateTime utc)
        {
            var local = utc.ToLocalTime();
            return $"{local:yyyy/MM/dd HH:mm:ss}";
        }

        // Resolve appropriate host GameObject for parsing the modal BSML.
        // Search order: provided parent.HostGameObject -> existing MemoPanelController host -> LevelDetailInjector.LastHostGameObject
        // -> named LevelDetail/StandardLevelDetailView transform -> MainMenu/Canvas fallback.
        private static GameObject ResolveHost(MemoPanelController parent)
        {
            try
            {
                GameObject host = null;
                if (parent != null)
                {
                    host = parent.HostGameObject ?? (parent.transform != null ? parent.transform.gameObject : null);
                    if (host != null) return host;
                }

                MapMemo.Plugin.Log?.Warn("MemoEditModal.ResolveHost: parent is null or has no host; searching for existing panel host");
                var existingPanel = MemoPanelController.instance ?? Resources.FindObjectsOfTypeAll<MemoPanelController>().FirstOrDefault();
                host = existingPanel != null ? (existingPanel.HostGameObject ?? existingPanel.transform.gameObject) : null;
                if (host != null) return host;

                if (LevelDetailInjector.LastHostGameObject != null)
                {
                    MapMemo.Plugin.Log?.Info("MemoEditModal.ResolveHost: using LevelDetailInjector.LastHostGameObject");
                    return LevelDetailInjector.LastHostGameObject;
                }

                // Name-based search for standard detail view
                var t = Resources.FindObjectsOfTypeAll<Transform>()
                    .FirstOrDefault(x => x.name.IndexOf("StandardLevelDetailView", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         x.name.IndexOf("LevelDetail", System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (t != null) return t.gameObject;

                // Final fallback: use a Canvas (prefer MainMenu canvas)
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                var canvasObj = allObjects.FirstOrDefault(go => go.GetComponent("Canvas") != null);
                var mainMenuCanvasObj = allObjects.FirstOrDefault(go => go.name.IndexOf("MainMenu", System.StringComparison.OrdinalIgnoreCase) >= 0 && go.GetComponent("Canvas") != null);
                host = (mainMenuCanvasObj ?? canvasObj);
                if (host != null)
                {
                    MapMemo.Plugin.Log?.Warn($"MemoEditModal.ResolveHost: using canvas host '{host.name}'");
                    // remember for future small optimizations
                    try { LevelDetailInjector.SetLastHostGameObject(host); } catch { }
                }
                return host;
            }
            catch (Exception e)
            {
                MapMemo.Plugin.Log?.Warn($"MemoEditModal.ResolveHost: error during host resolution: {e.Message}");
                return null;
            }
        }

        // Reposition the given modal so it appears roughly on the left half of the screen.
        // Prioritizes the parent Canvas width when available, otherwise falls back to Screen width.
        private static void RepositionModalToLeftHalf(HMUI.ModalView modal)
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
                    MapMemo.Plugin.Log?.Info($"MemoEditModal.RepositionModalToLeftHalf: shifted modal anchoredPosition by {offsetX} (newX={rt.anchoredPosition.x})");
                }
            }
            catch (System.Exception ex)
            {
                MapMemo.Plugin.Log?.Warn($"MemoEditModal.RepositionModalToLeftHalf: exception {ex}");
            }
        }

        // 一括で A〜Z ボタンにスタイルを適用するヘルパー
        // Reflection を使って private フィールド `charAButton`..`charZButton` を取得し、見た目を整えます。
        private static void ApplyAlphaButtonCosmetics(MemoEditModal ctrl)
        {
            if (ReferenceEquals(ctrl, null)) return;
            for (char ch = 'A'; ch <= 'Z'; ch++)
            {
                try
                {
                    // TODO:効く設定と効かない設定がある
                    var field = typeof(MemoEditModal).GetField($"char{ch}Button", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field == null) continue;
                    var btn = field.GetValue(ctrl) as ClickableText;
                    if (btn == null) continue;
                    // 文字とスタイル設定
                    var label = ctrl.isShift ? ch.ToString().ToLowerInvariant() : ch.ToString().ToUpperInvariant();
                    btn.text = label;
                    //btn.richText = true;
                    if (btn.fontMaterial != null)
                    {
                        btn.fontMaterial.SetFloat("_OutlineWidth", 0.12f);
                        btn.fontMaterial.SetColor("_OutlineColor", Color.white);
                    }
                    btn.color = new Color(1f, 1f, 1f, 0.9f);
                    btn.fontSize = 3.5f;
                    btn.fontStyle = FontStyles.Italic | FontStyles.Underline;
                }
                catch { /* 個別のボタン処理失敗は無視して続行 */ }
            }
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
        [UIAction("on-char-newline")] private void OnCharNewline() => Append("\n");
        [UIAction("on-char-backspace")]
        private void OnCharBackspace()
        {
            if (string.IsNullOrEmpty(memo)) return;
            memo = memo.Substring(0, memo.Length - 1);
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
                ApplyAlphaButtonCosmetics(this);
            }
            catch (Exception e)
            {
                MapMemo.Plugin.Log?.Warn($"MemoEditModal.OnCharShift: ApplyAlphaButtonCosmetics failed: {e.Message}");
            }
        }

    }
}