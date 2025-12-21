using IPA;
using IPA.Logging;
using SiraUtil.Zenject;
using System;

namespace MapMemo
{
    [Plugin(RuntimeOptions.DynamicInit)]
    /// <summary>
    /// プラグインのエントリポイント。Zenject のインストールとランタイム設定を提供します。
    /// </summary>
    public class Plugin
    {
        internal static Logger Log;
        // ログ出力の詳細化フラグ（デバッグ時のみ true にする）
        public static bool VerboseLogs = false;

        // LevelDetail にアタッチする際の優先挙動（AttachTo を優先するかどうか）
        // AttachTo が失敗した場合は SelectionHook が代替動作を行います。

        [Init]
        /// <summary>
        /// プラグイン初期化時に呼ばれます。ロガーの設定とメニューインストーラの登録を行います。
        /// </summary>
        public void Init(Logger logger, Zenjector zenjector)
        {
            Log = logger;
            if (VerboseLogs) Log.Info("MapMemo Init");
            zenjector.Install<Installers.MenuInstaller>(Location.Menu);
            Log?.Info("Menu installer registered");
        }

        [OnEnable]
        /// <summary>
        /// プラグイン有効化時のコールバック
        /// </summary>
        public void OnEnable()
        {
            // Log.Info("MapMemo OnEnable");
        }

        [OnDisable]
        /// <summary>
        /// プラグイン無効化時のコールバック
        /// </summary>
        public void OnDisable()
        {
            if (VerboseLogs) Log.Info("MapMemo OnDisable");
        }

        [OnStart]
        /// <summary>
        /// プラグイン開始時の処理（Harmony パッチ適用等を行う）。
        /// </summary>
        public void OnStart()
        {
            if (VerboseLogs) Log.Info("MapMemo OnStart");
            // メニューコンテキストにUIバインディングをインストール（SiraUtil/Zenject前提）
            try
            {
                var harmony = new HarmonyLib.Harmony("com.buri28.mapmemo");
                UI.Patches.MapMemoPatcher.ApplyPatches(harmony);
                if (VerboseLogs) Log.Info("Harmony patches applied");
            }
            catch (Exception e)
            {
                Log.Error($"Harmony init error: {e}");
            }
        }

        [OnExit]
        /// <summary>
        /// アプリケーション終了時の処理
        /// </summary>
        public void OnExit()
        {
            if (VerboseLogs) Log.Info("MapMemo OnExit");
        }
    }
}