using System;
using UnityEngine;

namespace Futulabs.HoloFramework.SpatialMapping
{
    /// <summary>
    /// Class for creating new spatial surfaces
    /// </summary>
    public class SpatialSurfaceFactory : ISpatialSurfaceFactory
    {
        public ISpatialSurface Create(
            DateTime        creationTime, 
            int             id, 
            bool            castShadows, 
            bool            recieveShadows, 
            Material        material, 
            bool            addCollider, 
            PhysicMaterial  physicMaterial)
        {
            GameObject instance = new GameObject(string.Format("Surface-{0}", id));
            return new SpatialSurface(instance, creationTime, id, castShadows, recieveShadows, material, addCollider, physicMaterial);
        }
    }
}
