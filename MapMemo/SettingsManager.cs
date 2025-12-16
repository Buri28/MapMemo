using IPA.Utilities;
using Newtonsoft.Json;
using System.IO;

namespace MapMemo
{
    public class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            UnityGame.UserDataPath, "MapMemo", "_settings.json");

        public int HistoryMaxCount { get; set; } = 500;
        public int HistoryShowCount { get; set; } = 3;

        public static SettingsManager Load()
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonConvert.DeserializeObject<SettingsManager>(json) ?? new SettingsManager();
            }
            return new SettingsManager();
        }

        public void Save()
        {
            Plugin.Log?.Info("SettingsManager Save");
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }
    }
}
