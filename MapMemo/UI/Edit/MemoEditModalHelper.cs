using System;
using System.Linq;
using UnityEngine;
using TMPro;
using HMUI;
using UnityEngine.UI;
using BeatSaberMarkupLanguage.Components;
using System.Globalization;
using System.Collections.Generic;
using System.IO;

namespace MapMemo.UI.Edit
{
    public static class MemoEditModalHelper
    {
        public static void ApplyAlphaButtonCosmetics(ModalView modal, bool isShift)
        {
            if (modal == null) return;
            try
            {
                foreach (var btn in modal.gameObject.GetComponentsInChildren<ClickableText>(true))
                {
                    try
                    {
                        if (btn == null) continue;
                        btn.fontSize = 3.8f;
                        btn.fontStyle = FontStyles.Italic | FontStyles.Underline;
                        btn.alignment = TextAlignmentOptions.Center;
                        btn.color = Color.cyan;
                        btn.DefaultColor = Color.cyan;
                        btn.HighlightColor = new Color(1f, 0.3f, 0f, 1f);
                        btn.outlineColor = Color.yellow;
                        btn.outlineWidth = 0.3f;

                        var layout = btn.gameObject.GetComponent<LayoutElement>();
                        if (layout == null)
                            layout = btn.gameObject.AddComponent<LayoutElement>();
                        layout.preferredWidth = 5f;
                        layout.minWidth = 5f;

                        var label = btn.text.Trim().Replace(" ", "");
                        label = isShift ? label.ToLowerInvariant() : label.ToUpperInvariant();
                        btn.text = EditLabel(label);
                    }
                    catch { /* ignore per-button failures */ }
                }
            }
            catch { /* ignore overall failures */ }
        }

        public static void UpdateAlphaButtonLabels(ModalView modal, bool isShift)
        {
            if (modal == null) return;

            try
            {
                var comps = modal.gameObject.GetComponentsInChildren<ClickableText>(true);
                Plugin.Log?.Info("MemoEditModal.UpdateAlphaButtonLabels: " + comps.Count() + " ClickableText components found under modal");
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
                Plugin.Log?.Warn($"MemoEditModalHelper.UpdateAlphaButtonLabels: {ex.Message}");
            }
        }
        public static void UpdateKanaModeButtonLabel(ModalView modal, bool isKanaMode)
        {
            if (modal == null) return;
            try
            {
                foreach (var btn in modal.gameObject.GetComponentsInChildren<ClickableText>(true))
                {
                    var stored = btn.text.Trim().Replace("　", "");

                    var labelConverted = isKanaMode ?
                        HiraganaToKatakana(stored) :
                        KatakanaToHiragana(stored);
                    Plugin.Log?.Info($"MemoEditModalHelper.UpdateKanaModeButtonLabel: changing button label from '{stored}' to '{labelConverted}'");
                    btn.text = EditLabel(labelConverted);
                }

                var kanaModeButton = modal.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true)
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
                Plugin.Log?.Warn($"MemoEditModalHelper.UpdateKanaModeButtonLabel: {ex.Message}");
            }
        }
        private static string HiraganaToKatakana(string input)
        {
            return new string(input.Select(c =>
                (c >= 'ぁ' && c <= 'ゖ') ? (char)(c + 0x60) : c
            ).ToArray());
        }
        private static string KatakanaToHiragana(string input)
        {
            return new string(input.Select(c =>
                (c >= 'ァ' && c <= 'ヶ') ? (char)(c - 0x60) : c
            ).ToArray());
        }


