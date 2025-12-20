using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using BeatSaberMarkupLanguage.Components;
using System.Globalization;

namespace MapMemo.Core
{
    // Models for deserialization
    public class KeyBindingsConfig
    {
        public List<KeyEntry> keys { get; set; }
        public List<string> excluded { get; set; }
    }

    public class KeyEntry
    {
        public int keyNo { get; set; }
        public string type { get; set; }
        public string label { get; set; }
        public string @char { get; set; }
        public string block { get; set; }
        public string matchOn { get; set; }
        public List<RangeModel> ranges { get; set; }

        // BSMLの変更前のtextから識別するidを取得して退避するためのプロパティ
        public string id { get; set; }
    }

    public class RangeModel
    {
        public string start { get; set; }
        public string end { get; set; }
    }

    public class KeyManager : MonoBehaviour
    {
        private string bindingsFilePath;
        public static KeyManager Instance { get; private set; }

        public List<KeyEntry> Keys { get; private set; } = new List<KeyEntry>();

        public Dictionary<string, List<string>> supportedEmojiMap = null;

        private void Awake()
        {
            Plugin.Log?.Info("KeyManager Awake");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// Load key bindings from the user's data directory (copies embedded resource if missing).
        /// </summary>
        public KeyManager Load(string userDataDir)
        {
            Directory.CreateDirectory(userDataDir);
            bindingsFilePath = Path.Combine(userDataDir, "#key_bindings.json");

            CopyEmbeddedIfMissing();
            LoadFromFile();
            supportedEmojiMap = GetSupportedEmojiMap();

            return this;
        }

        private void CopyEmbeddedIfMissing(bool forceOverwrite = false)
        {
            try
            {
                if (File.Exists(bindingsFilePath) && !forceOverwrite) return;

                var asm = typeof(KeyManager).Assembly;
                var resourceName = "MapMemo.Resources.#key_bindings.json";
                using (var stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Plugin.Log?.Warn($"KeyManager: Embedded resource not found: {resourceName}");
                        return;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(bindingsFilePath));
                    using (var fs = new FileStream(bindingsFilePath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fs);
                    }
                    Plugin.Log?.Info($"KeyManager: Copied embedded key bindings to {bindingsFilePath} (from {resourceName})");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"KeyManager: Failed to copy embedded resource: {ex}");
            }
        }

        private List<string> excludedRaw = new List<string>();
        public HashSet<int> ExcludedCodePoints { get; private set; } = new HashSet<int>();

