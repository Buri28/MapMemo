
using System.Collections.Generic;

namespace Mapmemo.Models
{


    /// <summary>
    /// キー割当のエントリを表すモデル。
    /// </summary>
    public class InputKeyEntry
    {
        /// <summary>キータイプ: 絵文字</summary>
        public const string InputKeyType_Emoji = "Emoji";
        /// <summary>キータイプ: リテラル</summary>
        public const string InputKeyType_Literal = "Literal";

        /// <summary>
        /// このエントリが絵文字タイプかどうかを判定します。
        /// </summary>
        /// <returns></returns>
        public bool IsEmojiType()
        {
            return string.Equals(this.type, InputKeyType_Emoji,
                System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// このエントリがリテラルタイプかどうかを判定します
        /// </summary>
        /// <returns></returns>
        public bool IsLiteralType()
        {
            return string.Equals(this.type, InputKeyType_Literal,
                System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// キー番号（InputKeyBindingsConfig の keys 配列内のインデックス）。
        /// </summary>
        public int keyNo { get; set; }
        /// <summary>
        /// キータイプ（"Emoji" または "Literal"）。
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// ラベル（表示用テキスト）。
        /// </summary>
        public string label { get; set; }
        /// <summary>
        /// 文字（絵文字やリテラル文字）。
        /// </summary>
        public string @char { get; set; }
        /// <summary>
        /// 範囲リスト（絵文字の場合に使用）。
        /// </summary>
        public List<RangeModel> ranges { get; set; }

        /// <summary>
        /// BSMLの変更前のtextから識別するidを取得して退避するためのプロパティ。
        /// </summary>
        public string id { get; set; }
    }
}