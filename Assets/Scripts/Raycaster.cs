using UnityEngine;
using System;
using Zenject;

namespace Futulabs.HoloFramework
{
    /// <summary>
    /// Class for doing raycasting into the scene. Use this since it takes into
    /// account the special arrangements in place for the UI.
    /// </summary>
    public class Raycaster : IRaycaster
    {
        private Settings _settings;

        public Raycaster([Inject] Settings settings)
        {
            _settings = settings;
        }

        public RaycastResult CastRay(Vector3 origin, Vector3 direction)
        {
            RaycastHit hitInfo;
            bool wasHit = Physics.Raycast(origin, direction, out hitInfo);
            // Check if we hit something on the UI layer. This is because the colliders of the UI components
            // are of almost equal thickness and we can't be sure there isn't actually an interactable component
            // where we hit. Therefore we do a new raycast, but only against the interactable UI layer. If this
            // hits, then we simply switch out the hit info to that hit's info
            if (wasHit && hitInfo.collider.gameObject.layer == _settings.UILayer)
            {
                RaycastHit secondaryHitInfo;
                int layerMask = 1 << _settings.InteractableUILayer;
                bool secondaryWasHit = Physics.Raycast(origin, direction, out secondaryHitInfo, Mathf.Infinity, layerMask);
                if (secondaryWasHit)
                {
                    hitInfo = secondaryHitInfo;
                }
            }
            GameObject hitObject = wasHit ? hitInfo.collider.gameObject : null;
            Vector3 hitPosition = wasHit ? hitInfo.point : Vector3.zero;
            Vector3 hitNormal = wasHit ? hitInfo.normal : Vector3.up;

            return new RaycastResult(wasHit, hitObject, hitPosition, hitNormal);
        }

        [Serializable]
        public class Settings
        {
            public int UILayer;
            public int InteractableUILayer;
        }
    }
}
