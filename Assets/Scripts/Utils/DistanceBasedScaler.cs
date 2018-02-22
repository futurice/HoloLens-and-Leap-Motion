using UniRx;
using UnityEngine;
using Zenject;

namespace Futulabs.HoloFramework.Utils
{
    /// <summary>
    /// Component that scales the gameobject based on distance from camera,
    /// so that the perceived size of the object stays the same.
    /// </summary>
    public class DistanceBasedScaler : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The scale of the object when it is 1 unit from the camera")]
        private float   _scalePerUnit = 1.0f;

        private Vector3 _headPosition;

        [Inject]
        private void Initialize([Inject(Id = "Head position")]  ReactiveProperty<Vector3>   headPosition)
        {
            headPosition.Subscribe(newPos => _headPosition = newPos);
        }

        private void LateUpdate()
        {
            transform.localScale = Vector3.one * _scalePerUnit * (_headPosition - transform.position).magnitude;
        }
    }
}

