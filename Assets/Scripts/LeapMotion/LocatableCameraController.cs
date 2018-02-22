using Futulabs.HoloFramework.LeapMotion;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.XR.WSA.WebCam;
using UnityEngine.Windows.Speech;

public class LocatableCameraController : ILocatableCameraController
{
    private enum State
    {
        NOT_IN_USE,
        STARTING_PHOTO_MODE,
        ERROR_STARTING_PHOTO_MODE,
        READY_TO_TAKE_PHOTO,
        TAKING_PHOTO,
        STOPPING_PHOTO_MODE
    };

    private PhotoCapture            _photoCaptureObject;
    private CameraParameters        _cameraParameters;
    private KeywordRecognizer       _keywordRecognizer;
    private string[]                _keywords = { "Take picture" };

    private ReactiveProperty<State> _currentState;
    private ICameraImageRequester   _currentRequester;

    public LocatableCameraController()
    {
        _keywordRecognizer = new KeywordRecognizer(_keywords);
        _keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        _keywordRecognizer.Start();

        _currentState = new ReactiveProperty<State>(State.NOT_IN_USE);
        _currentState.ObserveOn(Scheduler.MainThread).SubscribeOn(Scheduler.MainThread).Subscribe(state =>
        {
            if (state == State.NOT_IN_USE)
            {
                _currentRequester = null;
            }
            else if (state == State.STARTING_PHOTO_MODE)
            {
                _photoCaptureObject.StartPhotoModeAsync(_cameraParameters, OnPhotoModeStarted);
            }
            else if (state == State.TAKING_PHOTO)
            {
                _photoCaptureObject.TakePhotoAsync(OnPhotoCapturedToMemory);
            }
            else if (state == State.STOPPING_PHOTO_MODE)
            {
                _photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
            }
        });

#if !UNITY_EDITOR
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
#endif
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (_currentState.Value == State.READY_TO_TAKE_PHOTO)
        {
            _currentState.Value = State.TAKING_PHOTO;
        }
    }

    public bool RequestUsage(ICameraImageRequester requester)
    {
        if (_currentState.Value == State.NOT_IN_USE && _currentRequester == null)
        {
            _currentRequester = requester;
            _currentState.Value = State.STARTING_PHOTO_MODE;
            return true;
        }
        else if (_currentState.Value == State.ERROR_STARTING_PHOTO_MODE && requester == _currentRequester)
        {
            _currentState.Value = State.STARTING_PHOTO_MODE;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void ReleaseUsage(ICameraImageRequester requester)
    {
        if (requester == _currentRequester)
        {
            _currentState.Value = State.STOPPING_PHOTO_MODE;
        }
    }

#region Photo callbacks

    private void OnPhotoCaptureCreated(PhotoCapture photoCaptureObject)
    {
        _photoCaptureObject = photoCaptureObject;

        // Get highest available resolution
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending(res => res.width * res.height).First();

        // Setup camera parameters
        _cameraParameters = new CameraParameters();
        _cameraParameters.hologramOpacity = 0.0f;
        _cameraParameters.cameraResolutionWidth = cameraResolution.width;
        _cameraParameters.cameraResolutionHeight = cameraResolution.height;
        _cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            _currentState.Value = State.READY_TO_TAKE_PHOTO;
        }
        else
        {
            Debug.LogError("Failed to start photo mode");
            _currentState.Value = State.ERROR_STARTING_PHOTO_MODE;
        }
    }

    private void OnPhotoCapturedToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame frame)
    {
        if (result.success)
        {
            Debug.Log("Captured image to memory. Sending data to requester.");
            Matrix4x4 calibrationMatrix;
            frame.TryGetProjectionMatrix(out calibrationMatrix);
            List<byte> imageData = new List<byte>();
            frame.CopyRawImageDataIntoBuffer(imageData);
            _currentState.Value = State.READY_TO_TAKE_PHOTO;
            _currentRequester.ReceiveTakenPictureAsBytes(imageData, _cameraParameters.cameraResolutionWidth, _cameraParameters.cameraResolutionHeight, calibrationMatrix);
        }
        else
        {
            Debug.LogError("Failed to capture image to memory.");
            _currentState.Value = State.READY_TO_TAKE_PHOTO;
        }
    }

    private void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            _currentRequester = null;
            _currentState.Value = State.NOT_IN_USE;
        }
        else
        {
            Debug.LogError("Unexpected error when stopping photo mode");
            _currentRequester = null;
            _currentState.Value = State.NOT_IN_USE;
        }
    }

#endregion

}
