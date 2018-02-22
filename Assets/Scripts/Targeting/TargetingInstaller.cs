using Futulabs.HoloFramework.Targeting;
using UnityEngine;
using Zenject;

public class TargetingInstaller : MonoInstaller<TargetingInstaller>
{
    [SerializeField]
    private CanvasCursor    _cursor;

    public override void InstallBindings()
    {
        // Bind the cursor, targeter, and targeting manager, in that order
        Container.Bind<ICursor>().FromInstance(_cursor);
        Container.Bind<ITargeter>().To<GazeTargeter>().AsSingle();
        Container.Bind(typeof(ITargetingManager), typeof(ITickable)).To<TargetingManager>().AsSingle().NonLazy();
    }
}