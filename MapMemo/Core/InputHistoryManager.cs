using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapMemo.UI.Edit;
using UnityEngine;

namespace MapMemo.Core
{
    /// <summary>
    /// 入力履歴を管理し、ファイルへの保存と読み込みを提供する MonoBehaviour シングルトン。
    /// </summary>
    public class InputHistoryManager : MonoBehaviour
    {
        private string historyFilePath;
        private int maxHistoryCount = 500;

        public static InputHistoryManager Instance { get; private set; }
        public List<KeyValuePair<string, string>> historyList { get; set; } = new List<KeyValuePair<string, string>>();
        /// <summary>
        /// MonoBehaviour の初期化時に呼ばれ、シングルトンの登録を行います。
        /// </summary>
        private void Awake()
        {
            Plugin.Log?.Info("InputHistoryManager Awake");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// 履歴ファイルを読み込み、最大履歴件数を設定します。
        /// </summary>
        public InputHistoryManager LoadHistory(string userDataDir, int maxCount = 500)
        {
            Directory.CreateDirectory(userDataDir);
            historyFilePath = Path.Combine(userDataDir, "_input_history.txt");
            maxHistoryCount = maxCount;

            LoadHistory();

            return this;
        }

        /// <summary>
        /// 静的メソッド: UserData/MapMemo/_input_history.txt を削除します。
        /// </summary>
        public static void DeleteHistory()
        {
            var path = Path.Combine("UserData", "MapMemo", "_input_history.txt");
            if (File.Exists(path))
                File.Delete(path);

            InputHistoryManager.Instance?.ClearHistory();
            Plugin.Log?.Info("Input history deleted.");
        }

        /// <summary>
        /// 履歴にエントリを追加します。通常の1文字は無視し、絵文字は追加します。
        /// </summary>
        public void AddHistory(string text, string subText = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (historyList == null) historyList = new List<KeyValuePair<string, string>>();
            text = text.Trim().Replace("\r", "").Replace("\n", "");


            if (string.IsNullOrWhiteSpace(text)) return;

            // 絵文字は1文字でも履歴に追加するが、絵文字以外の1文字は無視する
            if (!InputKeyManager.Instance.IsOnlyEmoji(text) && text.Length < 2)
            {
                return;
            }

            historyList.RemoveAll(x => x.Key == subText && x.Value == text);
            historyList.Add(new KeyValuePair<string, string>(subText, text));
            while (historyList.Count > maxHistoryCount)
            {
                historyList.RemoveAt(0);
            }
        }

        /// <summary>
        /// 履歴ファイルを読み込み、内部の履歴リストを初期化します。
        /// </summary>
        private void LoadHistory()
        {
            if (!File.Exists(historyFilePath))
            {
                historyList = new List<KeyValuePair<string, string>>();
                return;
            }
            var historyLines = File.ReadAllLines(historyFilePath).ToList();
            historyList = historyLines
                .Select(line =>
                {
                    var parts = line.Split(new[] { ',' }, 2);
                    var splitIndex = line.IndexOf(',');
                    if (splitIndex >= 0)
                    {
                        var key = line.Substring(0, splitIndex).Trim();
                        var value = line.Substring(splitIndex + 1).Trim();
                        return new KeyValuePair<string, string>(key, value);
                    }
                    else
                    {
                        return new KeyValuePair<string, string>(null, line.Trim());
                    }
                })
                .ToList();
        }

        /// <summary>
        /// 履歴ファイルを削除し、メモリ上の履歴をクリアします。
        /// </summary>
        public void ClearHistory()
        {
            if (File.Exists(historyFilePath))
                File.Delete(historyFilePath);
            historyList = new List<KeyValuePair<string, string>>();
        }

        /// <summary>
        /// 保存する履歴の最大件数を設定します。
        /// </summary>
        public void SetMaxHistoryCount(int count)
        {
            maxHistoryCount = count;
            if (historyList == null) historyList = new List<KeyValuePair<string, string>>();
            while (historyList.Count > maxHistoryCount)
            {
                historyList.RemoveAt(0);
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
                if (string.IsNullOrEmpty(historyFilePath) || historyList == null) return;
                File.WriteAllLines(historyFilePath, historyList.Select(
                    kv => kv.Key != null ? $"{kv.Key},{kv.Value}" : kv.Value));
                Plugin.Log?.Info("Input history saved.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"Failed to save input history: {ex}");
            }
        }

        /// <summary>
        /// MonoBehaviour の破棄時に呼ばれ、履歴を保存します。
        /// </summary>
        private void OnDestroy()
        {
            Plugin.Log?.Info("OnDestroy: Saving input history.");
            try
            {
                SaveHistory();
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"Failed to save input history on destroy: {ex}");
            }
        }

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
