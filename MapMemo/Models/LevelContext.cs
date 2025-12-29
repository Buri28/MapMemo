

using System;
using System.Collections;
using System.Text;

namespace MapMemo.Models
{
    /// <summary>
    /// BeatmapLevel からキー/曲情報を抽出して扱いやすくするラッパークラス。
    /// </summary>
    public class LevelContext
    {
        private BeatmapLevel mapLevel;
        /// <summary>
        /// 指定した BeatmapLevel のラップを作成します。
        /// </summary>
        public LevelContext(BeatmapLevel mapLevel)
        {
            this.mapLevel = mapLevel;
        }
        /// <summary>
        /// レベル ID（キー）を取得します。未定義は "unknown" に正規化されます。
        /// </summary>
        public string GetLevelId()
        {
            return NormalizeUnknown(mapLevel.levelID);
        }
        /// <summary>
        /// 曲名を取得します。
        /// </summary>
        public string GetSongName()
        {
            return NormalizeUnknown(mapLevel.songName);
        }
        /// <summary>
        /// 曲作者名を取得します。
        /// </summary>
        public string GetSongAuthor()
        {
            return NormalizeUnknown(mapLevel.songAuthorName);
        }
        /// <summary>
        /// レベルコンテキストが有効（有意なキーを持つ）かどうかを判定します。
        /// </summary>
        public Boolean IsValid()
        {
            string key = GetLevelId();
            return !(key.Equals("unknown", StringComparison.OrdinalIgnoreCase)
                  || key.Equals("unknown|unknown", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 未定義またはプレースホルダを "unknown" に正規化します。
        /// </summary>
        private static string NormalizeUnknown(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "unknown";
            var trimmed = s.Trim();
            if (trimmed.Equals("unknown", StringComparison.OrdinalIgnoreCase)) return "unknown";
            if (trimmed.Equals("!Not Defined!", StringComparison.OrdinalIgnoreCase)) return "unknown";
            return trimmed;
        }

        /// <summary>
        /// レベル作者名を取得します。
        /// </summary>
        /// <returns></returns> 
        public string GetLevelAuthor()
        {
            if (mapLevel.allMappers != null && mapLevel.allMappers.Length > 0)
            {
                var joined = string.Join(",", mapLevel.allMappers);
                return NormalizeUnknown(joined);
            }

            return "unknown";
        }

        // /// <summary>
        // /// レベル作者名（ファイル名用）を取得します。
        // /// </summary>
        // public string GetLevelAuthorForFile()
        // {
        //     if (mapLevel.allMappers != null && mapLevel.allMappers.Length > 0)
        //     {
        //         var joined = string.Join("_", mapLevel.allMappers);
        //         return NormalizeUnknown(joined);
        //     }
        //     return "unknown";
        // }

        public void DebugLog()
        {
            // ジャンプディスタンスとリアクションタイムをログに出力
            Plugin.Log?.Info($"LevelContext: id='{GetLevelId()}' name='{GetSongName()}' author='{GetSongAuthor()}' levelAuthor='{GetLevelAuthor()}'");

            var characteristics = mapLevel.GetCharacteristics();
            foreach (var characteristic in characteristics)
            {
                var difficulties = mapLevel.GetDifficulties(characteristic);
                foreach (var difficulty in difficulties)
                {
                    Plugin.Log?.Info($"Characteristic: {characteristic.serializedName}    Difficulty: {difficulty}");
                    BeatmapBasicData basicData = mapLevel.GetDifficultyBeatmapData(characteristic, difficulty);
                    float bpm = mapLevel.beatsPerMinute;
                    float njs = basicData.noteJumpMovementSpeed;
                    float offset = basicData.noteJumpStartBeatOffset;

                    float secondsPerBeat = 60f / bpm;
                    float halfJumpDuration = 4f + offset;
                    if (halfJumpDuration < 0.25f) halfJumpDuration = 0.25f;
                    float reactionTime = secondsPerBeat * halfJumpDuration;
                    var jumpDistance = njs * (60f / bpm) * halfJumpDuration * 2f;
                    Plugin.Log?.Info(
                      $"BPM={bpm}, NJS={njs}, Offset={offset},"
                      + $"ReactionTime={reactionTime * 1000:F2}ms, JumpDistance={jumpDistance:F2}m");
                }
            }
        }
    }
}