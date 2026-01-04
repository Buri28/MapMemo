using System;
using System.Collections.Generic;
using System.IO;
using MapMemo.Utilities;
using UnityEngine;

namespace MapMemo.Domain
{
    /// <summary>
    /// 辞書（単語/候補リスト）の読み込みおよび検索を管理するシングルトンの MonoBehaviour。
    /// Unity のライフサイクル（Awake/OnDestroy）を利用します。
    /// </summary>
    public class DictionaryManager : MonoBehaviour
    {
        private readonly object _lock = new object();
        public List<KeyValuePair<string, string>> dictionaryWords { get; private set; } =
            new List<KeyValuePair<string, string>>();
        private bool loaded = false;
        private string userDictionaryPath;

        public static DictionaryManager Instance { get; private set; }

        /// <summary>
        /// MonoBehaviour の初期化時に呼ばれ、シングルトンの登録と DontDestroyOnLoad を設定します。
        /// </summary>
        private void Awake()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("DictionaryManager Awake");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// インスタンス用 API（辞書の読み込みなどを提供します）。
        /// </summary>
        public DictionaryManager Load()
        {
            userDictionaryPath = Path.Combine(
                BeatSaberUtils.GetBeatSaberUserDataPath("MapMemo"), "#dictionary.txt");
            LoadInternal();
            return this;
        }

        /// <summary>
        /// 辞書を読み込む内部処理です。埋め込みリソースのコピーとファイルの解析を行います。
        /// </summary>
        private void LoadInternal()
        {
            lock (_lock)
            {
                if (loaded) return;
                try
                {
                    bool isCopied = CopyResources(userDictionaryPath);
                    if (!isCopied)
                    {
                        dictionaryWords = new List<KeyValuePair<string, string>>();
                        loaded = true;
                        return;
                    }
                    // 辞書ファイルを読み込み、キーと値のペアに分割してリストに格納。
                    List<KeyValuePair<string, string>> list = LoadDictionary(userDictionaryPath);
                    dictionaryWords = list;
                    Plugin.Log?.Info($"DictionaryManager: Loaded {dictionaryWords.Count} dictionary words.");
                }
                catch (Exception ex)
                {
                    Plugin.Log?.Error($"DictionaryManager: Failed to load dictionary file: {ex.Message}");
                    dictionaryWords = new List<KeyValuePair<string, string>>();
                }
                loaded = true;
            }
        }

        /// <summary>
        /// 埋め込みリソースから辞書ファイルを UserData にコピーします。
        /// </summary>
        private static bool CopyResources(string userDictionaryPath)
        {
            // 埋め込みリソース（デフォルト辞書）を UserData にコピー（ファイルが存在しない場合）。
            if (!File.Exists(userDictionaryPath))
            {
                var asm = typeof(DictionaryManager).Assembly;
                var resourceName = "MapMemo.Resources.#dictionary.txt";
                using (var stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(userDictionaryPath));
                        using (var fs = new FileStream(
                                userDictionaryPath, FileMode.Create, FileAccess.Write))
                        {
                            stream.CopyTo(fs);
                        }
                        Plugin.Log?.Info($"DictionaryManager: "
                            + "Copied dictionary from embedded resource to UserData: " + userDictionaryPath);
                    }
                    else
                    {
                        Plugin.Log?.Error($"DictionaryManager: Embedded resource not found: {resourceName}");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 辞書ファイルを読み込み、キーと値のペアのリストを返します。
        /// </summary>
        private static List<KeyValuePair<string, string>> LoadDictionary(
            string userDictionaryPath)
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
            return list;
        }
    }
}