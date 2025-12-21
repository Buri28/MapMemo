using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using MapMemo.Core;

namespace MapMemo.UI.Edit
{
    /// <summary>
    /// サジェストリストの UI と関連ロジックを管理するヘルパー。
    /// </summary>
    public class SuggestionListController
    {
        private readonly CustomListTableData suggestionList;

        /// <summary>
        /// サジェストが選択されたときに発火するイベント（value, subText）。
        /// </summary>
        public event Action<string, string> SuggestionSelected;

        public SuggestionListController(CustomListTableData suggestionList)
        {
            this.suggestionList = suggestionList ?? throw new ArgumentNullException(nameof(suggestionList));

            this.suggestionList.CellSizeValue = 6f;
            this.suggestionList.ExpandCell = true;
            this.suggestionList.TableView.didSelectCellWithIdxEvent += OnCellSelectedInternal;
        }

        /// <summary>
        /// テーブルのセルが選択されたときに呼ばれる内部ハンドラ。
        /// 選択されたセルのテキストからリッチテキストを除去して `SuggestionSelected` イベントを発火します。
        /// </summary>
        private void OnCellSelectedInternal(TableView tableView, int index)
        {
            var selected = suggestionList.Data[index];
            // 選択された文字からリッチテキストを除去してから通知、サブテキストはそのまま渡す
            var text = StripRichText(selected.Text?.ToString() ?? "");
            // セルプール対策(ゼロ幅スペースは選択時には空文字扱いにする。空文字だと別のセルが退避されてしまう)
            text = text.Replace("\u200B", "");
            // 選択された文字からリッチテキストを除去してから通知、サブテキストはリッチテキストにしてないのでそのまま渡す
            SuggestionSelected?.Invoke(StripRichText(selected.Text?.ToString()), selected.Subtext?.ToString());
        }
        /// <summary>
        /// リッチテキストタグを除去してプレーンテキストを返します。
        /// </summary>
        private string StripRichText(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
        }
        /// <summary>
        /// サジェストリストをクリアして UI をリロードします。
        /// </summary>
        public void Clear()
        {
            suggestionList.Data.Clear();
            suggestionList.TableView.ClearSelection();
            suggestionList.TableView.ReloadData();
        }

        /// <summary>
        /// サジェスト項目をリストに追加します。subText はサブテキストとして表示されます。
        /// </summary>
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

        /// <summary>
        /// サジェスト候補を更新します。履歴／辞書／絵文字から候補を収集します。
        /// </summary>
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

        /// <summary>
        /// 空の候補（ゼロ幅スペース）を追加します（セルプール対策）。
        /// </summary>
        private void AddEmptySuggestion()
        {
            // セルプール対策(ゼロ幅スペースは選択時には空文字扱いにする。空文字だと別のセルが退避されてしまう)
            AddSuggestion("\u200B");
        }

        /// <summary>
        /// 絵文字候補を追加します。キーに一致する絵文字をすべて追加します。
        /// </summary>
        private void AddEmojiSuggestions(string search, HashSet<KeyValuePair<string, string>> already)
        {
            if (string.IsNullOrEmpty(search)) return;
            if (!InputKeyManager.Instance.IsOnlyEmoji(search)) return;

            if (Plugin.VerboseLogs) Plugin.Log?.Info($"SuggestionListController: Adding emoji suggestions for '{search}'");
            var supportedEmojis = InputKeyManager.Instance.supportedEmojiMap;
            // 絵文字マップのキーに該当する場合は、そのキーに対する絵文字をすべて追加
            foreach (var kvp in supportedEmojis)
            {
                var key = kvp.Key;
                if (StartsWithTextElement(key, search))
                {
                    var emojiList = kvp.Value;
                    foreach (var emoji in emojiList)
                    {
                        if (already.Add(new KeyValuePair<string, string>(key, emoji)))
                        {
                            if (Plugin.VerboseLogs) Plugin.Log?.Info($"Adding emoji suggestion: '{emoji}' for key '{key}'");
                            AddSuggestion(emoji, key);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 履歴から候補を追加します。
        /// </summary>
        private void AddHistorySuggestions(string search, HashSet<KeyValuePair<string, string>> already)
        {
            var history = InputHistoryManager.Instance.historyList;
            var historyShowCount = MemoSettingsManager.Instance.HistoryShowCount;
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"SuggestionListController: Adding history suggestions for '{search}' historyShowCount={historyShowCount}");

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
                    if (Plugin.VerboseLogs) Plugin.Log?.Info($"Adding history suggestion: Key='{h.Key}', Value='{h.Value}'");
                    AddSuggestion(h.Value, h.Key);
                }
            }
        }

        /// <summary>
        /// 指定した文字列が指定したプレフィックスで始まるかを、テキスト要素単位で判定します。
        /// </summary>
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


        /// <summary>
        /// 辞書から候補を追加します。
        /// </summary>
        private void AddDictionarySuggestions(
            string search, HashSet<KeyValuePair<string, string>> already)
        {
            if (string.IsNullOrEmpty(search) || search == ",") return;

            var matches = DictionaryManager.Instance.Search(search)
                .Distinct()
                .ToList();
            foreach (var pair in matches)
            {
                if (already.Add(pair))
                {
                    if (Plugin.VerboseLogs) Plugin.Log?.Info($"Adding dictionary suggestion: Key='{pair.Key}', Value='{pair.Value}'");
                    AddSuggestion(pair.Value, pair.Key);
                }
            }
        }

        /// <summary>
        /// 現在の選択を更新します（最初の要素を選択するか選択解除）。
        /// </summary>
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
