using UniRx;
using UnityEngine;
using Zenject;

namespace Futulabs.HoloFramework.Utils
{
    /// <summary>
    /// Class for keeping an object pointed towards the user's head
    /// </summary>
    public class ViewpointAligner : MonoBehaviour
    {
        private Vector3 _headPosition;

        [Inject]
        public void Initialize([Inject(Id = "Head position")]   ReactiveProperty<Vector3>   headPosition)
        {
            headPosition.Subscribe(newPos => _headPosition = newPos);
        }

        private void Update()
        {
            transform.rotation = Quaternion.LookRotation((transform.position - _headPosition).normalized, Vector3.up);
        }
    }
}
