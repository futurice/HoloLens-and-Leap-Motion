using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;
using Zenject;

namespace Futulabs.HoloFramework.SpatialMapping
{
    public class SpatialMapper : MonoBehaviour, ISpatialMapper
    {
        #region Update settings

        [Header("Update settings")]
        [SerializeField]
        private bool    _doUpdates              = true;
        [SerializeField]
        [Tooltip("How often a check is made for changes in surfaces")]
        private float   _updateInterval         = 2.0f;

        #endregion

        #region Mesh settings

        [Header("Mesh settings")]
        [SerializeField]
        [Tooltip("If created surfaces should have colliders attached to them")]
        private bool            _addColliders           = true;
        [SerializeField]
        [Tooltip("The layer the created meshes are supposed to be placed on")]
        private int             _meshLayer              = 0;
        [SerializeField]
        [Tooltip("The primary material used for drawing the meshes")]
        private Material        _meshMaterial           = null;
        [SerializeField]
        [Tooltip("Optional material if you want to customize physics interaction with the surfaces")]
        private PhysicMaterial  _physicMaterial         = null;
        [SerializeField]
        [Tooltip("If the surface meshes should throw shadows")]
        private bool            _castShadows            = false;
        [SerializeField]
        [Tooltip("If the surface meshes should recieve shadows")]
        private bool            _recieveShadows         = true;
        [SerializeField]
        [Tooltip("The level of detail of generated meshes")]
        private float           _trianglesPerCubicMeter = 200.0f;
        [SerializeField]
        [Tooltip("Override for the surface meshes' parent. Leave this empty to use this game object as the parent")]
        private Transform       _meshParent             = null;

        #endregion

        #region Bounding volume settings

        [Header("Bounding volume settings")]
        [SerializeField]
        private BoundingVolumeTypes _boundingVolumeType     = BoundingVolumeTypes.AXIS_ALIGNED_BOX;
        [SerializeField]
        [Tooltip("Distance from center to edges along x-axis")]
        private float               _boxXExtent             = 3;
        [SerializeField]
        [Tooltip("Distance from center to edges along y-axis")]
        private float               _boxYExtent             = 3;
        [SerializeField]
        [Tooltip("Distance from center to edges along z-axis")]
        private float               _boxZExtent             = 3;
        [SerializeField]
        private float               _sphereRadius           = 3;

        #endregion

        #region Surface handling variables

        private SurfaceObserver                     _surfaceObserver;

        private Dictionary<int, ISpatialSurface>    _surfaceEntries;
        private ISpatialSurfaceFactory              _surfaceEntryFactory;

        private float                               _previousUpdate;
        private bool                                _meshRequestInProgress;

        #endregion

        [Inject]
        public void Initialize(
            [Inject] ISpatialSurfaceFactory surfaceEntryFactory)
        {
            _surfaceObserver = new SurfaceObserver();

            UpdateBoundingVolume();

            _surfaceEntries = new Dictionary<int, ISpatialSurface>();
            _surfaceEntryFactory = surfaceEntryFactory;

            _previousUpdate = 0.0f;
            _meshRequestInProgress = false;
        }

        /// <summary>
        /// Checks if the surface observer needs to be updated, if a new mesh request should be started,
        /// finds the surface with highest priority, and sends a new request for a mesh
        /// </summary>
        private void Update()
        {
            if (_doUpdates)
            {
                // Updating the surface observer is a very expensive procedure, so only do it at the specified intervals
                if (Time.realtimeSinceStartup > _previousUpdate + _updateInterval)
                {
                    UpdateBoundingVolume();

                    try
                    {
                        _surfaceObserver.Update(SurfaceChangedHandler);
                    }
                    catch
                    {
                        // Bad callbacks can throw errors
                        Debug.LogError("SpatialMapper Error: Unexpected failure when updating the surface observer!");
                    }

                    _previousUpdate = Time.realtimeSinceStartup;
                }

                // If there is no current mesh request, start a new one
                if (!_meshRequestInProgress)
                {
                    ISpatialSurface highestPrioritySurface = GetHighestPrioritySurface();

                    if (highestPrioritySurface != null)
                    {
                        SurfaceData sd;
                        sd.id.handle = highestPrioritySurface.Id;
                        sd.outputMesh = highestPrioritySurface.Mesh;
                        sd.outputCollider = highestPrioritySurface.Collider;
                        sd.outputAnchor = highestPrioritySurface.Anchor;
                        sd.trianglesPerCubicMeter = _trianglesPerCubicMeter;
                        sd.bakeCollider = sd.outputCollider != null;

                        try
                        {
                            if (_surfaceObserver.RequestMeshAsync(sd, SurfaceDataReadyHandler))
                            {
                                _meshRequestInProgress = true;
                            }
                            else
                            {
                                Debug.LogErrorFormat("SpatialMapper Error: Mesh request for {0} failed. Check that {0} is a valid Surface ID.", sd.id);
                            }
                        }
                        catch
                        {
                            // Failure can happen if the data structure is filled out wrong
                            Debug.LogErrorFormat("SpatialMapper Error: Mesh request for {0} failed unexpectedly.", sd.id);
                        }
                    }
                }
            }
        }

        #region Helper functions

