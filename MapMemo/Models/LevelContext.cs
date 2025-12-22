

using System;

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
    }
}