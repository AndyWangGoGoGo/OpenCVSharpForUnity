using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using OpenCvSharp;

namespace VideoDetectorExample
{
    public class FrameToMatHelper : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("requestedDeviceName"), TooltipAttribute("Set the name of the device to use. (or device index number)")]
        protected string _requestedDeviceName = null;

        public virtual string requestedDeviceName
        {
            get { return _requestedDeviceName; }
            set
            {
                _requestedDeviceName = value;
                if (hasInitDone)
                {
                    Initialize();
                }
            }
        }

        [SerializeField, FormerlySerializedAs("requestedWidth"), TooltipAttribute("Set the width of camera.")]
        protected int _requestedWidth = 640;

        public virtual int requestedWidth
        {
            get { return _requestedWidth; }
            set
            {
                _requestedWidth = (int)Mathf.Clamp(value, 0f, float.MaxValue);
                if (hasInitDone)
                {
                    Initialize();
                }
            }
        }

        [SerializeField, FormerlySerializedAs("requestedHeight"), TooltipAttribute("Set the height of camera.")]
        protected int _requestedHeight = 480;

        public virtual int requestedHeight
        {
            get { return _requestedHeight; }
            set
            {
                _requestedHeight = (int)Mathf.Clamp(value, 0f, float.MaxValue);
                if (hasInitDone)
                {
                    Initialize();
                }
            }
        }

        [SerializeField, FormerlySerializedAs("requestedIsFrontFacing"), TooltipAttribute("Set whether to use the front facing camera.")]
        protected bool _requestedIsFrontFacing = false;

        public virtual bool RequestedIsFrontFacing
        {
            get { return _requestedIsFrontFacing; }
            set
            {
                _requestedIsFrontFacing = value;
                if (hasInitDone)
                {
                    Initialize(_requestedIsFrontFacing, requestedFPS, Rotate90Degree);
                }
            }
        }

        [SerializeField, FormerlySerializedAs("requestedFPS"), TooltipAttribute("Set the frame rate of camera.")]
        protected float _requestedFPS = 30f;

        public virtual float requestedFPS
        {
            get { return _requestedFPS; }
            set
            {
                _requestedFPS = Mathf.Clamp(value, -1f, float.MaxValue);
                if (hasInitDone)
                {
                    bool isPlaying = IsPlaying();
                    Stop();
                    webCamTexture.requestedFPS = _requestedFPS;
                    if (isPlaying)
                        Play();
                }
            }
        }

        [SerializeField, FormerlySerializedAs("rotate90Degree"), TooltipAttribute("Sets whether to rotate camera frame 90 degrees. (clockwise)")]
        protected bool _rotate90Degree = false;

        public virtual bool Rotate90Degree
        {
            get { return _rotate90Degree; }
            set
            {
                _rotate90Degree = value;
                if (hasInitDone)
                {
                    Initialize();
                }
            }
        }

        [SerializeField, FormerlySerializedAs("flipVertical"), TooltipAttribute("Determines if flips vertically.")]
        protected bool _flipVertical = false;

        public virtual bool FlipVertical
        {
            get { return _flipVertical; }
            set { _flipVertical = value; }
        }

        [SerializeField, FormerlySerializedAs("flipHorizontal"), TooltipAttribute("Determines if flips horizontal.")]
        protected bool _flipHorizontal = false;

        public virtual bool FlipHorizontal
        {
            get { return _flipHorizontal; }
            set { _flipHorizontal = value; }
        }

        [SerializeField, FormerlySerializedAs("timeoutFrameCount"), TooltipAttribute("The number of frames before the initialization process times out.")]
        protected int _timeoutFrameCount = 300;

        public virtual int timeoutFrameCount
        {
            get { return _timeoutFrameCount; }
            set { _timeoutFrameCount = (int)Mathf.Clamp(value, 0f, float.MaxValue); }
        }

        /// <summary>
        /// UnityEvent that is triggered when this instance is initialized.
        /// </summary>
        public UnityEvent onInitialized;

        protected WebCamTexture webCamTexture;
        protected WebCamDevice webCamDevice;

        protected Mat frameMat;
        protected Mat rotatedFrameMat;
        protected Color32[] colors;

        /// <summary>
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        protected bool isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        protected bool hasInitDone = false;

        protected IEnumerator initCoroutine;
        protected ScreenOrientation screenOrientation;
        protected int screenWidth;
        protected int screenHeight;


        [System.Serializable]
        public enum ErrorCode : int
        {
            UNKNOWN = 0,
            CAMERA_DEVICE_NOT_EXIST = 1,
            TIMEOUT = 2,
        }

        [System.Serializable]
        public class ErrorUnityEvent : UnityEngine.Events.UnityEvent<ErrorCode>
        {

        }

        protected virtual void OnValidate()
        {
            _requestedWidth = (int)Mathf.Clamp(_requestedWidth, 0f, float.MaxValue);
            _requestedHeight = (int)Mathf.Clamp(_requestedHeight, 0f, float.MaxValue);
            _requestedFPS = Mathf.Clamp(_requestedFPS, -1f, float.MaxValue);
            _timeoutFrameCount = (int)Mathf.Clamp(_timeoutFrameCount, 0f, float.MaxValue);
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (hasInitDone)
            {
                // Catch the orientation change of the screen and correct the mat image to the correct direction.
                if (screenOrientation != Screen.orientation && (screenWidth != Screen.width || screenHeight != Screen.height))
                {
                    if (frameMat != null)
                    {
                        frameMat.Dispose();
                        frameMat = null;
                    }
                    if (rotatedFrameMat != null)
                    {
                        rotatedFrameMat.Dispose();
                        rotatedFrameMat = null;
                    }

                    frameMat = new Mat(webCamTexture.height, webCamTexture.width, MatType.CV_8UC4);
                    screenOrientation = Screen.orientation;
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;

                    bool isRotatedFrame = false;
#if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL)
                    if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown) {
                        if (!Rotate90Degree)
                            isRotatedFrame = true;
                    } else if (Rotate90Degree) {
                        isRotatedFrame = true;
                    }
#else
                    if (Rotate90Degree)
                        isRotatedFrame = true;
#endif
                    if (isRotatedFrame)
                        rotatedFrameMat = new Mat(webCamTexture.width, webCamTexture.height, MatType.CV_8UC4);

                    if (onInitialized != null)
                        onInitialized.Invoke();
                }
                else
                {
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;
                }
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        protected virtual void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public virtual void Initialize()
        {
            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }

            if (onInitialized == null)
                onInitialized = new UnityEvent();

            initCoroutine = _Initialize();
            StartCoroutine(initCoroutine);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="requestedWidth">Requested width.</param>
        /// <param name="requestedHeight">Requested height.</param>
        public virtual void Initialize(int requestedWidth, int requestedHeight)
        {
            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }

            this._requestedWidth = requestedWidth;
            this._requestedHeight = requestedHeight;
            if (onInitialized == null)
                onInitialized = new UnityEvent();

            initCoroutine = _Initialize();
            StartCoroutine(initCoroutine);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="requestedIsFrontFacing">If set to <c>true</c> requested to using the front camera.</param>
        /// <param name="requestedFPS">Requested FPS.</param>
        /// <param name="rotate90Degree">If set to <c>true</c> requested to rotate camera frame 90 degrees. (clockwise)</param>
        public virtual void Initialize(bool requestedIsFrontFacing, float requestedFPS = 30f, bool rotate90Degree = false)
        {
            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }

            _requestedDeviceName = null;
            this._requestedIsFrontFacing = requestedIsFrontFacing;
            this._requestedFPS = requestedFPS;
            this._rotate90Degree = rotate90Degree;
            if (onInitialized == null)
                onInitialized = new UnityEvent();

            initCoroutine = _Initialize();
            StartCoroutine(initCoroutine);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
        /// <param name="requestedWidth">Requested width.</param>
        /// <param name="requestedHeight">Requested height.</param>
        /// <param name="requestedIsFrontFacing">If set to <c>true</c> requested to using the front camera.</param>
        /// <param name="requestedFPS">Requested FPS.</param>
        /// <param name="rotate90Degree">If set to <c>true</c> requested to rotate camera frame 90 degrees. (clockwise)</param>
        public virtual void Initialize(string deviceName, int requestedWidth, int requestedHeight, bool requestedIsFrontFacing = false, float requestedFPS = 30f, bool rotate90Degree = false)
        {
            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }

            this._requestedDeviceName = deviceName;
            this._requestedWidth = requestedWidth;
            this._requestedHeight = requestedHeight;
            this._requestedIsFrontFacing = requestedIsFrontFacing;
            this._requestedFPS = requestedFPS;
            this._rotate90Degree = rotate90Degree;
            if (onInitialized == null)
                onInitialized = new UnityEvent();

            initCoroutine = _Initialize();
            StartCoroutine(initCoroutine);
        }

        /// <summary>
        /// Initializes this instance by coroutine.
        /// </summary>
        protected virtual IEnumerator _Initialize()
        {
            if (hasInitDone)
            {
                ReleaseResources();
            }

            isInitWaiting = true;

            // Creates the camera
            if (!String.IsNullOrEmpty(requestedDeviceName))
            {
                int requestedDeviceIndex = -1;
                if (Int32.TryParse(requestedDeviceName, out requestedDeviceIndex))
                {
                    if (requestedDeviceIndex >= 0 && requestedDeviceIndex < WebCamTexture.devices.Length)
                    {
                        webCamDevice = WebCamTexture.devices[requestedDeviceIndex];
                        if (requestedFPS < 0)
                        {
                            webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight);
                        }
                        else
                        {
                            webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, (int)requestedFPS);
                        }
                    }
                }
                else
                {
                    for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
                    {
                        if (WebCamTexture.devices[cameraIndex].name == requestedDeviceName)
                        {
                            webCamDevice = WebCamTexture.devices[cameraIndex];
                            if (requestedFPS < 0)
                            {
                                webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight);
                            }
                            else
                            {
                                webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, (int)requestedFPS);
                            }
                            break;
                        }
                    }
                }
                if (webCamTexture == null)
                    Debug.Log("Cannot find camera device " + requestedDeviceName + ".");
            }

            if (webCamTexture == null)
            {
                // Checks how many and which cameras are available on the device
                for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
                {
                    if (WebCamTexture.devices[cameraIndex].isFrontFacing == RequestedIsFrontFacing)
                    {
                        webCamDevice = WebCamTexture.devices[cameraIndex];
                        if (requestedFPS < 0)
                        {
                            webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight);
                        }
                        else
                        {
                            webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, (int)requestedFPS);
                        }
                        break;
                    }
                }
            }

            if (webCamTexture == null)
            {
                if (WebCamTexture.devices.Length > 0)
                {
                    webCamDevice = WebCamTexture.devices[0];
                    if (requestedFPS < 0)
                    {
                        webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight);
                    }
                    else
                    {
                        webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, (int)requestedFPS);
                    }
                }
                else
                {
                    isInitWaiting = false;
                    Debug.LogError("OnFrameToMatHelperErrorOccurred " + ErrorCode.CAMERA_DEVICE_NOT_EXIST);
                    yield break;
                }
            }

            // Starts the camera
            webCamTexture.Play();

            int initFrameCount = 0;
            bool isTimeout = false;

            while (true)
            {
                if (initFrameCount > timeoutFrameCount)
                {
                    isTimeout = true;
                    break;
                }
                // If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
#if UNITY_IOS && !UNITY_EDITOR
                else if (webCamTexture.width > 16 && webCamTexture.height > 16) {
#else
                else if (webCamTexture.didUpdateThisFrame)
                {
#if UNITY_IOS && !UNITY_EDITOR
                    while (webCamTexture.width <= 16) {
                        if (initFrameCount > timeoutFrameCount) {
                            isTimeout = true;
                            break;
                        }else {
                            initFrameCount++;
                        }
                        webCamTexture.GetPixels32 ();
                        yield return new WaitForEndOfFrame ();
                    }
                    if (isTimeout) break;
#endif
#endif

                    Debug.Log("WebCamTextureToMatHelper:: " + "devicename:" + webCamTexture.deviceName + " name:" + webCamTexture.name + " width:" + webCamTexture.width + " height:" + webCamTexture.height + " fps:" + webCamTexture.requestedFPS
                    + " videoRotationAngle:" + webCamTexture.videoRotationAngle + " videoVerticallyMirrored:" + webCamTexture.videoVerticallyMirrored + " isFrongFacing:" + webCamDevice.isFrontFacing);

                    if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
                        colors = new Color32[webCamTexture.width * webCamTexture.height];

                    frameMat = new Mat(webCamTexture.height, webCamTexture.width, MatType.CV_8UC4);
                    screenOrientation = Screen.orientation;
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;

                    bool isRotatedFrame = false;
#if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL)
                    if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown) {
                        if (!Rotate90Degree)
                        isRotatedFrame = true;
                    } else if (Rotate90Degree) {
                        isRotatedFrame = true;
                    }
#else
                    if (Rotate90Degree)
                        isRotatedFrame = true;
#endif
                    if (isRotatedFrame)
                        rotatedFrameMat = new Mat(webCamTexture.width, webCamTexture.height, MatType.CV_8UC4);

                    isInitWaiting = false;
                    hasInitDone = true;
                    initCoroutine = null;

                    if (onInitialized != null)
                        onInitialized.Invoke();

                    break;
                }
                else
                {
                    initFrameCount++;
                    yield return null;
                }
            }

            if (isTimeout)
            {
                webCamTexture.Stop();
                webCamTexture = null;
                isInitWaiting = false;
                initCoroutine = null;

                Debug.LogError("OnFrameToMatHelperErrorOccurred " + ErrorCode.TIMEOUT);
            }
        }

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        /// <returns><c>true</c>, if this instance has been initialized, <c>false</c> otherwise.</returns>
        public virtual bool IsInitialized()
        {
            return hasInitDone;
        }

        /// <summary>
        /// Starts the camera.
        /// </summary>
        public virtual void Play()
        {
            if (hasInitDone)
                webCamTexture.Play();
        }

        /// <summary>
        /// Pauses the active camera.
        /// </summary>
        public virtual void Pause()
        {
            if (hasInitDone)
                webCamTexture.Pause();
        }

        /// <summary>
        /// Stops the active camera.
        /// </summary>
        public virtual void Stop()
        {
            if (hasInitDone)
                webCamTexture.Stop();
        }

        /// <summary>
        /// Indicates whether the active camera is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the active camera is playing, <c>false</c> otherwise.</returns>
        public virtual bool IsPlaying()
        {
            return hasInitDone ? webCamTexture.isPlaying : false;
        }

        /// <summary>
        /// Indicates whether the active camera device is currently front facng.
        /// </summary>
        /// <returns><c>true</c>, if the active camera device is front facng, <c>false</c> otherwise.</returns>
        public virtual bool IsFrontFacing()
        {
            return hasInitDone ? webCamDevice.isFrontFacing : false;
        }

        /// <summary>
        /// Returns the active camera device name.
        /// </summary>
        public virtual string GetDeviceName()
        {
            return hasInitDone ? webCamTexture.deviceName : "";
        }

        /// <summary>
        /// Returns the active camera width.
        /// </summary>
        public virtual int GetWidth()
        {
            if (!hasInitDone)
                return -1;
            return (rotatedFrameMat != null) ? frameMat.Height : frameMat.Width;
        }

        /// <summary>
        /// Returns the active camera height.
        /// </summary>
        public virtual int GetHeight()
        {
            if (!hasInitDone)
                return -1;
            return (rotatedFrameMat != null) ? frameMat.Width : frameMat.Height;
        }

        /// <summary>
        /// Returns the active camera framerate.
        /// </summary>
        public virtual float GetFPS()
        {
            return hasInitDone ? webCamTexture.requestedFPS : -1f;
        }

        /// <summary>
        /// Returns the active WebcamTexture.
        /// </summary>
        public virtual WebCamTexture GetWebCamTexture()
        {
            return hasInitDone ? webCamTexture : null;
        }

        /// <summary>
        /// Returns the active WebcamDevice.
        /// </summary>
        public virtual WebCamDevice GetWebCamDevice()
        {
            return webCamDevice;
        }

        /// <summary>
        /// Returns the camera to world matrix.
        /// </summary>
        /// <returns>The camera to world matrix.</returns>
        public virtual Matrix4x4 GetCameraToWorldMatrix()
        {
            return Camera.main.cameraToWorldMatrix;
        }

        /// <summary>
        /// Returns the projection matrix matrix.
        /// </summary>
        /// <returns>The projection matrix.</returns>
        public virtual Matrix4x4 GetProjectionMatrix()
        {
            return Camera.main.projectionMatrix;
        }

        /// <summary>
        /// Indicates whether the video buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the video buffer has been updated <c>false</c> otherwise.</returns>
        public virtual bool DidUpdateThisFrame()
        {
            if (!hasInitDone)
                return false;

#if UNITY_IOS && !UNITY_EDITOR
            if (webCamTexture.width > 16 && webCamTexture.height > 16) {
                return true;
            } else {
                return false;
            }
#else
            return webCamTexture.didUpdateThisFrame;
#endif
        }

        public virtual Texture GetTexture2D()
        {
            return webCamTexture;
        }

        /// <summary>
        /// Gets the mat of the current frame.
        /// The Mat object's type is 'CV_8UC4' (RGBA).
        /// </summary>
        /// <returns>The mat of the current frame.</returns>
        public virtual Mat GetMat()
        {
            if (!hasInitDone || !webCamTexture.isPlaying)
            {
                return (rotatedFrameMat != null) ? rotatedFrameMat : frameMat;
            }
            colors = webCamTexture.GetPixels32();
            frameMat = Utils.Texture2DToMat(webCamTexture);

#if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL)
            if (rotatedFrameMat != null) {
                if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown) {
                    // (Orientation is Portrait, rotate90Degree is false)
                    if (webCamDevice.isFrontFacing){ 
                        FlipMat (frameMat, !FlipHorizontal, !FlipVertical);
                    }else{
                        FlipMat (frameMat, FlipHorizontal, FlipVertical);
                    }
                } else {
                    // (Orientation is Landscape, rotate90Degrees=true)
                    FlipMat (frameMat, FlipVertical, FlipHorizontal);
                }
                //Core.rotate (frameMat, rotatedFrameMat, Core.ROTATE_90_CLOCKWISE);
                Cv2.Flip(frameMat, rotatedFrameMat, FlipMode.Y);
                return rotatedFrameMat;
            } else {
                if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown) {
                    // (Orientation is Portrait, rotate90Degree is ture)
                    if (webCamDevice.isFrontFacing){ 
                        FlipMat (frameMat, FlipHorizontal, FlipVertical);
                    }else{
                        FlipMat (frameMat, !FlipHorizontal, !FlipVertical);
                    }
                } else {
                    // (Orientation is Landscape, rotate90Degree is false)
                    FlipMat (frameMat, FlipVertical, FlipHorizontal);
                }
                return frameMat;
            }
