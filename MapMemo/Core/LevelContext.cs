

using System;

namespace MapMemo.Core
{
    public class LevelContext
    {
        private BeatmapLevel mapLevel;
        public LevelContext(BeatmapLevel mapLevel)
        {
            this.mapLevel = mapLevel;
        }
        public string GetLevelId()
        {
            return NormalizeUnknown(mapLevel.levelID);
        }
        public string GetSongName()
        {
            return NormalizeUnknown(mapLevel.songName);
        }
        public string GetSongAuthor()
        {
            return NormalizeUnknown(mapLevel.songAuthorName);
        }
        public Boolean IsValid()
        {
            string key = GetLevelId();
            return !(key.Equals("unknown", StringComparison.OrdinalIgnoreCase)
                  || key.Equals("unknown|unknown", StringComparison.OrdinalIgnoreCase));
        }

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