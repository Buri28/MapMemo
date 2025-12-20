using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapMemo.UI.Edit;
using UnityEngine;

namespace MapMemo.Core
{
    public class InputHistoryManager : MonoBehaviour
    {
        private string historyFilePath;
        private int maxHistoryCount = 500;

        public static InputHistoryManager Instance { get; private set; }
        public List<KeyValuePair<string, string>> historyList { get; set; } = new List<KeyValuePair<string, string>>();
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

        public InputHistoryManager LoadHistory(string userDataDir, int maxCount = 500)
        {
            Directory.CreateDirectory(userDataDir);
            historyFilePath = Path.Combine(userDataDir, "_input_history.txt");
            maxHistoryCount = maxCount;

            LoadHistory();

            return this;
        }

        // 静的メソッド: UserData/MapMemo/_input_history.txt を削除
        public static void ClearHistoryStatic()
        {
            var path = Path.Combine("UserData", "MapMemo", "_input_history.txt");
            if (File.Exists(path))
                File.Delete(path);
        }

        public void AddHistory(string text, string subText = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (historyList == null) historyList = new List<KeyValuePair<string, string>>();
            text = text.Trim().Replace("\r", "").Replace("\n", "");


            if (string.IsNullOrWhiteSpace(text)) return;

            // 絵文字は1文字でも履歴に追加するが、絵文字以外の1文字は無視する
            if (!KeyManager.Instance.IsOnlyEmoji(text) && text.Length < 2)
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

        public void ClearHistory()
        {
            if (File.Exists(historyFilePath))
                File.Delete(historyFilePath);
            historyList = new List<KeyValuePair<string, string>>();
        }

        public void SetMaxHistoryCount(int count)
        {
            maxHistoryCount = count;
            if (historyList == null) historyList = new List<KeyValuePair<string, string>>();
            while (historyList.Count > maxHistoryCount)
            {
                historyList.RemoveAt(0);
            }
        }
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

        private void OnApplicationQuit()
        {
            Plugin.Log?.Info("OnApplicationQuit: Saving input history.");
            SaveHistory();
        }
    }
}
