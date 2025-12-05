using Zenject;
using MapMemo.UI;

namespace MapMemo.Installers
{
    public class MenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MemoPanelController>().AsSingle();
        }
    }
}