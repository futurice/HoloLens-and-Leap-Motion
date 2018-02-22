using System;
using UniRx;
using UnityEngine;
using Zenject;

namespace Futulabs.HoloFramework.Utils
{
    /// <summary>
    /// This class tries to keep an object at a certain distance in front of the user's
    /// face, but also outside spatially mapped data
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GazeFollower : MonoBehaviour
    {
        private bool        _followGaze;

        private Vector3     _headPosition;
        private Vector3     _viewDirection;
        private Collider    _collider;

        private bool        _moving;
        private Vector3     _currentTarget;
        private Vector3     _currentVelocity;

        private Settings    _settings;

        [Inject]
        public void Initialize(
            [Inject(Id = "Head position")]  ReactiveProperty<Vector3>   headPosition,
            [Inject(Id = "View direction")] ReactiveProperty<Vector3>   viewDirection,
            [Inject]                        Settings                    settings)
        {
            _followGaze = true;

            headPosition.Subscribe(newPos => _headPosition = newPos);
            viewDirection.Subscribe(newDir => _viewDirection = newDir);
            _collider = GetComponent<Collider>();

            _currentTarget = transform.position;
            _currentVelocity = Vector3.zero;

            _settings = settings;
        }

        private void Update()
        {
            // Only update if we are following the user's gaze
            if (_followGaze)
            {
                // If collider is still null, try getting the collider
                if (_collider == null)
                {
                    _collider = GetComponent<Collider>();
                }

                // Only do something if we have a collider
                if (_collider != null)
                {
                    // Fire two rays; one to check if the gaze is outside collider's bounds and one to check against spatial mapping data
                    Ray ray = new Ray(_headPosition, _viewDirection);
                    RaycastHit boundsHitInfo;
                    RaycastHit spatialHitInfo;
                    int spatialLayerMask = 1 << _settings.SpatialMappingLayer;
                    bool withinBounds = _collider.Raycast(ray, out boundsHitInfo, Mathf.Infinity);
                    bool spatialDataHit = Physics.Raycast(ray, out spatialHitInfo, Mathf.Infinity, spatialLayerMask);
                    
                    if (!withinBounds)
                    {
                        // If the gaze doesn't hit the object, move the object's center towards the gaze
                        // If we hit spatial mapping that is closer than the default distance, use that hit point instead
                        _currentTarget = (spatialDataHit && ((spatialHitInfo.point - _headPosition).magnitude < _settings.PreferredDistance)) ?
                            spatialHitInfo.point :
                            _headPosition + _settings.PreferredDistance * _viewDirection;
                    }
                    else if (spatialDataHit && ((spatialHitInfo.point - _headPosition).magnitude < (transform.position - _headPosition).magnitude))
                    {
                        // If the mapped environment is closer than the object, move object towards the head to get it out of the mapped environment
                        float deltaDistance = (transform.position - _headPosition).magnitude - (spatialHitInfo.point - _headPosition).magnitude;
                        _currentTarget = transform.position - deltaDistance * transform.forward;
                    }

                    // Check if the object should be moving
                    _moving = (transform.position - _currentTarget).magnitude > _settings.Margin;

                    if (_moving)
                    {
                        transform.position = Vector3.SmoothDamp(transform.position, _currentTarget, ref _currentVelocity, _settings.MoveTime);
                    }
                    else
                    {
                        _currentVelocity = Vector3.zero;
                    }
                }
            }
        }

        [Serializable]
        public class Settings
        {
            [Header("Movement parameters")]
            public float    MaxSpeed;
            public float    MoveTime;
            public float    Margin;

            [Header("Positioning parameters")]
            public float    PreferredDistance;

            [Header("Raycasting parameters")]
            public int      SpatialMappingLayer;
        }
        
    }
}
