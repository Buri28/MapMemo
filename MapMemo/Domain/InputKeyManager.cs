using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Mapmemo.Models;
using MapMemo.Utilities;
using System.Globalization;

namespace MapMemo.Domain
{
    /// <summary>
    /// キー入力（絵文字、リテラルなど）の定義を読み込み、絵文字判定等の機能を提供する Manager。
    /// </summary>
    public class InputKeyManager : MonoBehaviour
    {
        private string bindingsFilePath;
        public static InputKeyManager Instance { get; private set; }
        /// <summary>
        /// キー割当エントリのリスト。 
        /// 
        public List<InputKeyEntry> Keys { get; private set; } = new List<InputKeyEntry>();
        /// <summary>
        /// サポートされている絵文字のマップ（ラベルごとに絵文字リスト）。
        /// </summary>
        public Dictionary<string, List<string>> SupportedEmojiMap { get; private set; } = null;

        /// <summary>
        /// 除外コードポイントの元データリスト（文字列）。
        /// </summary>
        private List<string> excludedRaw = new List<string>();
        /// <summary>
        /// 除外コードポイントのセット（整数）。 
        /// </summary>
        public HashSet<int> ExcludedCodePoints { get; private set; } = new HashSet<int>();

        /// <summary>
        /// 1キーあたりの最大絵文字数（範囲展開時の安全策）。
        /// </summary>
        private int MAX_EMOJI_PER_KEY = 2000;

