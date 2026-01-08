using System;
using HarmonyLib;
using IPA.Utilities;
using MapMemo.Services;
using Zenject;


namespace MapMemo.Events
{
    /// <summary>
    /// リザルト画面のイベントを監視し、メモ保存処理をトリガーするオブザーバークラス。
    /// </summary>
    public class ResultObserver : IInitializable, IDisposable
    {
        /// <summary>
        /// リザルト画面がアクティブかどうかを示すフラグ
        /// </summary>
        // private bool _isResultsActive = false;
        /// <summary>
        /// コンストラクタで注入されるシーン遷移データ。
        /// </summary>
        private readonly StandardLevelScenesTransitionSetupDataSO _transitionSetupData;
        /// <summary>
        /// コンストラクタで注入されるリザルト画面コントローラー。
        /// </summary>
        private readonly ResultsViewController _resultsViewController;
        /// <summary>
        /// 最後に保存されたレベル完了結果。
        /// </summary>
        // private LevelCompletionResults _lastResults;
        /// <summary>
        /// コンストラクタ。
        /// Zenject によって依存関係が注入されます。
        /// </summary>
        /// <param name="transitionSetupData"></param>
        /// <param name="resultsViewController"></param>
        public ResultObserver(
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
            // シーン遷移完了時のイベントに登録
            // _transitionSetupData.didFinishEvent += OnLevelFinished;
            //　リザルト画面がアクティブになった時
            // _resultsViewController.didActivateEvent += OnResultsActivated;
            // リザルト画面が非アクティブになった時
            // _resultsViewController.didDeactivateEvent += OnResultsDeactivated;
            // 「OK/Continue」ボタンが押された時
            _resultsViewController.continueButtonPressedEvent += OnBackToDetail;
            // 「Restart」ボタンが押された時
            _resultsViewController.restartButtonPressedEvent += OnRestartPressed;
        }



        /// <summary>
        /// 破棄処理。
        /// </summary>
        public void Dispose()
        {
            if (_resultsViewController != null)
            {
                // _transitionSetupData.didFinishEvent -= OnLevelFinished;
                // _resultsViewController.didActivateEvent -= OnResultsActivated;
                // _resultsViewController.didDeactivateEvent -= OnResultsDeactivated;
                _resultsViewController.continueButtonPressedEvent -= OnBackToDetail;
                _resultsViewController.restartButtonPressedEvent -= OnRestartPressed;
            }
        }
        // シーン遷移のイベントで結果を保存
        // private async void OnLevelFinished(
        //     StandardLevelScenesTransitionSetupDataSO data, LevelCompletionResults results)
        // {
        //     // リザルト画面に遷移するときの処理
        //     if (Plugin.VerboseLogs) Plugin.Log.Info("Level finished event triggered.");

        //     _lastResults = results;
        // }

        /// <summary>
        /// リザルト画面がアクティブになった時の処理。
        /// </summary>
        /// <param name="firstActivation"></param>
        /// <param name="addedToHierarchy"></param>
        /// <param name="screenSystemEnabling"></param>
        // private async void OnResultsActivated(
        //     bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        // {
        //     // リザルト画面が表示された時の処理
        //     if (Plugin.VerboseLogs) Plugin.Log.Info("Results screen activated: {results.modifiedScore}");
        //     if (_lastResults != null)
        //     {
        //         if (Plugin.VerboseLogs) Plugin.Log.Info($"Result score: {_lastResults.modifiedScore}");
        //         await MemoService.Instance.HandleLevelCompletion(_transitionSetupData, _lastResults);
        //     }
        //     else
        //     {
        //         if (Plugin.VerboseLogs) Plugin.Log.Info("Results data is null.");
        //     }
        //     _lastResults = null;
        // }

        /// <summary>
        /// リザルト画面が非アクティブになった時の処理。
        /// </summary>
        /// <param name="removedFromHierarchy"></param>
        /// <param name="screenSystemDisabling"></param>
        // private void OnResultsDeactivated(
        //     bool removedFromHierarchy, bool screenSystemDisabling)
        // {
        //     // リザルト画面が非表示になった時の処理
        //     if (Plugin.VerboseLogs) Plugin.Log.Info("Results screen deactivated.");

        //     _isResultsActive = false;
        // }
        /// <summary>
        /// リザルト画面からマップ詳細画面に戻る直前の処理。
        /// </summary>
        /// <param name="controller"></param>
        private async void OnBackToDetail(ResultsViewController controller)
        {
            // ここでマップ詳細画面に戻る直前の処理を行う
            if (Plugin.VerboseLogs) Plugin.Log.Info("Returning to menu from results screen.");
            // FieldAccessorを使ってprivateな変数 _levelCompletionResults を取得
            var results = FieldAccessor<ResultsViewController, LevelCompletionResults>.Get(
                ref controller, "_levelCompletionResults");

            await MemoService.Instance.HandleResultTransition(_transitionSetupData, results);

        }
        /// <summary>
        /// リザルト画面から再挑戦ボタンが押された時の処理。
        /// </summary>
        private async void OnRestartPressed(ResultsViewController controller)
        {
            // 再挑戦が選ばれた場合の処理
            if (Plugin.VerboseLogs) Plugin.Log.Info("Restart button pressed on results screen.");
            var results = FieldAccessor<ResultsViewController, LevelCompletionResults>.Get(
                            ref controller, "_levelCompletionResults");

            await MemoService.Instance.HandleResultTransition(_transitionSetupData, results);
        }
    }
}