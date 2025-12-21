
using System;
using System.Linq;
using BeatSaberMarkupLanguage.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MapMemo.UI.Edit
{
    /// <summary>
    /// キークリック用のコントローラー。ClickableText のリスナー設定やボタン外観の初期化を行います。
    /// </summary>
    public class InputKeyController
    {
        private ClickableText[] keys;
        private TextMeshProUGUI[] buttons;

        public InputKeyController(ClickableText[] keys, TextMeshProUGUI[] buttons)
        {
            this.keys = keys;
            this.buttons = buttons;
        }
        /// <summary>
        /// すべての ClickableText に対してクリックリスナーを設定します。
        /// </summary>
        public void SetupKeyClickListeners()
        {
            foreach (var btn in keys)
            {
                if (btn == null || btn.gameObject == null) continue;
                var stored = (btn.text ?? "").Trim();
                //if (string.IsNullOrEmpty(stored) || !IsEmojiString(stored)) continue;
                if (string.IsNullOrEmpty(stored)) continue;

                // Ensure a click listener component is present
                var listener = btn.gameObject.GetComponent<InputKeyClickListener>() ??
                    btn.gameObject.AddComponent<InputKeyClickListener>();
            }
            if (Plugin.VerboseLogs) Plugin.Log?.Info("KeyController.SetupKeyClickListeners: completed key listener setup");
        }

        /// <summary>
        /// クリック可能なテキストコンポーネントの見た目を初期化する
        /// </summary>
        /// <param name="modal"></param>
        /// <param name="isShift"></param>
        public void InitializeAppearance(bool isShift)
        {
            try
            {
                var comps = keys;
                foreach (var btn in comps)
                {
                    if (btn == null) continue;

                    // ボタンの見た目を整える
                    {
                        btn.fontSize = 3.8f;
                        btn.fontStyle = FontStyles.Italic | FontStyles.Underline;
                        btn.alignment = TextAlignmentOptions.Center;
                        btn.color = Color.cyan;
                        btn.DefaultColor = Color.cyan;
                        btn.HighlightColor = new Color(1f, 0.3f, 0f, 1f);
                        btn.outlineColor = Color.yellow;
                        btn.outlineWidth = 0.3f;
                    }
                    // レイアウト要素を追加して幅を制限
                    {
                        var layout = btn.gameObject.GetComponent<LayoutElement>();
                        if (layout == null)
                            layout = btn.gameObject.AddComponent<LayoutElement>();
                        layout.preferredWidth = 5f;
                        layout.minWidth = 5f;
                    }
                    // // ラベルを設定
                    // {
                    //     var label = btn.text.Trim().Replace(" ", "");
                    //     // 識別用コンポーネントを追加
                    //     var idComp = btn.gameObject.AddComponent<KeyIdentifier>();
                    //     if (label.StartsWith("emoji-"))
                    //     {
                    //         // 絵文字の場合は初期設定のtextをidとして扱う
                    //         idComp.Id = label;
                    //         btn.text = MemoEditModalHelper.GetEmojiKeyById(label);
                    //     }
                    //     else
                    //     {
                    //         // ラベルの大文字小文字変換
                    //         label = isShift ? label.ToLowerInvariant() : label.ToUpperInvariant();
                    //         btn.text = EditLabel(label);
                    //     }
                    // }
                    ApplyKeyBindings(btn);
                }
            }
            catch { /* ignore overall failures */ }
        }

        /// <summary>
        /// KeyManager の設定に基づき、ClickableText 要素のラベルを置換・初期化します（表示を上書きします）。
        /// </summary>
        /// <param name="ct">対象の ClickableText</param>
        private static void ApplyKeyBindings(ClickableText ct)
        {
            try
            {
                if (ct == null) return;
                var entry = MapMemo.Core.InputKeyManager.Instance?.FindForClickableTextEntry(ct);
                if (entry == null)
                {
                    // Plugin.Log?.Info($"ApplyKeyBindings: no KeyEntry found for ClickableText '{ct.gameObject.name}' with text '{ct.text}'");
                    return;
                }

                // BSML変更前のtextをidとして退避する
                entry.id = ct.text.Trim().Replace("　", ""); // 全角スペースを除去
                if (string.Equals(entry.type, "EmojiRange", StringComparison.OrdinalIgnoreCase))
                {
                    // 絵文字の場合のラベル設定
                    ct.text = entry.label;
                }
                else if (string.Equals(entry.type, "Literal", StringComparison.OrdinalIgnoreCase))
                {
                    // リテラル文字の場合のラベル設定
                    var label = entry.label ?? entry.@char ?? "";
                    if (Plugin.VerboseLogs) Plugin.Log?.Info($"ApplyKeyBindings: setting Literal label '{label}' for ClickableText '{ct.gameObject.name}'");
                    ct.text = EditLabel(label);
                }
                // すでに登録されているリスナーに KeyEntry をセット
                var listener = ct.gameObject.GetComponent<InputKeyClickListener>();
                listener.SetKeyEntry(entry);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"ApplyKeyBindings failed: {ex.Message}");
            }
        }



        /// <summary>
        /// A〜Z ボタンの大文字と小文字を切り替える
        /// </summary>
        /// <param name="isShift">true=小文字モード</param>
        public void UpdateAlphaButtonLabels(bool isShift)
        {
            try
            {
                var comps = keys;
                Plugin.Log?.Info("InputKeyController.UpdateAlphaButtonLabels: " + comps.Count() + " ClickableText components found under modal");
                foreach (var btn in comps)
                {
                    try
                    {
                        var stored = btn.text.Trim().Replace("　", "");
                        var label = isShift ? stored.ToLowerInvariant() : stored.ToUpperInvariant();
                        btn.text = EditLabel(label);
                    }
                    catch { /* ignore per-button failures */ }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"InputKeyController.UpdateAlphaButtonLabels: {ex.Message}");
            }
        }

        /// <summary>
        /// カナモードボタンのラベルを切り替える
        /// </summary>
        /// <param name="isKanaMode">true=カナ（かな）モード</param>
        public void UpdateKanaModeButtonLabel(bool isKanaMode)
        {

            try
            {
                foreach (var btn in keys)
                {
                    var stored = btn.text.Trim().Replace("　", "");

                    var labelConverted = isKanaMode ?
                        HiraganaToKatakana(stored) :
                        KatakanaToHiragana(stored);
                    // Plugin.Log?.Info($"InputKeyController.UpdateKanaModeButtonLabel: changing button label from '{stored}' to '{labelConverted}'");
                    btn.text = EditLabel(labelConverted);
                }

                var kanaModeButton = buttons
                    .FirstOrDefault(btn => btn.text.Trim().Replace("　", "") == "カナ"
                                        || btn.text.Trim().Replace("　", "") == "かな");
                if (kanaModeButton != null)
                {
                    string label = isKanaMode ? "かな" : "カナ";
                    kanaModeButton.text = EditLabel(label);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"InputKeyController.UpdateKanaModeButtonLabel: {ex.Message}");
            }
        }

        /// <summary>
        /// ひらがなをカタカナに変換する
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HiraganaToKatakana(string input)
        {
            return new string(input.Select(c =>
                (c >= 'ぁ' && c <= 'ゖ') ? (char)(c + 0x60) : c
            ).ToArray());
        }

        /// <summary>
        /// カタカナをひらがなに変換する
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string KatakanaToHiragana(string input)
        {
            return new string(input.Select(c =>
                (c >= 'ァ' && c <= 'ヶ') ? (char)(c - 0x60) : c
            ).ToArray());
        }
        /// <summary>
        /// ボタンラベルを編集用に整形します。 
        /// </summary>
        private static string EditLabel(string label)
        {
            return "  " + label + "  ";
        }

    }
}