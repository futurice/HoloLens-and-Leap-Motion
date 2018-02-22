using UniRx;
using UnityEngine;
using Zenject;


namespace Futulabs.HoloFramework.Targeting
{
    /// <summary>
    /// Standard targeter using the user's gaze to target things.
    /// </summary>
    public class GazeTargeter : ITargeter
    {
        private IRaycaster      _raycaster = null;

        private Vector3         _headPosition;
        private Vector3         _viewDirection;

        public GazeTargeter(
            [Inject]                        IRaycaster                  raycaster,
            [Inject(Id = "Head position")]  ReactiveProperty<Vector3>   headPosition,
            [Inject(Id = "View direction")] ReactiveProperty<Vector3>   viewDirection)
        {
            _raycaster = raycaster;

            // Simply update head position and gaze direction as new values come in
            headPosition.Subscribe(newPos => _headPosition = newPos);
            viewDirection.Subscribe(newDir => _viewDirection = newDir);
        }
        
        /// <summary>
        /// Fire a beam from the head's current position in the gaze's direction. If it hits anything, give the hit point
        /// and the normal at that point
        /// </summary>
        public TargetingResult AcquireTarget()
        {
            RaycastResult result = _raycaster.CastRay(_headPosition, _viewDirection);
            return result.WasHit ?
                new TargetingResult(result.HitObject, result.HitPosition, result.HitNormal) :
                new TargetingResult(null);
        }
    }
}
