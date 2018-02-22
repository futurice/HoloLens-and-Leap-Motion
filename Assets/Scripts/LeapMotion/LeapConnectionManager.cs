using Futulabs.HoloFramework.LeapMotion;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using Zenject;

#if UNITY_EDITOR
using System.Net.Sockets;
using System.Net;
using System.Text;
#endif

#if !UNITY_EDITOR
using System.IO;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
#endif

public class LeapConnectionManager : ILeapConnectionManager, ICameraImageRequester
{
    private Text                            _infoText;
    private ReactiveProperty<string>        _currentText;

#if UNITY_EDITOR
    private IPEndPoint                      _localEndPoint;
    private UdpClient                       _client;

    private IPEndPoint                      _leapClientEndPoint;
#endif

#if !UNITY_EDITOR
    private StreamSocketListener            _tcpListener;
    private StreamSocket                    _tcpSocket;
    private DatagramSocket                  _udpSocket;
    private HostName                        _localHostName;
#endif

    private const string                    _calibrationFile                = "calibration.txt";

    private bool?                           _doCalibration                  = null;
    private bool                            _choosingToCalibrate            = false;
    private KeywordRecognizer               _keywordRecognizer;
    private string[]                        _keywords                       = { "Calibrate", "Load" };

    private int                             _numberOfImagesToSend;
    private int                             _numberOfImagesSent             = 0;
    private GameObject                      _handAlignementCanvas;

    private ILocatableCameraController      _cameraController;
    
    private Subject<Matrix4x4>              _leapToLocatableCamera;
    private ReactiveProperty<bool>          _calibrationStatus;
    private Subject<LeapFrameData>          _frameStream;

    #region Control message strings
    private const string                    _readyForCalibrationString      = "Leap Motion is running and ready for calibration";
    private const string                    _leapCalibrationSuccessString   = "Calibration successfull";
    private const string                    _doCalibrationMessage           = "Do calibration";
    private const string                    _skipCalibrationMessage         = "Skip calibration";
    private const string                    _leapCalibrationFailureString   = "Calibration failed";
    private const string                    _holoCalibrationSuccessString   = "Hololens calibration success";
    private const string                    _holoCalibrationFailString      = "Hololens calibration fail. Redo calibration";
    private const string                    _pauseStreamingString           = "Pause data streaming";
    private const string                    _resumeStreamingString          = "Resume data streaming";
    private const string                    _endStreamingString             = "End data streaming";
    #endregion

    public LeapConnectionManager(
        [Inject(Id = "Info text field")]            Text                        infoText,
        [Inject(Id = "Hand alignment canvas")]      GameObject                  handAlignmentCanvas,
        [Inject(Id = "Calibration image amount")]   int                         numberOfImagesToSend,
        [Inject]                                    ILocatableCameraController  cameraController,
        [Inject(Id = "Leap to locatable camera")]   Subject<Matrix4x4>          leapToLocatableCamera,
        [Inject(Id = "Calibration status")]         ReactiveProperty<bool>      calibrationStatus,
        [Inject(Id = "Frame stream")]               Subject<LeapFrameData>      frameStream)
    {
        _infoText = infoText;
        _handAlignementCanvas = handAlignmentCanvas;
        _currentText = new ReactiveProperty<string>("");
        _currentText.ObserveOn(Scheduler.MainThread).SubscribeOn(Scheduler.MainThread).Subscribe(text => 
        {
            if (!string.IsNullOrEmpty(text))
            {
                _infoText.text = text;

                if (text.StartsWith( "Leap client ready. Start taking pictures.\nPictures sent: 0"))
                {
                    _handAlignementCanvas.SetActive(true);
                }
                
                if (text == "All images sent. Waiting for result.")
                {
                    _handAlignementCanvas.SetActive(false);
                }
            }
        });
        _numberOfImagesToSend = numberOfImagesToSend;
        _cameraController = cameraController;
        _leapToLocatableCamera = leapToLocatableCamera;
        _calibrationStatus = calibrationStatus;
        _frameStream = frameStream;

        _keywordRecognizer = new KeywordRecognizer(_keywords);
        _keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        _keywordRecognizer.Start();
    }

    #region Hololens version

#if !UNITY_EDITOR
    
