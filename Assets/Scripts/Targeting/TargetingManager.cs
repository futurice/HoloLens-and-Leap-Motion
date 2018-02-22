using UnityEngine;
using Zenject;

namespace Futulabs.HoloFramework.Targeting
{
    public class TargetingManager : ITargetingManager, ITickable
    {
        private ITargeter       _targeter               = null;

        private TargetingResult _latestResult;
        private GameObject      _focusedObject          = null;
        private IInteractable   _focusedInteractable    = null;

        private ICursor         _cursor                 = null;

        private bool            _updating               = true;

        /// <summary>
        /// Turns on and off cursor, depending on if we are updating the targeting.
        /// </summary>
        public bool Updating
        {
            set
            {
                if (_updating != value)
                {
                    _updating = value;

                    if (_updating)
                    {
                        // Turn on cursor
                        _cursor.Show();
                        // Immediatly update the targeting
                        UpdateAndHandleTargeting();
                    }
                    else
                    {
                        // Hide the cursor
                        _cursor.Hide();
                    }
                }
            }
        }

        public TargetingManager(
            [Inject] ITargeter  targeter,
            [Inject] ICursor    cursor)
        {
            _targeter = targeter;
            _cursor = cursor;
            _updating = true;
        }

        public void Tick()
        {
            if (_updating)
            {
                UpdateAndHandleTargeting();
            }
        }

        private void UpdateAndHandleTargeting()
        {
            // Have the targeter check for a target
            _latestResult = _targeter.AcquireTarget();

            if (_latestResult.TargetedObject != null)
            {
                if (_focusedObject == null || _focusedObject != _latestResult.TargetedObject)
                {
                    HandleNewTargetHit();
                }
                else
                {
                    HandleRepeatHit();
                }
            }
            else
            {
                HandleMiss();
            }
        }

        private void HandleNewTargetHit()
        {
            // If another interactable was previously in focus, have it lose focus
            if (_focusedInteractable != null) _focusedInteractable.LoseFocus();

            // Set the hit object as the currently focused object
            _focusedObject = _latestResult.TargetedObject;
            // Check if the object is interactable
            _focusedInteractable = _focusedObject.GetComponent(typeof(IInteractable)) as IInteractable;
            // If the new object is interactable, have it gain focus
            if (_focusedInteractable != null) _focusedInteractable.GainFocus();

            // Update cursor
            _cursor.HandleTarget(_latestResult, _focusedInteractable);
        }

        private void HandleRepeatHit()
        {
            // Just update the cursor
            _cursor.HandleTarget(_latestResult, _focusedInteractable);
        }

        private void HandleMiss()
        {
            // If we previously had an interactable object in focus, have it lose focus
            if (_focusedInteractable != null) _focusedInteractable.LoseFocus();

            // Make sure focused object and interactable are set to null
            _focusedObject = null;
            _focusedInteractable = null;

            // Update cursor to handle the miss
            _cursor.HandleMiss();
        }
    }
}
