using UniRx;
using UnityEngine;
using UnityEngine.XR.WSA;
using Zenject;

namespace Futulabs.HoloFramework
{
    public class StabilizationPlaneManager : IStabilizationPlaneManager, ITickable
    {
        private IRaycaster  _raycaster;
        private Vector3     _headPosition;
        private Vector3     _viewDirection;
        private float       _defaultDistance;

        public StabilizationPlaneManager
            (
            [Inject]                                        IRaycaster                  raycaster,
            [Inject(Id = "Head position")]                  ReactiveProperty<Vector3>   headPosition,
            [Inject(Id = "View direction")]                 ReactiveProperty<Vector3>   viewDirection,
            [Inject(Id = "Stab plane default distance")]    float                       defaultDistance
            )
        {
            _raycaster = raycaster;
            headPosition.Subscribe(newPos => _headPosition = newPos);
            viewDirection.Subscribe(newDir => _viewDirection = newDir);
            _defaultDistance = defaultDistance;
        }

        public void Tick()
        {
            RaycastResult result = _raycaster.CastRay(_headPosition, _viewDirection);

            // If we hit something, place the focus point at the hit.
            // If not, place it the default distance straight in front of the user's head
            Vector3 fPoint = result.WasHit ? result.HitPosition : _headPosition + _defaultDistance * _viewDirection;
            // Always have the normal pointing the opposite direction of the view vector
            Vector3 normal = -_viewDirection;

            HolographicSettings.SetFocusPointForFrame(fPoint, normal);
        }
    }
}
