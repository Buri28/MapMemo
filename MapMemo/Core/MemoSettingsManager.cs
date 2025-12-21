using IPA.Utilities;
using Newtonsoft.Json;
using System.IO;

namespace MapMemo.Core
{
    /// <summary>
    /// MapMemo の設定を読み書きするユーティリティクラス。
    /// </summary>
    public class MemoSettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            UnityGame.UserDataPath, "MapMemo", "_settings.json");

        public int HistoryMaxCount { get; set; } = 500;
        public int HistoryShowCount { get; set; } = 3;

        /// <summary>
        /// 設定ファイルを読み込み、インスタンスを返します。
        /// </summary>
        public static MemoSettingsManager Load()
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonConvert.DeserializeObject<MemoSettingsManager>(json) ?? new MemoSettingsManager();
            }
            return new MemoSettingsManager();
        }

        /// <summary>
        /// 設定をファイルに保存します。
        /// </summary>
        public void Save()
        {
            Plugin.Log?.Info("SettingsManager Save");
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }
    }
}
