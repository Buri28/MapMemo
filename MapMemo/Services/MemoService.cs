using System;
using System.IO;
using MapMemo.Domain;
using System.Threading.Tasks;
using Mapmemo.Models;
using MapMemo.Models;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using MapMemo.Utilities;
using MapMemo.UI.Menu;

namespace MapMemo.Services
{

    /// <summary>
    /// MemoEditModal に関する小さなユーティリティ関数群（位置調整や日時フォーマットなど）。
    /// </summary>
    public class MemoService
    {
        // Zenject経由だとタイミングによってインスタンス生成が間に合わないため自前でシングルトンにする
        public static MemoService Instance { get; private set; } = new MemoService();

        /// <summary>
        /// 各種リソースファイルを読み込みます（辞書、履歴、キーバインド設定）。
        /// </summary>
        public void LoadResources()
        {
            // 辞書ファイルを読み込む
            DictionaryManager.Instance.Load();
            // 入力履歴ファイルを読み込む
            InputHistoryManager.Instance.LoadHistory();
            // キーバインド設定を読み込む (UserData に resource をコピーしてからロード)
            InputKeyManager.Instance.Load();
            // bsr情報を読み込む
            // BsrManager.Instance.LoadBsrInfo();
            BeatSaverManager.Instance.LoadCache();

        }

        /// <summary>
        /// 指定されたレベルコンテキストに対応するメモを読み込みます。
        /// </summary>
        /// <param name="levelContext">レベルコンテキスト</param>
        public MemoEntry LoadMemo(LevelContext levelContext)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoService.LoadMemo: loading memo for level");
            var existingMemoInfo = MemoRepository.Load(levelContext);
            return existingMemoInfo;
        }

        /// <summary>
        /// 指定されたレベルコンテキストに対応するメモを非同期で保存します。
        /// </summary>
        /// <param name="levelContext">レベルコンテキスト</param>
        /// <param name="text">保存するメモのテキスト</param>
        public async Task SaveMemoAsync(
            LevelContext levelContext, string text, string bsrCode = null, bool autoCreateEmptyMemo = false)
        {
            var entry = new MemoEntry
            {
                key = levelContext.GetLevelId(),
                songName = levelContext.GetSongName(),
                songAuthor = levelContext.GetSongAuthor(),
                levelAuthor = levelContext.GetLevelAuthor(),
                bsrCode = bsrCode ?? "",
                memo = text,
                autoCreateEmptyMemo = autoCreateEmptyMemo
            };
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal.OnSave: "
                + $"key='{entry.key}' song='{entry.songName}' "
                + $"author='{entry.songAuthor}' len={text.Length}"
                + $" levelAuthor='{entry.levelAuthor}'"
                + $" bsrCode='{entry.bsrCode}'");

