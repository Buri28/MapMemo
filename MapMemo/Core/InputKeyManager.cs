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
    /// <summary>
    /// デシリアライズ用の設定モデル（キー割当と除外コードポイント）。
    /// </summary>
    public class InputKeyBindingsConfig
    {
        public List<InputKeyEntry> keys { get; set; }
        public List<string> excluded { get; set; }
    }

    /// <summary>
    /// キー割当のエントリを表すモデル。
    /// </summary>
    public class InputKeyEntry
    {
        public int keyNo { get; set; }
        public string type { get; set; }
        public string label { get; set; }
        public string @char { get; set; }
        public string block { get; set; }
        public List<RangeModel> ranges { get; set; }

        // BSMLの変更前のtextから識別するidを取得して退避するためのプロパティ
        public string id { get; set; }
    }

    public class RangeModel
    {
        public string start { get; set; }
        public string end { get; set; }
    }

    /// <summary>
    /// キー入力（絵文字、リテラルなど）の定義を読み込み、絵文字判定等の機能を提供する Manager。
    /// </summary>
    public class InputKeyManager : MonoBehaviour
    {
        private string bindingsFilePath;
        public static InputKeyManager Instance { get; private set; }

        public List<InputKeyEntry> Keys { get; private set; } = new List<InputKeyEntry>();

        public Dictionary<string, List<string>> supportedEmojiMap = null;

        /// <summary>
        /// MonoBehaviour の初期化時に呼ばれ、シングルトン登録と DontDestroyOnLoad を実行します。
        /// </summary>
        private void Awake()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("KeyManager Awake");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// ユーザーデータディレクトリからキー割当ファイルを読み込みます（ファイルが無ければ埋め込みリソースをコピーします）。
        /// </summary>
        public InputKeyManager Load(string userDataDir)
        {
            Directory.CreateDirectory(userDataDir);
            bindingsFilePath = Path.Combine(userDataDir, "#key_bindings.json");

            CopyEmbeddedIfMissing();
            LoadFromFile();
            supportedEmojiMap = GetSupportedEmojiMap();

            if (Plugin.VerboseLogs)
            {
                WriteDebugLog("", supportedEmojiMap);
            }
            return this;
        }

        /// <summary>
        /// デバッグ用に全絵文字をログに出力し、ファイルに保存します。
        /// </summary>
        private static void WriteDebugLog(string message,
            Dictionary<string, List<string>> emojiMap)
        {
            int totalEmojis = emojiMap.Values.Sum(list => list.Count);
            message = $"All Emoji List ({totalEmojis} emojis)\n" + message + "\n";

            foreach (var kvList in emojiMap)
            {
                message += $"\nKey '{kvList.Key}' has {kvList.Value.Count} emojis:\n";
                foreach (var emoji in kvList.Value)
                {
                    int codePoint = char.ConvertToUtf32(emoji, 0);
                    message += $"key '{kvList.Key}' emoji '{emoji}' code point 0x{codePoint:X}\n";
                }
            }
            message += "\nEnd of Emoji List";

            string path = Path.Combine(Application.persistentDataPath,
            Path.Combine(Environment.CurrentDirectory, "UserData", "MapMemo", "_all_emoji_log.txt"));
            File.WriteAllText(path, message + Environment.NewLine);
        }


        /// <summary>
        /// 埋め込みリソースからキー割当ファイルを UserData にコピーします（必要なら上書き）。
        /// </summary>
        private void CopyEmbeddedIfMissing(bool forceOverwrite = false)
        {
            try
            {
                if (File.Exists(bindingsFilePath) && !forceOverwrite) return;

                var asm = typeof(InputKeyManager).Assembly;
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

        /// <summary>
        /// キー割当ファイルを読み込み、`Keys` と `ExcludedCodePoints` を初期化します。破損時はバックアップおよびリカバリを試行します。
        /// </summary>
        private void LoadFromFile()
        {
            try
            {
                if (!File.Exists(bindingsFilePath))
                {
                    Keys = new List<InputKeyEntry>();
                    Plugin.Log?.Warn($"KeyManager: bindings file not found: {bindingsFilePath}");
                    return;
                }

                var json = File.ReadAllText(bindingsFilePath);
                var cfg = JsonConvert.DeserializeObject<InputKeyBindingsConfig>(json);
                Keys = cfg?.keys ?? new List<InputKeyEntry>();

                // 除外するコードポイントを解析します（例: "0x1FA7B" や 10進表記）
                excludedRaw = cfg?.excluded ?? new List<string>();
                ExcludedCodePoints = new HashSet<int>(excludedRaw
                    .Select(s => ParseHexOrDecimal(s))
                    .Where(v => v > 0));

                Plugin.Log?.Info($"KeyManager: Loaded {Keys.Count} key bindings from {bindingsFilePath}; excluded={ExcludedCodePoints.Count}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"KeyManager: Failed to load key bindings: {ex}");
                // リカバリ試行: 既存ファイルが破損している可能性があるため、バックアップを作成し埋め込みリソースで置き換えて再試行します。
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
                        var cfg2 = JsonConvert.DeserializeObject<InputKeyBindingsConfig>(json2);
                        Keys = cfg2?.keys ?? new List<InputKeyEntry>();
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

                Keys = new List<InputKeyEntry>();
                ExcludedCodePoints = new HashSet<int>();


            }
        }

        public InputKeyEntry GetByKeyNo(int keyNo, string type)
        {
            return Keys.FirstOrDefault(k => k.keyNo == keyNo
                && string.Equals(k.type, type, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// EmojiRange を解析して、ラベルごとの絵文字リストマップを構築して返します。
        /// </summary>
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
        /// 文字列を 16 進（"0x..."）または 10 進の整数として解析します。
        /// </summary>
        /// <param name="s">解析する文字列</param>
        /// <returns>解析結果の整数（失敗時は 0）</returns>
        private static int ParseHexOrDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Trim();
            // "0x1F600" のような 16 進表記や 10 進表記に対応します
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
        public InputKeyEntry FindForClickableTextEntry(ClickableText ct)
        {
            if (ct == null) return null;
            var txt = (ct.text ?? "").Trim().Replace("　", ""); // 全角スペースを除去
            if (string.IsNullOrEmpty(txt)) return null;

            // Emoji タブでは 'emoji-N' のリテラルが使われるため、N をパースして keyNo を取得します
            if (txt.StartsWith("emoji-", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(txt.Substring("emoji-".Length), out int kn))
                    return GetByKeyNo(kn, "EmojiRange");
            }

            // BSML 内で 'literal-<keyNo>' として埋め込まれたリテラルキーに対応します
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
        //         // excludedRaw が null または空で ExcludedCodePoints を持つ場合は、16 進表記でシリアライズします
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

        /// <summary>
        /// 指定したコードポイントがサポート対象かどうかを判定します（除外リストを考慮）。
        /// </summary>
        private bool IsEmojiSupported(int codePoint)
        {

            // 可能であれば KeyManager の除外リスト（JSON）を優先し、無ければ組み込みのリストを利用します。
            try
            {
                var km = MapMemo.Core.InputKeyManager.Instance;
                if (km != null && km.ExcludedCodePoints != null && km.ExcludedCodePoints.Count > 0)
                {
                    return !km.ExcludedCodePoints.Contains(codePoint);
                }
            }
            catch { /* ignore and fallback */ }
            return true;
        }
    }
}
