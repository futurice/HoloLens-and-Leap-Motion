using UnityEngine;

namespace Futulabs.HoloFramework.Utils
{
    /// <summary>
    /// A class for fitting a box collider around a RectTransform and keeping it the right size.
    /// This is mainly meant for UI components, since XR doesn't really like the graphic raycaster
    /// </summary>
    public class UIBoxColliderFitter : MonoBehaviour
    {
        [SerializeField]
        private RectTransform   _rectToFitTo;
        private BoxCollider     _collider;

        private void Start()
        {
            _collider = GetComponent<BoxCollider>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }
            _collider.isTrigger = true;
        }

        private void Update()
        {
            if (_collider.size.x != _rectToFitTo.rect.width || _collider.size.y != _rectToFitTo.rect.height)
            {
                _collider.size = new Vector3(_rectToFitTo.rect.width, _rectToFitTo.rect.height, 1.0f);
            }
        }
    }
}
