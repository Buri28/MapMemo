using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
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
            AddEmojiSuggestions(search, already);

            UpdateSelection();
            suggestionList.TableView.ReloadData();
        }

        private void AddEmptySuggestion()
        {
            AddSuggestion("");
        }

        private void AddEmojiSuggestions(string search, HashSet<KeyValuePair<string, string>> already)
        {
            if (string.IsNullOrEmpty(search)) return;

            Plugin.Log?.Info($"SuggestionListController: Adding emoji suggestions for '{search}'");
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
                        if (already.Add(new KeyValuePair<string, string>(search, emoji)))
                        {
                            Plugin.Log?.Info($"Adding emoji suggestion: '{emoji}' for key '{search}'");
                            AddSuggestion(emoji, search);
                        }
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
                .Where(h =>
                        (h.Key != null && StartsWithTextElement(h.Key, search)) ||
                        StartsWithTextElement(h.Value, search))
                .Distinct()
                .Take(historyShowCount)
                .ToList();

            foreach (var h in historyMatches)
            {
                if (already.Add(h))
                {
                    Plugin.Log?.Info($"Adding history suggestion: Key='{h.Key}', Value='{h.Value}'");
                    AddSuggestion(h.Value, h.Key);
                }
            }
        }
        // private static bool StartsWithTextElement(string text, string search)
        // {
        //     if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(search)) return false;

        //     Plugin.Log?.Info("StartsWithTextElement(text:search):" + DumpCodePoints(GetTextElements(text))
        //     + ":" + DumpCodePoints(GetTextElements(search)));

        //     var textEnum = StringInfo.GetTextElementEnumerator(text);
        //     var searchEnum = StringInfo.GetTextElementEnumerator(search);

        //     if (!textEnum.MoveNext() || !searchEnum.MoveNext()) return false;

        //     string firstTextElement = textEnum.GetTextElement();
        //     string searchElement = searchEnum.GetTextElement();

        //     return firstTextElement == searchElement;
        // }
        // private static IEnumerable<string> GetTextElements(string text)
        // {
        //     var enumerator = StringInfo.GetTextElementEnumerator(text);
        //     while (enumerator.MoveNext())
        //     {
        //         yield return enumerator.GetTextElement();
        //     }
        // }
        // private static string DumpCodePoints(IEnumerable<string> text)
        // {
        //     return string.Join(" ", text.Select(r => $"U+{r:X}"));
        // }
        public static bool StartsWithTextElement(string text, string prefix)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(prefix)) return false;

            var textEnum = StringInfo.GetTextElementEnumerator(text);
            var prefixEnum = StringInfo.GetTextElementEnumerator(prefix);

            var textBuilder = new List<string>();
            var prefixBuilder = new List<string>();

            while (prefixEnum.MoveNext())
            {
                prefixBuilder.Add(prefixEnum.GetTextElement());
            }

            for (int i = 0; i < prefixBuilder.Count; i++)
            {
                if (!textEnum.MoveNext()) return false;
                textBuilder.Add(textEnum.GetTextElement());
            }

            return string.Join("", textBuilder) == string.Join("", prefixBuilder);
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
                    Plugin.Log?.Info($"Adding dictionary suggestion: Key='{pair.Key}', Value='{pair.Value}'");
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
