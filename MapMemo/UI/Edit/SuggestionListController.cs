using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using MapMemo.Core;
using TMPro;

namespace MapMemo.UI.Edit
{
    /// <summary>
    /// Suggestion list helper that owns the suggestion list UI and related logic.
    /// </summary>
    public class SuggestionListController
    {
        private readonly CustomListTableData suggestionList;
        private int historyShowCount;

        public event Action<string, string> SuggestionSelected;

        public SuggestionListController(CustomListTableData suggestionList, int historyShowCount)
        {
            this.suggestionList = suggestionList ?? throw new ArgumentNullException(nameof(suggestionList));
            this.historyShowCount = historyShowCount;

            this.suggestionList.CellSizeValue = 6f;
            this.suggestionList.ExpandCell = true;
            this.suggestionList.TableView.didSelectCellWithIdxEvent += OnCellSelectedInternal;
        }

        private void OnCellSelectedInternal(TableView tableView, int index)
        {
            var selected = suggestionList.Data[index];
            // 選択された文字からリッチテキストを除去してから通知、サブテキストはリッチテキストにしてないのでそのまま渡す
            SuggestionSelected?.Invoke(StripRichText(selected.Text?.ToString()), selected.Subtext?.ToString());
        }
        private string StripRichText(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
        }
        public void Clear()
        {
            suggestionList.Data.Clear();
            suggestionList.TableView.ClearSelection();
            suggestionList.TableView.ReloadData();
        }

        public void AddSuggestion(string value, string subText = null)
        {
            CustomListTableData.CustomCellInfo cellInfo;
            if (string.IsNullOrEmpty(subText))
            {
                cellInfo = new CustomListTableData.CustomCellInfo(value);
            }
            else
            {
                cellInfo = new CustomListTableData.CustomCellInfo(value, subText);
            }
            cellInfo.Text = $"<color=#00FFFF>{cellInfo.Text}</color>";
            suggestionList.Data.Add(cellInfo);
        }

        public void UpdateSuggestions(string pendingText)
        {
            suggestionList.Data.Clear();

            string search = (pendingText ?? "").Replace("\n", "").Replace("\r", "");
            if (string.IsNullOrEmpty(search))
            {
                suggestionList.TableView.ReloadData();
                return;
            }

            AddEmptySuggestion();
            var already = new HashSet<KeyValuePair<string, string>>();
            AddHistorySuggestions(search, already);
            AddDictionarySuggestions(search, already);
            AddEmojiSuggestions(search);

            UpdateSelection();
            suggestionList.TableView.ReloadData();
        }

        private void AddEmptySuggestion()
        {
            AddSuggestion("");
        }

        private void AddEmojiSuggestions(string search)
        {
            if (string.IsNullOrEmpty(search)) return;

            var supportedEmojis = MemoEditModalHelper.emojiMap;
            // 絵文字マップのキーに該当する場合は、そのキーに対する絵文字をすべて追加
            if (supportedEmojis.ContainsKey(search))
            {
                var range = supportedEmojis[search];
                for (int codePoint = range.Start; codePoint <= range.End; codePoint++)
                {
                    string emoji = char.ConvertFromUtf32(codePoint);
                    if (MemoEditModalHelper.IsEmojiSupported(
                        search, emoji, codePoint, range.Start, range.End))
                    {
                        AddSuggestion(emoji);
                    }
                }
                return;
            }
        }

        private void AddHistorySuggestions(string search, HashSet<KeyValuePair<string, string>> already)
        {
            var history = InputHistoryManager.Instance.historyList;

            var historyMatches = history
                .AsEnumerable()
                .Reverse()
                .Where(h => (h.Key != null && h.Key.StartsWith(search)) || h.Value.StartsWith(search))
                .Distinct()
                .Take(historyShowCount)
                .ToList();

            foreach (var h in historyMatches)
            {
                if (already.Add(h))
                {
                    AddSuggestion(h.Value, h.Key);
                }
            }
        }

        private void AddDictionarySuggestions(string search, HashSet<KeyValuePair<string, string>> already)
        {
            if (string.IsNullOrEmpty(search) || search == ",") return;

            var matches = DictionaryManager.GetMatches(search)
                .Distinct()
                .ToList();
            foreach (var pair in matches)
            {
                if (already.Add(pair))
                {
                    AddSuggestion(pair.Value, pair.Key);
                }
            }
        }

        private void UpdateSelection()
        {
            if (suggestionList.Data.Count > 0)
            {
                suggestionList.TableView.SelectCellWithIdx(0, false);
            }
            else
            {
                suggestionList.TableView.ClearSelection();
            }
        }
    }
}
