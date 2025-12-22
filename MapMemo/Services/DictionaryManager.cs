using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapMemo.UI.Edit;
using UnityEngine;

namespace MapMemo.Services
{
    /// <summary>
    /// 辞書（単語/候補リスト）の読み込みおよび検索を管理するシングルトンの MonoBehaviour。
    /// Unity のライフサイクル（Awake/OnDestroy）を利用します。
    /// </summary>
    public class DictionaryManager : MonoBehaviour
    {
        private readonly object _lock = new object();
        private List<KeyValuePair<string, string>> dictionaryWords =
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
        /// Unity の初期化前でも静的呼び出しに対応するため、必要なら GameObject とインスタンスを作成します。
        /// </summary>
        // private static DictionaryManager EnsureInstance()
        // {
        //     if (Instance != null) return Instance;
        //     var go = new GameObject("DictionaryManager");
        //     return go.AddComponent<DictionaryManager>();
        // }

        /// <summary>
        /// 読み込み済みの辞書エントリの読み取り専用リストを返します。
        /// </summary>
        // public IReadOnlyList<KeyValuePair<string, string>> DictionaryWords
        // {
        //     get { lock (_lock) { return dictionaryWords; } }
        // }

        /// <summary>
        /// インスタンス用 API（辞書の読み込みなどを提供します）。
        /// </summary>
        public DictionaryManager Load(string baseDir = null)
        {
            var basePath = baseDir ?? Environment.CurrentDirectory;
            userDictionaryPath = Path.Combine(
                basePath, "#dictionary.txt");
            LoadInternal();
            return this;
        }

        // /// <summary>
        // /// 必要なら辞書を読み込みます（既に読み込まれていれば何もしません）。
        // /// </summary>
        // public void EnsureLoadedInstance()
        // {
        //     if (!loaded) LoadInternal();
        // }

        /// <summary>
        /// 辞書を再読み込みします（内部キャッシュをクリアして再ロード）。
        /// </summary>
        // public void ReloadInstance()
        // {
        //     lock (_lock)
        //     {
        //         loaded = false;
        //     }
        //     LoadInternal();
        // }

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
                        Plugin.Log?.Info($"DictionaryManager: Copied dictionary from embedded resource to UserData: {userDictionaryPath}");
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

        /// <summary>
        /// 指定したプレフィックスに一致する辞書エントリを返します。
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> Search(string prefix)
        {
            // EnsureLoadedInstance();
            if (string.IsNullOrEmpty(prefix) || prefix == ",")
            {
                return Enumerable.Empty<KeyValuePair<string, string>>();
            }
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"DictionaryManager: Searching for prefix '{prefix}'" +
                $" among {dictionaryWords.Count} dictionary words.");

            // キーがヒットしなかったら値を見るのではなく、キーがなかったら値をみる
            return dictionaryWords.Where(
                pair =>
                    ((!string.IsNullOrEmpty(pair.Key) && SuggestionListHandler.StartsWithTextElement(pair.Key, prefix)) ||
                    (string.IsNullOrEmpty(pair.Key) && SuggestionListHandler.StartsWithTextElement(pair.Value, prefix))
            ));

            // return dictionaryWords.Where(
            //     pair =>
            //         (!string.IsNullOrEmpty(pair.Key)
            //          && SuggestionListController.StartsWithTextElement(pair.Key, prefix)) ||
            //         (!string.IsNullOrEmpty(pair.Value)
            //          && SuggestionListController.StartsWithTextElement(pair.Value, prefix))
            // );
        }
    }
}