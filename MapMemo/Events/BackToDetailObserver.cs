using System;
using IPA.Utilities;
using MapMemo.Services;
using Zenject;


namespace MapMemo.Events
{
    public class BackToDetailObserver : IInitializable, IDisposable
    {
        /// <summary>
        /// コンストラクタで注入されるシーン遷移データ。
        /// </summary>
        private readonly StandardLevelScenesTransitionSetupDataSO _transitionSetupData;
        /// <summary>
        /// コンストラクタで注入されるリザルト画面コントローラー。
        /// </summary>
        private readonly ResultsViewController _resultsViewController;

        /// <summary>
        /// コンストラクタ。
        /// Zenject によって依存関係が注入されます。
        /// </summary>
        /// <param name="transitionSetupData"></param>
        /// <param name="resultsViewController"></param>
        public BackToDetailObserver(
            StandardLevelScenesTransitionSetupDataSO transitionSetupData,
            ResultsViewController resultsViewController)
        {
            _transitionSetupData = transitionSetupData;
            _resultsViewController = resultsViewController;
        }

        /// <summary>
        /// 初期化処理。
        /// </summary>
        public void Initialize()
        {
            // 「OK/Continue」ボタンが押された時
            _resultsViewController.continueButtonPressedEvent += OnBackToMenu;
            // // 「Restart」ボタンが押された時
            // _resultsViewController.restartButtonPressedEvent += OnRestartPressed;
        }

        /// <summary>
        /// 破棄処理。
        /// </summary>
        public void Dispose()
        {
            if (_resultsViewController != null)
            {
                _resultsViewController.continueButtonPressedEvent -= OnBackToMenu;
                // _resultsViewController.restartButtonPressedEvent -= OnRestartPressed;
            }
        }

        /// <summary>
        /// リザルト画面からメニューに戻る直前の処理。
        /// </summary>
        /// <param name="controller"></param>
        private async void OnBackToMenu(ResultsViewController controller)
        {
            // ここでマップ詳細画面に戻る直前の処理を行う
            if (Plugin.VerboseLogs) Plugin.Log.Info("Returning to menu from results screen.");
            // FieldAccessorを使ってprivateな変数 _levelCompletionResults を取得
            var results = FieldAccessor<ResultsViewController, LevelCompletionResults>.Get(
                ref controller, "_levelCompletionResults");

            if (results != null)
            {
                if (Plugin.VerboseLogs) Plugin.Log.Info($"Result score: {results.modifiedScore}");
                await MemoService.Instance.HandleLevelCompletion(_transitionSetupData, results);
            }

        }

        // private void OnRestartPressed(ResultsViewController controller)
        // {
        //     // 再挑戦が選ばれた場合の処理
        //     Plugin.Log.Info("リスタートが選択されました。");
        // }
    }
}