using IPA;
using IPA.Logging;
using System;

namespace MapMemo
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Logger Log;
        // Global runtime flags to reduce noisy logging in release runs.
        // Set these to true during debugging when you need extra information.
        public static bool VerboseLogs = false;
        public static bool Diagnostics = false;

        [Init]
        public void Init(Logger logger)
        {
            Log = logger;
            Log.Info("MapMemo Init");
        }

        [OnEnable]
        public void OnEnable()
        {
            Log.Info("MapMemo OnEnable");
        }

        [OnDisable]
        public void OnDisable()
        {
            Log.Info("MapMemo OnDisable");
        }

        [OnStart]
        public void OnStart()
        {
            Log.Info("MapMemo OnStart");
            // メニューコンテキストにUIバインディングをインストール（SiraUtil/Zenject前提）
            try
            {
                var harmony = new HarmonyLib.Harmony("com.buri28.mapmemo");
                UI.HarmonyPatches.TryApplyPatches(harmony);
                Log.Info("Harmony patches applied");
            }
            catch (Exception e)
            {
                Log.Error($"Harmony init error: {e}");
            }
        }

        [OnExit]
        public void OnExit()
        {
            Log.Info("MapMemo OnExit");
        }
    }
}