    public async void ReceiveTakenPictureAsBytes(List<byte> imageData, int width, int height, Matrix4x4 calibrationMatrix)
    {
        _currentText.Value = "Image captured. Processing.";

        DataWriter writer = new DataWriter(_tcpSocket.OutputStream);
        byte[] dataArray = BGRA2BGR(imageData);

        // If this is the first image to be sent, first send message with necessary info
        if (_numberOfImagesSent == 0)
        {
            string message = CreateCalibrationMessage(calibrationMatrix, width, height, dataArray.Length);
            writer.WriteString(message);
            await writer.StoreAsync();
        }

        // Send the current image
        writer.WriteBytes(dataArray);
        await writer.StoreAsync();
        _numberOfImagesSent++;
        writer.DetachStream();

        // If we have more images to send then update the info text. Otherwise, tell the camera controller we don't need it anymore and update the info text.
        if (_numberOfImagesSent < _numberOfImagesToSend)
        {
            _currentText.Value = string.Format("Image sent. Ready to take next picture.\nPictures sent: {0}/{1}", _numberOfImagesSent, _numberOfImagesToSend);
        }
        else
        {
            _cameraController.ReleaseUsage(this);
            _currentText.Value = "All images sent. Waiting for result.";
            _numberOfImagesSent = 0;
        }
    }

    /// <summary>
    /// Function for starting the listening for messages.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    public async void StartSockets(int tcpPort, int udpPort)
    {
        _tcpListener = new StreamSocketListener();
        _tcpListener.Control.KeepAlive = true;
        _tcpListener.ConnectionReceived += ConnectionReceived;
        _udpSocket = new DatagramSocket();
        _udpSocket.MessageReceived += LeapDataReceived;

        _localHostName = GetIpv4HostName();

        try
        {
            await _tcpListener.BindEndpointAsync(_localHostName, tcpPort.ToString());
            Debug.LogFormat("Bound TCP-socket to endpoint {0}/{1}", _localHostName.ToString(), tcpPort.ToString());
            await _udpSocket.BindEndpointAsync(_localHostName, udpPort.ToString());
            Debug.LogFormat("Bound UDP-socket to endpoint {0}/{1}", _localHostName.ToString(), udpPort.ToString());
            _currentText.Value = "Sockets bound. Ready to calibrate.";
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Callback for when a connection is established
    /// </summary>
    /// <param name="sender">The socket data of the sender.</param>
    /// <param name="args">Object containing info about the event and the data received.</param>
    private async void ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        Debug.Log("Message received.");
        _tcpSocket = args.Socket;
        ListenForMessages();
    }

    /// <summary>
    /// Have the TCP socket listen for sent messages
    /// </summary>
    private async void ListenForMessages()
    {
        Stream inStream = _tcpSocket.InputStream.AsStreamForRead();
        StreamReader reader = new StreamReader(inStream);
        while (true)
        {
            string message = await reader.ReadLineAsync();

            if (message == _readyForCalibrationString)
            {
                Debug.Log("Leap client running. Choosing to calibrate or load previous result");
                _choosingToCalibrate = true;
                _currentText.Value = "Leap client ready. To do calibration say 'Calibrate' or say 'Load' to load previous result.";

                // Let thread sleep while we wait for user to choose between doing calibration or loading a previous result
                while (_doCalibration == null)
                {
                    Thread.Sleep(500);
                }

                DataWriter writer = new DataWriter(_tcpSocket.OutputStream);

                if (_doCalibration.Value)
                {
                    _currentText.Value = "Calibration chosen. Waiting for camera.";
                    // Tell Leap client that we're gonna do calibration
                    writer.WriteString(_doCalibrationMessage);
                    await writer.StoreAsync();
                    writer.DetachStream();

                    // Request use of camera. Wait until we get it.
                    while (!_cameraController.RequestUsage(this))
                    {
                        Thread.Sleep(500);
                    }

                    _currentText.Value = string.Format("Leap client ready. Start taking pictures.\nPictures sent: {0}/{1}", _numberOfImagesSent, _numberOfImagesToSend);
                }
                else
                {
                    _currentText.Value = "Loading from file chosen. Loading now.";
                    // Load the latest result
                    Matrix4x4 transform = await ReadCalibrationFromFile();
                    _leapToLocatableCamera.OnNext(transform);
                    _calibrationStatus.Value = true;

                    // Notify Leap client to not do calibration
                    writer.WriteString(_skipCalibrationMessage);
                    await writer.StoreAsync();
                    writer.DetachStream();
                }
                
            }
            else if (message.StartsWith(_leapCalibrationSuccessString))
            {
                _currentText.Value = "Calibration succesful.";
                // Get the values and construct the Leap to camera transform
                Matrix4x4 transform = ReadCalibrationFromMessage(message);
                _leapToLocatableCamera.OnNext(transform);
                _calibrationStatus.Value = true;

                // Write result to file
                WriteCalibrationToFile(transform);

                // Send message to Leap client that everything is OK.
                // TODO: Make it possible to redo calibration
                DataWriter dw = new DataWriter(_tcpSocket.OutputStream);
                dw.WriteString(_holoCalibrationSuccessString);
                await dw.StoreAsync();
                dw.DetachStream();
            }
            else if (message == _leapCalibrationFailureString)
            {
                // TODO: Handle failed calibration
            }
            else
            {
                Debug.LogFormat("Received message: {0}", message);
            }
        }
    }

    /// <summary>
    /// Delegate for when a datagram is received
    /// </summary>
    private async void LeapDataReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        try
        {
            Stream inStream = args.GetDataStream().AsStreamForRead();
            StreamReader reader = new StreamReader(inStream);
            string data = await reader.ReadLineAsync();
            LeapFrameData frameData = JsonUtility.FromJson<LeapFrameData>(data);
            // Temp workaround because JsonUtility creates objects even if you have an empty JSON string
            if (!data.Contains("left_arm"))
            {
                frameData.left_arm = null;
            }
            if (!data.Contains("right_arm"))
            {
                frameData.right_arm = null;
            }

            _frameStream.OnNext(frameData);
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat("Exception when receiving Leap data: {0}", e.Message);
        }
    }

#region Utility functions

