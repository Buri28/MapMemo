

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace MapMemo.Utilities
{
    /// <summary>
    /// 文字列操作に関するユーティリティメソッドを提供します
    /// </summary>
    public static class StringHelper
    {
        // /// <summary>
        // /// 文字要素が ASCII アルファベットか判定します。
        // /// </summary>
        // public static bool IsAsciiAlphabet(string textElement)
        // {
        //     if (string.IsNullOrEmpty(textElement)) return false;
        //     if (textElement.Length == 1)
        //     {
        //         var c = textElement[0];
        //         return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        //     }
        //     // 複数文字（結合文字など）はアルファベット扱いしない
        //     return false;
        // }

        /// <summary>
        /// 文字要素が半角扱い（0.5）か判定します。
        /// - ASCII 全体（U+0000–U+007F）
        /// - 半角カナ（U+FF61–U+FFDC）
        /// - 特殊に扱うコードポイント（U+27E8, U+27E9）
        /// 複数文字からなる要素はすべてのコードユニットが半角範囲にある場合のみ true を返します。
        /// </summary>
        public static bool IsHalfWidthElement(string textElement)
        {
            if (string.IsNullOrEmpty(textElement)) return false;
            // 単一文字で特殊コードポイントを許可
            if (textElement.Length == 1)
            {
                var c = textElement[0];
                if (c == '\u27E8' || c == '\u27E9') return true;
                if (c <= 0x7F) return true;
                if (c >= '\uFF61' && c <= '\uFFDC') return true;
                return false;
            }
            // 複数文字の場合はすべてが半角範囲にあるかを確認
            foreach (var ch in textElement)
            {
                if (!(ch <= 0x7F || (ch >= '\uFF61' && ch <= '\uFFDC'))) return false;
            }
            return true;
        }

        /// <summary>
        /// 文字列から改行コードを削除します。
        /// </summary>
        public static string RemoveLineBreaks(string s)
        {
            return s?.Replace("\r", "").Replace("\n", "");
        }

        /// <summary>
        /// BSML タグ文字を安全な文字にエスケープします。
        /// '<' を '⟨' (U+27E8)、'>'
        /// を '⟩' (U+27E9) に置換します。
        /// </summary>
        public static string EscapeBsmlTag(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string safe = s.Replace("<", "\u27E8").Replace(">", "\u27E9");
            return safe;
        }

        /// <summary>
        /// 濁点変換マップ
        /// </summary>
        private static readonly Dictionary<string, string> DakutenMap = new Dictionary<string, string>
        {
            // ひらがな
            {"か", "が"}, {"き", "ぎ"}, {"く", "ぐ"}, {"け", "げ"}, {"こ", "ご"},
            {"さ", "ざ"}, {"し", "じ"}, {"す", "ず"}, {"せ", "ぜ"}, {"そ", "ぞ"},
            {"た", "だ"}, {"ち", "ぢ"}, {"つ", "づ"}, {"て", "で"}, {"と", "ど"},
            {"は", "ば"}, {"ひ", "び"}, {"ふ", "ぶ"}, {"へ", "べ"}, {"ほ", "ぼ"},
            // カタカナ
            {"カ", "ガ"}, {"キ", "ギ"}, {"ク", "グ"}, {"ケ", "ゲ"}, {"コ", "ゴ"},
            {"サ", "ザ"}, {"シ", "ジ"}, {"ス", "ズ"}, {"セ", "ゼ"}, {"ソ", "ゾ"},
            {"タ", "ダ"}, {"チ", "ヂ"}, {"ツ", "ヅ"}, {"テ", "デ"}, {"ト", "ド"},
            {"ハ", "バ"}, {"ヒ", "ビ"}, {"フ", "ブ"}, {"ヘ", "ベ"}, {"ホ", "ボ"},
        };


        /// <summary>
        /// 濁点変換を行います。
        /// </summary>
        /// <param name="lastChar">変換対象の文字</param>
        /// <param name="isDakuten">濁点変換かどうか</param>
        /// <returns>変換後の文字（変換不可の場合は元の文字）</returns>
        public static string GetDakutenConverted(string lastChar, bool isDakuten)
        {
            if (string.IsNullOrEmpty(lastChar)) return lastChar;

            var reverseDakutenMap = DakutenMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            if (isDakuten)
            {
                if (DakutenMap.TryGetValue(lastChar, out var converted))
                {
                    return converted;
                }
            }
            else
            {
                if (reverseDakutenMap.TryGetValue(lastChar, out var reverted))
                {
                    return reverted;
                }
            }
            return lastChar;
        }

        /// <summary>
        /// 文字が濁点・半濁点変換可能かどうかを判定します。
        /// </summary>
        /// <param name="lastChar">変換対象の文字</param>
        /// <param name="newChar">変換後の文字（変換不可の場合は空文字）</param>
        /// <returns>変換可能な場合は true、不可の場合は false</returns>
        public static bool IsDakutenConvertible(string lastChar, out string newChar)
        {
            newChar = "";
            if (string.IsNullOrEmpty(lastChar)) return false;
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"StringHelper.IsDakutenConvertible: "
                    + $"lastChar='{lastChar}'");
            if (DakutenMap.TryGetValue(lastChar, out var converted))
            {
                newChar = converted;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 半濁点変換マップ
        /// </summary>
        private static readonly Dictionary<string, string> HandakutenMap = new Dictionary<string, string>
        {
            // ひらがな
            {"は", "ぱ"}, {"ひ", "ぴ"}, {"ふ", "ぷ"}, {"へ", "ぺ"}, {"ほ", "ぽ"},
            // カタカナ
            {"ハ", "パ"}, {"ヒ", "ピ"}, {"フ", "プ"}, {"ヘ", "ペ"}, {"ホ", "ポ"},
        };

        /// <summary>
        /// 半濁点変換を行います
        /// </summary>
        /// <param name="lastChar">変換対象の文字</param>
        /// <param name="isHandakuten">半濁点変換かどうか</param>
        /// <returns>変換後の文字（変換不可の場合は元の文字）</returns>
        public static string GetHandakutenConverted(string lastChar, bool isHandakuten)
        {
            if (string.IsNullOrEmpty(lastChar)) return lastChar;

            var reverseHandakutenMap = HandakutenMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            if (isHandakuten)
            {
                if (HandakutenMap.TryGetValue(lastChar, out var converted))
                {
                    return converted;
                }
            }
            else
            {
                if (reverseHandakutenMap.TryGetValue(lastChar, out var reverted))
                {
                    return reverted;
                }
            }
            return lastChar;
        }

        /// <summary>
        /// 文字が半濁点変換可能かどうかを判定します
        /// </summary>
        /// <param name="lastChar">変換対象の文字</param>
        /// <param name="newChar">変換後の文字（変換不可の場合は空文字）</param>
        /// <returns>変換可能な場合は true、不可の場合は false</returns
        public static bool IsHandakutenConvertible(string lastChar, out string newChar)
        {
            newChar = "";
            if (string.IsNullOrEmpty(lastChar)) return false;
            if (HandakutenMap.TryGetValue(lastChar, out var converted))
            {
                newChar = converted;
                return true;
            }
            return false;
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


        // /// <summary>
        // /// テキストが最大行数を超えるかどうかを判定します。
        // /// </summary>
        // public static bool isOverMaxLine(string text, int maxLines)
        // {
        //     var lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
        //     var linesCount = lines.Length;
        //     return linesCount > maxLines;
        // }
    }
}