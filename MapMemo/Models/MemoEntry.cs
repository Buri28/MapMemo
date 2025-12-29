using System;

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
    }
}