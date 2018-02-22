using UnityEngine;
using Zenject;

public class ProgramManagerInstaller : MonoInstaller<ProgramManagerInstaller>
{
    public override void InstallBindings()
    {
        Container.Bind<ProgramManager>().AsSingle().NonLazy();
    }
}