using System.Collections.Generic;

namespace Mapmemo.Models
{
    /// <summary>
    /// デシリアライズ用の設定モデル（キー割当と除外コードポイント）。
    /// </summary>
    public class InputKeyBindingsConfig
    {
        /// <summary>
        /// キー割当のリスト。
        /// </summary>
        public List<InputKeyEntry> keys { get; set; }
        /// <summary>
        /// 除外コードポイントのリスト。
        /// </summary>
        public List<string> excluded { get; set; }
    }
}