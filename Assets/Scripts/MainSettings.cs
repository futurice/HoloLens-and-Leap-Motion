using UnityEngine;
using Zenject;
using Futulabs.HoloFramework;
using Futulabs.HoloFramework.Targeting;
using Futulabs.HoloFramework.Utils;

[CreateAssetMenu(fileName = "MainSettings", menuName = "Installers/MainSettings")]
public class MainSettings : ScriptableObjectInstaller<MainSettings>
{
    public HeadAndGazeHandler.Settings  _headAndGazeSettings;
    public CanvasCursor.Settings        _canvasPointCursorSettings;
    public Raycaster.Settings           _raycasterSettings;
    public GazeFollower.Settings        _gazeFollowingSettings;

    public override void InstallBindings()
    {
        Container.BindInstance(_headAndGazeSettings);
        Container.BindInstance(_canvasPointCursorSettings);
        Container.BindInstance(_raycasterSettings);
        Container.BindInstance(_gazeFollowingSettings);
    }
}