        /// <summary>
        /// MonoBehaviour の初期化時に呼ばれ、シングルトン登録と DontDestroyOnLoad を実行します。
        /// </summary>
        private void Awake()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("KeyManager Awake");
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// ユーザーデータディレクトリからキー割当ファイルを読み込みます（ファイルが無ければ埋め込みリソースをコピーします）。
        /// </summary>
        public InputKeyManager Load()
        {
            Directory.CreateDirectory(BeatSaberUtils.GetBeatSaberUserDataPath("MapMemo"));
            bindingsFilePath = Path.Combine(BeatSaberUtils.GetBeatSaberUserDataPath("MapMemo"), "#key_bindings.json");

            CopyEmbeddedIfMissing();
            LoadFromFile();
            SupportedEmojiMap = GetSupportedEmojiMap();

            if (Plugin.VerboseLogs)
            {
                WriteDebugLog("", SupportedEmojiMap);
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

            var dir = BeatSaberUtils.GetBeatSaberUserDataPath("MapMemo");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "_all_emoji_log.txt");
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
                    Plugin.Log?.Info($"KeyManager: Copied embedded key bindings to "
                                   + $"{bindingsFilePath} (from {resourceName})");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"KeyManager: Failed to copy embedded resource: {ex}");
            }
        }
        /// <summary>
        /// キー割当ファイルを読み込み、`Keys` と `ExcludedCodePoints` を初期化します。
        /// 破損時はバックアップおよびリカバリを試行します。
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

                Plugin.Log?.Info($"KeyManager: Loaded {Keys.Count}" +
                                 $"key bindings from {bindingsFilePath}; " +
                                 $"excluded={ExcludedCodePoints.Count}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"KeyManager: Failed to load key bindings: {ex}");
                // リカバリ試行: 既存ファイルが破損している可能性があるため、
                // バックアップを作成し埋め込みリソースで置き換えて再試行します。
                try
                {
                    if (File.Exists(bindingsFilePath))
                    {
                        var bak = bindingsFilePath + ".corrupt." +
                                    DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".bak";
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
                        Plugin.Log?.Info($"KeyManager: Recovery successful, "
                                        + $"loaded {Keys.Count} key bindings; " +
                                        $"excluded={ExcludedCodePoints.Count}");
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


        /// <summary>
        /// 指定した keyNo と type に一致する InputKeyEntry を返します。
        /// </summary>
        /// <param name="keyNo">キー番号</param>
        /// <param name="type">キータイプ（例: "Emoji", "Literal"）</param>
        public InputKeyEntry GetInputKeyEntryByKeyNo(int keyNo, string type)
        {
            if (Plugin.VerboseLogs)
            {
                // Plugin.Log?.Info($"GetInputKeyEntryByKeyNo:"
                //                 + $" keyNo={keyNo}, type={type} Keys.Count={Keys.Count}");
                // Keys.ForEach(k =>
                // {
                //     Plugin.Log?.Info($"  Key Entry: "
                //         + $"keyNo={k.keyNo}, type={k.type}, label={k.label}");
                // });
            }

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
                if (!string.Equals(keyEntry.type, "Emoji", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (keyEntry.ranges == null || keyEntry.ranges.Count == 0)
                    continue;

                var emojis = new List<string>();
                foreach (var range in keyEntry.ranges)
                {
                    int start = ParseHexOrDecimal(range.start);
                    int end = ParseHexOrDecimal(range.end);

                    if (start <= 0 || end <= 0 || end < start) continue;
                    // 異常に大きな範囲はスキップします
                    if (end - start > MAX_EMOJI_PER_KEY)
                    {
                        Plugin.Log?.Warn($"KeyManager: Skipping huge emoji range {start:X}-{end:X}");
                        continue;
                    }
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
            if (s.StartsWith("U+", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(2);
            // "0x1F600" のような 16 進表記や 10 進表記に対応します
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                var hex = s.Substring(2);
                if (int.TryParse(hex, NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out int v))
                {
                    return v <= 0x10FFFF ? v : 0;
                }
                return 0;
            }
            if (int.TryParse(s, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int v2))
            {
                return v2 <= 0x10FFFF ? v2 : 0;
            }
            return 0;
        }

        /// <summary>
        /// 指定したコードポイントがサポート対象かどうかを判定します（除外リストを考慮）。
        /// </summary>
        private bool IsEmojiSupported(int codePoint)
        {
            if (ExcludedCodePoints != null && ExcludedCodePoints.Count > 0)
            {
                return !ExcludedCodePoints.Contains(codePoint);
            }
            return true;
        }

        /// <summary>
        /// アルファベットキーエントリのラベルと文字を大文字/小文字に更新します。
        /// </summary>
        /// <param name="isShift"></param>
        public void UpdateAlphaKeyEntries(bool isShift)
        {
            foreach (var keyEntry in Keys)
            {
                try
                {
                    var label = isShift ?
                        keyEntry.label.ToLowerInvariant() :
                        keyEntry.label.ToUpperInvariant();
                    keyEntry.label = label;

                    var charVal = isShift ?
                        keyEntry.@char?.ToLowerInvariant() :
                        keyEntry.@char?.ToUpperInvariant();
                    keyEntry.@char = charVal;
                }
                catch
                {
                    Plugin.Log?.Warn($"InputKeyController.UpdateAlphaButtonLabels: "
                        + $"failed to update KeyEntry label for "
                        + $"label '{keyEntry.label}'"
                        + $"@char '{keyEntry.@char}'"
                        + $"keyNo '{keyEntry.keyNo}'"
                        + $" type '{keyEntry.type}'");
                }
            }
        }

        /// <summary>
        /// かなキーエントリのラベルと文字をひらがな/カタカナに更新します。
        /// </summary>
        /// <param name="isKanaMode"></param>
        public void UpdateKanaKeyEntries(bool isKanaMode)
        {
            foreach (var keyEntry in Keys)
            {
                try
                {
                    var label = isKanaMode ?
                        StringUtils.HiraganaToKatakana(keyEntry.label) :
                        StringUtils.KatakanaToHiragana(keyEntry.label);
                    keyEntry.label = label;

                    var charVal = isKanaMode ?
                        StringUtils.HiraganaToKatakana(keyEntry.@char) :
                        StringUtils.KatakanaToHiragana(keyEntry.@char);
                    keyEntry.@char = charVal;
                }
                catch
                {
                    Plugin.Log?.Warn($"InputKeyController.UpdateKanaButtonLabels: "
                        + $"failed to update KeyEntry label for "
                        + $"label '{keyEntry.label}'"
                        + $"@char '{keyEntry.@char}'"
                        + $"keyNo '{keyEntry.keyNo}'"
                        + $" type '{keyEntry.type}'");
                }
            }
        }

        /// <summary>
        /// 濁点・半濁点付きキーエントリのラベルと文字を更新します。
        /// </summary>
        /// <param name="dakutenMode"></param>
        public void UpdateDakutenKeyEntries(int dakutenMode)
        {
            foreach (var keyEntry in Keys)
            {
                try
                {
                    var label = StringUtils.ConvertDakutenHandakuten(
                        keyEntry.label, dakutenMode);
                    keyEntry.label = label;

                    var charVal = StringUtils.ConvertDakutenHandakuten(
                        keyEntry.@char, dakutenMode);
                    keyEntry.@char = charVal;
                }
                catch
                {
                    Plugin.Log?.Warn($"InputKeyController.UpdateDakutenButtonLabels: "
                        + $"failed to update KeyEntry label for "
                        + $"label '{keyEntry.label}'"
                        + $"@char '{keyEntry.@char}'"
                        + $"keyNo '{keyEntry.keyNo}'"
                        + $" type '{keyEntry.type}'");
                }
            }
        }
    }
}
