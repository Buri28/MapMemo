using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MapMemo.UI.Edit;
using UnityEngine;

namespace MapMemo.Core
{
    public class DictionaryManager : MonoBehaviour
    {
        private readonly object _lock = new object();
        private List<KeyValuePair<string, string>> dictionaryWords = new List<KeyValuePair<string, string>>();
        private bool loaded = false;
        private string userDictionaryPath;

        public static DictionaryManager Instance { get; private set; }

        private void Awake()
        {
            Plugin.Log?.Info("DictionaryManager Awake");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        // Create instance on demand (for static callers before Unity set up)
        private static DictionaryManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("DictionaryManager");
            return go.AddComponent<DictionaryManager>();
        }

        public IReadOnlyList<KeyValuePair<string, string>> DictionaryWords
        {
            get { lock (_lock) { return dictionaryWords; } }
        }

        // Instance API
        public DictionaryManager Load(string baseDir = null)
        {
            var basePath = baseDir ?? Environment.CurrentDirectory;
            userDictionaryPath = Path.Combine(basePath, "UserData", "MapMemo", "#dictionary.txt");
            LoadInternal();
            return this;
        }

        public void EnsureLoadedInstance()
        {
            if (!loaded) LoadInternal();
        }

        public void ReloadInstance()
        {
            lock (_lock)
            {
                loaded = false;
            }
            LoadInternal();
        }

        private void LoadInternal()
        {
            lock (_lock)
            {
                if (loaded) return;
                try
                {
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
                                dictionaryWords = new List<KeyValuePair<string, string>>();
                                loaded = true;
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
                        dictionaryWords = new List<KeyValuePair<string, string>>();
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log?.Error($"DictionaryManager: Failed to load dictionary file: {ex.Message}");
                    dictionaryWords = new List<KeyValuePair<string, string>>();
                }
                loaded = true;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> GetMatchesInstance(string prefix)
        {
            EnsureLoadedInstance();
            if (string.IsNullOrEmpty(prefix) || prefix == ",")
            {
                return Enumerable.Empty<KeyValuePair<string, string>>();
            }
            Plugin.Log?.Info($"DictionaryManager: Searching for prefix '{prefix}'" +
                $" among {dictionaryWords.Count} dictionary words.");
            return dictionaryWords.Where(
                pair =>
                    (!string.IsNullOrEmpty(pair.Key) && SuggestionListController.StartsWithTextElement(pair.Key, prefix)) ||
                    (!string.IsNullOrEmpty(pair.Value) && SuggestionListController.StartsWithTextElement(pair.Value, prefix))
            );
        }

        // Static compatibility wrappers
        public static void EnsureLoaded() => EnsureInstance().EnsureLoadedInstance();
        public static void Reload() => EnsureInstance().ReloadInstance();
        public static DictionaryManager LoadStatic(string baseDir = null) => EnsureInstance().Load(baseDir);
        public static IEnumerable<KeyValuePair<string, string>> GetMatches(string prefix) => EnsureInstance().GetMatchesInstance(prefix);

        private void OnDestroy()
        {
            Plugin.Log?.Info("DictionaryManager OnDestroy");
        }
    }
}