            // 非同期で保存
            await MemoRepository.SaveAsync(entry, isEmptyFile: autoCreateEmptyMemo);
        }

        /// <summary>
        /// 履歴の最大保存件数を取得または設定します。
        /// </summary>
        /// <returns></returns>
        public int GetHistoryMaxCount()
        {
            return MemoSettingsManager.Instance.HistoryMaxCount;
        }

        /// <summary>
        /// 履歴の最大保存件数を保存します。
        /// </summary>
        /// <param name="value"></param>
        public void SaveHistoryMaxCount(int value)
        {
            MemoSettingsManager.Instance.HistoryMaxCount = value;
        }

        /// <summary>
        /// 履歴の表示件数を取得または設定します。
        /// </summary>
        /// <returns></returns>
        public int GetHistoryShowCount()
        {
            return MemoSettingsManager.Instance.HistoryShowCount;
        }

        /// <summary>
        /// 履歴の表示件数を保存します。
        /// </summary>
        /// <param name="value"></param>
        public void SaveHistoryShowCount(int value)
        {
            MemoSettingsManager.Instance.HistoryShowCount = value;
        }

        /// <summary>
        /// BSRコードをツールチップに表示するかどうかを取得または設定します。
        /// </summary>
        /// <returns></returns>
        public bool GetTooltipShowBsr()
        {
            return MemoSettingsManager.Instance.TooltipShowBsr;
        }
        /// <summary>
        /// BSRコードをツールチップに表示するかどうかを保存します
        /// /// </summary>
        /// <param name="value"></param>
        public void SaveTooltipShowBsr(bool value)
        {
            MemoSettingsManager.Instance.TooltipShowBsr = value;
        }
        /// <summary>
        /// レーティング情報をツールチップに表示するかどうかを取得または設定します。
        /// </summary>
        public bool GetTooltipShowRating()
        {
            return MemoSettingsManager.Instance.TooltipShowRating;
        }
        /// <summary>
        /// レーティング情報をツールチップに表示するかどうかを保存します。
        /// </summary>
        /// <param name="value"></param>
        public void SaveTooltipShowRating(bool value)
        {
            MemoSettingsManager.Instance.TooltipShowRating = value;
        }
        /// <summary>
        /// 空のメモを自動作成するかどうかを取得または
        /// 設定します。
        /// </summary>
        public bool IsAutoCreateEmptyMemo()
        {
            return MemoSettingsManager.Instance.AutoCreateEmptyMemo;
        }
        /// <summary>
        /// 空のメモを自動作成するかどうかを保存します。
        /// </summary>
        /// <param name="value"></param>
        public void SaveAutoCreateEmptyMemo(bool value)
        {
            MemoSettingsManager.Instance.AutoCreateEmptyMemo = value;
        }
        public string GetBeatSaverAccessMode()
        {
            return MemoSettingsManager.Instance.BeatSaverAccessMode;
        }
        public void SaveBeatSaverAccessMode(string value)
        {
            MemoSettingsManager.Instance.BeatSaverAccessMode = value;
        }

        /// <summary>
        /// 入力履歴ファイルを削除します。
        /// </summary>
        public void DeleteHistory()
        {
            InputHistoryManager.DeleteHistory();
        }

        /// <summary>
        /// 入力履歴にエントリを追加します。
        /// </summary>
        /// <param name="text">追加するテキスト</param>
        /// <param name="subText">サブテキスト（省略可能）
        public void AddHistory(string text, string subText = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (string.IsNullOrWhiteSpace(text)) return;
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoService.AddHistory: "
                + $"Adding history text='{text}' subText='{(subText ?? "null")}'");
            string addText = StringUtils.RemoveLineBreaks(text);
            string addSubText = StringUtils.RemoveLineBreaks(subText);
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoService.AddHistory2: "
                + $"Adding history text='{addText}' subText='{(addSubText ?? "null")}'");
            if (addText.Length < 2) return;

            // 絵文字は1文字でも履歴に追加するが、絵文字以外の1文字は無視する
            if (!IsOnlyEmoji(addText) && addText.Length < 2)
            {
                return;
            }
            InputHistoryManager.Instance.AddHistory(addText, addSubText);
        }

        /// <summary>
        /// 履歴から指定したプレフィックスで始まるエントリを取得します。
        /// </summary>
        /// <param name="search">検索するプレフィックス</param>
        /// <returns>一致する履歴エントリのリスト</returns>
        public List<KeyValuePair<string, string>> SearchHistory(string search)
        {
            var history = InputHistoryManager.Instance.HistoryList;
            int historyShowCount = GetHistoryShowCount();

            if (Plugin.VerboseLogs) Plugin.Log?.Info($"SuggestionListController: "
                + $"Adding history suggestions for '{search}' historyShowCount={historyShowCount}");

            var historyMatches = history
                .AsEnumerable()
                .Reverse()
                .Where(h =>
                        (h.Key != null && StartsWithTextElement(h.Key, search)) ||
                        StartsWithTextElement(h.Value, search))
                .Distinct()
                .Take(historyShowCount)
                .ToList();
            return historyMatches;
        }

        /// <summary>
        /// 辞書から指定したプレフィックスで始まるエントリを取得します。
        /// </summary>
        /// <param name="search">検索するプレフィックス</param>
        /// <returns>一致する辞書エントリのリスト</returns>
        public List<KeyValuePair<string, string>> SearchDictionary(string search)
        {
            if (string.IsNullOrEmpty(search) || search == ",")
            {
                return new List<KeyValuePair<string, string>>();
            }
            var dictionaryWords = DictionaryManager.Instance.dictionaryWords;

            if (Plugin.VerboseLogs) Plugin.Log?.Info($"DictionaryManager: "
                + $"Searching for prefix '{search}'"
                + $" among {dictionaryWords.Count} dictionary words.");

            // キーがヒットしなかったら値を見るのではなく、キーがなかったら値をみる
            return dictionaryWords.Where(
                pair =>
                    ((!string.IsNullOrEmpty(pair.Key) && StartsWithTextElement(pair.Key, search)) ||
                    (string.IsNullOrEmpty(pair.Key) && StartsWithTextElement(pair.Value, search))
            ))
            .Distinct()
            .ToList();
        }

        /// <summary>
        /// 指定した文字列に対応する絵文字エントリを取得します。
        /// </summary>
        /// <param name="search">検索する文字列</param>
        /// <returns>一致する絵文字エントリのリスト</returns>
        public List<KeyValuePair<string, List<string>>> SearchEmojis(string search)
        {
            // Dictionary<string, List<string>>
            var supportedEmojis = InputKeyManager.Instance.SupportedEmojiMap;

            // 絞り込んで変換
            var matchedEmojis = supportedEmojis
                .Where(kv => StartsWithTextElement(kv.Key, search))
                .ToList();
            return matchedEmojis;
        }

        /// <summary>
        /// 指定した文字列が指定したプレフィックスで始まるかを、テキスト要素単位で判定します。
        /// </summary>
        /// <param name="text">対象の文字列</param>
        /// <param name="prefix">プレフィックス文字列</param>
        /// <returns>始まる場合は true、そうでなければ false</returns>
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
        /// 指定したキー番号と範囲タイプに対応する InputKeyEntry を取得します。
        /// </summary>
        /// <param name="keyNo">キー番号</param>
        /// <param name="rangeType">範囲タイプ</param>
        /// <returns>対応する InputKeyEntry、存在しない場合は null</returns
        public InputKeyEntry GetInputKeyEntry(int keyNo, string rangeType)
        {
            return InputKeyManager.Instance?.GetInputKeyEntryByKeyNo(keyNo, rangeType);
        }

        /// <summary>
        /// 入力文字列が絵文字のみで構成されているかを判定する 1文字の場合
        /// </summary>
        /// <param name="input">入力文字列</param>
        /// <returns>絵文字のみで構成されている場合は true、そうでなければ false</returns>
        public bool IsOnlyEmoji(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            var supportedEmojis = InputKeyManager.Instance.SupportedEmojiMap;
            foreach (var emojiList in supportedEmojis.Values)
            {
                foreach (var emoji in emojiList)
                {
                    if (input == emoji)
                        return true;
                }

            }
            return false;
        }

        /// <summary>
        /// 指定文字列の重み付き長さを返す（改行は無視）。
        /// </summary>
        // public double GetWeightedLength(string text)
        // {
        //     if (string.IsNullOrEmpty(text)) return 0.0;
        //     var oneLine = text.Replace("\r", "").Replace("\n", "");
        //     var indices = StringInfo.ParseCombiningCharacters(oneLine);
        //     double length = 0.0;
        //     for (int i = 0; i < indices.Length; i++)
        //     {
        //         int start = indices[i];
        //         int end = (i + 1 < indices.Length) ? indices[i + 1] : oneLine.Length;
        //         var elem = oneLine.Substring(start, end - start);

        //         var weightedRate = GetWeightedRate(elem);
        //         if (weightedRate == 1.0)
        //         {
        //             weightedRate = StringHelper.IsHalfWidthElement(elem) ? 0.5 : 1.0;
        //         }

        //         if (Plugin.VerboseLogs) Plugin.Log?.Info($"GetWeightedLength: elem='{elem}' weightedRate={weightedRate}");
        //         length += weightedRate;
        //     }
        //     return length;
        // }

        /// <summary>
        /// 指定文字列を重み付き長さでカットします。改行は無視。
        /// </summary>
        ///  <param name="text">対象の文字列</param>
        /// <param name="maxLength">最大長さ（重み付き）</param>
        /// <returns>カットされた文字列と、完全に収まったかどうかのタプル</returns>
        public (string, bool, double) GetWeightedCutString(string text, double maxLength)
        {
            if (string.IsNullOrEmpty(text)) return ("", true, 0);
            var oneLine = text.Replace("\r", "").Replace("\n", "");
            var indices = StringInfo.ParseCombiningCharacters(oneLine);
            double length = 0.0;
            int cutIndex = oneLine.Length;

            for (int i = 0; i < indices.Length; i++)
            {
                int start = indices[i];
                int end = (i + 1 < indices.Length) ? indices[i + 1] : oneLine.Length;

                // 安全チェックを追加
                if (start < 0 || start >= oneLine.Length || end > oneLine.Length || end < start)
                {
                    continue; // 無効な範囲はスキップ
                }

                var elem = oneLine.Substring(start, end - start);

                var weightedRate = GetWeightedRate(elem);
                if (weightedRate == 1.0)
                {
                    weightedRate = StringUtils.IsHalfWidthElement(elem) ? 0.5 : 1.0;
                }

                length += weightedRate;
                if (length > maxLength)
                {
                    cutIndex = start;
                    break;
                }
            }

            // ここでcutIndexが範囲外にならないように修正
            if (cutIndex < 0) cutIndex = 0;
            if (cutIndex > oneLine.Length) cutIndex = oneLine.Length;

            return (oneLine.Substring(0, cutIndex), cutIndex == oneLine.Length, length);
        }

        private static double GetWeightedRate(string text)
        {
            // 画面のpref-width=108で全角29文字入る　29文字を1.0とする
            var baseCount = 29.0;
            // pref-width=108で何文字入るか
            var charRateDict = new Dictionary<string, double>()
            {
                { "Q", 54.0 },{ "W", 34.0 },{ "E", 62.0 },{ "R", 54.0 },
                { "T", 67.0 },{ "Y", 57.0 },{ "U", 53.0 },{ "I", 106.0 },
                { "O", 53.0 },{ "P", 56.0 },{ "A", 54.0 },{ "S", 58.0 },
                { "D", 52.0 },{ "F", 64.0 },{ "G", 55.0 },{ "H", 51.0 },
                { "J", 63.0 },{ "K", 55.0 },{ "L", 70.0 },{ "Z", 66.0 },
                { "X", 56.0 },{ "C", 57.0 },{ "V", 56.0 },{ "B", 53.0 },
                { "N", 52.0 },{ "M", 41.0 },
                { "q", 59.0 },{ "w", 40.0 },{ "e", 59.0 },{ "r", 88.0 },
                { "t", 96.0 },{ "y", 62.0 },{ "u", 58.0 },{ "i", 118.0 },
                { "o", 58.0 },{ "p", 59.0 },{ "a", 59.0 },{ "s", 64.0 },
                { "d", 59.0 },{ "f", 94.0 },{ "g", 59.0 },{ "h", 58.0 },
                { "j", 118.0 },{ "k", 60.0 },{ "l", 118.0 },{ "z", 73.0 },
                { "x", 62.0 },{ "c", 61.0 },{ "v", 65.0 },{ "b", 59.0 },
                { "n", 58.0 },{ "m", 39.0 },
            };
            var weightedRate = 1.0;

            if (charRateDict.ContainsKey(text))
            {
                var charCount = charRateDict[text];
                weightedRate = baseCount / charCount;
            }
            return weightedRate;
        }

        /// <summary>
        /// UTC の日時をローカル時間に変換してフォーマットした文字列を返します。
        /// </summary>
        /// <param name="utc">UTC の日時</param>
        /// <returns>ローカル時間に変換してフォーマットした文字列</returns>
        public string FormatLocal(DateTime utc)
        {
            var local = utc.ToLocalTime();
            return $"{local:yyyy/MM/dd HH:mm:ss}";
        }

        /// <summary>
        /// レベル完了時の処理を行います。
        /// </summary>
        /// <param name="data">シーン遷移データ</param>
        /// <param name="results">レベル完了結果</param>
        // public async Task HandleLevelCompletion(
        //     StandardLevelScenesTransitionSetupDataSO data, LevelCompletionResults results)
        // {
        //     bool flowControl = await CreateEmptyMemoIfNeeded(data);
        //     if (!flowControl)
        //     {
        //         return;
        //     }
        // }


        /// <summary>
        /// リザルト画面から遷移する際の処理を行います。
        /// </summary>
        /// <param name="transitionSetupData">シーン遷移データ</param
        public async Task HandleResultTransition(
            StandardLevelScenesTransitionSetupDataSO transitionSetupData,
            LevelCompletionResults results)
        {
            var levelId = transitionSetupData.beatmapLevel.levelID;
            var levelHash = Utilities.BeatSaberUtils.GetLevelHash(levelId);


            if (MemoSettingsManager.Instance.BeatSaverAccessMode == "Manual")
            {
                // マニュアルの場合、BeatSaverからのデータ取得をしない
                if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoEditModal.InitializeParameters: "
                + $"BeatSaverAccessMode is 'Manual', skipping BeatSaver data fetch.");
                // 空のメモを作成する場合はBSRコードなしで作成
                await CreateEmptyMemoIfNeeded(transitionSetupData);

                if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoService.HandleResultTransition: Refreshing MemoPanelController instance");
                await MemoPanelController.Instance.Refresh();
            }
            else
            {
                if (!BeatSaberUtils.IsCustomLevel(levelId))
                {
                    // カスタムレベルでない場合はBeatSaverからデータを取得しない
                    if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoEditModal.InitializeParameters: "
                    + $"Level ID '{levelId}' is not a custom level, skipping BeatSaver data fetch.");
                    // 空のメモを作成する場合はBSRコードなしで作成
                    await CreateEmptyMemoIfNeeded(transitionSetupData);
                }
                else
                {
                    // BeatSaverからデータを取得してMemoPanelを更新
                    UpdateBeatSaverDataAsync(levelHash, async map =>
                    {

                        if (Plugin.VerboseLogs) Plugin.Log?.Info($"MemoEditModal.InitializeParameters: "
                        + $"Using cached BeatSaver map info: id='{map.id}' for hash '{levelHash}'");
                        // 空のメモを作成する場合はBSRコード付きで作成
                        await CreateEmptyMemoIfNeeded(transitionSetupData, map);

                        if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoService.UpdateBeatSaverDataAsync: Refreshing MemoPanelController instance");
                        await MemoPanelController.Instance.Refresh();
                    },
                    error =>
                    {
                        Plugin.Log?.Warn("Failed to fetch BeatSaver data: " + error);
                    });
                }
            }
        }

        /// <summary>
        /// 空のメモを自動作成する必要がある場合に作成します。
        /// </summary>
        /// <param name="data">シーン遷移データ</param>
        /// <returns>空のメモを作成した場合は true、そうでなければ false</returns>
        private async Task<bool> CreateEmptyMemoIfNeeded(
            StandardLevelScenesTransitionSetupDataSO data, BeatSaverMap beatSaverMap = null)
        {
            // 空のメモを自動作成する設定が有効な場合にのみ処理を行う
            if (!IsAutoCreateEmptyMemo()) return false;
            if (Plugin.VerboseLogs) Plugin.Log.Info("AutoCreateEmptyMemo is enabled.");

            // 自動的に空のメモを作成する
            MemoEntry existingMemo = LoadMemo(new LevelContext(data.beatmapLevel));
            // 既にメモが存在する場合は何もしない
            if (existingMemo != null) return false;
            var levelId = data.beatmapLevel.levelID;
            var levelHash = Utilities.BeatSaberUtils.GetLevelHash(levelId);
            if (Plugin.VerboseLogs) Plugin.Log.Info($"ResultListener: "
                + $"Auto-creating empty memo for level ID: {levelId}, Hash: {levelHash}");

            await SaveMemoAsync(new LevelContext(data.beatmapLevel), "", beatSaverMap?.id, true);

            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoService.CreateEmptyMemoIfNeeded: Refreshing MemoPanelController instance");
            await MemoPanelController.Instance.Refresh();
            return true;
        }

        /// <summary>
        /// BeatSaverのデータを非同期で更新します。
        /// </summary>
        /// <param name="hash">レベルのハッシュ値</param>
        /// <param name="onSuccess">成功時のコールバック</param>
        public void UpdateBeatSaverDataAsync(
            string hash, Action<BeatSaverMap> onSuccess, Action<string> onError)
        {
            BeatSaverManager.Instance.TryRequestAsync(hash, onSuccess, onError);
        }


    }

}