    /// <summary>
    /// Helper function for determining the device's IPv4 HostName
    /// </summary>
    /// <returns>The HostName corresponding to the device's IPv4 address</returns>
    private HostName GetIpv4HostName()
    {
        HostName host = null;
        IReadOnlyList<HostName> hostNames = NetworkInformation.GetHostNames();
        for (int i = 0; i < hostNames.Count; ++i)
        {
            if (hostNames[i].Type == HostNameType.Ipv4)
            {
                host = hostNames[i];
                break;
            }
        }
        return host;
    }

    /// <summary>
    /// Delegate for when a phrase is recognized
    /// </summary>
    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (_choosingToCalibrate)
        {
            if (args.text == "Calibrate")
            {
                _doCalibration = true;
            }
            else if (args.text == "Load")
            {
                _doCalibration = false;
            }

            _choosingToCalibrate = false;
        }
    }

    /// <summary>
    /// Transforms a list of bytes representing a BGRA image to an array of bytes representing the corresponding BGR image
    /// </summary>
    /// <param name="bgra">List of bytes representing the original image</param>
    /// <returns>Array of bytes represting the input image as a BGR image</returns>
    private byte[] BGRA2BGR(List<byte> bgra)
    {
        List<byte> bgr = new List<byte>();

        int stride = 4;
        int limit = bgra.Count;
        for (int i = 0; i < limit; i += stride)
        {
            bgr.Add(bgra[i]);
            bgr.Add(bgra[i + 1]);
            bgr.Add(bgra[i + 2]);
        }

        return bgr.ToArray();
    }

    /// <summary>
    /// Creates a message which includes the needed calibration data, how many images will be sent, and how big each image is
    /// </summary>
    /// <param name="calibMatrix">The camera's calibration matrix</param>
    /// <param name="width">Width of each image in pixels</param>
    /// <param name="height">Height of image in pixels</param>
    /// <param name="imgSize">The size of the whole image in bytes</param>
    /// <returns></returns>
    private string CreateCalibrationMessage(Matrix4x4 calibMatrix, int width, int height, int imgSize)
    {
        string message = "";

        // fx
        message += (calibMatrix.m00 * width / 2).ToString();
        // fy
        message += ";" + (calibMatrix.m11 * height / 2).ToString();
        // cx
        message += ";" + ((calibMatrix.m02 + 1) / 2 * width).ToString();
        // cy
        message += ";" + ((calibMatrix.m12 + 1) / 2 * height).ToString();
        // Image width
        message += ";" + width.ToString();
        // Image height
        message += ";" + height.ToString();
        // Number of images that will be sent
        message += ";" + _numberOfImagesToSend.ToString();
        // Image size
        message += ";" + imgSize.ToString();

        return message;
    }

    /// <summary>
    /// Reads the latest calibration result from file
    /// </summary>
    /// <returns>The calibration matrix</returns>
    private async Task<Matrix4x4> ReadCalibrationFromFile()
    {
        StorageFolder folder = ApplicationData.Current.LocalFolder;
        StorageFile file = await folder.GetFileAsync(_calibrationFile);
        string calibrationString = await FileIO.ReadTextAsync(file);
        string[] values = calibrationString.Split(';');
        Matrix4x4 transform = Matrix4x4.zero;
        for (int i = 0; i < 4; ++i)
        {
            Vector4 row;
            row.x = float.Parse(values[i * 4]);
            row.y = float.Parse(values[i * 4 + 1]);
            row.z = float.Parse(values[i * 4 + 2]);
            row.w = float.Parse(values[i * 4 + 3]);

            transform.SetRow(i, row);
        }

        return transform;
    }

    /// <summary>
    /// Reads the calibration result from a received message from the Leap client
    /// </summary>
    /// <param name="message">The Leap client's message</param>
    /// <returns>The calibration matrix</returns>
    private Matrix4x4 ReadCalibrationFromMessage(string message)
    {
        string[] values = message.Split(';');
        Matrix4x4 transform = Matrix4x4.zero;
        // Rotation first row
        transform.m00 = float.Parse(values[1]);
        transform.m01 = float.Parse(values[2]);
        transform.m02 = float.Parse(values[3]);
        // Rotation second row
        transform.m10 = float.Parse(values[4]);
        transform.m11 = float.Parse(values[5]);
        transform.m12 = float.Parse(values[6]);
        // Rotation third row
        transform.m20 = float.Parse(values[7]);
        transform.m21 = float.Parse(values[8]);
        transform.m22 = float.Parse(values[9]);
        // Translation
        transform.m03 = float.Parse(values[10]);
        transform.m13 = float.Parse(values[11]);
        transform.m23 = float.Parse(values[12]);
        transform.m33 = 1.0f;

        return transform;
    }

    /// <summary>
    /// Write the calibration matrix to file
    /// </summary>
    /// <param name="mat">The calibration matrix</param>
    private async void WriteCalibrationToFile(Matrix4x4 mat)
    {
        StorageFolder folder = ApplicationData.Current.LocalFolder;
        StorageFile file = await folder.CreateFileAsync(_calibrationFile, CreationCollisionOption.ReplaceExisting);
        string calibrationString = "";
        // Write to file one row at a time, using ';' as a separator
        for (int i = 0; i < 4; ++i)
        {
            Vector4 current = mat.GetRow(i);
            if (i > 0)
            {
                calibrationString += ";";
            }
            calibrationString += current.x.ToString();
            calibrationString += ";";
            calibrationString += current.y.ToString();
            calibrationString += ";";
            calibrationString += current.z.ToString();
            calibrationString += ";";
            calibrationString += current.w.ToString();
        }
        await FileIO.WriteTextAsync(file, calibrationString);
    }

    #endregion
#endif
    #endregion

    #region Desktop version
#if UNITY_EDITOR

    public void StartSockets(int tcpPort, int udpPort)
    {
        // Stump to stop editor from crying
    }

    public void ReceiveTakenPictureAsBytes(List<byte> imageData, int width, int height, Matrix4x4 calibrationMatrix)
    {
        // Stump to stop editor from crying
    }

    private void LeapMotionClientStartedCallback(IAsyncResult ar)
    {
        // Stump to stop editor from crying
    }

    private void SendBeginningCalibrationMessage()
    {
        // Stump to stop editor from crying
    }

    private void LeapMotionClientFrameDataCallback(IAsyncResult ar)
    {
        // Stump to stop editor from crying
    }

    private IPAddress GetIpv4Adress()
    {
        // Stump to stop editor from crying
        return null;
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        // Stump to stop editor from crying
    }
#endif
    #endregion

}
