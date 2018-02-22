using Futulabs.HoloFramework.SpatialMapping;
using UnityEngine;
using Zenject;

public class SpatialMappingInstaller : MonoInstaller<SpatialMappingInstaller>
{
    [SerializeField]
    private SpatialMapper _spatialMapper;

    public override void InstallBindings()
    {
        Container.Bind<ISpatialSurfaceFactory>().To<SpatialSurfaceFactory>().AsSingle();
        Container.BindInstance(_spatialMapper);
    }
}