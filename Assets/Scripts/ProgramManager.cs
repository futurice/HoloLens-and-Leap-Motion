using Futulabs.HoloFramework.LeapMotion;
using Futulabs.HoloFramework.Targeting;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using Zenject;

public class ProgramManager
{
    private enum States
    {
        PROGRAM_STARTED,
        STARTUP_DONE,
        CALIBRATION_DONE,
        READY_FOR_USE
    };

    private States                  _currentState;

    private KeywordRecognizer       _keywordRecognizer;
    private string[]                _keywords = { "Do startup", "Looks good" };

    private Text                    _infotext;
    private GameObject              _infoTextObject;

    private ITargetingManager       _targetingManager;

    private int                     _tcpPort;
    private int                     _udpPort;
    private ILeapConnectionManager  _connectionManager;
    private ReactiveProperty<bool>  _calibrationStatus;
    private ILeapHandDataVisualizer _handVisualizer;

    public ProgramManager(
        [Inject(Id = "Info text field")]        Text infoText,
        [Inject(Id = "Info text root object")]  GameObject infotextObject,
        [Inject]                                ITargetingManager targetingManager,
        [Inject(Id = "TCP Port")]               int tcpPort,
        [Inject(Id = "UDP Port")]               int udpPort,
        [Inject]                                ILeapConnectionManager connectionManager,
        [Inject(Id = "Calibration status")]     ReactiveProperty<bool> calibrationStatus,
        [Inject]                                ILeapHandDataVisualizer handVisualizer)
    {
        // Assign all variables
        _currentState = States.PROGRAM_STARTED;

        _keywordRecognizer = new KeywordRecognizer(_keywords);
        _keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        _keywordRecognizer.Start();

        _infotext = infoText;
        _infoTextObject = infotextObject;

        _targetingManager = targetingManager;

        _tcpPort = tcpPort;
        _udpPort = udpPort;
        _connectionManager = connectionManager;
        _calibrationStatus = calibrationStatus;
        _handVisualizer = handVisualizer;

        // Initialize the info text
        _infoTextObject.SetActive(true);
        _infotext.text = "Hello there! Say 'Do startup' to get going.";
        _infotext.SetAllDirty();

        // Start observing the calibration status
        // Make sure to observe and subscribe on main thread, since the text field can only be updated on the main thread
        _calibrationStatus.ObserveOn(Scheduler.MainThread).SubscribeOn(Scheduler.MainThread).Subscribe(calibDone =>
        {
            if (calibDone)
            {
                _currentState = States.CALIBRATION_DONE;
                // TODO: Add option to redo calibration instead.
                _infotext.text = "Calibration done. Check the results. Say 'Looks good' when done.";
                _handVisualizer.ShowHands();
            }
        });
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (_currentState == States.PROGRAM_STARTED)
        {
            if (args.text == "Do startup")
            {
                _targetingManager.Updating = false;
                _connectionManager.StartSockets(_tcpPort, _udpPort);
                _currentState = States.STARTUP_DONE;
            }
        }
        else if (_currentState == States.CALIBRATION_DONE)
        {
            if (args.text == "Looks good")
            {
                _targetingManager.Updating = true;
                _handVisualizer.HideHands();
                _infotext.text = "Ready for use.";
                _currentState = States.READY_FOR_USE;
            }
        }
    }
}
