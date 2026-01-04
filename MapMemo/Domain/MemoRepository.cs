using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mapmemo.Models;
using MapMemo.Models;
using MapMemo.Utilities;
using Newtonsoft.Json;

namespace MapMemo.Domain
{
    /// <summary>
    /// メモの永続化（ファイルベース）を提供する静的ユーティリティクラス。
    /// 将来的にはインターフェイス化して差し替え可能にできます。
    /// </summary>
    public static class MemoRepository
    {
        /// <summary> メモ保存用のユーザーデータディレクトリパス。</summary>
        private static readonly string UserDataDir =
            BeatSaberUtils.GetBeatSaberUserDataPath("MapMemo");

        /// <summary>
        /// メモ保存用ディレクトリを作成します。
        /// </summary>
        public static void EnsureDir()
        {
            if (!Directory.Exists(UserDataDir)) Directory.CreateDirectory(UserDataDir);
        }

        /// <summary>
        /// 未定義または空の文字列を 'unknown' に正規化します。
        /// </summary>
        private static string NormalizeUnknown(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "unknown";
            var trimmed = s.Trim();
            // BSML/Unity の未定義表示やプレースホルダを 'unknown' に正規化
            if (trimmed.Equals("unknown", StringComparison.OrdinalIgnoreCase)) return "unknown";
            if (trimmed.Equals("!Not Defined!", StringComparison.OrdinalIgnoreCase)) return "unknown";
            return trimmed;
        }

        /// <summary>
        /// key、songName、levelAuthor からファイル名を構築します。
        /// </summary>
        public static string BuildFileName(string hash, string songName, string levelAuthor, string bsrCode = null)
        {
            // 空やnullを許容し、フォールバック名を用いる
            var normalizedKey = hash;

            string effectiveKey = SanitizeFileSegment(normalizedKey);
            string sanitizedName = SanitizeFileSegment(NormalizeUnknown(songName));
            string sanitizedLevelAuthor = SanitizeFileSegment(NormalizeUnknown(levelAuthor));
            if (!string.IsNullOrEmpty(bsrCode))
            {
                sanitizedLevelAuthor += $" [{SanitizeFileSegment(bsrCode)}]";
            }

            return Path.Combine(UserDataDir,
                $"{effectiveKey}({sanitizedName} - {sanitizedLevelAuthor}).json");
        }

        /// <summary>
        /// ファイル名として不正な文字を安全な文字に置き換えます。
        /// </summary>
        public static string SanitizeFileSegment(string s)
        {
            var invalid = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
            foreach (var c in invalid)
            {
                s = s.Replace(c.ToString(), "_");
            }
            return s;
        }

        /// <summary>
        /// 同期的にメモを読み込みます。UI スレッドでの同期表示に使用します。
        /// </summary>
        public static MemoEntry Load(LevelContext levelContext)
        {
            var key = levelContext.GetLevelId();

            EnsureDir();
            FileInfo existFileInfo = GetMatchKeyFile(levelContext.GetLevelHash());
            if (existFileInfo == null) return null;
            string path = existFileInfo.FullName;

            if (!File.Exists(path)) return null;
            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var entry = JsonConvert.DeserializeObject<MemoEntry>(json);
                if (entry == null) return null;
                entry.key = entry.key ?? key;
                entry.songName = entry.songName ?? levelContext.GetSongName();
                entry.songAuthor = entry.songAuthor ?? levelContext.GetSongAuthor();
                entry.levelAuthor = entry.levelAuthor ?? levelContext.GetLevelAuthor();
                return entry;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 指定されたキーにマッチするメモファイルを取得します。
        /// </summary>
        /// <param name="key">メモのキー（LevelId）</param>
        public static FileInfo GetMatchKeyFile(string hash)
        {
            EnsureDir();
            var prefix = hash;

            var files = Directory.GetFiles(UserDataDir, $"{prefix}(*.json");
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoRepository.GetMatchKeyFile: "
                + $"Searching for keyPrefix='{prefix}' found {files.Length} files");
            if (files.Length == 0) return null;
            return new FileInfo(files[0]);
        }

        /// <summary>
        /// メモを非同期で保存します。メモが空文字の場合はファイルを削除します。
        /// </summary>
        public static async Task SaveAsync(MemoEntry entry)
        {
            EnsureDir();
            DeleteExistingMemoFile(entry);
            // 既存ファイルがあれば削除（キー変更や曲名・作者名変更に対応）
            // メモが空文字（0文字）の場合はファイルを削除して終了
            if (string.IsNullOrEmpty(entry.memo))
            {
                return;
            }

            string path = BuildFileName(entry.GetLevelHash() ?? "unknown",
                            entry.songName ?? "unknown",
                            entry.levelAuthor ?? "unknown",
                            entry.bsrCode ?? "unknown");
            entry.updatedAt = DateTime.UtcNow;
            var json = JsonConvert.SerializeObject(entry, Formatting.Indented);
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoRepository.SaveAsync: "
                                            + $"path='{path}' key='{entry.key}' "
                                            + $"song='{entry.songName}' author='{entry.songAuthor}'"
                                            + $" levelAuthor='{entry.levelAuthor}'"
                                            + $" len={entry.memo.Length}");
            using (var sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                await sw.WriteAsync(json);
            }
        }

        /// <summary>
        /// 既存のメモファイルを削除します（キー変更や曲名・作者名変更に対応）。
        /// </summary> 
        private static void DeleteExistingMemoFile(MemoEntry entry)
        {
            FileInfo existFileInfo = GetMatchKeyFile(entry.GetLevelHash());
            if (existFileInfo != null)
            {
                try
                {
                    File.Delete(existFileInfo.FullName);
                    if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoRepository.SaveAsync: "
                        + $"Deleted old file due to key/name/author change "
                        + $"path='{existFileInfo.FullName}' hash='{entry.GetLevelHash()}'");
                }
                catch (Exception e)
                {
                    Plugin.Log?.Warn($"MemoRepository.SaveAsync: "
                                    + $"Failed to delete old file '{existFileInfo.FullName}': {e.Message}");
                }
            }
        }
    }
}
