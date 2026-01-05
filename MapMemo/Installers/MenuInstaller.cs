using MapMemo.Domain;
using MapMemo.Events;
using MapMemo.UI.Common;
using MapMemo.UI.Settings;
using UnityEngine;
using Zenject;

namespace MapMemo.Installers
{
    /// <summary>
    /// Zenject 用のインストーラ。Menu コンテキストに必要な MonoBehaviour を配置します。
    /// </summary>
    public class MenuInstaller : Installer
    {
        /// <summary>
        /// Zenject コンテナへのバインディングを登録します。
        /// </summary>
        public override void InstallBindings()
        {
            Plugin.Log?.Info("MenuInstaller InstallBindings");

            var go = new GameObject("MapMemoSettingsViewInstaller");
            go.SetActive(true);
            Object.DontDestroyOnLoad(go); // シーン遷移で消えないようにする

            // マネージャコンポーネントのバインディング
            go.AddComponent<InputHistoryManager>();
            go.AddComponent<DictionaryManager>();
            go.AddComponent<InputKeyManager>();
            go.AddComponent<MemoSettingsManager>();
            go.AddComponent<BeatSaverManager>();

            ///Container.InstantiateComponent<ResultManager>(go);
            // Container.BindInterfacesAndSelfTo<ResultListener>().AsSingle();
            Container.BindInterfacesAndSelfTo<BackToDetailObserver>().AsSingle();
            // UI ヘルパーコンポーネントのバインディング
            go.AddComponent<UIHelper>();


            // 設定画面コントローラーのバインディング
            var settingsController =
                Container.InstantiateComponent<MapMemoSettingsController>(go);
            Container.BindInterfacesAndSelfTo<MapMemoSettingsController>()
                .FromInstance(settingsController)
                .AsSingle();
        }
    }

}