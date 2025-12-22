using System.Collections.Generic;

namespace Mapmemo.Models
{
    /// <summary>
    /// デシリアライズ用の設定モデル（キー割当と除外コードポイント）。
    /// </summary>
    public class InputKeyBindingsConfig
    {
        public List<InputKeyEntry> keys { get; set; }
        public List<string> excluded { get; set; }
    }
}