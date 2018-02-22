using System;
using UnityEngine;
using UnityEngine.XR.WSA;

namespace Futulabs.HoloFramework.SpatialMapping
{
    /// <summary>
    /// Class representing a spatial surface
    /// </summary>
    public class SpatialSurface : ISpatialSurface
    {
        private GameObject          _instance;
        private int                 _id;
        private SurfaceMeshStatuses _meshStatus;
        private DateTime            _updateTime;
        private MeshFilter          _mesh;
        private MeshCollider        _collider;
        private WorldAnchor         _anchor;

        public SpatialSurface(
            GameObject instance, 
            DateTime creationTime, 
            int id, 
            bool castShadows, 
            bool recieveShadows, 
            Material material, 
            bool addCollider, 
            PhysicMaterial physicMaterial)
        {
            _instance = instance;
            _id = id;
            _meshStatus = SurfaceMeshStatuses.NONE;
            _updateTime = creationTime;
            _mesh = _instance.AddComponent<MeshFilter>();
            if (addCollider)
            {
                _collider = _instance.AddComponent<MeshCollider>();
                _collider.sharedMaterial = physicMaterial;
            }
            _anchor = _instance.AddComponent<WorldAnchor>();
            MeshRenderer mr = _instance.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = recieveShadows;
            mr.sharedMaterial = material;
        }

        #region Getters and Setters
        public WorldAnchor Anchor
        {
            get
            {
                return _anchor;
            }
        }

        public MeshCollider Collider
        {
            get
            {
                return _collider;
            }
        }

        public int Id
        {
            get
            {
                return _id;
            }
        }

        public GameObject Instance
        {
            get
            {
                return _instance;
            }
        }

        public MeshFilter Mesh
        {
            get
            {
                return _mesh;
            }
        }

        public SurfaceMeshStatuses MeshStatus
        {
            get
            {
                return _meshStatus;
            }

            set
            {
                _meshStatus = value;
            }
        }

        public DateTime UpdateTime
        {
            get
            {
                return _updateTime;
            }

            set
            {
                _updateTime = value;
            }
        }
        #endregion
    }
}
