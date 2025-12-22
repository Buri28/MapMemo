using IPA.Utilities;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using Mapmemo.Models;

namespace MapMemo.Domain
{
    /// <summary>
    /// MapMemo の設定を読み書きするユーティリティクラス。
    /// </summary>
    public class MemoSettingsManager : MonoBehaviour
    {
        public static MemoSettingsManager Instance { get; private set; }
        private static readonly string SettingsPath = Path.Combine(
            UnityGame.UserDataPath, "MapMemo", "#settings.json");
        public MemoSettingEntity settingsEntity { get; set; }

        /// <summary>
        /// 履歴の最大保存件数を取得または設定します。設定時は保存も行います。
        /// </summary>
        public int HistoryMaxCount
        {
            get => settingsEntity.HistoryMaxCount;
            set { settingsEntity.HistoryMaxCount = value; Save(); }
        }
        /// <summary>
        /// 履歴の表示件数を取得または設定します。 設定時は保存も行います。
        /// </summary>
        public int HistoryShowCount
        {
            get => settingsEntity.HistoryShowCount;
            set { settingsEntity.HistoryShowCount = value; Save(); }
        }

        /// <summary>
        /// MonoBehaviour の初期化時に呼ばれ、シングルトン登録と DontDestroyOnLoad を実行します。
        /// </summary>
        private void Awake()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("MemoSettingsManager Awake");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            settingsEntity = Load();
            if (Plugin.VerboseLogs) Plugin.Log?.Info(
                $"Settings loaded: HistoryMaxCount={settingsEntity.HistoryMaxCount}, " +
                $"HistoryShowCount={settingsEntity.HistoryShowCount}");
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// 設定ファイルを読み込み、インスタンスを返します。
        /// </summary>
        private MemoSettingEntity Load()
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonConvert.DeserializeObject<MemoSettingEntity>(json) ?? new MemoSettingEntity();
            }
            return new MemoSettingEntity();
        }

        /// <summary>
        /// 設定をファイルに保存します。
        /// </summary>
        private void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            var json = JsonConvert.SerializeObject(settingsEntity, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
            if (Plugin.VerboseLogs) Plugin.Log?.Info("Settings saved.");
        }
    }
}
