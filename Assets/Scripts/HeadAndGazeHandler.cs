using UnityEngine;
using Zenject;
using UniRx;
using System;

namespace Futulabs.HoloFramework
{
    public class HeadAndGazeHandler : IHeadTracker, IGazeTracker, ITickable
    {
        private Camera                      _hololens;
        private ReactiveProperty<Vector3>   _headPosition;
        private ReactiveProperty<Vector3>   _viewDirection;

        private Settings                    _settings;

        public Vector3 HeadPosition
        {
            get
            {
                return _headPosition.Value;
            }
        }

        public Vector3 ViewDirection
        {
            get
            {
                return _viewDirection.Value;
            }
        }

    public HeadAndGazeHandler
            (
            [Inject]                            Camera                      hololens,
            [Inject(Id = "Head position")]      ReactiveProperty<Vector3>   headPosition,
            [Inject(Id = "View direction")]     ReactiveProperty<Vector3>   viewDirection,
            [Inject]                            Settings                    settings
            )
        {
            _hololens = hololens;
            _headPosition = headPosition;
            _viewDirection = viewDirection;
            _settings = settings;
        }

        public void Tick()
        {
            _headPosition.Value = _hololens.transform.position;
            
            if (_settings.LimitViewDirChangeRate)
            {
                float maxRadDelta = (_settings.ViewDirChangeRate * Time.deltaTime) * Mathf.Deg2Rad;
                _viewDirection.Value = Vector3.RotateTowards(_viewDirection.Value, _hololens.transform.forward, maxRadDelta, 10.0f).normalized;
            }
            else
            {
                _viewDirection.Value = _hololens.transform.forward;
            }
        }

        [Serializable]
        public class Settings
        {
            public bool     LimitViewDirChangeRate;
            public float    ViewDirChangeRate;
        }
    }
}