        public static void RepositionModalToLeftHalf(ModalView modal)
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
            catch (Exception ex)
            {
                MapMemo.Plugin.Log?.Warn($"MemoEditModal.RepositionModalToLeftHalf: exception {ex}");
            }
        }

        public static string EditLabel(string label)
        {
            return "  " + label + "  ";
        }

        public static string FormatLocal(DateTime utc)
        {
            var local = utc.ToLocalTime();
            return $"{local:yyyy/MM/dd HH:mm:ss}";
        }


        public static void SetupKeyClickListeners(ModalView modal)
        {
            var comps = modal.gameObject.GetComponentsInChildren<ClickableText>(true);
            if (Plugin.VerboseLogs)
            {
                var msg = $"MemoEditModal.SetupKeyClickListeners: found {comps.Count()} ClickableText components under modal";
                Plugin.Log?.Info(msg);
            }

            foreach (var btn in comps)
            {
                if (btn == null || btn.gameObject == null) continue;
                var stored = (btn.text ?? "").Trim();
                //if (string.IsNullOrEmpty(stored) || !IsEmojiString(stored)) continue;
                if (string.IsNullOrEmpty(stored)) continue;

                // Ensure a click listener component is present
                var listener = btn.gameObject.GetComponent<KeyClickListener>() ??
                    btn.gameObject.AddComponent<KeyClickListener>();
                listener.controller = MemoEditModalController.Instance;
            }
            Plugin.Log?.Info("MemoEditModal.SetupEmojiClickListeners: completed emoji listener setup");
        }

        // public static bool IsSingleEmoji(string s)
        // {
        //     // for (int i = 0; i < s.Length; i++)
        //     if (s.Length == 1)
        //     {
        //         int i = 0;
        //         var cat = CharUnicodeInfo.GetUnicodeCategory(s, i);
        //         if (cat == UnicodeCategory.OtherSymbol) return true;
        //         int cp = char.IsHighSurrogate(s[i]) && i + 1 < s.Length ? char.ConvertToUtf32(s, i) : s[i];

        //         // common emoji/rich symbol ranges
        //         if (cp >= 0x1F000 && cp <= 0x1FFFF) return true; // emojis/transport/misc symbols
        //         if (cp >= 0x2600 && cp <= 0x26FF) return true; // miscellaneous symbols
        //         if (cp >= 0x2700 && cp <= 0x27BF) return true; // dingbats

        //         if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length) i++; // skip low surrogate
        //     }
        //     return false;
        // }

        public static bool IsOnlyEmoji(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            var enumerator = StringInfo.GetTextElementEnumerator(input);
            while (enumerator.MoveNext())
            {
                string element = enumerator.GetTextElement();
                if (!IsEmoji(element))
                    return false;
            }
            return true;
        }

        public static Dictionary<string, (string Block, string Subcategory, int Page, int Start, int End)> emojiMap
            = new Dictionary<string, (string Block, string Subcategory, int Page, int Start, int End)>
        {
            { "😀", ("Emoticons", "Faces", 1, 0x1F600, 0x1F613) }, // 😀〜😓 20
            { "😔", ("Emoticons", "Faces", 2, 0x1F614, 0x1F626) },       // 😔〜😦 20
            { "😧", ("Emoticons", "Faces", 3, 0x1F627, 0x1F637) },       // 😧〜😷 17
            { "😸", ("Emoticons", "Faces", 4, 0x1F638, 0x1F644) },       // 😸〜🙄 13
            { "🙅", ("Emoticons", "Faces", 5, 0x1F645, 0x1F64F) },       // 🙅〜🙏 13

            { "🌰", ("Pictographs", "Nature - Flowers", 1, 0x1F330, 0x1F344) }, // 🌰〜🍄 20
            { "🍅", ("Pictographs", "Nature - Fruits", 2, 0x1F345, 0x1F353) }, // 🍅〜🍓 15
            { "🍔", ("Pictographs", "Nature - Food", 3, 0x1F354, 0x1F367) }, // 🍔〜🍧 20
            { "🍨", ("Pictographs", "Nature - Food", 4, 0x1F368, 0x1F374) }, // 🍨〜🍴 20
            { "🍵", ("Pictographs", "Nature - Food", 5, 0x1F375, 0x1F37F) }, // 🍵〜🍿 11

            { "🚀", ("Transport", "Vehicles - Land", 1, 0x1F680, 0x1F68F) },    // 🚀〜🚏 16
            { "🚐", ("Transport", "Vehicles - Land", 2, 0x1F690, 0x1F6A4) },    // 🚐〜🚤 21
            { "🚥", ("Transport", "Vehicles - Air", 3, 0x1F6A5, 0x1F6B0) },     // 🚥〜🚰 12
            { "🛩", ("Transport", "Vehicles - Air", 4, 0x1F6E9, 0x1F6EC) },     // 🛩〜🛬 4

            // ここは表示できない
            // { "🦰", ("Supplemental", "Body Parts", 1, 0x1F9B0, 0x1F9D4) },      // 🦰〜🧔 15
            // { "🧕", ("Supplemental", "People - Professions", 2, 0x1F9D5, 0x1F9E6) }, // 🧕〜🧦 18
            // { "🧧", ("ExtendedA", "Objects - Magic", 3, 0x1F9E7, 0x1F9FA) },    // 🧧〜🧺 20
            // { "🧻", ("ExtendedA", "Objects - Magic", 4, 0x1F9FB, 0x1FA78) },    // 🧻〜🩸 20
            // { "🩹", ("ExtendedA", "Objects - Magic", 5, 0x1FA79, 0x1FA98) },    // 🩹〜🪘 20
            // { "🪙", ("ExtendedA", "Objects - Magic", 6, 0x1FA99, 0x1FAAC) },    // 🪙〜🪬 20
            // { "🪭", ("ExtendedA", "Objects - Magic", 7, 0x1FAAD, 0x1FAC1) },    // 🪭〜🫁 20
            // { "🫂", ("ExtendedA", "Objects - Magic", 8, 0x1FAC2, 0x1FADB) },    // 🫂〜🫛 18
            // { "🫠", ("ExtendedA", "Objects - Magic", 9, 0x1FAE0, 0x1FAE8) },    // 🫠〜🫨 9
            // { "🫰", ("ExtendedA", "Objects - Magic", 10, 0x1FAF0, 0x1FAF8) },    // 🫰〜🫸 9

            { "☀", ("MiscSymbols", "Weather", 1, 0x2600, 0x2614) },            // ☀️〜☔ 21
            { "☕", ("MiscSymbols", "Weather", 2, 0x2615, 0x2629) },            // ☕〜☩ 21
            { "☪", ("MiscSymbols", "Weather", 3, 0x262A, 0x263E) },            // ☪〜☾ 21
            { "☿", ("MiscSymbols", "Weather", 4, 0x263F, 0x2653) },            // ☿〜♓ 21
            { "♔", ("MiscSymbols", "Weather", 5, 0x2654, 0x2668) },            // ♔〜♨ 21
            { "♩", ("MiscSymbols", "Weather", 6, 0x2669, 0x267D) },            // ♩〜♽ 21
            { "♾", ("MiscSymbols", "Weather", 7, 0x267E, 0x2691) },            // ♾〜⚑ 21 ⚔
            { "⚒", ("MiscSymbols", "Weather", 8, 0x2692, 0x26A1) },            // ⚒〜⚡ 16
            
            { "⚢", ("MiscSymbols", "Weather", 9, 0x26A2, 0x26BC) },            // ⚢〜⚼ 27
            { "⚽", ("MiscSymbols", "Weather", 10, 0x26BD, 0x26CC) },            // ⚽〜⛌ 16 ⛌ 
            { "⛍", ("MiscSymbols", "Weather", 11, 0x26CD, 0x26E1) },            // ⛍〜⛡ 21
            { "⛢", ("MiscSymbols", "Weather", 12, 0x26E2, 0x26EF) },            // ⛢〜⛯ 14
            { "⛰", ("MiscSymbols", "Weather", 13, 0x26F0, 0x26FF) },            // ⛰〜⛿ 21

            { "✀", ("Dingbats", "Effects - Sparkles", 1, 0x2700, 0x2712) },    // 〜✒ 19
            { "✓", ("Dingbats", "Effects - Sparkles", 2, 0x2713, 0x2725) },    // 〜✥ 19
            { "✦", ("Dingbats", "Effects - Sparkles", 3, 0x2726, 0x2739) },    // 〜✹ 20
            { "✺", ("Dingbats", "Effects - Sparkles", 4, 0x273A, 0x274E) },    // 〜❎ 21
            { "❏", ("Dingbats", "Effects - Sparkles", 5, 0x274F, 0x2767) },    // 〜❧ 25   

            { "❨", ("Dingbats", "Effects - Sparkles", 6, 0x2768, 0x2775) },    // 〜❵ 192
            { "❶", ("Dingbats", "Effects - Sparkles", 7, 0x2776, 0x2793) },    // 〜➓ 30
            { "➔", ("Dingbats", "Effects - Sparkles", 8, 0x2794, 0x27BF) },    // 〜➿ 44
            { "🀄", ("Mahjong", "Game Tiles", 1, 0x1F000, 0x1F02B) },          // 🀄〜🂫 44

            { "🄀", ("EnclosedAlpha", "Alphanumeric Buttons", 1, 0x1F100, 0x1F10A) }, // 〜🄊 10  
            { "🄐", ("EnclosedAlpha", "Alphanumeric Buttons", 2, 0x1F110, 0x1F12E) }, // 〜🄮 31
            { "🄰", ("EnclosedAlpha", "Alphanumeric Buttons", 3, 0x1F130, 0x1F14F) }, // 〜🅏 32
            { "🅐", ("EnclosedAlpha", "Alphanumeric Buttons", 4, 0x1F150, 0x1F169) }, // 〜🅩 26
            { "🅰", ("EnclosedAlpha", "Alphanumeric Buttons", 5, 0x1F170, 0x1F189) }, // 〜🆉 26
            { "🆊", ("EnclosedAlpha", "Alphanumeric Buttons", 4, 0x1F18A, 0x1F19A) }, // 〜🆚 17
            { "🇦", ("EnclosedAlpha", "Alphanumeric Buttons", 6, 0x1F1E6, 0x1F1FF) }, // 〜🇿 26

            { "🈀", ("EnclosedIdeo", "Japanese Symbols", 1, 0x1F200, 0x1F23B) },     // 〜🈻 47  
            { "🉀", ("GeoShapes", "Colored Squares", 1, 0x1F240, 0x1F265) },    // 〜🉥 17
            { "🟠", ("GeoShapes", "Colored Squares", 1, 0x1F7E0, 0x1F7EB) },    // ～🟫 12
        };
        private static readonly HashSet<int> excludedCodePoints = new HashSet<int>
        {
            0x1FA7B, 0x1FA7C, 0x1FA7D, 0x1FA7E, 0x1FA7F,
            0x1FA89, 0x1FA8A, 0x1FA8B, 0x1FA8C, 0x1FA8D, 0x1FA8E, 0x1FA8F,
            0x1FAC6, 0x1FAC7, 0x1FAC8, 0x1FAC9, 0x1FACA, 0x1FACB, 0x1FACC, 0x1FACD,
            0x1FADC, 0x1FADD, 0x1FADE, 0x1FADF, 0x1FAE9, 0x1FAEA, 0x1FAEB, 0x1FAEC,
            0x1FAED, 0x1FAEE, 0x1FAEF, 0x1FAF9, 0x1FAFA, 0x1FAFB, 0x1FAFC, 0x1FAFD,
            0x1FAFE, 0x1FAFF,
            0x1F10B, 0x1F10C, 0x1F10D, 0x1F10E, 0x1F10F,
            0x1F12F,
            0x1F16A, 0x1F16B, 0x1F16C, 0x1F16D, 0x1F16E, 0x1F16F,
            0x1F19B, 0x1F19C, 0x1F19D, 0x1F19E, 0x1F19F, 0x1F1A0, 0x1F1A1, 0x1F1A2,
            0x1F1A3, 0x1F1A4, 0x1F1A5, 0x1F1A6, 0x1F1A7, 0x1F1A8, 0x1F1A9, 0x1F1AA,
            0x1F1AB, 0x1F1AC, 0x1F1AD, 0x1F1AE, 0x1F1AF, 0x1F1B0, 0x1F1B1, 0x1F1B2,
            0x1F1B3, 0x1F1B4, 0x1F1B5, 0x1F1B6, 0x1F1B7, 0x1F1B8, 0x1F1B9, 0x1F1BA,
            0x1F1BB, 0x1F1BC, 0x1F1BD, 0x1F1BE, 0x1F1BF, 0x1F1C0, 0x1F1C1, 0x1F1C2,
            0x1F1C3, 0x1F1C4, 0x1F1C5, 0x1F1C6, 0x1F1C7, 0x1F1C8, 0x1F1C9, 0x1F1CA,
            0x1F1CB, 0x1F1CC, 0x1F1CD, 0x1F1CE, 0x1F1CF, 0x1F1D0, 0x1F1D1, 0x1F1D2,
            0x1F1D3, 0x1F1D4, 0x1F1D5, 0x1F1D6, 0x1F1D7, 0x1F1D8, 0x1F1D9, 0x1F1DA,
            0x1F1DB, 0x1F1DC, 0x1F1DD, 0x1F1DE, 0x1F1DF, 0x1F1E0, 0x1F1E1, 0x1F1E2,
            0x1F1E3, 0x1F1E4, 0x1F1E5,
            0x1F203, 0x1F204, 0x1F205, 0x1F206, 0x1F207, 0x1F208, 0x1F209, 0x1F20A,
            0x1F20B, 0x1F20C, 0x1F20D, 0x1F20E, 0x1F20F, 0x1F23C, 0x1F23D, 0x1F23E,
            0x1F23F, 0x1F249, 0x1F24A, 0x1F24B, 0x1F24C, 0x1F24D, 0x1F24E, 0x1F24F,
            0x1F252, 0x1F253, 0x1F254, 0x1F255, 0x1F256, 0x1F257, 0x1F258, 0x1F259,
            0x1F25A, 0x1F25B, 0x1F25C, 0x1F25D, 0x1F25E, 0x1F25F, 0x1F266, 0x1F267,
            0x1F268, 0x1F269, 0x1F26A, 0x1F26B, 0x1F26C, 0x1F26D, 0x1F26E, 0x1F26F,
            0x1F270, 0x1F271, 0x1F272, 0x1F273, 0x1F274, 0x1F275, 0x1F276, 0x1F277,
            0x1F278, 0x1F279, 0x1F27A, 0x1F27B, 0x1F27C, 0x1F27D, 0x1F27E, 0x1F27F,
            0x1F280, 0x1F281, 0x1F282, 0x1F283, 0x1F284, 0x1F285, 0x1F286, 0x1F287,
            0x1F288, 0x1F289, 0x1F28A, 0x1F28B, 0x1F28C, 0x1F28D, 0x1F28E, 0x1F28F,
            0x1F290, 0x1F291, 0x1F292, 0x1F293, 0x1F294, 0x1F295, 0x1F296, 0x1F297,
            0x1F298, 0x1F299, 0x1F29A, 0x1F29B, 0x1F29C, 0x1F29D, 0x1F29E, 0x1F29F,
            0x1F2A0, 0x1F2A1, 0x1F2A2, 0x1F2A3, 0x1F2A4, 0x1F2A5, 0x1F2A6, 0x1F2A7,
            0x1F2A8, 0x1F2A9, 0x1F2AA, 0x1F2AB, 0x1F2AC, 0x1F2AD, 0x1F2AE, 0x1F2AF,
            0x1F2B0, 0x1F2B1, 0x1F2B2, 0x1F2B3, 0x1F2B4, 0x1F2B5, 0x1F2B6, 0x1F2B7,
            0x1F2B8, 0x1F2B9, 0x1F2BA, 0x1F2BB, 0x1F2BC, 0x1F2BD, 0x1F2BE, 0x1F2BF,
            0x1F2C0, 0x1F2C1,
            0x1F2C2, 0x1F2C3, 0x1F2C4, 0x1F2C5, 0x1F2C6, 0x1F2C7, 0x1F2C8, 0x1F2C9,
            0x1F2CA, 0x1F2CB, 0x1F2CC, 0x1F2CD, 0x1F2CE, 0x1F2CF, 0x1F2D0, 0x1F2D1,
            0x1F2D2, 0x1F2D3, 0x1F2D4, 0x1F2D5, 0x1F2D6, 0x1F2D7, 0x1F2D8, 0x1F2D9,
            0x1F2DA, 0x1F2DB, 0x1F2DC, 0x1F2DD, 0x1F2DE, 0x1F2DF, 0x1F2E0, 0x1F2E1,
            0x1F2E2, 0x1F2E3, 0x1F2E4, 0x1F2E5, 0x1F2E6, 0x1F2E7, 0x1F2E8, 0x1F2E9,
            0x1F2EA, 0x1F2EB, 0x1F2EC, 0x1F2ED, 0x1F2EE, 0x1F2EF, 0x1F2F0, 0x1F2F1,
            0x1F2F2, 0x1F2F3, 0x1F2F4, 0x1F2F5, 0x1F2F6, 0x1F2F7, 0x1F2F8, 0x1F2F9,
            0x1F2FA, 0x1F2FB, 0x1F2FC, 0x1F2FD, 0x1F2FE, 0x1F2FF, 0x1FABE
        };

        // public static Dictionary<string, (string Block, string Subcategory, int Page, int Start, int End)> GetEmojiMap()
        // {
        //     return emojiMap
        //         .Where(kv =>
        //         {
        //             var (block, subcategory, page, start, end) = kv.Value;

        //             // Plugin.Log?.Info($"MemoEditModalHelper.GetEmojiMap: checking emoji range '{kv.Key}' ({block}/{subcategory}) U+{start:X}-U+{end:X}");
        //             for (int codePoint = start; codePoint <= end; codePoint++)
        //             {
        //                 string emoji = char.ConvertFromUtf32(codePoint);

        //                 if (IsEmojiSupported(kv.Key, emoji, codePoint, start, end))
        //                 {
        //                     // Plugin.Log?.Info($"MemoEditModalHelper.GetEmojiMap: including emoji range '{kv.Key}' ({block}/{subcategory}) U+{start:X}-U+{end:X}");
        //                     return true;
        //                 }
        //             }

        //             return true;
        //         })
        //         .ToDictionary(kv => kv.Key, kv => kv.Value);
        // }
        // public static Dictionary<string, (string Block, string Subcategory, int Page, int Start, int End)> GetEmojiMap()
        // {
        //     var result = new Dictionary<string, (string Block, string Subcategory, int Page, int Start, int End)>();

        //     foreach (var kv in emojiMap)
        //     {
        //         var (block, subcategory, page, start, end) = kv.Value;

        //         var filtered = Enumerable.Range(start, end - start + 1)
        //             .Where(cp => !excludedCodePoints.Contains(cp))
        //             .ToList();

        //         if (filtered.Count > 0)
        //         {
        //             int newStart = filtered.First();
        //             int newEnd = filtered.Last();
        //             Plugin.Log?.Info($"MemoEditModalHelper.GetEmojiMap: including emoji range '{kv.Key}' ({block}/{subcategory}) U+{newStart:X}-U+{newEnd:X}");
        //             result[kv.Key] = (block, subcategory, page, newStart, newEnd);
        //         }
        //     }

        //     return result;
        // }


        public static bool IsEmojiSupported(string key, string emoji, int codePoint, int start, int end)
        {
            if (excludedCodePoints.Contains(codePoint))
            {
                // WriteDebugLog($"key '{key}' emoji '{emoji}' code point 0x{codePoint:X} (range 0x{start:X}-0x{end:X}) is excluded");
                return false;
            }
            WriteDebugLog($"key '{key}' emoji '{emoji}' code point 0x{codePoint:X} (range 0x{start:X}-0x{end:X})");
            return true;
        }

        private static void WriteDebugLog(string message)
        {
            string path = Path.Combine(Application.persistentDataPath,
                Path.Combine(Environment.CurrentDirectory, "emoji_debug_log.txt"));
            File.AppendAllText(path, message + Environment.NewLine);
        }

        public static bool IsEmoji(string textElement)
        {
            int codepoint = Char.ConvertToUtf32(textElement, 0);
            Plugin.Log?.Info($"MemoEditModalHelper.IsEmoji: checking textElement '{textElement}' codepoint 0x{codepoint:X}");
            return emojiMap.Values.Any(range => codepoint >= range.Start && codepoint <= range.End);
        }
    }

}
