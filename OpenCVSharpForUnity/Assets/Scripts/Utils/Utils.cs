using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VideoDetectorExample
{
    public class Utils
    {
        public static Texture2D MatToTexture2D(Mat mat)
        {
            Texture2D texture2D = new Texture2D(mat.Width, mat.Height);
            texture2D.LoadImage(mat.ToBytes());      
            texture2D.Apply();
            return texture2D;
        }

        public static Mat Texture2DToMat(Texture2D texture2D)
        {
            Mat mat = Mat.FromImageData(texture2D.EncodeToPNG());
            return mat;
        }

        public static Mat Texture2DToMat(WebCamTexture webCamTexture)
        {
            Texture2D texture2D = GetTexture2D(webCamTexture);
            return Mat.FromImageData(texture2D.EncodeToPNG(), ImreadModes.AnyColor); 
        }

        public static Texture2D GetTexture2D(WebCamTexture webCamTexture)
        {
            Texture2D temp2d = new Texture2D(webCamTexture.width,webCamTexture.height);
            //temp2d.SetPixels32(webCamTexture.GetPixels32());
            temp2d.SetPixels(webCamTexture.GetPixels());
            //After changing the pixels in memory,you need to notify the GPU to update,so you call the Apply method.
            temp2d.Apply();
            return temp2d;
        }

        /**
        * Gets the readable path of a file in the "StreamingAssets" folder.
        * <p>
        * <br>Set a relative file path from the starting point of the "StreamingAssets" folder. e.g. "foobar.txt" or "hogehoge/foobar.txt".
        * <br>[Android]The target file that exists in the "StreamingAssets" folder is copied into the folder of the Application.persistentDataPath. If refresh flag is false, when the file has already been copied, the file is not copied. If refresh flag is true, the file is always copyied. 
        * <br>[WebGL]If the target file has not yet been copied to WebGL's virtual filesystem, you need to use getFilePathAsync() at first.
        * 
        * @param filepath a relative file path starting from "StreamingAssets" folder
        * @param refresh [Android]If refresh flag is false, when the file has already been copied, the file is not copied. If refresh flag is true, the file is always copyied.
        * @return returns the file path in case of success and returns empty in case of error.
        */
        public static string GetFilePath(string filepath, bool refresh = false)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            filepath = filepath.TrimStart (ChTrims);

            string srcPath = Path.Combine(Application.streamingAssetsPath, filepath);
            string destPath = Path.Combine(Application.persistentDataPath, "opencvsharpe");
            destPath = Path.Combine(destPath, filepath);

            if (!refresh && File.Exists(destPath))
                return destPath;

            using (WWW request = new WWW (srcPath)) {
                while (!request.isDone) {;}

                if (!string.IsNullOrEmpty(request.error)) {
                    Debug.LogWarning (request.error);
                    return String.Empty;
                }

                //create Directory
                String dirPath = Path.GetDirectoryName (destPath);
                if (!Directory.Exists (dirPath))
                    Directory.CreateDirectory (dirPath);

                File.WriteAllBytes (destPath, request.bytes);
            }

            return destPath;
#elif UNITY_WEBGL && !UNITY_EDITOR
            filepath = filepath.TrimStart (ChTrims);

            string destPath = Path.Combine(Path.AltDirectorySeparatorChar.ToString(), "opencvsharpe");
            destPath = Path.Combine(destPath, filepath);

            if (File.Exists(destPath)){
                return destPath;
            }else{
                return String.Empty;
            }
#else
            filepath = filepath.TrimStart(ChTrims);

            string destPath = Path.Combine(Application.streamingAssetsPath, filepath);

            if (File.Exists(destPath))
            {
                return destPath;
            }
            else
            {
                return String.Empty;
            }
#endif
        }

        /**
        * Gets the readable path of a file in the "StreamingAssets" folder by using coroutines.
        * <p>
        * <br>Set a relative file path from the starting point of the "StreamingAssets" folder.  e.g. "foobar.txt" or "hogehoge/foobar.txt".
        * <br>[Android]The target file that exists in the "StreamingAssets" folder is copied into the folder of the Application.persistentDataPath. If refresh flag is false, when the file has already been copied, the file is not copied. If refresh flag is true, the file is always copyied. 
        * <br>[WebGL]The target file in the "StreamingAssets" folder is copied to the WebGL's virtual filesystem. If refresh flag is false, when the file has already been copied, the file is not copied. If refresh flag is true, the file is always copyied. 
        * 
        * @param filepath a relative file path starting from "StreamingAssets" folder
        * @param completed a callback function that is called when process is completed. Returns the file path in case of success and returns empty in case of error.
        * @param progress a callback function that is called when process is progress. Returns the file path and a value of 0 to 1.
        * @param refresh [Android][WebGL]If refresh flag is false, when the file has already been copied, the file is not copied. If refresh flag is true, the file is always copyied.
        */
        public static IEnumerator GetFilePathAsync(string filepath, Action<string> completed, Action<string, float> progress = null, bool refresh = false)
        {
#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            filepath = filepath.TrimStart (ChTrims);

            string srcPath = Path.Combine(Application.streamingAssetsPath, filepath);
#if UNITY_ANDROID
            string destPath = Path.Combine(Application.persistentDataPath, "opencvsharpe");
#else
            string destPath = Path.Combine(Path.AltDirectorySeparatorChar.ToString(), "opencvsharpe");
#endif
            destPath = Path.Combine(destPath, filepath);

            if (!refresh && File.Exists(destPath)){
                if (progress != null)
                    progress(destPath, 0);
                yield return null;
                if (progress != null)
                    progress(destPath, 1);
                if (completed != null)
                    completed (destPath);
            } else {
#if UNITY_WEBGL
                using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get (srcPath)) {
                    request.Send ();
                    while (!request.isDone) {
                        if (progress != null)
                        progress(filepath, request.downloadProgress);

                        yield return null;
                    }

                    if (request.isHttpError || request.isNetworkError) {
                        Debug.LogWarning (request.error);
                        if (completed != null)
                            completed (String.Empty);
                    }

                    //create Directory
                    String dirPath = Path.GetDirectoryName (destPath);
                    if (!Directory.Exists (dirPath))
                        Directory.CreateDirectory (dirPath);

                    File.WriteAllBytes (destPath, request.downloadHandler.data);
                }
#else
                using (WWW request = new WWW (srcPath)) {

                    while (!request.isDone) {
                        if (progress != null)
                            progress(filepath, request.progress);

                        yield return null;
                    }

                    if (!string.IsNullOrEmpty(request.error)) {
                        Debug.LogWarning (request.error);
                            if (completed != null)
                                completed (String.Empty);
                    }

                    //create Directory
                    String dirPath = Path.GetDirectoryName (destPath);
                    if (!Directory.Exists (dirPath))
                        Directory.CreateDirectory (dirPath);

                    File.WriteAllBytes(destPath, request.bytes);
                }
#endif

                    if (completed != null) completed (destPath);
            }
#else
            filepath = filepath.TrimStart(ChTrims);

            string destPath = Path.Combine(Application.streamingAssetsPath, filepath);

            if (File.Exists(destPath))
            {
                if (progress != null)
                    progress(destPath, 0);
                yield return null;
                if (progress != null)
                    progress(destPath, 1);
                if (completed != null)
                    completed(destPath);
            }
            else
            {
                if (progress != null)
                    progress(String.Empty, 0);
                yield return null;
                if (completed != null)
                    completed(String.Empty);
            }
#endif

            yield break;
        }

        private static char[] ChTrims = {
            '.',
            #if UNITY_WINRT_8_1 && !UNITY_EDITOR
            '/',
            '\\'
            #else
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar
            #endif
        };
    }
}

