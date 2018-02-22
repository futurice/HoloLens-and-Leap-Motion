using UnityEngine;
using System.Collections.Generic;

namespace Futulabs.HoloFramework.LeapMotion
{
    public enum FingerType
    {
        THUMB,
        INDEX,
        MIDDLE,
        RING,
        PINKY
    }

    /// <summary>
    /// Handles the communication with the computer the Leap Motion is connected to.
    /// </summary>
    public interface ILeapConnectionManager
    {
        void StartSockets(int tcpPort, int udpPort);
    }

    /// <summary>
    /// Transforms incoming Leap Frame data to the correct coordinate system.
    /// </summary>
    public interface ILeapFrameTransformer
    {

    }

    /// <summary>
    /// Handles the visualization of Leap Motion hand data.
    /// </summary>
    public interface ILeapHandDataVisualizer
    {
        void ShowHands();

        void HideHands();
    }

    /// <summary>
    /// Provides access to the locatable camera.
    /// </summary>
    public interface ILocatableCameraController
    {
        /// <summary>
        /// Request the use of the locatable camera. Only one requester can have the camera in use at a time.
        /// </summary>
        /// <param name="requester">The object making the request. The results from the capture will be sent to this object</param>
        /// <returns>True if usage is given, false if the camera is already used by another object.</returns>
        bool RequestUsage(ICameraImageRequester requester);

        /// <summary>
        /// Releases the usage of the locatable camera so another ubject can make use of it
        /// </summary>
        /// <param name="requester">The requester asking for release. If it is not the current requester then usage remains with that requester.</param>
        void ReleaseUsage(ICameraImageRequester requester);
    }

    /// <summary>
    /// Wants to request the locatable camera to take an image.
    /// </summary>
    public interface ICameraImageRequester
    {
        /// <summary>
        /// Function used for providing the camera's results to the object.
        /// </summary>
        /// <param name="imageData">The image as a byte array.</param>
        /// <param name="calibrationMatrix">The camera's calibration matrix.</param>
        void ReceiveTakenPictureAsBytes(List<byte> imageData, int width, int height, Matrix4x4 calibrationMatrix);
    }
}
