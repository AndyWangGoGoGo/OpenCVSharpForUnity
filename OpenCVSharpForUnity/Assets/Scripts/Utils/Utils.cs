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
            Texture2D temp2d = new Texture2D(webCamTexture.width, webCamTexture.height);
            //temp2d.SetPixels32(webCamTexture.GetPixels32());
            temp2d.SetPixels(webCamTexture.GetPixels());
            //After changing the pixels in memory,you need to notify the GPU to update,so you call the Apply method.
            temp2d.Apply();
            return temp2d;
        }
    }
}

