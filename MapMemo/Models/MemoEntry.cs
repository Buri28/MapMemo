using System;
using MapMemo.Utilities;

namespace Mapmemo.Models
{
    /// <summary>
    /// メモのデータモデル
    /// </summary>
    public class MemoEntry
    {
        /// <summary>
        /// メモのキー（LevelId）
        /// </summary>
        public string key { get; set; }

        /// <summary>
        /// レベルハッシュを取得します。
        /// </summary>
        public string GetLevelHash()
        {
            return BeatSaberUtils.GetLevelHash(key);
        }

        /// <summary>
        /// 曲名
        /// </summary>
        public string songName { get; set; }

        /// <summary>
        /// 曲の作者
        /// </summary>
        public string songAuthor { get; set; }
        /// <summary>
        /// レベルの作者
        /// </summary>
        public string levelAuthor { get; set; }

        /// <summary>
        /// メモ内容
        /// </summary>
        public string memo { get; set; }

        /// <summary>
        /// 更新日時（UTC）
        /// </summary>
        public DateTime updatedAt { get; set; } // UTC（協定世界時）
        /// <summary>
        /// BSRコード
        /// </summary>
        /// 
        public string bsrCode { get; set; }
        /// <summary>
        /// BeatSaverのURLを取得します。
        /// </summary>
        public string beatSaverUrl
        {
            get => string.IsNullOrEmpty(bsrCode) ?
                "" : $"https://beatsaver.com/maps/{bsrCode}";
        }
        /// <summary>
        /// 自動作成された空のメモかどうかを示すフラグ
        /// </summary>
        public bool autoCreateEmptyMemo { get; set; }

        /// <summary>
        /// オブジェクトの文字列表現を取得します。
        /// </summary>
        public override string ToString()
        {
            return $"MemoEntry(key={key}, songName={songName}, songAuthor={songAuthor}, "
                + $"levelAuthor={levelAuthor}, memoLen={memo?.Length}, "
                + $"updatedAt={updatedAt}, bsrCode={bsrCode}, "
                + $"autoCreateEmptyMemo={autoCreateEmptyMemo})";
        }
    }
}