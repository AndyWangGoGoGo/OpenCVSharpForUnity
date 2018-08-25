using OpenCvSharp;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VideoDetectorExample
{
    [RequireComponent(typeof(FrameToMatHelper))]
    public class WebCamDetector : MonoBehaviour
    {
        public RawImage PatternRawImage;

        Texture2D _targetPatternTex2d;
        FrameToMatHelper _frameToMatHelper;
        Mat _grayMat;

        PatternDetector _patternDetector;
        Mat[] _matArray;
        Pattern[] _patternsArray;
        Renderer _frameRenderer;
        Texture2D _previewTex2d;
        string _screenshotPath;
        WaitForSeconds _waitForSeconds;

        void Start()
        {
            _frameToMatHelper = gameObject.GetComponent<FrameToMatHelper>();
            _screenshotPath = Application.persistentDataPath + "/Screenshot/";

            var files = GetFiles(_screenshotPath);
            _matArray = InitMatArray(files);

            if (_matArray.Length <= 0)
            {
                OnCapturePatternButtonClick();
            }
            else
            {
                _patternDetector = new PatternDetector(null, null, true);
                _patternsArray = new Pattern[_matArray.Length];

                for (int i = 0; i < _matArray.Length; i++)
                {
                    Cv2.CvtColor(_matArray[i], _matArray[i], ColorConversionCodes.BGR2RGB);
                    _patternsArray[i] = new Pattern();
                    _patternDetector.BuildPatternFromImage(_matArray[i], _patternsArray[i]);
                }
                _frameToMatHelper.Initialize();
                Debug.Log("patternMatArray length: " + _matArray.Length);
                _waitForSeconds = new WaitForSeconds(1f / _frameToMatHelper.GetFPS());
            }
        }

        /// <summary>
        /// Get the PNG format file in the specified folder. 
        /// </summary>
        /// <param name="screenshotpath"></param>
        string[] GetFiles(string screenshotpath)
        {
            if (!Directory.Exists(screenshotpath))
            {
                Directory.CreateDirectory(screenshotpath);
            }

            var pngFiles = Directory.GetFiles(screenshotpath, "*.png", SearchOption.AllDirectories);
            return pngFiles;
        }

        /// <summary>
        /// Read the PNG format file into the Mat format. 
        /// </summary>
        /// <param name="screenshotpath"></param>
        Mat[] InitMatArray(string[] files)
        {
            Mat[] matArray = new Mat[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                var path = files[i].Replace('\\', '/');
                matArray[i] = Cv2.ImRead(path);
            }
            return matArray;
        }

        public void OnFrameToMatHelperInitialized()
        {
            Debug.Log("OnFrameToMatHelperInitialized");

            Mat webCamTextureMat = _frameToMatHelper.GetMat();

            _targetPatternTex2d = new Texture2D(200,200);

            _previewTex2d = new Texture2D(webCamTextureMat.Width, webCamTextureMat.Height, TextureFormat.RGBA32, false);
            _frameRenderer = gameObject.GetComponent<Renderer>();
            _frameRenderer.material.mainTexture = _previewTex2d;

            _grayMat = new Mat(webCamTextureMat.Rows, webCamTextureMat.Cols, MatType.CV_8UC1);

            gameObject.transform.localScale = new Vector3(webCamTextureMat.Width, webCamTextureMat.Height, 1);

            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            float width = webCamTextureMat.Width;
            float height = webCamTextureMat.Height;

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }

            //if WebCamera is frontFaceing,flip Mat.
            if (_frameToMatHelper.GetWebCamDevice().isFrontFacing)
            {
                _frameToMatHelper.FlipHorizontal = true;
            }
            StartCoroutine(DetectorFrames());
        }

        IEnumerator DetectorFrames()
        {
            if (_frameToMatHelper.IsPlaying() && _frameToMatHelper.DidUpdateThisFrame())
            {
                while (true)
                {
                    yield return _waitForSeconds;
                    Mat rgbaMat = _frameToMatHelper.GetMat();

                    Cv2.CvtColor(rgbaMat, _grayMat, ColorConversionCodes.RGBA2GRAY);

                    bool patternFound = false;
                    for (int i = 0; i < _patternsArray.Length; i++)
                    {
                        patternFound = _patternDetector.FindPattern(_grayMat, _patternsArray[i]);
                        if (patternFound)
                        {
                            _targetPatternTex2d = Utils.MatToTexture2D(_patternsArray[i].frame);
                            PatternRawImage.texture = _targetPatternTex2d;
                            PatternRawImage.rectTransform.localScale = new Vector3(1.0f, (float)_patternsArray[i].frame.Height / (float)_patternsArray[i].frame.Width, 1.0f);
                        }
                        yield return 0;
                    }
                    _previewTex2d = Utils.MatToTexture2D(rgbaMat);
                    _frameRenderer.material.mainTexture = _previewTex2d;
                }
            }
        }
       
        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            _frameToMatHelper.Dispose();
            if (_grayMat != null)
            {
                _grayMat.Dispose();
            }

            if (_matArray != null)
            {
                foreach (var item in _matArray)
                {
                    item.Dispose();
                }
            }

            Debug.Log("OnWebCamDetectorDisposed");
        }

        public void OnPlayButtonClick()
        {
            _frameToMatHelper.Play();
        }

        public void OnPauseButtonClick()
        {
            _frameToMatHelper.Pause();
        }

        public void OnStopButtonClick()
        {
            _frameToMatHelper.Stop();
        }

        public void OnChangeCameraButtonClick()
        {
            _frameToMatHelper.Initialize(null, _frameToMatHelper.requestedWidth, _frameToMatHelper.requestedHeight, !_frameToMatHelper.RequestedIsFrontFacing);
        }

        public void OnCapturePatternButtonClick()
        {
            SceneManager.LoadScene("CapturePattern");
        }
    }
}
