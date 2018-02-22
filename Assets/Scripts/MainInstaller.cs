using Futulabs.HoloFramework;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class MainInstaller : MonoInstaller<MainInstaller>
{
    [SerializeField]
    [Tooltip("General purpose text field for displaying instructions to user")]
    private Text        InfoTextField;
    [SerializeField]
    [Tooltip("Root object for the text field, so it can be turned on and off")]
    private GameObject  InfoTextObject;

    [SerializeField]
    private float       _stabPlaneDefaultDistance;

    public override void InstallBindings()
    {
        // General purpose field for displaying info
        Container.BindInstance(InfoTextField).WithId("Info text field");
        Container.BindInstance(InfoTextObject).WithId("Info text root object");

        // Bind the main camera (the Hololens) as the default for anyone that needs a camera
        Container.Bind<Camera>().FromInstance(Camera.main);
        // Reactive properties for tracking the head's current position and view direction
        Container.BindInstance(new ReactiveProperty<Vector3>(Vector3.zero)).WithId("Head position");
        Container.BindInstance(new ReactiveProperty<Vector3>(Vector3.forward)).WithId("View direction");
        // Bind the head and gaze handler, which updates the reactive properties
        Container.Bind(typeof(IHeadTracker), typeof(IGazeTracker), typeof(ITickable)).To<HeadAndGazeHandler>().AsSingle().NonLazy();

        // Bind the raycaster implementation
        Container.Bind<IRaycaster>().To<Raycaster>().AsSingle().NonLazy();

        // Bind the stabilization plane manager
        Container.BindInstance(_stabPlaneDefaultDistance).WithId("Stab plane default distance");
        Container.Bind<IStabilizationPlaneManager>().To<StabilizationPlaneManager>().AsSingle().NonLazy();
    }
}