using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using TMPro;
// Note: Avoid UnityEngine.UI dependency; use UnityEngine.Canvas explicitly
using HMUI;

namespace MapMemo.UI
{
    public class MemoEditModal : BSMLAutomaticViewController
    {
        public string ResourceName => "MapMemo.Resources.MemoEdit.bsml";

        private string key;
        private string songName;
        private string songAuthor;

        [UIValue("memo")] private string memo = "";
        [UIComponent("modal")] private ModalView modal;
        [UIComponent("memoText")] private TextMeshProUGUI memoText;



        public static async void Show(MemoPanelController parent, string key, string songName, string songAuthor)
        {
            var modalCtrl = new MemoEditModal();
            modalCtrl.key = key;
            modalCtrl.songName = songName;
            modalCtrl.songAuthor = songAuthor;
            // 既存メモの読み込み
            var existing = await MemoRepository.LoadAsync(key, songName, songAuthor);
            modalCtrl.memo = existing?.memo ?? "";
            var content = Utilities.GetResourceContent(typeof(MemoEditModal).Assembly, "MapMemo.Resources.MemoEdit.bsml");
            if (string.IsNullOrEmpty(content))
            {
                MapMemo.Plugin.Log?.Error("MemoEditModal.Show: BSML content not found for MapMemo.Resources.MemoEdit.bsml");
                return;
            }
            // フォールバック: parent が null の場合は現在のコントローラを検索してメニュー右パネルに挿入
            GameObject host = null;
            if (parent != null)
            {
                host = parent.HostGameObject ?? (parent.transform != null ? parent.transform.gameObject : null);
            }
            else
            {
                MapMemo.Plugin.Log?.Warn("MemoEditModal.Show: parent is null; looking up existing MemoPanelController");
                var existingPanel = MemoPanelController.LastInstance ?? Resources.FindObjectsOfTypeAll<MemoPanelController>().FirstOrDefault();
                host = existingPanel != null ? (existingPanel.HostGameObject ?? existingPanel.transform.gameObject) : null;
                if (host == null && LevelDetailInjector.LastHostGameObject != null)
                {
                    MapMemo.Plugin.Log?.Warn("MemoEditModal.Show: using LevelDetailInjector.LastHostGameObject");
                    host = LevelDetailInjector.LastHostGameObject;
                }
            }
            // 標準レベル詳細ビュー（名前ベース）を優先してホストに使用（型参照なし）
            if (host == null)
            {
                MapMemo.Plugin.Log?.Warn("MemoEditModal.Show: host not found from panel; attempting LevelDetail container lookup (name-based)");
                var t = Resources.FindObjectsOfTypeAll<Transform>()
                    .FirstOrDefault(x => x.name.IndexOf("StandardLevelDetailView", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         x.name.IndexOf("LevelDetail", System.StringComparison.OrdinalIgnoreCase) >= 0);
                host = t != null ? t.gameObject : null;
            }
            // 最終フォールバック: シーン内の任意の Canvas を持つ GameObject を探索（型名で検索）
            if (host == null)
            {
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                var canvasObj = allObjects.FirstOrDefault(go => go.GetComponent("Canvas") != null);
                // MainMenu を含む名前を優先
                var mainMenuCanvasObj = allObjects.FirstOrDefault(go => go.name.IndexOf("MainMenu", System.StringComparison.OrdinalIgnoreCase) >= 0 && go.GetComponent("Canvas") != null);
                host = (mainMenuCanvasObj ?? canvasObj);
                if (host != null)
                {
                    MapMemo.Plugin.Log?.Warn($"MemoEditModal.Show: using canvas host '{host.name}'");
                }
            }
            if (host == null)
            {
                MapMemo.Plugin.Log?.Error("MemoEditModal.Show: could not resolve host GameObject for modal");
                return;
            }
            MapMemo.Plugin.Log?.Info($"MemoEditModal.Show: parsing BSML on host '{host.name}'");
            try
            {
                BSMLParser.Instance.Parse(content, host, modalCtrl);
            }
            catch (System.Exception ex)
            {
                MapMemo.Plugin.Log?.Error($"MemoEditModal.Show: BSML parse failed: {ex}");
            }
            if (modalCtrl.modal != null)
            {
                MapMemo.Plugin.Log?.Info("MemoEditModal.Show: modal component bound; showing");
                modalCtrl.modal.Show(true);
            }
            else
            {
                MapMemo.Plugin.Log?.Warn("MemoEditModal.Show: modal component is null after parse; attempting fallback lookup");
                var anyModal = Resources.FindObjectsOfTypeAll<HMUI.ModalView>()
                    .FirstOrDefault(m => m.name == "modal" || m.name.IndexOf("MemoEdit", System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (anyModal != null)
                {
                    MapMemo.Plugin.Log?.Info($"MemoEditModal.Show: found ModalView '{anyModal.name}'; showing");
                    anyModal.Show(true);
                }
                else
                {
                    MapMemo.Plugin.Log?.Error("MemoEditModal.Show: could not find ModalView to show");
                }
            }
        }

        [UIAction("on-save")]
        public async void OnSave()
        {
            try
            {
                var text = memo ?? "";
                if (text.Length > 256) text = text.Substring(0, 256);
                var entry = new MemoEntry { key = key ?? "unknown", songName = songName ?? "unknown", songAuthor = songAuthor ?? "unknown", memo = text };
                MapMemo.Plugin.Log?.Info($"MemoEditModal.OnSave: key='{entry.key}' song='{entry.songName}' author='{entry.songAuthor}' len={text.Length}");
                await MemoRepository.SaveAsync(entry);
                // 表示更新（transform未参照で安全にフォールバック）
                var parentPanel = MemoPanelController.LastInstance;
                if (parentPanel != null)
                {
                    MapMemo.Plugin.Log?.Info("MemoEditModal.OnSave: refreshing MemoPanelController");
                    await parentPanel.RefreshAsync();
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
            MapMemo.Plugin.Log?.Info($"MemoEditModal.Append: add='{s}' code={(int)s[0]} len-before={memo.Length}");
            var appended = memo + s;
            if (appended.Length > 256) appended = appended.Substring(0, 256);
            memo = appended;
            MapMemo.Plugin.Log?.Info($"MemoEditModal.Append: len-after={memo.Length}");
            NotifyPropertyChanged("memo");
            if (memoText != null)
            {
                memoText.text = memo;
                memoText.ForceMeshUpdate();
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
        [UIAction("on-char-A")] private void OnCharA_Eng() => Append("A");
        [UIAction("on-char-B")] private void OnCharB() => Append("B");
        [UIAction("on-char-C")] private void OnCharC() => Append("C");
        [UIAction("on-char-D")] private void OnCharD() => Append("D");
        [UIAction("on-char-E")] private void OnCharE_Eng() => Append("E");
        [UIAction("on-char-F")] private void OnCharF() => Append("F");
        [UIAction("on-char-G")] private void OnCharG() => Append("G");
        [UIAction("on-char-H")] private void OnCharH() => Append("H");
        [UIAction("on-char-I")] private void OnCharI_Eng() => Append("I");
        [UIAction("on-char-J")] private void OnCharJ() => Append("J");
        [UIAction("on-char-K")] private void OnCharK() => Append("K");
        [UIAction("on-char-L")] private void OnCharL() => Append("L");
        [UIAction("on-char-M")] private void OnCharM() => Append("M");
        [UIAction("on-char-N")] private void OnCharN_Eng() => Append("N");
        [UIAction("on-char-O")] private void OnCharO_Eng() => Append("O");
        [UIAction("on-char-P")] private void OnCharP() => Append("P");
        [UIAction("on-char-Q")] private void OnCharQ() => Append("Q");
        [UIAction("on-char-R")] private void OnCharR() => Append("R");
        [UIAction("on-char-S")] private void OnCharS() => Append("S");
        [UIAction("on-char-T")] private void OnCharT() => Append("T");
        [UIAction("on-char-U")] private void OnCharU_Eng() => Append("U");
        [UIAction("on-char-V")] private void OnCharV() => Append("V");
        [UIAction("on-char-W")] private void OnCharW() => Append("W");
        [UIAction("on-char-X")] private void OnCharX() => Append("X");
        [UIAction("on-char-Y")] private void OnCharY() => Append("Y");
        [UIAction("on-char-Z")] private void OnCharZ() => Append("Z");

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
    }
}