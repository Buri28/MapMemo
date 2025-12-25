

using System;
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
        /// テキストが最大行数を超えるかどうかを判定します。
        /// </summary>
        public static bool isOverMaxLine(string text, int maxLines)
        {
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            var linesCount = lines.Length;
            return linesCount > maxLines;
        }

        /// <summary>
        /// 指定文字列の重み付き長さを返す（半角=0.5、その他=1、改行は無視）。
        /// </summary>
        public static double GetWeightedLength(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0.0;
            var oneLine = text.Replace("\r", "").Replace("\n", "");
            var indices = StringInfo.ParseCombiningCharacters(oneLine);
            double length = 0.0;
            for (int i = 0; i < indices.Length; i++)
            {
                int start = indices[i];
                int end = (i + 1 < indices.Length) ? indices[i + 1] : oneLine.Length;
                var elem = oneLine.Substring(start, end - start);
                length += IsHalfWidthElement(elem) ? 0.5 : 1.0;
            }
            return length;
        }
    }
}