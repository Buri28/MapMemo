using MapMemo.Core;
using MapMemo.UI;
using MapMemo.UI.Edit;
using MapMemo.UI.Settings;
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

            go.AddComponent<InputHistoryManager>();
            var controller = go.AddComponent<MapMemoSettingsViewController>();

            Container.BindInterfacesAndSelfTo<MapMemoSettingsViewController>()
                .FromInstance(controller)
                .AsSingle();
        }
    }

}