        /// <summary>
        /// Helper function for updating the surface observer's bounding volume
        /// </summary>
        private void UpdateBoundingVolume()
        {
            switch(_boundingVolumeType)
            {
                case BoundingVolumeTypes.AXIS_ALIGNED_BOX:
                    _surfaceObserver.SetVolumeAsAxisAlignedBox(
                        Camera.main.transform.position, 
                        new Vector3(_boxXExtent, _boxYExtent, _boxZExtent)
                        );
                    break;

                case BoundingVolumeTypes.FRUSTUM:
                    _surfaceObserver.SetVolumeAsFrustum(GeometryUtility.CalculateFrustumPlanes(Camera.main));
                    break;

                case BoundingVolumeTypes.ORIENTED_BOX:
                    _surfaceObserver.SetVolumeAsOrientedBox(
                        Camera.main.transform.position,
                        new Vector3(_boxXExtent, _boxYExtent, _boxZExtent),
                        Camera.main.transform.rotation
                        );
                    break;

                case BoundingVolumeTypes.SPHERE:
                    _surfaceObserver.SetVolumeAsSphere(
                        Camera.main.transform.position,
                        _sphereRadius
                        );
                    break;

                default:
                    Debug.LogError("SpatialMapper Error: Unknown bounding volume type!");
                    break;
            }

        }

        /// <summary>
        /// Get the surface with the highest priority for retrieving the mesh. Surfaces
        /// without meshes are prioritized before those with outdated ones, and older
        /// ones are prioritized before newer ones.
        /// </summary>
        /// <returns></returns>
        private ISpatialSurface GetHighestPrioritySurface()
        {
            ISpatialSurface highestPrioritySurface = null;

            foreach (KeyValuePair<int, ISpatialSurface> surface in _surfaceEntries)
            {
                // Only consider surfaces with out of date/without meshes
                if (surface.Value.MeshStatus != SurfaceMeshStatuses.UP_TO_DATE)
                {
                    if (highestPrioritySurface == null)
                    {
                        highestPrioritySurface = surface.Value;
                    }
                    else
                    {
                        // Missing meshes before outdated meshes
                        if (surface.Value.MeshStatus < highestPrioritySurface.MeshStatus)
                        {
                            highestPrioritySurface = surface.Value;
                        }
                        // Older meshes first
                        else if (surface.Value.UpdateTime < highestPrioritySurface.UpdateTime)
                        {
                            highestPrioritySurface = surface.Value;
                        }
                    }
                }
            }
            return highestPrioritySurface;
        }

        #endregion

        #region Surface event handlers

        /// <summary>
        /// This method handles observed changes to surfaces (added, updated, or removed).
        /// These changes have their source in the update loop where the surface observer 
        /// is updated.
        /// </summary>
        /// <param name="id">The unique id of the surface</param>
        /// <param name="changeType">Says if the surface has been added, updated, or removed</param>
        /// <param name="bounds">The axis aligned bounding box containing the surface</param>
        /// <param name="updateTime">Time of the update</param>
        private void SurfaceChangedHandler(SurfaceId id, SurfaceChange changeType, Bounds bounds, DateTime updateTime)
        {
            ISpatialSurface surface = null;
            switch (changeType)
            {
                case SurfaceChange.Added:
                case SurfaceChange.Updated:
                    if (_surfaceEntries.TryGetValue(id.handle, out surface))
                    {
                        if (surface.MeshStatus == SurfaceMeshStatuses.UP_TO_DATE)
                        {
                            surface.MeshStatus = SurfaceMeshStatuses.OUTDATED;
                            surface.UpdateTime = updateTime;
                        }
                    }
                    else
                    {
                        surface = _surfaceEntryFactory.Create(updateTime, id.handle, _castShadows, _recieveShadows, _meshMaterial, _addColliders, _physicMaterial);
                        surface.Instance.transform.SetParent(_meshParent != null ? _meshParent : transform, false);
                        _surfaceEntries[id.handle] = surface;
                    }
                    break;

                case SurfaceChange.Removed:
                    if (_surfaceEntries.TryGetValue(id.handle, out surface))
                    {
                        _surfaceEntries.Remove(id.handle);
                        Mesh mesh = surface.Mesh.mesh;
                        if (mesh)
                        {
                            Destroy(mesh);
                        }
                        Destroy(surface.Instance);
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// This methods handles the result of a request for mesh data for a surface.
        /// These requests have their source in the update loop anytime a request
        /// isn't in progress.
        /// </summary>
        /// <param name="data">The resulting data</param>
        /// <param name="outputWritten">If any data has been written</param>
        /// <param name="elapsedBakeTimeSeconds">Time it took from request to this function being evoked</param>
        private void SurfaceDataReadyHandler(SurfaceData data, bool outputWritten, float elapsedBakeTimeSeconds)
        {
            _meshRequestInProgress = false;
            ISpatialSurface surface;
            if (_surfaceEntries.TryGetValue(data.id.handle, out surface))
            {
                surface.MeshStatus = SurfaceMeshStatuses.UP_TO_DATE;
            }
            else
            {
                Debug.LogError("SpatialMapper Error: Id of request surface could not be found!");
            }
        }

        #endregion
    }
}
