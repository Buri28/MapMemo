using MapMemo.UI;
using UnityEngine;
using Zenject;

namespace MapMemo.Installers
{
    public class MenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Plugin.Log?.Info("MenuInstaller InstallBindings");
            // Container.BindInterfacesAndSelfTo<MemoPanelController>().AsSingle();
            var go = new GameObject("MapMemoSettingsViewInstaller");
            go.SetActive(true);
            Object.DontDestroyOnLoad(go); // シーン遷移で消えないようにする

            // TODO: Replace 'MapMemoSettingsViewController' with the correct component if needed
            // var controller = go.AddComponent<MapMemoSettingsViewInstaller>();

            // Container.BindInterfacesAndSelfTo<MapMemoSettingsViewInstaller>()
            //     .FromInstance(controller)
            //     .AsSingle();

            // var gameObject = new GameObject("MapMemoSettingsViewController");
            // gameObject.SetActive(true);
            // Container.Bind<MapMemoSettingsViewController>()
            //     .FromInstance(gameObject.AddComponent<MapMemoSettingsViewController>())
            //     .AsSingle();

            var controller = go.AddComponent<MapMemoSettingsViewController>();

            Container.BindInterfacesAndSelfTo<MapMemoSettingsViewController>()
                .FromInstance(controller)
                .AsSingle();
        }
    }

}