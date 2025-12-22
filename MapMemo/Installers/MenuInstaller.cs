using MapMemo.Services;
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

            go.AddComponent<InputHistoryManager>();
            go.AddComponent<DictionaryManager>();
            go.AddComponent<InputKeyManager>();
            go.AddComponent<MemoSettingsManager>();
            go.AddComponent<UIHelper>();
            var controller = go.AddComponent<MapMemoSettingsController>();

            Container.BindInterfacesAndSelfTo<MapMemoSettingsController>()
                .FromInstance(controller)
                .AsSingle();

            Container.Bind<MemoEditModalService>().AsSingle();
        }
    }

}