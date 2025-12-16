using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapMemo.UI
{
    public class InputHistoryManager
    {
        private readonly string historyFilePath;
        private int maxHistoryCount = 500;
        public InputHistoryManager(string userDataDir, int maxCount = 500)
        {
            Directory.CreateDirectory(userDataDir);
            historyFilePath = Path.Combine(userDataDir, "_input_history.txt");
            maxHistoryCount = maxCount;
        }

        // 静的メソッド: UserData/MapMemo/_input_history.txt を削除
        public static void ClearHistoryStatic()
        {
            var path = Path.Combine("UserData", "MapMemo", "_input_history.txt");
            if (File.Exists(path))
                File.Delete(path);
        }

        public void AddHistory(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 2) return;
            var history = LoadHistory();
            history.RemoveAll(x => x == text);
            history.Add(text);
            while (history.Count > maxHistoryCount)
            {
                history.RemoveAt(0);
            }
            File.WriteAllLines(historyFilePath, history);
        }

        public List<string> LoadHistory()
        {
            if (!File.Exists(historyFilePath)) return new List<string>();
            return File.ReadAllLines(historyFilePath).ToList();
        }

        public void ClearHistory()
        {
            if (File.Exists(historyFilePath))
                File.Delete(historyFilePath);
        }

        public void SetMaxHistoryCount(int count)
        {
            maxHistoryCount = count;
            var history = LoadHistory();
            while (history.Count > maxHistoryCount)
            {
                history.RemoveAt(0);
            }
            File.WriteAllLines(historyFilePath, history);
        }
    }
}
