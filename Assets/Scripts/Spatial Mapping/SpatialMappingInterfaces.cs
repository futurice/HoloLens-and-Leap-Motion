using System;
using UnityEngine;
using UnityEngine.XR.WSA;

namespace Futulabs.HoloFramework.SpatialMapping
{
    /// <summary>
    /// The different bounding volume types the spatial mapper can use
    /// </summary>
    public enum BoundingVolumeTypes
    {
        AXIS_ALIGNED_BOX,
        FRUSTUM,
        ORIENTED_BOX,
        SPHERE
    }

    /// <summary>
    /// The different states of a surface's mesh. Either there is no mesh, it
    /// needs an update, or it is up to date
    /// </summary>
    public enum SurfaceMeshStatuses
    {
        NONE,
        OUTDATED,
        UP_TO_DATE
    }

    /// <summary>
    /// Manages the generation of spatial mapping meshes.
    /// </summary>
    public interface ISpatialMapper
    {

    }

    /// <summary>
    /// Represents a mapped surface.
    /// </summary>
    public interface ISpatialSurface
    {
        /// <summary>
        /// The GameObject that represents the surface's position in the world
        /// </summary>
        GameObject Instance { get; }

        /// <summary>
        /// Unique ID of the surface
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The surface's mesh filter
        /// </summary>
        MeshFilter Mesh { get; }

        /// <summary>
        /// The collider attached to the surface
        /// </summary>
        MeshCollider Collider { get; }

        /// <summary>
        /// Component to keep the surface at the same position in the real world
        /// </summary>
        WorldAnchor Anchor { get; }

        /// <summary>
        /// Status of the surface's mesh
        /// </summary>
        SurfaceMeshStatuses MeshStatus { get; set; }

        /// <summary>
        /// When the mesh was last updated
        /// </summary>
        DateTime UpdateTime { get; set; }
    }

    /// <summary>
    /// Dynamically creates new spatial surfaces.
    /// </summary>
    public interface ISpatialSurfaceFactory
    {
        /// <summary>
        /// Create a new spatial surface
        /// </summary>
        /// <param name="creationTime">When this surface was created</param>
        /// <param name="id">Unique ID of the surface</param>
        /// <param name="castShadows">If the surface's mesh should throw shadows</param>
        /// <param name="recieveShadows">If other objects can cast shadows on the surface</param>
        /// <param name="material">The material to use when drawing the surface</param>
        /// <param name="addCollider">Should the surface have a collider attached to it?</param>
        /// <param name="physicMaterial">Material for physics interactions</param>
        /// <returns>A new spatial surface</returns>
        ISpatialSurface Create(DateTime creationTime, int id, bool castShadows, bool recieveShadows, Material material, bool addCollider, PhysicMaterial physicMaterial);
    }
}