using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapMemo.Utilities;
using UnityEngine;

namespace MapMemo.Domain
{
    /// <summary>
    /// 入力履歴を管理し、ファイルへの保存と読み込みを提供する MonoBehaviour シングルトン。
    /// </summary>
    public class InputHistoryManager : MonoBehaviour
    {
        /// <summary> 履歴ファイルのパス。</summary>
        private string historyFilePath;
        /// <summary> シングルトンインスタンス。</summary> 
        public static InputHistoryManager Instance { get; private set; }
        /// <summary> 入力履歴のリスト。</summary>
        public List<KeyValuePair<string, string>> HistoryList { get; set; }
            = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// MonoBehaviour の初期化時に呼ばれ、シングルトンの登録を行います。
        /// </summary>
        private void Awake()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("InputHistoryManager Awake");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// 履歴ファイルを読み込みます。
        /// </summary>
        public InputHistoryManager LoadHistory()
        {
            Directory.CreateDirectory(BeatSaberUtils.GetBeatSaberUserDataPath("MapMemo"));
            historyFilePath = Path.Combine(BeatSaberUtils.GetBeatSaberUserDataPath("MapMemo"), "#input_history.txt");

            LoadHistoryInternal();

            return this;
        }

        /// <summary>
        /// 静的メソッド: UserData/MapMemo/#input_history.txt を削除します。
        /// </summary>
        public static void DeleteHistory()
        {
            var path = Path.Combine("UserData", "MapMemo", "#input_history.txt");
            if (File.Exists(path))
                File.Delete(path);

            Instance?.ClearHistory();
            Plugin.Log?.Info("Input history deleted.");
        }

        /// <summary>
        /// 履歴にエントリを追加します。通常の1文字は無視し、絵文字は追加します。
        /// </summary>
        public void AddHistory(string text, string subText = null)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("Adding to input history: " +
                                                    $"'{text}' (subText: '{subText}')");

            // 完全一致の重複を削除
            HistoryList.RemoveAll(x =>
                string.Equals(
                    StringUtils.RemoveLineBreaks(x.Key),
                    subText,
                    StringComparison.Ordinal) &&
                string.Equals(
                    StringUtils.RemoveLineBreaks(x.Value),
                    text,
                    StringComparison.Ordinal));

            HistoryList.Add(new KeyValuePair<string, string>(subText, text));
            while (HistoryList.Count > MemoSettingsManager.Instance.HistoryMaxCount)
            {
                HistoryList.RemoveAt(0);
            }
        }

        /// <summary>
        /// 履歴ファイルを読み込み、内部の履歴リストを初期化します。
        /// </summary>
        private void LoadHistoryInternal()
        {
            if (!File.Exists(historyFilePath))
            {
                HistoryList = new List<KeyValuePair<string, string>>();
                return;
            }
            var historyLines = File.ReadAllLines(historyFilePath).ToList();
            HistoryList = historyLines
                .Select(line =>
                {
                    var splitIndex = line.IndexOf(',');
                    string key = splitIndex >= 0 ? line.Substring(0, splitIndex) : null;
                    string value = splitIndex >= 0 ? line.Substring(splitIndex + 1) : line;

                    key = StringUtils.RemoveLineBreaks(key);
                    value = StringUtils.RemoveLineBreaks(value);

                    return new KeyValuePair<string, string>(key, value);
                })
                .Distinct()
                .ToList();

            Plugin.Log?.Info($"Input history loaded. {HistoryList.Count} entries.");
        }

        /// <summary>
        /// 履歴ファイルを削除し、メモリ上の履歴をクリアします。
        /// </summary>
        public void ClearHistory()
        {
            if (File.Exists(historyFilePath))
                File.Delete(historyFilePath);
            HistoryList = new List<KeyValuePair<string, string>>();
        }

        /// <summary>
        /// 保存する履歴の最大件数を設定します。
        /// </summary>
        public void UpdateHistoryList(int count)
        {
            if (HistoryList == null) HistoryList = new List<KeyValuePair<string, string>>();
            while (HistoryList.Count > MemoSettingsManager.Instance.HistoryMaxCount)
            {
                HistoryList.RemoveAt(0);
            }
        }

        /// <summary>
        /// 現在の履歴をファイルに保存します。
        /// </summary>
        public void SaveHistory()
        {
            // ファイルに保存処理
            try
            {
                if (string.IsNullOrEmpty(historyFilePath) || HistoryList == null) return;
                File.WriteAllLines(historyFilePath, HistoryList.Select(
                    kv => kv.Key != null ? $"{kv.Key},{kv.Value}" : kv.Value));
                Plugin.Log?.Info($"Input history saved. {HistoryList.Count} entries.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"Failed to save input history: {ex}");
            }
        }

        // /// <summary>
        // /// MonoBehaviour の破棄時に呼ばれ、履歴を保存します。
        // /// </summary>
        // private void OnDestroy()
        // {
        //     Plugin.Log?.Info("OnDestroy: Saving input history.");
        //     try
        //     {
        //         SaveHistory();
        //     }
        //     catch (Exception ex)
        //     {
        //         Plugin.Log?.Error($"Failed to save input history on destroy: {ex}");
        //     }
        // }

        /// <summary>
        /// アプリケーション終了時に履歴を保存します。
        /// </summary>
        private void OnApplicationQuit()
        {
            Plugin.Log?.Info("OnApplicationQuit: Saving input history.");
            SaveHistory();
        }
    }
}
