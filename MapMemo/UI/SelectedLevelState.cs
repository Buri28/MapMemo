using System;

namespace MapMemo.UI
{
    internal sealed class SelectedLevelSnapshot
    {
        public string SongName { get; }
        public string SongAuthor { get; }
        public string LevelId { get; }
        public string Hash { get; }
        public DateTime UpdatedAtUtc { get; }

        public SelectedLevelSnapshot(string songName, string songAuthor, string levelId, string hash)
        {
            SongName = songName ?? "unknown";
            SongAuthor = songAuthor ?? "unknown";
            LevelId = levelId ?? "unknown";
            Hash = hash ?? "unknown";
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    internal static class SelectedLevelState
    {
        private static readonly object _gate = new object();
        private static SelectedLevelSnapshot _last;

        public static void Update(string songName, string songAuthor, string levelId, string hash)
        {
            try
            {
                var snap = new SelectedLevelSnapshot(Normalize(songName), Normalize(songAuthor), Normalize(levelId), Normalize(hash));
                lock (_gate)
                {
                    _last = snap;
                }
                MapMemo.Plugin.Log?.Info($"MapMemo: SelectedLevelState updated => name='{snap.SongName}' author='{snap.SongAuthor}' levelId='{snap.LevelId}' hash='{snap.Hash}'");
            }
            catch { }
        }

        public static bool TryGet(out SelectedLevelSnapshot snapshot)
        {
            lock (_gate)
            {
                snapshot = _last;
                return snapshot != null;
            }
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "unknown";
            var t = s.Trim();
            if (t.Equals("!Not Defined!", StringComparison.OrdinalIgnoreCase)) return "unknown";
            if (t.Equals("unknown", StringComparison.OrdinalIgnoreCase)) return "unknown";
            return t;
        }
    }
}
