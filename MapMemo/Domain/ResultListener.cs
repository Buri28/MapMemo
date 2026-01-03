/// <summary>
/// 結果リスナーインターフェース。
/// </summary>
using Zenject;
using System;
using System.Reflection;
using System.Linq;

namespace MapMemo.Domain
{
    public class ResultListener : IInitializable, IDisposable
    {
        private readonly StandardLevelScenesTransitionSetupDataSO _sceneTransition;

        // Zenjectが自動的にこの引数にインスタンスを渡してくれます
        [Inject]
        public ResultListener(StandardLevelScenesTransitionSetupDataSO sceneTransition)
        {
            Plugin.Log.Info("ResultListener Constructed");
            _sceneTransition = sceneTransition;
        }

        public void Initialize()
        {
            Plugin.Log.Info("ResultListener Initialized");
            // イベント購読開始
            _sceneTransition.didFinishEvent += OnLevelFinished;

            // TODO テスト
            // BeatLeaderアセンブリから internal クラスを取得
            Type type = Type.GetType("BeatLeader.Manager.LeaderboardEvents, BeatLeader");
            if (type == null) return;
            Plugin.Log.Info("ResultListener: Found BeatLeader LeaderboardEvents type");

            // イベント情報を取得
            EventInfo eventInfo = type.GetEvent("ScoreStatsRequestedEvent", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (eventInfo != null)
            {
                Plugin.Log.Info("ResultListener: Subscribing to BeatLeader ScoreStatsRequestedEvent");
                // 自分のメソッドを動的にデリゲートとして登録
                //MethodInfo methodInfo = this.GetType().GetMethod(nameof(OnScoreStatsRequested), BindingFlags.Instance | BindingFlags.NonPublic);
                //Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, methodInfo);var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;var handlerType = eventInfo.EventHandlerType;
                var handlerType = eventInfo.EventHandlerType;
                var invoke = handlerType.GetMethod("Invoke");
                Plugin.Log.Info($"ResultListener: Event handler signature: {string.Join(", ", invoke.GetParameters().Select(p => p.ParameterType.Name))}");
                try
                {
                    var methodInfo = this.GetType().GetMethod(nameof(OnScoreStatsRequested), BindingFlags.Instance | BindingFlags.NonPublic);
                    var handler = Delegate.CreateDelegate(handlerType, this, methodInfo); // ここで例外が出る可能性
                    eventInfo.AddEventHandler(null, handler);
                    Plugin.Log.Info("ResultListener: Subscribed successfully");
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"ResultListener: Failed subscribe: {ex}");
                }
            }
        }

        private void OnScoreStatsRequestedAdapter(object sender, int someId) => OnScoreStatsRequested(someId);

        private void OnScoreStatsRequested(int someId)
        {
            Plugin.Log.Info($"ResultListener Event Fired! ID: {someId}");
        }

        public void Dispose()
        {
            Plugin.Log.Info("ResultListener Disposed");
            // メモリリーク防止のため解除
            _sceneTransition.didFinishEvent -= OnLevelFinished;
        }

        private void OnLevelFinished(
            StandardLevelScenesTransitionSetupDataSO data,
            LevelCompletionResults results)
        {
            Plugin.Log.Info($"ResultListener: Level Finished with Score: {results.modifiedScore}, Rank: {results.rank}");
            // ここでリザルトを処理


        }
    }
}