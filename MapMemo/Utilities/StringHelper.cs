

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

        public static string RemoveLineBreaks(string s)
        {
            return s?.Replace("\r", "").Replace("\n", "");
        }

    }
}