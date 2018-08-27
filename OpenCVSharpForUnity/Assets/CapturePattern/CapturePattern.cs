using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using OpenCvSharp;
using System.IO;

namespace VideoDetectorExample
{
    [RequireComponent(typeof(FrameToMatHelper))]
    public class CapturePattern : MonoBehaviour
    {
        public RawImage PatternRawImage;
        Texture2D _previewTex2d;
        FrameToMatHelper _frameToMatHelper;
        OpenCvSharp.Rect _patternRect;
        OpenCvSharp.Mat _rgbMat;
        OpenCvSharp.ORB _detector;
        OpenCvSharp.KeyPoint[] _keypoints;
        Renderer _frameRendere;
        string _screenshotFolder;

        // Use this for initialization
        void Start ()
        {
            _frameToMatHelper = gameObject.GetComponent<FrameToMatHelper> ();
            _frameToMatHelper.Initialize ();


            _detector = OpenCvSharp.ORB.Create (500);
            _detector.MaxFeatures = 1000;
            _keypoints = new OpenCvSharp.KeyPoint[] { };

            _screenshotFolder = Application.persistentDataPath + "/Screenshot/";
            if (!Directory.Exists(_screenshotFolder))
            {
                Directory.CreateDirectory(_screenshotFolder);
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnFrameToMatHelperInitialized ()
        {
            Debug.Log ("OnFrameToMatHelperInitialized");

            OpenCvSharp.Mat webCamTextureMat = _frameToMatHelper.GetMat ();
                    
            _previewTex2d = new Texture2D (webCamTextureMat.Width, webCamTextureMat.Height, TextureFormat.RGBA32, false);

            _rgbMat = new OpenCvSharp.Mat (webCamTextureMat.Rows, webCamTextureMat.Cols, OpenCvSharp.MatType.CV_8UC3);
                      
            gameObject.transform.localScale = new Vector3 (webCamTextureMat.Width, webCamTextureMat.Height, 1);
                    
            float width = webCamTextureMat.Width;
            float height = webCamTextureMat.Height;

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }
            _frameRendere = gameObject.GetComponent<Renderer>();
            _frameRendere.material.mainTexture = _previewTex2d;


            //if WebCamera is frontFaceing,flip Mat.
            if (_frameToMatHelper.GetWebCamDevice ().isFrontFacing) {
                _frameToMatHelper.FlipHorizontal = true;
            }               


            int patternWidth = (int)(Mathf.Min (webCamTextureMat.Width, webCamTextureMat.Height) * 0.8f);

            _patternRect = new OpenCvSharp.Rect (webCamTextureMat.Width / 2 - patternWidth / 2, webCamTextureMat.Height / 2 - patternWidth / 2, patternWidth, patternWidth);
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnFrameToMatHelperDisposed ()
        {
            Debug.Log ("OnFrameToMatHelperDisposed");

            if (_rgbMat != null) {
                _rgbMat.Dispose ();
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnFrameToMatHelperErrorOccurred (FrameToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnFrameToMatHelperErrorOccurred " + errorCode);
        }
        
        // Update is called once per frame
        void Update ()
        {
            if (_frameToMatHelper.IsPlaying () && _frameToMatHelper.DidUpdateThisFrame ()) {

                OpenCvSharp.Mat rgbaMat = _frameToMatHelper.GetMat ();

                OpenCvSharp.Cv2.CvtColor(rgbaMat, _rgbMat, OpenCvSharp.ColorConversionCodes.RGBA2RGB);
                _keypoints = _detector.Detect (rgbaMat);

                OpenCvSharp.Cv2.DrawKeypoints(_rgbMat, _keypoints, rgbaMat, OpenCvSharp.Scalar.All(-1), OpenCvSharp.DrawMatchesFlags.NotDrawSinglePoints);

                OpenCvSharp.Cv2.Rectangle(rgbaMat, _patternRect.TopLeft, _patternRect.BottomRight, new OpenCvSharp.Scalar(255, 0, 0, 255), 5);
                _previewTex2d = Utils.MatToTexture2D(rgbaMat);
                _frameRendere.material.mainTexture = _previewTex2d;
            }
        }

        void OnDestroy ()
        {
            _frameToMatHelper.Dispose ();

            _detector.Dispose ();
            if (_keypoints != null)
                _keypoints = null;
        }
        
        public void OnPlayButtonClick ()
        {
            _frameToMatHelper.Play ();
        }
        
        public void OnPauseButtonClick ()
        {
            _frameToMatHelper.Pause ();
        }

        public void OnStopButtonClick ()
        {
            _frameToMatHelper.Stop ();
        }
        
        public void OnChangeCameraButtonClick ()
        {
            _frameToMatHelper.Initialize (null, _frameToMatHelper.requestedWidth, _frameToMatHelper.requestedHeight, !_frameToMatHelper.RequestedIsFrontFacing);
        }

        public void OnCaptureButtonClick ()
        {
            OpenCvSharp.Mat patternMat = new OpenCvSharp.Mat (_rgbMat, _patternRect);

            Texture2D patternTexture = new Texture2D (patternMat.Width, patternMat.Height, TextureFormat.RGBA32, false);

            patternTexture = Utils.MatToTexture2D (patternMat);
            
            PatternRawImage.texture = patternTexture;

            PatternRawImage.gameObject.SetActive (true);
        }

        /// <summary>
        /// Raises the save button click event.
        /// </summary>
        public void OnSaveButtonClick ()
        {
            if (PatternRawImage.texture != null) {
                Texture2D patternTexture = (Texture2D)PatternRawImage.texture;
                OpenCvSharp.Mat patternMat = new OpenCvSharp.Mat (_patternRect.Size, OpenCvSharp.MatType.CV_8UC3);
                patternMat = Utils.Texture2DToMat (patternTexture);
                OpenCvSharp.Cv2.CvtColor(patternMat, patternMat, OpenCvSharp.ColorConversionCodes.RGB2BGR);

                string savePath = _screenshotFolder + DateTime.Now.ToString("hh_mm_ss") + "_patternImg.png";
                Debug.Log ("savePath " + savePath);
            
                OpenCvSharp.Cv2.ImWrite(savePath, patternMat);

                SceneManager.LoadScene("WebCamDetector");
            }
        }
    }
}
