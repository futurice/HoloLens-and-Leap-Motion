using UnityEngine;

namespace Futulabs.HoloFramework
{
    public struct RaycastResult
    {
        public bool WasHit;
        public GameObject HitObject;
        public Vector3 HitPosition;
        public Vector3 HitNormal;

        public RaycastResult(bool wasHit, GameObject hitObject, Vector3 hitPosition, Vector3 hitNormal = new Vector3())
        {
            WasHit = wasHit;
            HitObject = hitObject;
            HitPosition = hitPosition;
            HitNormal = hitNormal;
        }
    }

    /// <summary>
    /// Raycaster wrapper for custom raycasting behaviour.
    /// </summary>
    public interface IRaycaster
    {
        /// <summary>
        /// Do a raycast from a certain point in a specific direction
        /// </summary>
        /// <param name="origin">Start point for the ray</param>
        /// <param name="direction">The direction the raycast should be done in</param>
        /// <returns>Result of the raycasting</returns>
        RaycastResult CastRay(Vector3 origin, Vector3 direction);
    }

    /// <summary>
    /// Tracks the head's position.
    /// </summary>
    public interface IHeadTracker
    {
        /// <summary>
        /// Get the head's current position
        /// </summary>
        Vector3 HeadPosition { get; }
    }

    /// <summary>
    /// Tracks the view direction.
    /// </summary>
    public interface IGazeTracker
    {
        /// <summary>
        /// Get the current view direction
        /// </summary>
        Vector3 ViewDirection { get; }
    }

    /// <summary>
    /// Manages the stabilization plane.
    /// </summary>
    public interface IStabilizationPlaneManager
    {

    }
}
