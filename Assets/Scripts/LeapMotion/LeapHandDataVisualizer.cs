using Futulabs.HoloFramework.LeapMotion;
using System;
using UniRx;
using UnityEngine;
using Zenject;

public class LeapHandDataVisualizer : ILeapHandDataVisualizer
{
    private enum Side { LEFT, RIGHT};

    private GameObject[,]                       _fingertips = new GameObject[2, 5];

    private bool[]                              _isVisible  = { false, false };

    private System.IObservable<LeapFrameData>   _mainThreadFrameStream;
    private FrameStreamObserver                 _frameStreamObserver;
    private IDisposable                         _frameDataSub;

    public LeapHandDataVisualizer(
        [Inject(Id = "Fingertip prefab")]           GameObject                      fingerTipPrefab,
        [Inject(Id = "Fingertip parent")]           Transform                       fingertipParent,
        [Inject(Id = "Transformed frame stream")]   UniRx.Subject<LeapFrameData>    transformedFrameStream)
    {
        for (int i = 0; i < 2; ++i)
        {
            for (int j = 0; j < 5; ++j)
            {
                _fingertips[i, j] = UnityEngine.Object.Instantiate(fingerTipPrefab);
                _fingertips[i, j].transform.SetParent(fingertipParent);
                _fingertips[i, j].SetActive(false);
            }
        }

        _mainThreadFrameStream = transformedFrameStream.ObserveOn(Scheduler.MainThread).SubscribeOn(Scheduler.MainThread) as System.IObservable<LeapFrameData>;
        _frameStreamObserver = new FrameStreamObserver(this);
    }

    public void ShowHands()
    {
        _frameDataSub = _mainThreadFrameStream.Subscribe(_frameStreamObserver);
    }

    public void HideHands()
    {
        _frameDataSub.Dispose();
        for (int i = 0; i < 2; ++i)
        {
            for (int j = 0; j < 5; ++j)
            {
                _fingertips[i, j].SetActive(false);
            }
        }
    }

    private void UpdateArm(LeapArmData data, Side side)
    {
        // Check if the arm's visibility has changed
        bool isVisible = data != null;
        if (isVisible != _isVisible[(int)side])
        {
            SwitchHandVisibility(side);
        }
        _isVisible[(int)side] = isVisible;

        // If the arm is visible then update the position of the visualizations
        if (isVisible)
        {
            SetFingerPosition(side, FingerType.THUMB,   data.hand.Thumb.TipPosition);
            SetFingerPosition(side, FingerType.INDEX,   data.hand.IndexFinger.TipPosition);
            SetFingerPosition(side, FingerType.MIDDLE,  data.hand.MiddleFinger.TipPosition);
            SetFingerPosition(side, FingerType.RING,    data.hand.RingFinger.TipPosition);
            SetFingerPosition(side, FingerType.PINKY,   data.hand.Pinky.TipPosition);
        }
    }

    private void SwitchHandVisibility(Side side)
    {
        for (int i = 0; i < 5; ++i)
        {
            _fingertips[(int)side, i].SetActive(!_fingertips[(int)side, i].activeSelf);
        }
    }

    private void SetFingerPosition(Side side, FingerType finger, Vector3 leapFingerPos)
    {
        _fingertips[(int)side, (int)finger].transform.localPosition = leapFingerPos;
    }

    private class FrameStreamObserver : System.IObserver<LeapFrameData>
    {
        private LeapHandDataVisualizer _visualiser;

        public FrameStreamObserver(LeapHandDataVisualizer visualiser)
        {
            _visualiser = visualiser;
        }
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(LeapFrameData frame)
        {
            if (frame != null)
            {
                _visualiser.UpdateArm(frame.left_arm, Side.LEFT);
                _visualiser.UpdateArm(frame.right_arm, Side.RIGHT);
            }
        }
    }
}
