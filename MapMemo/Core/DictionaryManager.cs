using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MapMemo.Core
{
    public static class DictionaryManager
    {
        private static List<KeyValuePair<string, string>> dictionaryWords = new List<KeyValuePair<string, string>>();
        private static bool loaded = false;

        public static IReadOnlyList<KeyValuePair<string, string>> DictionaryWords => dictionaryWords;

        public static void EnsureLoaded()
        {
            if (!loaded) Load();
        }

        public static void Reload()
        {
            loaded = false;
            Load();
        }

        public static void Load()
        {
            if (loaded) return;
            try
            {
                string userDictionaryPath = Path.Combine(Environment.CurrentDirectory, "UserData", "MapMemo", "#dictionary.txt");
                // Copy embedded resource if missing
                if (!File.Exists(userDictionaryPath))
                {
                    var asm = typeof(DictionaryManager).Assembly;
                    var resourceName = "MapMemo.Resources.#dictionary.txt";
                    using (var stream = asm.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(userDictionaryPath));
                            using (var fs = new FileStream(userDictionaryPath, FileMode.Create, FileAccess.Write))
                            {
                                stream.CopyTo(fs);
                            }
                            Plugin.Log?.Info($"DictionaryManager: Copied dictionary from embedded resource to UserData: {userDictionaryPath}");
                        }
                        else
                        {
                            Plugin.Log?.Warn($"DictionaryManager: Embedded dictionary resource not found: {resourceName}");
                            return;
                        }
                    }
                }

                try
                {
                    var list = new List<KeyValuePair<string, string>>();
                    foreach (var line in File.ReadLines(userDictionaryPath))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(new[] { ',' }, 2);
                        if (parts.Length == 2)
                        {
                            list.Add(new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim()));
                        }
                        else
                        {
                            list.Add(new KeyValuePair<string, string>(null, line.Trim()));
                        }
                    }
                    dictionaryWords = list;
                    Plugin.Log?.Info($"DictionaryManager: Loaded {dictionaryWords.Count} dictionary words.");
                }
                catch (Exception ex)
                {
                    Plugin.Log?.Error($"DictionaryManager: Failed to load dictionary: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"DictionaryManager: Failed to load dictionary file: {ex.Message}");
                dictionaryWords = new List<KeyValuePair<string, string>>();
            }
            loaded = true;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetMatches(string prefix)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(prefix) || prefix == ",")
            {
                return Enumerable.Empty<KeyValuePair<string, string>>();
            }
            Plugin.Log?.Info($"DictionaryManager: Searching for prefix '{prefix}'" +
                $" among {dictionaryWords.Count} dictionary words.");
            return dictionaryWords.Where(
                pair => pair.Key != null ?
                        pair.Key.StartsWith(prefix) : pair.Value.StartsWith(prefix));
        }
    }
}

