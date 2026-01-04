

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using MapMemo.Domain;
using UnityEngine;

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
            // if (Plugin.VerboseLogs) DebugCustomLevelInfo();
        }
        /// <summary>
        /// レベル ID（キー）を取得します。未定義は "unknown" に正規化されます。
        /// </summary>
        public string GetLevelId()
        {
            return NormalizeUnknown(mapLevel.levelID);
        }
        /// <summary>
        /// レベルハッシュを取得します。
        /// </summary>
        public string GetLevelHash()
        {
            return Utilities.BeatSaberUtils.GetLevelHash(mapLevel.levelID);
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

        // public void DebugCustomLevelInfo()
        // {
        //     try
        //     {
        //         // カスタムレベルのフォルダ情報をログに出力
        //         var loader = Resources.FindObjectsOfTypeAll<CustomLevelLoader>().FirstOrDefault();

        //         if (loader != null)
        //         {
        //             var field = typeof(CustomLevelLoader).GetField("_loadedBeatmapSaveData", BindingFlags.NonPublic | BindingFlags.Instance);
        //             var dict = field?.GetValue(loader) as IDictionary;

        //             if (dict != null)
        //             {
        //                 foreach (DictionaryEntry entry in dict)
        //                 {
        //                     string levelId = entry.Key as string;
        //                     var loadedSaveData = entry.Value;

        //                     if (loadedSaveData == null)
        //                         continue;

        //                     var folderInfoField = loadedSaveData.GetType().GetField("customLevelFolderInfo", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //                     var folderInfo = folderInfoField?.GetValue(loadedSaveData);

        //                     if (folderInfo != null)
        //                     {
        //                         var folderPath = folderInfo.GetType().GetField("folderPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(folderInfo) as string;
        //                         var levelName = folderInfo.GetType().GetField("levelName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(folderInfo) as string;
        //                         // var levelInfoJsonString = folderInfo.GetType().GetField("levelInfoJsonString", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(folderInfo) as string;
        //                         Plugin.Log?.Info($"LevelID: {levelId}, Folder: {folderPath}, levelName: {levelName}");
        //                         // + $", levelInfoJsonString: {levelInfoJsonString}");

        //                         string bsrCode = null;

        //                         if (!string.IsNullOrEmpty(levelName))
        //                         {
        //                             int spaceIndex = levelName.IndexOf(' ');
        //                             if (spaceIndex > 0)
        //                             {
        //                                 bsrCode = levelName.Substring(0, spaceIndex); // "4cb5c"
        //                                 Plugin.Log?.Info($"Extracted BSR Code: {bsrCode}");
        //                             }
        //                         }

        //                     }
        //                 }
        //             }
        //         }

        //     }
        //     catch (Exception e)
        //     {
        //         Plugin.Log?.Error($"DebugLog error: {e}");
        //     }
        // }
        public void DebugLog()
        {
            // ジャンプディスタンスとリアクションタイムをログに出力
            Plugin.Log?.Info($"LevelContext: id='{GetLevelId()}' "
            + $"name='{GetSongName()}' author='{GetSongAuthor()}' "
            + $"levelAuthor='{GetLevelAuthor()}'"
            + $" hash='{GetLevelHash()}'");

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