using UnityEngine;

namespace Futulabs.HoloFramework.Targeting
{
    /// <summary>
    /// The result from acquiring a target
    /// </summary>
    public struct TargetingResult
    {
        /// <summary>
        /// The object considered targeted
        /// </summary>
        public GameObject   TargetedObject;
        /// <summary>
        /// The exact point on the object that is targeted
        /// </summary>
        public Vector3      TargetedPointOnObject;
        /// <summary>
        /// The normal at the targeted point
        /// </summary>
        public Vector3      NormalAtTargetedPoint;

        public TargetingResult(GameObject targetedObject, Vector3 targetedPointOnObject = new Vector3(), Vector3 normalAtTargetedPoint = new Vector3())
        {
            TargetedObject = targetedObject;
            TargetedPointOnObject = targetedPointOnObject;
            NormalAtTargetedPoint = normalAtTargetedPoint;
        }
    }

    /// <summary>
    /// Decides if/when target acquisition should be done and handles the targeting result.
    /// </summary>
    public interface ITargetingManager
    {
        /// <summary>
        /// Turn target updating on and off
        /// </summary>
        bool Updating { set; }
    }


    /// <summary>
    /// Determines what object is being targeted by the user.
    /// </summary>
    public interface ITargeter
    {
        /// <summary>
        /// Checks if any object should be considered targeted
        /// </summary>
        /// <returns>The result of the targeting</returns>
        TargetingResult AcquireTarget();
    }

    /// <summary>
    /// Represents the user's view in the scene.
    /// </summary>
    public interface ICursor
    {
        /// <summary>
        /// Hide the cursor from view
        /// </summary>
        void Hide();

        /// <summary>
        /// Make the cursor visible
        /// </summary>
        void Show();

        /// <summary>
        /// Handle a target being targeted
        /// </summary>
        /// <param name="targetingResult">The result of acquiring a target</param>
        /// <param name="interactableObject">The IInteractable component on the target. Is null if object isn't interactable</param>
        void HandleTarget(TargetingResult targetingResult, IInteractable interactableObject);

        /// <summary>
        /// Handle not having any object targeted
        /// </summary>
        void HandleMiss();
    }

    /// <summary>
    /// Object that can be interacted with.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// When an object is targeted it has focus. This is called when the object first gains focus
        /// </summary>
        void GainFocus();

        /// <summary>
        /// This is called when focus is moved away from this object, i.e. it is no longer being targeted
        /// </summary>
        void LoseFocus();
    }
}
