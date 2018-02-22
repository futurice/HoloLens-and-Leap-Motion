using Futulabs.HoloFramework.LeapMotion;
using UniRx;
using UnityEngine;
using Zenject;

public class LeapMotionInstaller : MonoInstaller<LeapMotionInstaller>
{
    [SerializeField]
    [Tooltip("The port the TCP client should use")]
    private int         TCPPort                     = 6000;
    [SerializeField]
    [Tooltip("The port the UDP client should use")]
    private int         UDPPort                     = 6001;

    [SerializeField]
    private GameObject  _handAlignmentCanvas;

    private int         ImageAmountForCalibration   = 3;

    [SerializeField]
    private GameObject  FingerTipPrefab;


    public override void InstallBindings()
    {
        // The different data streams
        Container.BindInstance(new Subject<Matrix4x4>()).WithId("Leap to locatable camera");
        Container.BindInstance(new Subject<LeapFrameData>()).WithId("Frame stream");
        Container.BindInstance(new Subject<LeapFrameData>()).WithId("Transformed frame stream");

        // Frame transformer
        Container.Bind(typeof(ILeapFrameTransformer)).To<LeapFrameTransformer>().AsSingle().NonLazy();

        // Locatable camera controller
        Container.Bind(typeof(ILocatableCameraController)).To<LocatableCameraController>().AsSingle();

        // Connection/calibration manager
        Container.BindInstance(_handAlignmentCanvas).WithId("Hand alignment canvas");
        Container.BindInstance(new ReactiveProperty<bool>(false)).WithId("Calibration status");
        Container.BindInstance(TCPPort).WithId("TCP Port");
        Container.BindInstance(UDPPort).WithId("UDP Port");
        Container.BindInstance(ImageAmountForCalibration).WithId("Calibration image amount");
        Container.Bind<ILeapConnectionManager>().To<LeapConnectionManager>().AsSingle();

        // Hand data visualizers
        Container.BindInstance(FingerTipPrefab).WithId("Fingertip prefab");
        Container.BindInstance(Camera.main.transform).WithId("Fingertip parent");
        Container.Bind(typeof(ILeapHandDataVisualizer)).To<LeapHandDataVisualizer>().AsSingle();
    }
}