        private void LoadFromFile()
        {
            try
            {
                if (!File.Exists(bindingsFilePath))
                {
                    Keys = new List<KeyEntry>();
                    Plugin.Log?.Warn($"KeyManager: bindings file not found: {bindingsFilePath}");
                    return;
                }

                var json = File.ReadAllText(bindingsFilePath);
                var cfg = JsonConvert.DeserializeObject<KeyBindingsConfig>(json);
                Keys = cfg?.keys ?? new List<KeyEntry>();

                // parse excluded codepoints (strings like "0x1FA7B" or decimal)
                excludedRaw = cfg?.excluded ?? new List<string>();
                ExcludedCodePoints = new HashSet<int>(excludedRaw
                    .Select(s => ParseHexOrDecimal(s))
                    .Where(v => v > 0));

                Plugin.Log?.Info($"KeyManager: Loaded {Keys.Count} key bindings from {bindingsFilePath}; excluded={ExcludedCodePoints.Count}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"KeyManager: Failed to load key bindings: {ex}");
                // Attempt recovery: the existing file may be corrupted. Back it up and copy embedded resource to replace it, then retry once.
                try
                {
                    if (File.Exists(bindingsFilePath))
                    {
                        var bak = bindingsFilePath + ".corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".bak";
                        File.Move(bindingsFilePath, bak);
                        Plugin.Log?.Warn($"KeyManager: Backed up corrupted bindings file to {bak}");
                        // Force-copy embedded resource to replace corrupted file
                        CopyEmbeddedIfMissing(forceOverwrite: true);
                        // Retry load
                        var json2 = File.ReadAllText(bindingsFilePath);
                        var cfg2 = JsonConvert.DeserializeObject<KeyBindingsConfig>(json2);
                        Keys = cfg2?.keys ?? new List<KeyEntry>();
                        excludedRaw = cfg2?.excluded ?? new List<string>();
                        ExcludedCodePoints = new HashSet<int>(excludedRaw
                            .Select(s => ParseHexOrDecimal(s))
                            .Where(v => v > 0));
                        Plugin.Log?.Info($"KeyManager: Recovery successful, loaded {Keys.Count} key bindings; excluded={ExcludedCodePoints.Count}");
                        return;
                    }
                }
                catch (Exception rex)
                {
                    Plugin.Log?.Error($"KeyManager: Recovery attempt failed: {rex}");
                }

                Keys = new List<KeyEntry>();
                ExcludedCodePoints = new HashSet<int>();


            }
        }

        public KeyEntry GetByKeyNo(int keyNo, string type)
        {
            return Keys.FirstOrDefault(k => k.keyNo == keyNo
                && string.Equals(k.type, type, StringComparison.OrdinalIgnoreCase));
        }

        private Dictionary<string, List<string>> GetSupportedEmojiMap()
        {
            var dict = new Dictionary<string, List<string>>();
            foreach (var keyEntry in Keys)
            {
                if (!string.Equals(keyEntry.type, "EmojiRange", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (keyEntry.ranges == null || keyEntry.ranges.Count == 0)
                    continue;
                var emojis = new List<string>();
                foreach (var range in keyEntry.ranges)
                {
                    int start = ParseHexOrDecimal(range.start);
                    int end = ParseHexOrDecimal(range.end);
                    for (int cp = start; cp <= end; cp++)
                    {
                        if (IsEmojiSupported(cp))
                        {
                            string emoji = char.ConvertFromUtf32(cp);
                            emojis.Add(emoji);
                        }
                    }
                }
                if (emojis.Count > 0)
                {
                    dict[keyEntry.label] = emojis;
                }
            }
            return dict;
        }

        /// <summary>
        /// Parse a string as either hex (0x...) or decimal integer.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static int ParseHexOrDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Trim();
            // support forms like "0x1F600" or decimal strings
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                var hex = s.Substring(2);
                if (int.TryParse(
                    hex, System.Globalization.NumberStyles.HexNumber,
                    null, out int v)) return v;
                return 0;
            }
            if (int.TryParse(s, out int v2)) return v2;
            return 0;
        }

        /// <summary>
        /// ClickableText に対応する KeyEntry を探す
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public KeyEntry FindForClickableTextEntry(ClickableText ct)
        {
            if (ct == null) return null;
            var txt = (ct.text ?? "").Trim().Replace("　", ""); // 全角スペースを除去
            if (string.IsNullOrEmpty(txt)) return null;

            // Emoji tab uses literal text "emoji-N"; try to parse keyNo
            if (txt.StartsWith("emoji-", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(txt.Substring("emoji-".Length), out int kn))
                    return GetByKeyNo(kn, "EmojiRange");
            }

            // Support literal keys encoded in the BSML as "literal-<keyNo>"
            if (txt.StartsWith("literal-", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(txt.Substring("literal-".Length), out int lkn))
                    return GetByKeyNo(lkn, "Literal");
            }
            return null;
        }

        public void Reload()
        {
            LoadFromFile();
        }

        // public void Save()
        // {
        //     try
        //     {
        //         if (string.IsNullOrEmpty(bindingsFilePath)) return;
        //         var cfg = new KeyBindingsConfig { keys = Keys, excluded = excludedRaw };
        //         // If excludedRaw is null or empty but we have ExcludedCodePoints, serialize those as hex strings
        //         if ((cfg.excluded == null || cfg.excluded.Count == 0) && ExcludedCodePoints != null && ExcludedCodePoints.Count > 0)
        //         {
        //             cfg.excluded = ExcludedCodePoints.Select(v => "0x" + v.ToString("X")).ToList();
        //         }
        //         File.WriteAllText(bindingsFilePath, JsonConvert.SerializeObject(cfg, Formatting.Indented));
        //         Plugin.Log?.Info("KeyManager: Saved key bindings file.");
        //     }
        //     catch (Exception ex)
        //     {
        //         Plugin.Log?.Error($"KeyManager: Failed to save key bindings: {ex}");
        //     }
        // }

        /// <summary>
        /// 入力文字列が絵文字のみで構成されているかを判定する 1文字の場合
        /// </summary>  
        public bool IsOnlyEmoji(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            var supportedEmojis = supportedEmojiMap;
            foreach (var emojiList in supportedEmojis.Values)
            {
                foreach (var emoji in emojiList)
                {
                    if (input == emoji)
                        return true;
                }

            }
            return false;
            // var enumerator = StringInfo.GetTextElementEnumerator(input);
            // while (enumerator.MoveNext())
            // {
            //     string element = enumerator.GetTextElement();
            //     if (!IsEmoji(element))
            //         return false;
            // }
            // return true;
        }

        /// 絵文字かどうかを判定する 1文字の場合
        /// <param name="textElement">判定する文字列（テキスト要素）1文字</param>   
        // public bool IsEmoji(string textElement)
        // {
        //     int codepoint = Char.ConvertToUtf32(textElement, 0);

        //     // 絵文字かどうかを判定
        //     foreach (var keyEntry in Keys)
        //     {
        //         var (keyNo, block, ranges) = keyEntry.keyNo > 0 && keyEntry.ranges != null
        //             ? (keyEntry.keyNo, keyEntry.block, keyEntry.ranges.Select(r =>
        //             {
        //                 int start = ParseHexOrDecimal(r.start);
        //                 int end = ParseHexOrDecimal(r.end);
        //                 return (start, end);
        //             }).Where(t => t.start > 0 && t.end >= t.start).ToList())
        //             : (0, null, new List<(int, int)>());
        //         foreach (var (start, end) in ranges)
        //         {
        //             if (codepoint >= start && codepoint <= end)
        //             {
        //                 bool supported = IsEmojiSupported(codepoint);
        //                 if (supported)
        //                 {
        //                     return true;
        //                 }
        //             }
        //         }
        //     }
        //     return false;
        // }

        private bool IsEmojiSupported(int codePoint)
        {

            // Prefer excluded list from KeyManager (JSON) when present; otherwise use built-in list.
            try
            {
                var km = MapMemo.Core.KeyManager.Instance;
                if (km != null && km.ExcludedCodePoints != null && km.ExcludedCodePoints.Count > 0)
                {
                    return !km.ExcludedCodePoints.Contains(codePoint);
                }
            }
            catch { /* ignore and fallback */ }
            return true;
        }

        private void OnDestroy()
        {
            Plugin.Log?.Info("KeyManager OnDestroy: auto-save disabled (file is copied only if missing).");
            // Intentionally not calling Save() to avoid overwriting user's file and changing formatting.
        }

        private void OnApplicationQuit()
        {
            // Auto-save disabled: do not save on application quit to avoid reformatting user's file.
        }
    }


}
