

using System;
using System.Globalization;
using System.Net;
using System.Text;

namespace MapMemo.Utilities
{
    /// <summary>
    /// 文字列操作に関するユーティリティメソッドを提供します
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// 文字要素が ASCII アルファベットか判定します。
        /// </summary>
        public static bool IsAsciiAlphabet(string textElement)
        {
            if (string.IsNullOrEmpty(textElement)) return false;
            if (textElement.Length == 1)
            {
                var c = textElement[0];
                return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
            }
            // 複数文字（結合文字など）はアルファベット扱いしない
            return false;
        }

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

        /// <summary>
        /// 与えた文字列を表示上の行幅（maxCharsPerLine）で折り返し、必要な箇所に '\n' を自動挿入した文字列を返します。
        /// タグ (例: &lt;color=...&gt;, &lt;/u&gt; など) は長さカウントから除外され、タグ内部に改行が入らないよう扱います。
        /// 既存の '\n' はそのまま行区切りとして扱い、累積長をリセットします。
        /// </summary>
        public static string InsertLineBreaks(string text, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(text) || maxCharsPerLine <= 0) return text ?? "";
            var sb = new StringBuilder();
            var indices = StringInfo.ParseCombiningCharacters(text);
            double cur = 0.0;

            for (int i = 0; i < indices.Length; i++)
            {
                int start = indices[i];
                int end = (i + 1 < indices.Length) ? indices[i + 1] : text.Length;
                var elem = text.Substring(start, end - start);



                // タグ開始なら '>' までまとめて出力し、長さカウントには含めない
                if (elem.Length > 0 && elem[0] == '<')
                {
                    int tagEnd = text.IndexOf('>', start);
                    if (tagEnd == -1)
                    {
                        var rest = text.Substring(start);
                        sb.Append(rest);
                        break;
                    }
                    else
                    {
                        var tag = text.Substring(start, tagEnd - start + 1);
                        sb.Append(tag);
                        while (i + 1 < indices.Length && indices[i + 1] <= tagEnd) i++;
                        continue;
                    }
                }

                if (elem == "\r" || elem == "\n")
                {
                    sb.Append(elem);
                    cur = 0.0;
                    continue;
                }

                double add = IsHalfWidthElement(elem) ? 0.5 : 1.0;
                if (cur + add > maxCharsPerLine)
                {
                    sb.Append('\n');
                    cur = 0.0;
                }
                sb.Append(elem);
                cur += add;
            }

            return sb.ToString();
        }

        /// <summary>
        /// 指定テキストが maxLines、かつ各行 maxCharsPerLine に収まるかを判定します。
        /// InsertLineBreaks による表示シミュレーションで行数を数えます。
        /// </summary>
        public static bool FitsWithinLines(string text, int maxLines, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(text)) return true;
            var simulated = InsertLineBreaks(text.Replace("\r", ""), maxCharsPerLine);
            var lines = simulated.Split(new[] { '\n' }, StringSplitOptions.None);
            int count = lines.Length;
            // 表示が改行で終わっている場合（最後の要素が空文字）を考慮
            if (simulated.EndsWith("\n")) count--;
            return count <= maxLines;
        }
    }
}