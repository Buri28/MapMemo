using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MapMemo
{
    public class MemoEntry
    {
        public string key { get; set; }
        public string songName { get; set; }
        public string songAuthor { get; set; }
        public string memo { get; set; }
        public DateTime updatedAt { get; set; } // UTC
    }

    public static class MemoRepository
    {
        private static readonly string UserDataDir = Path.Combine(Environment.CurrentDirectory, "UserData", "MapMemo");

        public static void EnsureDir()
        {
            if (!Directory.Exists(UserDataDir)) Directory.CreateDirectory(UserDataDir);
        }

        private static string NormalizeUnknown(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "unknown";
            var trimmed = s.Trim();
            // BSML/Unity の未定義表示やプレースホルダを 'unknown' に正規化
            if (trimmed.Equals("unknown", StringComparison.OrdinalIgnoreCase)) return "unknown";
            if (trimmed.Equals("!Not Defined!", StringComparison.OrdinalIgnoreCase)) return "unknown";
            return trimmed;
        }

        public static string BuildFileName(string key, string songName, string songAuthor)
        {
            // 空やnullを許容し、フォールバック名を用いる
            var normalizedKey = NormalizeUnknown(key);
            // Beat Saberのカスタム譜面はlevelIDが"custom_level_<HASH>"なので、表示/ファイル名では接頭辞を省く
            if (normalizedKey.StartsWith("custom_level_", StringComparison.OrdinalIgnoreCase))
            {
                normalizedKey = normalizedKey.Substring("custom_level_".Length);
            }
            string effectiveKey = SanitizeFileSegment(normalizedKey);
            string sanitizedName = SanitizeFileSegment(NormalizeUnknown(songName));
            string sanitizedAuthor = SanitizeFileSegment(NormalizeUnknown(songAuthor));
            return Path.Combine(UserDataDir, $"{effectiveKey}({sanitizedName} - {sanitizedAuthor}).json");
        }

        public static string SanitizeFileSegment(string s)
        {
            var invalid = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
            foreach (var c in invalid)
            {
                s = s.Replace(c.ToString(), "_");
            }
            return s;
        }

        // public static async Task<MemoEntry> LoadAsync(string key, string songName, string songAuthor)
        // {
        //     EnsureDir();
        //     string path = BuildFileName(key, songName, songAuthor);
        //     if (!File.Exists(path)) return null;
        //     try
        //     {
        //         using (var sr = new StreamReader(path, Encoding.UTF8))
        //         {
        //             string json = await sr.ReadToEndAsync();
        //             var entry = JsonConvert.DeserializeObject<MemoEntry>(json);
        //             if (entry == null)
        //             {
        //                 return null;
        //             }
        //             // フィールドの補完（古いファイル形式等に備え）
        //             entry.key = entry.key ?? key;
        //             entry.songName = entry.songName ?? songName;
        //             entry.songAuthor = entry.songAuthor ?? songAuthor;
        //             return entry;
        //         }
        //     }
        //     catch
        //     {
        //         return null;
        //     }
        // }

        // 同期版ロード。UI スレッドでの利用を想定し、Show の同期表示に使います。
        public static MemoEntry Load(string key, string songName, string songAuthor)
        {
            EnsureDir();
            string path = BuildFileName(key, songName, songAuthor);
            if (!File.Exists(path)) return null;
            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var entry = JsonConvert.DeserializeObject<MemoEntry>(json);
                if (entry == null) return null;
                entry.key = entry.key ?? key;
                entry.songName = entry.songName ?? songName;
                entry.songAuthor = entry.songAuthor ?? songAuthor;
                return entry;
            }
            catch
            {
                return null;
            }
        }

        public static async Task SaveAsync(MemoEntry entry)
        {
            EnsureDir();
            // 空フィールドにフォールバック
            string path = BuildFileName(entry.key ?? "unknown", entry.songName ?? "unknown", entry.songAuthor ?? "unknown");
            // メモが空文字（0文字）の場合はファイルを削除して終了
            if (string.IsNullOrEmpty(entry.memo))
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        MapMemo.Plugin.Log?.Info($"MemoRepository.SaveAsync: Deleted file for empty memo path='{path}' key='{entry.key}'");
                    }
                }
                catch (Exception e)
                {
                    MapMemo.Plugin.Log?.Warn($"MemoRepository.SaveAsync: Failed to delete file '{path}': {e.Message}");
                }
                return;
            }
            entry.updatedAt = DateTime.UtcNow;
            var json = JsonConvert.SerializeObject(entry, Formatting.Indented);
            MapMemo.Plugin.Log?.Info($"MemoRepository.SaveAsync: path='{path}' key='{entry.key}' song='{entry.songName}' author='{entry.songAuthor}'");
            using (var sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                await sw.WriteAsync(json);
            }
        }
    }
}
