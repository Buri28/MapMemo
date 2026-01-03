using System;
using System.Collections.Generic;
using System.IO;
using Mapmemo.Models;
using Mapmemo.Networking;
using Newtonsoft.Json;
using UnityEngine;

namespace MapMemo.Domain
{
    public class BeatSaverManager : MonoBehaviour
    {
        public static BeatSaverManager Instance { get; private set; }

        private readonly string _cachePath = Path.Combine(
            Path.Combine(Environment.CurrentDirectory, "UserData", "MapMemo"), "#BeatSaverCache.json");
        private Dictionary<string, BeatSaverMap> _cache;

        private void Awake()
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info("BsrManager Awake");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        public void LoadCache()
        {
            if (File.Exists(_cachePath))
            {
                try
                {
                    string json = File.ReadAllText(_cachePath);
                    _cache = JsonConvert.DeserializeObject<Dictionary<string, BeatSaverMap>>(json)
                             ?? new Dictionary<string, BeatSaverMap>();
                }
                catch
                {
                    _cache = new Dictionary<string, BeatSaverMap>();
                }
            }
            else
            {
                _cache = new Dictionary<string, BeatSaverMap>();
            }
            Plugin.Log?.Info($"BeatSaverManager: Loaded cache with {_cache.Count} entries.");
        }

        private void SaveCache()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                File.WriteAllText(_cachePath, json);
            }
            catch
            {
                // ログ出力などしてもOK
            }
        }

        public BeatSaverMap TryGetCache(string hash)
        {
            if (_cache.TryGetValue(hash, out BeatSaverMap map))
            {
                return map;
            }
            else
            {
                return null;
            }
        }

        public void TryRequestAsync(string hash, Action<BeatSaverMap> onSuccess, Action<string> onError)
        {
            StartCoroutine(BeatSaverClient.Instance.GetMapInfoFromLevelHash(hash, (result, url) =>
            {
                Plugin.Log?.Info($"BeatSaverManager: Fetched map info from {url} for hash {hash}");
                // 結果に取得日時(DataTimeStamp)をセット
                result.DataTimeStamp = DateTime.Now;

                Store(hash, result);
                onSuccess?.Invoke(result);
            }, error =>
            {
                Plugin.Log.Warn("API error: " + error);
                onError?.Invoke(error);
            }));
        }

        /// <summary>
        /// キャッシュにマップ情報を保存します。
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="map"></param>
        public void Store(string hash, BeatSaverMap map)
        {
            _cache[hash] = map;
            SaveCache();
        }
    }
}
