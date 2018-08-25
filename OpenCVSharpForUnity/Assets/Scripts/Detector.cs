using OpenCvSharp;
using System.Collections.Generic;
using UnityEngine;

namespace VideoDetectorExample
{
    //Lowe's algorithm
    //Recomment ratio value between 0.4~0.6.
    //ratio=0.4:High precision matching is requied.
    //ratio=0.6:More matching points are required.
    //ratio=0.5:Under normal conditions.
    public class Detector : MonoBehaviour
    {

        public SpriteRenderer SrcSprite;
        private Texture2D t2d;
        // Use this for initialization
        void Start()
        {
            OnOrb();
            //OnFast();
            //OnHarris();
        }

        void Detect()
        {
            var gray = new Mat(Application.streamingAssetsPath + "/bryce_01.jpg", ImreadModes.GrayScale);
            KeyPoint[] keyPoints = null;
            using (var orb = ORB.Create(500))
            {
                keyPoints = orb.Detect(gray);
                Debug.Log(string.Format("KeyPoint has {0} items.", keyPoints.Length));
            }
        }

        void DetectAndCompute()
        {
            var gray = new Mat(Application.streamingAssetsPath + "/bryce_01.jpg", ImreadModes.GrayScale);
            KeyPoint[] keyPoints = null;
            using (var orb = ORB.Create(500))
            using (Mat descriptor = new Mat())
            {
                orb.DetectAndCompute(gray, new Mat(), out keyPoints, descriptor);
                Debug.Log(string.Format("keyPoints has {0} items.", keyPoints.Length));
                Debug.Log(string.Format("descriptor has {0} items.", descriptor.Rows));
            }
        }

        void OnHarris()
        {
            Mat image01 = Cv2.ImRead(Application.streamingAssetsPath + "/bryce_01.jpg");
            Mat image02 = Cv2.ImRead(Application.streamingAssetsPath + "/bryce_02.jpg");

            Mat image1 = new Mat(), image2 = new Mat();
            Cv2.CvtColor(image01, image1, ColorConversionCodes.RGB2GRAY);
            Cv2.CvtColor(image02, image2, ColorConversionCodes.RGB2GRAY);

            KeyPoint[] keyPoint1 = null, keyPoint2 = null;

            using (var gFTTDetector = GFTTDetector.Create(500))
            using (var orb = ORB.Create(20))
            using (Mat descriptor1 = new Mat())
            using (Mat descriptor2 = new Mat())
            using (var matcher = new BFMatcher(NormTypes.L2))
            {
                keyPoint1 = gFTTDetector.Detect(image1);
                keyPoint2 = gFTTDetector.Detect(image2);

                orb.Compute(image1, ref keyPoint1, descriptor1);
                orb.Compute(image2, ref keyPoint2, descriptor2);

                List<DMatch> goodMatchePoints = new List<DMatch>();
                DMatch[][] dm = matcher.KnnMatch(descriptor1, descriptor2, 2);

                #region matched 30
                //for (int i = 0; i < dm.Length; i++)
                //{
                //    if (dm[i][0].Distance < 0.6 * dm[i][1].Distance)
                //    {
                //        goodMatchePoints.Add(dm[i][0]);
                //    }
                //}
                #endregion
                #region matched 48
                float minRatio = 1.0f / 1.5f;
                for (int i = 0; i < dm.Length; i++)
                {
                    DMatch bestMatch = dm[i][0];
                    DMatch betterMatch = dm[i][1];

                    float distanceRatio = bestMatch.Distance / betterMatch.Distance;

                    if (distanceRatio < minRatio)
                    {
                        goodMatchePoints.Add(bestMatch);
                    }
                }
                #endregion

                var dstMat = new Mat();
                Debug.Log(string.Format("matchePoints has {0} items", goodMatchePoints.Count));
                Cv2.DrawMatches(image01, keyPoint1, image02, keyPoint2, goodMatchePoints, dstMat);
                t2d = VideoDetectorExample.Utils.MatToTexture2D(dstMat);
            }
            Sprite dst_sp = Sprite.Create(t2d, new UnityEngine.Rect(0, 0, t2d.width, t2d.height), Vector2.zero);
            SrcSprite.sprite = dst_sp;
        }