#else
            FlipMat(frameMat, FlipVertical, FlipHorizontal);
            if (rotatedFrameMat != null)
            {
                //Cv2.Transpose(frameMat, rotatedFrameMat);
                Cv2.Flip(frameMat, rotatedFrameMat, FlipMode.Y);
                return rotatedFrameMat;
            }
            else
            {
                return frameMat;
            }
#endif
        }

        /// <summary>
        /// Flips the mat.
        /// </summary>
        /// <param name="mat">Mat.</param>
        protected virtual void FlipMat(Mat mat, bool flipVertical, bool flipHorizontal)
        {
            //Since the order of pixels of WebCamTexture and Mat is opposite, the initial value of flipCode is set to 0 (flipVertical).
            int flipCode = 0;

            if (webCamDevice.isFrontFacing)
            {
                if (webCamTexture.videoRotationAngle == 0)
                {
                    flipCode = -1;
                }
                else if (webCamTexture.videoRotationAngle == 90)
                {
                    flipCode = -1;
                }
                if (webCamTexture.videoRotationAngle == 180)
                {
                    flipCode = int.MinValue;
                }
                else if (webCamTexture.videoRotationAngle == 270)
                {
                    flipCode = int.MinValue;
                }
            }
            else
            {
                if (webCamTexture.videoRotationAngle == 180)
                {
                    flipCode = 1;
                }
                else if (webCamTexture.videoRotationAngle == 270)
                {
                    flipCode = 1;
                }
            }

            if (flipVertical)
            {
                if (flipCode == int.MinValue)
                {
                    flipCode = 0;
                }
                else if (flipCode == 0)
                {
                    flipCode = int.MinValue;
                }
                else if (flipCode == 1)
                {
                    flipCode = -1;
                }
                else if (flipCode == -1)
                {
                    flipCode = 1;
                }
            }

            if (flipHorizontal)
            {
                if (flipCode == int.MinValue)
                {
                    flipCode = 1;
                }
                else if (flipCode == 0)
                {
                    flipCode = -1;
                }
                else if (flipCode == 1)
                {
                    flipCode = int.MinValue;
                }
                else if (flipCode == -1)
                {
                    flipCode = 0;
                }
            }

            if (flipCode > int.MinValue)
            {
                Cv2.Flip(mat, mat, (FlipMode)flipCode);
            }
        }

        /// <summary>
        /// Gets the buffer colors.
        /// </summary>
        /// <returns>The buffer colors.</returns>
        public virtual Color32[] GetBufferColors()
        {
            return colors;
        }

        /// <summary>
        /// Cancel Init Coroutine.
        /// </summary>
        protected virtual void CancelInitCoroutine()
        {
            if (initCoroutine != null)
            {
                StopCoroutine(initCoroutine);
                ((IDisposable)initCoroutine).Dispose();
                initCoroutine = null;
            }
        }

        /// <summary>
        /// To release the resources.
        /// </summary>
        protected virtual void ReleaseResources()
        {
            isInitWaiting = false;
            hasInitDone = false;

            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                WebCamTexture.Destroy(webCamTexture);
                webCamTexture = null;
            }
            if (frameMat != null)
            {
                frameMat.Dispose();
                frameMat = null;
            }
            if (rotatedFrameMat != null)
            {
                rotatedFrameMat.Dispose();
                rotatedFrameMat = null;
            }
        }

        /// <summary>
        /// Releases all resource used by the <see cref="FrameToMatHelper"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="FrameToMatHelper"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="FrameToMatHelper"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="FrameToMatHelper"/> so
        /// the garbage collector can reclaim the memory that the <see cref="FrameToMatHelper"/> was occupying.</remarks>
        public virtual void Dispose()
        {
            if (colors != null)
                colors = null;

            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }
            else if (hasInitDone)
            {
                ReleaseResources();
            }
        }
    }
}