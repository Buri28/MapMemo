using System;

namespace MapMemo
{
    public static class KeyResolver
    {
        // 優先: hash -> fallback: levelID
        public static string Resolve(string hash, string levelId)
        {
            if (IsMeaningful(hash)) return hash.Trim();
            if (IsMeaningful(levelId)) return levelId.Trim();
            return string.Empty;
        }

        private static bool IsMeaningful(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var t = s.Trim();
            if (t.Equals("unknown", StringComparison.OrdinalIgnoreCase)) return false;
            if (t.Equals("!Not Defined!", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
    }
}