        void OnFast()
        {
            Mat image01 = Cv2.ImRead(Application.streamingAssetsPath + "/bryce_01.jpg");
            Mat image02 = Cv2.ImRead(Application.streamingAssetsPath + "/bryce_02.jpg");

            Mat image1 = new Mat(), image2 = new Mat();
            Cv2.CvtColor(image01, image1, ColorConversionCodes.RGB2GRAY);
            Cv2.CvtColor(image02, image2, ColorConversionCodes.RGB2GRAY);
            KeyPoint[] keyPoint1 = Cv2.FAST(image1, 50, true);
            KeyPoint[] keyPoint2 = Cv2.FAST(image2, 50, true);
            using (Mat descriptor1 = new Mat())
            using (Mat descriptor2 = new Mat())
            using (var orb = ORB.Create(50))
            using (var matcher = new BFMatcher())
            {
                orb.Compute(image1, ref keyPoint1, descriptor1);
                orb.Compute(image2, ref keyPoint2, descriptor2);
                Debug.Log(string.Format("keyPoints has {0},{1} items.", keyPoint1.Length, keyPoint2.Length));
                Debug.Log(string.Format("descriptor has {0},{1} items.", descriptor1.Rows, descriptor2.Rows));

                List<DMatch> goodMatchePoints = new List<DMatch>();
                var dm = matcher.KnnMatch(descriptor1, descriptor2, 2);

                #region matched 175
                for (int i = 0; i < dm.Length; i++)
                {
                    if (dm[i][0].Distance < 0.6 * dm[i][1].Distance)
                    {
                        goodMatchePoints.Add(dm[i][0]);
                    }
                }
                #endregion

                #region matched 90
                float minRatio = 1.0f / 1.5f;
                for (int i = 0; i < dm.Length; i++)
                {
                    DMatch bestMatch = dm[i][0];
                    DMatch betterMatch = dm[i][1];
                    float distanceRatio = bestMatch.Distance / betterMatch.Distance;
                    if (distanceRatio < minRatio)
                    {
                        goodMatchePoints.Add(bestMatch);
                    }
                }
                #endregion

                var dstMat = new Mat();
                Debug.Log(string.Format("matchePoints has {0} items", goodMatchePoints.Count));
                Cv2.DrawMatches(image01, keyPoint1, image02, keyPoint2, goodMatchePoints, dstMat);
                t2d = Utils.MatToTexture2D(dstMat);
            }

            Sprite dst_sp = Sprite.Create(t2d, new UnityEngine.Rect(0, 0, t2d.width, t2d.height), Vector2.zero);
            SrcSprite.sprite = dst_sp;
        }

        void OnOrb()
        {
            Mat image01 = Cv2.ImRead(Application.streamingAssetsPath + "/bryce_01.jpg");
            Mat image02 = Cv2.ImRead(Application.streamingAssetsPath + "/bryce_02.jpg");

            Mat image1 = new Mat(), image2 = new Mat();
            Cv2.CvtColor(image01, image1, ColorConversionCodes.RGB2GRAY);
            Cv2.CvtColor(image02, image2, ColorConversionCodes.RGB2GRAY);

            KeyPoint[] keyPoint1 = null;
            KeyPoint[] keyPoint2 = null;
            using (ORB orb = ORB.Create(500))
            using (Mat descriptor1 = new Mat())
            using (Mat descriptor2 = new Mat())
            using (var matcher = new BFMatcher())
            {
                orb.DetectAndCompute(image1, new Mat(), out keyPoint1, descriptor1);
                orb.DetectAndCompute(image2, new Mat(), out keyPoint2, descriptor2);
                Debug.Log(string.Format("keyPoints has {0},{1} items.", keyPoint1.Length, keyPoint2.Length));
                Debug.Log(string.Format("descriptor has {0},{1} items.", descriptor1.Rows, descriptor2.Rows));

                List<DMatch> goodMatchePoints = new List<DMatch>();
                var dm = matcher.KnnMatch(descriptor1, descriptor2, 2);

                #region matched 90
                float minRatio = 1.0f / 1.5f;
                for (int i = 0; i < dm.Length; i++)
                {
                    DMatch bestMatch = dm[i][0];
                    DMatch betterMatch = dm[i][1];
                    float distanceRatio = bestMatch.Distance / betterMatch.Distance;
                    if (distanceRatio < minRatio)
                    {
                        goodMatchePoints.Add(bestMatch);
                    }
                }
                #endregion

                var dstMat = new Mat();
                Debug.Log(string.Format("matchePoints has {0} items", goodMatchePoints.Count));
                Cv2.DrawMatches(image01, keyPoint1, image02, keyPoint2, goodMatchePoints, dstMat);
                t2d = Utils.MatToTexture2D(dstMat);
            }

            Sprite dst_sp = Sprite.Create(t2d, new UnityEngine.Rect(0, 0, t2d.width, t2d.height), Vector2.zero);
            SrcSprite.sprite = dst_sp;
            //SrcSprite.preserveAspect = true;
        }
    }
}
