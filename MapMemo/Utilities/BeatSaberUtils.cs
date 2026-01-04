


using System;
using System.IO;
using IPA.Utilities;

namespace MapMemo.Utilities
{
    /// <summary>
    /// Beat Saber 関連のユーティリティメソッドを提供します。
    /// </summary>
    public static class BeatSaberUtils
    {
        public static string GetLevelHash(string levelId)
        {
            if (Plugin.VerboseLogs) Plugin.Log?.Info($"BeatSaberUtils: Getting hash for levelID='{levelId}'");
            if (string.IsNullOrEmpty(levelId) || !levelId.StartsWith("custom_level_"))
            {
                return levelId;
            }
            return levelId != "unknown" ? levelId.Substring("custom_level_".Length) : "unknown";
        }
        /// <summary>
        /// UserData パスを取得する。
        /// パスが存在しない場合は null を返す。
        /// </summary>
        public static string GetBeatSaberUserDataPath(string baseDir = "MapMemo")
        {
            try
            {
                // IPA の UnityGame API から UserData パスを取得
                string userDataPath = Path.Combine(UnityGame.UserDataPath, baseDir);

                // Menu/Gameplay 共にノイズになるため Debug に降格
                if (Plugin.VerboseLogs) Plugin.Log?.Debug($"BeatSaberUtils:UserData path: {userDataPath}");

                return userDataPath;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"BeatSaberUtils: Error determining Beat Saber UserData path: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }


    }
}