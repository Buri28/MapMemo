using System;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Components;
using HMUI;

namespace MapMemo.Services
{
    /// <summary>
    /// サジェストリストの UI と関連ロジックを管理するヘルパー。
    /// </summary>
    public class SuggestionListHandler
    {
        private readonly CustomListTableData suggestionList;

        /// <summary>
        /// サジェストが選択されたときに発火するイベント（value, subText）。
        /// </summary>
        public event Action<string, string> SuggestionSelected;

        private MemoService memoService = MemoService.Instance;

        public SuggestionListHandler(CustomListTableData suggestionList)
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
            var text = StripRichText(selected.Text?.ToString() ?? "");
            // セルのプールで混入するゼロ幅スペースを削除してからイベントを投げる
            text = text.Replace("\u200B", "");
            SuggestionSelected?.Invoke(text, selected.Subtext?.ToString());
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
            if (!memoService.IsOnlyEmoji(search)) return;

            if (Plugin.VerboseLogs) Plugin.Log?.Info($"SuggestionListController: Adding emoji suggestions for '{search}'");

            var matchedEmojis = memoService.SearchEmojis(search);
            foreach (var kvp in matchedEmojis)
            {
                var key = kvp.Key;
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

        /// <summary>
        /// 履歴から候補を追加します。
        /// </summary>
        private void AddHistorySuggestions(string search, HashSet<KeyValuePair<string, string>> already)
        {

            var historyMatches = memoService.SearchHistory(search);
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
        /// 辞書から候補を追加します。
        /// </summary>
        private void AddDictionarySuggestions(
            string search, HashSet<KeyValuePair<string, string>> already)
        {
            if (string.IsNullOrEmpty(search) || search == ",") return;

            var matches = memoService.SearchDictionary(search);
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
        /// 指定したプレフィックスに一致する辞書エントリを返します。
        /// </summary>
        private IEnumerable<KeyValuePair<string, string>> SearchDictionary(string prefix)
        {
            return memoService.SearchDictionary(prefix);
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
