using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Futulabs.HoloFramework.Targeting
{
    /// <summary>
    /// Cursor that uses a colored image to show the where the user is looking
    /// </summary>
    public class CanvasCursor : MonoBehaviour, ICursor
    {
        [SerializeField]
        private Image       _cursorImage;

        private Settings    _settings;

        private Vector3     _headPosition;
        private Vector3     _viewDirection;

        [Inject]
        public void Initialize(
            [Inject]                        Settings settings,
            [Inject(Id = "Head position")]  ReactiveProperty<Vector3> headPosition,
            [Inject(Id = "View direction")] ReactiveProperty<Vector3> viewDirection)
        {
            _settings = settings;

            headPosition.Subscribe(newPos => _headPosition = newPos);
            viewDirection.Subscribe(newDir => _viewDirection = newDir);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void HandleTarget(TargetingResult targetingResult, IInteractable interactableObject)
        {
            // Place cursor at targeted point, offset slightly along the normal
            transform.position = targetingResult.TargetedPointOnObject + _settings.SurfaceOffset * targetingResult.NormalAtTargetedPoint;
            // Rotate cursor to point along normal at the targeted point
            transform.localRotation = Quaternion.FromToRotation(Vector3.forward, targetingResult.NormalAtTargetedPoint);
            // Choose color based on if the targeted object is interactable or not
            _cursorImage.color = interactableObject != null ? _settings.InteractableColor : _settings.NonInteractableColor;
        }

        public void HandleMiss()
        {
            // Place the cursor the default distance in front of camera
            transform.position = _headPosition + _settings.DefaultDistance * _viewDirection;
            // Set the cursor to be aimed at the camera
            transform.localRotation = Quaternion.FromToRotation(Vector3.forward, -_viewDirection);
            _cursorImage.color = _settings.DefaultColor;
        }

        [Serializable]
        public class Settings
        {
            public float SurfaceOffset;
            public float DefaultDistance;

            public Color DefaultColor;
            public Color InteractableColor;
            public Color NonInteractableColor;
        }
    }
}
