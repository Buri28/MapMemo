// MyCoolMod/Networking/BeatSaver/BeatSaverClient.cs
using System;
using System.Collections;
using System.Diagnostics;
using BeatSaberMarkupLanguage;
using Mapmemo.Models;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Mapmemo.Networking
{
    public class BeatSaverClient
    {
        private const string BaseUrl = "https://api.beatsaver.com/maps/hash/";

        public static BeatSaverClient Instance { get; } = new BeatSaverClient();

        public IEnumerator GetMapInfoFromLevelHash(
            string levelHash,
            Action<BeatSaverMap, string> onSuccess,
            Action<string> onError)
        {

            if (string.IsNullOrEmpty(levelHash))
            {
                onError?.Invoke("Invalid levelHash format.");
                yield break;
            }

            string url = BaseUrl + levelHash;
            UnityEngine.Debug.Log("BeatSaverClient: Requesting URL: " + url);
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();
            UnityEngine.Debug.Log("BeatSaverClient: Received response from URL: " + url);
            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                UnityEngine.Debug.Log("BeatSaverClient: Received response from URL: " + json);
                BeatSaverMap map = JsonConvert.DeserializeObject<BeatSaverMap>(json);
                onSuccess?.Invoke(map, url);
            }
            else
            {
                UnityEngine.Debug.Log("BeatSaverClient: error: " + www.error);
                onError?.Invoke(www.error);
            }
        }

        // public static string ExtractHash(string levelId)
        // {
        //     const string prefix = "custom_level_";
        //     if (levelId.StartsWith(prefix) && levelId.Length > prefix.Length)
        //     {
        //         return levelId.Substring(prefix.Length);
        //     }
        //     return null;
        // }
    }
}

