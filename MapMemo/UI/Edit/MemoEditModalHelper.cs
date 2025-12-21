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
    /// <summary>
    /// MemoEditModal に関する小さなユーティリティ関数群（位置調整や日時フォーマットなど）。
    /// </summary>
    public static class MemoEditModalHelper
    {
        /// <summary>
        /// モーダルを画面左半分に移動して表示位置を調整します。
        /// </summary>
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

        /// <summary>
        /// UTC の日時をローカル時間に変換してフォーマットした文字列を返します。
        /// </summary>
        public static string FormatLocal(DateTime utc)
        {
            var local = utc.ToLocalTime();
            return $"{local:yyyy/MM/dd HH:mm:ss}";
        }

        // public static bool IsOnlyEmoji(string input)
        // {
        //     if (string.IsNullOrWhiteSpace(input)) return false;

        //     var enumerator = StringInfo.GetTextElementEnumerator(input);
        //     while (enumerator.MoveNext())
        //     {
        //         string element = enumerator.GetTextElement();
        //         if (!IsEmoji(element))
        //             return false;
        //     }
        //     return true;
        // }

        // public static Dictionary<string, (int keyNo, string Block, List<(int Start, int End)>)> emojiMap
        //     = new Dictionary<string, (int, string, List<(int, int)>)>
        // {
        //     { "😀", (1, "Emoticons", new List<(int, int)>{(0x1F600, 0x1F613)}) },  // 😀〜😓 20
        //     { "😔", (2, "Emoticons", new List<(int, int)>{(0x1F614, 0x1F626)}) },  // 😔〜😦 20
        //     { "😧", (3, "Emoticons", new List<(int, int)>{(0x1F627, 0x1F637)}) },  // 😧〜😷 17
        //     { "🤔", (4, "Emoticons", new List<(int, int)>{(0x1F910, 0x1F927)}) },  // 🤐〜🤧 24
        //     { "😸", (5, "Emoticons", new List<(int, int)>{(0x1F638, 0x1F644)}) },  // 😸〜🙄 13
        //     { "🙅", (6, "Emoticons", new List<(int, int)>{(0x1F645, 0x1F64F)}) },  // 🙅〜🙏 13
        //     { "🌰", (7, "Pictographs", new List<(int, int)>{(0x1F330, 0x1F344)}) }, // 🌰〜🍄 20
        //     { "🍅", (8, "Pictographs", new List<(int, int)>{(0x1F345, 0x1F353)}) }, // 🍅〜🍓 15
        //     { "🍔", (9, "Pictographs", new List<(int, int)>{(0x1F354, 0x1F367)}) }, // 🍔〜🍧 20
        //     { "🍨", (10, "Pictographs", new List<(int, int)>{(0x1F368, 0x1F374)}) }, // 🍨〜🍴 20
        //     { "🍵", (11, "Pictographs", new List<(int, int)>{(0x1F375, 0x1F37F)}) }, // 🍵〜🍿 11
        //     { "🚀", (12, "Transport", new List<(int, int)>{(0x1F680, 0x1F68F)}) },    // 🚀〜🚏 16
        //     { "🚐", (13, "Transport", new List<(int, int)>{(0x1F690, 0x1F6A4)}) },    // 🚐〜🚤 21
        //     { "🚥", (14, "Transport", new List<(int, int)>{(0x1F6A5, 0x1F6B0)}) },     // 🚥〜🚰 12
        //     { "🛩", (15, "Transport", new List<(int, int)>{(0x1F6E9, 0x1F6EC)}) },     // 🛩〜🛬 4
        //     // ここは表示できない
        //     // { "🦰", ("Supplemental", "Body Parts", 1, 0x1F9B0, 0x1F9D4) },      // 🦰〜🧔 15
        //     // { "🧕", ("Supplemental", "People - Professions", 2, 0x1F9D5, 0x1F9E6) }, // 🧕〜🧦 18
        //     // { "🧧", ("ExtendedA", "Objects - Magic", 3, 0x1F9E7, 0x1F9FA) },    // 🧧〜🧺 20
        //     // { "🧻", ("ExtendedA", "Objects - Magic", 4, 0x1F9FB, 0x1FA78) },    // 🧻〜🩸 20
        //     // { "🩹", ("ExtendedA", "Objects - Magic", 5, 0x1FA79, 0x1FA98) },    // 🩹〜🪘 20
        //     // { "🪙", ("ExtendedA", "Objects - Magic", 6, 0x1FA99, 0x1FAAC) },    // 🪙〜🪬 20
        //     // { "🪭", ("ExtendedA", "Objects - Magic", 7, 0x1FAAD, 0x1FAC1) },    // 🪭〜🫁 20
        //     // { "🫂", ("ExtendedA", "Objects - Magic", 8, 0x1FAC2, 0x1FADB) },    // 🫂〜🫛 18
        //     // { "🫠", ("ExtendedA", "Objects - Magic", 9, 0x1FAE0, 0x1FAE8) },    // 🫠〜🫨 9
        //     // { "🫰", ("ExtendedA", "Objects - Magic", 10, 0x1FAF0, 0x1FAF8) },    // 🫰〜🫸 9

        //     { "☀", (16, "MiscSymbols", new List<(int, int)>{(0x2600, 0x2614)}) },  // ☀️〜☔ 21
        //     { "☕", (17, "MiscSymbols", new List<(int, int)>{(0x2615, 0x2629)}) }, // ☕〜☩ 21
        //     { "☪", (18, "MiscSymbols", new List<(int, int)>{(0x262A, 0x263E)}) },  // ☪〜☾ 21
        //     { "☿", (19, "MiscSymbols", new List<(int, int)>{(0x263F, 0x2653)}) },  // ☿〜♓ 21
        //     { "♔", (20, "MiscSymbols", new List<(int, int)>{(0x2654, 0x2668)}) },  // ♔〜♨ 21
        //     { "♩", (21,    "MiscSymbols", new List<(int, int)>{(0x2669, 0x267D)}) },   // ♩〜♽ 21
        //     { "♾", (22, "MiscSymbols", new List<(int, int)>{(0x267E, 0x2691)}) },   // ♾〜⚑ 21 ⚔
        //     { "⚒", (23, "MiscSymbols", new List<(int, int)>{(0x2692, 0x26A1)}) },   // ⚒〜⚡ 16
        //     { "⚢", (24, "MiscSymbols", new List<(int, int)>{(0x26A2, 0x26BC)}) },   // ⚢〜⚼ 27
        //     { "⚽", (25, "MiscSymbols", new List<(int, int)>{(0x26BD, 0x26CC)}) },  // ⚽〜⛌ 16 ⛌ 
        //     { "⛍", (26, "MiscSymbols", new List<(int, int)>{(0x26CD, 0x26E1)}) },  // ⛍〜⛡ 21
        //     { "⛢", (27, "MiscSymbols", new List<(int, int)>{(0x26E2, 0x26EF)}) },  // ⛢〜⛯ 14
        //     { "⛰", (28, "MiscSymbols", new List<(int, int)>{(0x26F0, 0x26FF)}) },  // ⛰〜⛿ 21
        //     { "✀", (29, "Dingbats", new List<(int, int)>{(0x2700, 0x2712)}) },    // 〜✒ 19
        //     { "✓", (30, "Dingbats", new List<(int, int)>{(0x2713, 0x2725)}) },    // 〜✥ 19

        //     { "✦", (31, "Dingbats", new List<(int, int)>{(0x2726, 0x2739)}) },    // 〜✹ 20
        //     { "✺", (32, "Dingbats", new List<(int, int)>{(0x273A, 0x274E)}) },    // 〜❎ 21
        //     { "❏", (33, "Dingbats", new List<(int, int)>{(0x274F, 0x2767)}) },    // 〜❧ 25   
        //     { "❨", (34, "Dingbats", new List<(int, int)>{(0x2768, 0x2775)}) },    // 〜❵ 192
        //     { "❶", (35, "Dingbats", new List<(int, int)>{(0x2776, 0x2793)}) },    // 〜➓ 30
        //     { "➔", (36, "Dingbats", new List<(int, int)>{(0x2794, 0x27BF)}) },    // 〜➿ 44
        //     { "🀄", (37, "Mahjong", new List<(int, int)>{(0x1F000, 0x1F02B)}) },          // 🀄〜🂫 44
        //     { "🄀", (38, "EnclosedAlpha", new List<(int, int)>{(0x1F100, 0x1F10A)}) }, // 〜🄊 10  
        //     { "🄐", (39, "EnclosedAlpha", new List<(int, int)>{(0x1F110, 0x1F12E)}) }, // 〜🄮 31
        //     { "🄰", (40, "EnclosedAlpha", new List<(int, int)>{(0x1F130, 0x1F14F)}) }, // 〜🅏 32
        //     { "🅐", (41, "EnclosedAlpha", new List<(int, int)>{(0x1F150, 0x1F169)}) }, // 〜🅩 26
        //     { "🅰", (42, "EnclosedAlpha", new List<(int, int)>{(0x1F170, 0x1F189)}) }, // 〜🆉 26
        //     { "🆊", (43, "EnclosedAlpha", new List<(int, int)>{(0x1F18A, 0x1F19A)}) }, // 〜🆚 17
        //     { "🇦", (44, "EnclosedAlpha", new List<(int, int)>{(0x1F1E6, 0x1F1FF)}) }, // 〜🇿 26
        //     { "🈀", (45, "EnclosedIdeo", new List<(int, int)>{(0x1F200, 0x1F23B)}) },     // 〜🈻 47  

        //     { "🉀", (46, "GeoShapes", new List<(int, int)>{(0x1F240, 0x1F265)}) },    // 〜🉥 17
        //     { "🟠", (47, "GeoShapes", new List<(int, int)>{(0x1F7E0, 0x1F7EB)}) },    // ～🟫 12
        // };
        // private static readonly HashSet<int> excludedCodePoints = new HashSet<int>
        // {
        //     0x1FA7B, 0x1FA7C, 0x1FA7D, 0x1FA7E, 0x1FA7F,
        //     0x1FA89, 0x1FA8A, 0x1FA8B, 0x1FA8C, 0x1FA8D, 0x1FA8E, 0x1FA8F,
        //     0x1FAC6, 0x1FAC7, 0x1FAC8, 0x1FAC9, 0x1FACA, 0x1FACB, 0x1FACC, 0x1FACD,
        //     0x1FADC, 0x1FADD, 0x1FADE, 0x1FADF, 0x1FAE9, 0x1FAEA, 0x1FAEB, 0x1FAEC,
        //     0x1FAED, 0x1FAEE, 0x1FAEF, 0x1FAF9, 0x1FAFA, 0x1FAFB, 0x1FAFC, 0x1FAFD,
        //     0x1FAFE, 0x1FAFF,
        //     0x1F10B, 0x1F10C, 0x1F10D, 0x1F10E, 0x1F10F,
        //     0x1F12F,
        //     0x1F16A, 0x1F16B, 0x1F16C, 0x1F16D, 0x1F16E, 0x1F16F,
        //     0x1F19B, 0x1F19C, 0x1F19D, 0x1F19E, 0x1F19F, 0x1F1A0, 0x1F1A1, 0x1F1A2,
        //     0x1F1A3, 0x1F1A4, 0x1F1A5, 0x1F1A6, 0x1F1A7, 0x1F1A8, 0x1F1A9, 0x1F1AA,
        //     0x1F1AB, 0x1F1AC, 0x1F1AD, 0x1F1AE, 0x1F1AF, 0x1F1B0, 0x1F1B1, 0x1F1B2,
        //     0x1F1B3, 0x1F1B4, 0x1F1B5, 0x1F1B6, 0x1F1B7, 0x1F1B8, 0x1F1B9, 0x1F1BA,
        //     0x1F1BB, 0x1F1BC, 0x1F1BD, 0x1F1BE, 0x1F1BF, 0x1F1C0, 0x1F1C1, 0x1F1C2,
        //     0x1F1C3, 0x1F1C4, 0x1F1C5, 0x1F1C6, 0x1F1C7, 0x1F1C8, 0x1F1C9, 0x1F1CA,
        //     0x1F1CB, 0x1F1CC, 0x1F1CD, 0x1F1CE, 0x1F1CF, 0x1F1D0, 0x1F1D1, 0x1F1D2,
        //     0x1F1D3, 0x1F1D4, 0x1F1D5, 0x1F1D6, 0x1F1D7, 0x1F1D8, 0x1F1D9, 0x1F1DA,
        //     0x1F1DB, 0x1F1DC, 0x1F1DD, 0x1F1DE, 0x1F1DF, 0x1F1E0, 0x1F1E1, 0x1F1E2,
        //     0x1F1E3, 0x1F1E4, 0x1F1E5,
        //     0x1F203, 0x1F204, 0x1F205, 0x1F206, 0x1F207, 0x1F208, 0x1F209, 0x1F20A,
        //     0x1F20B, 0x1F20C, 0x1F20D, 0x1F20E, 0x1F20F, 0x1F23C, 0x1F23D, 0x1F23E,
        //     0x1F23F, 0x1F249, 0x1F24A, 0x1F24B, 0x1F24C, 0x1F24D, 0x1F24E, 0x1F24F,
        //     0x1F252, 0x1F253, 0x1F254, 0x1F255, 0x1F256, 0x1F257, 0x1F258, 0x1F259,
        //     0x1F25A, 0x1F25B, 0x1F25C, 0x1F25D, 0x1F25E, 0x1F25F, 0x1F266, 0x1F267,
        //     0x1F268, 0x1F269, 0x1F26A, 0x1F26B, 0x1F26C, 0x1F26D, 0x1F26E, 0x1F26F,
        //     0x1F270, 0x1F271, 0x1F272, 0x1F273, 0x1F274, 0x1F275, 0x1F276, 0x1F277,
        //     0x1F278, 0x1F279, 0x1F27A, 0x1F27B, 0x1F27C, 0x1F27D, 0x1F27E, 0x1F27F,
        //     0x1F280, 0x1F281, 0x1F282, 0x1F283, 0x1F284, 0x1F285, 0x1F286, 0x1F287,
        //     0x1F288, 0x1F289, 0x1F28A, 0x1F28B, 0x1F28C, 0x1F28D, 0x1F28E, 0x1F28F,
        //     0x1F290, 0x1F291, 0x1F292, 0x1F293, 0x1F294, 0x1F295, 0x1F296, 0x1F297,
        //     0x1F298, 0x1F299, 0x1F29A, 0x1F29B, 0x1F29C, 0x1F29D, 0x1F29E, 0x1F29F,
        //     0x1F2A0, 0x1F2A1, 0x1F2A2, 0x1F2A3, 0x1F2A4, 0x1F2A5, 0x1F2A6, 0x1F2A7,
        //     0x1F2A8, 0x1F2A9, 0x1F2AA, 0x1F2AB, 0x1F2AC, 0x1F2AD, 0x1F2AE, 0x1F2AF,
        //     0x1F2B0, 0x1F2B1, 0x1F2B2, 0x1F2B3, 0x1F2B4, 0x1F2B5, 0x1F2B6, 0x1F2B7,
        //     0x1F2B8, 0x1F2B9, 0x1F2BA, 0x1F2BB, 0x1F2BC, 0x1F2BD, 0x1F2BE, 0x1F2BF,
        //     0x1F2C0, 0x1F2C1,
        //     0x1F2C2, 0x1F2C3, 0x1F2C4, 0x1F2C5, 0x1F2C6, 0x1F2C7, 0x1F2C8, 0x1F2C9,
        //     0x1F2CA, 0x1F2CB, 0x1F2CC, 0x1F2CD, 0x1F2CE, 0x1F2CF, 0x1F2D0, 0x1F2D1,
        //     0x1F2D2, 0x1F2D3, 0x1F2D4, 0x1F2D5, 0x1F2D6, 0x1F2D7, 0x1F2D8, 0x1F2D9,
        //     0x1F2DA, 0x1F2DB, 0x1F2DC, 0x1F2DD, 0x1F2DE, 0x1F2DF, 0x1F2E0, 0x1F2E1,
        //     0x1F2E2, 0x1F2E3, 0x1F2E4, 0x1F2E5, 0x1F2E6, 0x1F2E7, 0x1F2E8, 0x1F2E9,
        //     0x1F2EA, 0x1F2EB, 0x1F2EC, 0x1F2ED, 0x1F2EE, 0x1F2EF, 0x1F2F0, 0x1F2F1,
        //     0x1F2F2, 0x1F2F3, 0x1F2F4, 0x1F2F5, 0x1F2F6, 0x1F2F7, 0x1F2F8, 0x1F2F9,
        //     0x1F2FA, 0x1F2FB, 0x1F2FC, 0x1F2FD, 0x1F2FE, 0x1F2FF, 0x1FABE, 0x1F91F
        // };

        // public static bool IsEmojiSupported(string key, string emoji, int codePoint, int start, int end)
        // {
        //     // Prefer excluded list from KeyManager (JSON) when present; otherwise use built-in list.
        //     try
        //     {
        //         var km = MapMemo.Core.KeyManager.Instance;
        //         if (km != null && km.ExcludedCodePoints != null && km.ExcludedCodePoints.Count > 0)
        //         {
        //             return !km.ExcludedCodePoints.Contains(codePoint);
        //         }
        //     }
        //     catch { /* ignore and fallback */ }

        //     return !excludedCodePoints.Contains(codePoint);
        // }
        // public static string GetEmojiKeyById(string id)
        // {
        //     if (id.StartsWith("emoji-"))
        //     {
        //         if (int.TryParse(id.Substring("emoji-".Length), out int keyNo))
        //         {
        //             return GetEmojiKeyByKeyNo(keyNo);
        //         }
        //     }
        //     return "";
        // }
        // public static string GetEmojiKeyByKeyNo(int keyNo)
        // {
        //     foreach (var kv in emojiMap)
        //     {
        //         var (kNo, block, ranges) = kv.Value;
        //         if (kNo == keyNo)
        //         {
        //             return kv.Key;
        //         }
        //     }
        //     return "";
        // }



        // // デバッグ用に全絵文字を出力
        // public static void WriteDebugLog(string message)
        // {
        //     message = "All Emoji List \n\n" + message + "\n";
        //     foreach (var kv in emojiMap)
        //     {
        //         var (keyNo, block, ranges) = kv.Value;
        //         message += $"\nBlock '{block}' key '{kv.Key}'\n";
        //         foreach (var (start, end) in ranges)
        //         {
        //             for (int codePoint = start; codePoint <= end; codePoint++)
        //             {
        //                 string emoji = char.ConvertFromUtf32(codePoint);
        //                 message += $"key '{kv.Key}' emoji '{emoji}' code point 0x{codePoint:X} (range 0x{start:X}-0x{end:X})\n";
        //             }
        //         }
        //     }
        //     string path = Path.Combine(Application.persistentDataPath,
        //         Path.Combine(Environment.CurrentDirectory, "UserData", "MapMemo", "_all_emoji_log.txt"));
        //     File.WriteAllText(path, message + Environment.NewLine);
        // }


        // /// 絵文字かどうかを判定する
        // /// <param name="textElement">判定する文字列（テキスト要素）1文字</param>   
        // public static bool IsEmoji(string textElement)
        // {
        //     int codepoint = Char.ConvertToUtf32(textElement, 0);

        //     // 絵文字かどうかを判定
        //     foreach (var kv in emojiMap)
        //     {
        //         var (keyNo, block, ranges) = kv.Value;
        //         foreach (var (start, end) in ranges)
        //         {
        //             if (codepoint >= start && codepoint <= end)
        //             {
        //                 bool supported = IsEmojiSupported(kv.Key, textElement, codepoint, start, end);
        //                 if (supported)
        //                 {
        //                     return true;
        //                 }
        //             }
        //         }
        //     }
        //     return false;
        // }
    }

}
