using System;
using Newtonsoft.Json;

namespace Mapmemo.Models
{
    /// <summary>
    /// BeatSaver から取得したマップ情報を表すモデル。
    /// </summary>
    public class BeatSaverMap
    {
        /// <summary>
        /// マップのID。(BsrCode)
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// マップの説明。
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// 最終公開日時。
        /// </summary>
        public DateTime lastPublishedAt { get; set; }
        public Stats stats;

        public DateTime DataTimeStamp { get; set; }
    }
    public class Stats
    {
        /// <summary>
        /// プレイ回数。
        /// </summary>
        public int plays;
        /// <summary>
        /// ダウンロード数。
        /// </summary>
        public int downloads;
        /// <summary>
        /// アップボート数。
        /// </summary>
        public int upvotes;
        /// <summary>
        /// ダウンボート数。
        /// </summary>
        public int downvotes;
        /// <summary>
        /// 評価スコア（0.0〜1.0）	。
        /// 全体的な人気度を表す指標（高いほど好評）
        /// </summary>
        public float score;
    }
}