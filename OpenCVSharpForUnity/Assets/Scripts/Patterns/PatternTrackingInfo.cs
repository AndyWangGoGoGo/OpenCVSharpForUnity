using OpenCvSharp;
using UnityEngine;

namespace VideoDetectorExample
{
    public class PatternTrackingInfo
    {
        public Mat Homography;
        public MatOfPoint2f Points2d;
        public Matrix4x4 Pose3d;

        public PatternTrackingInfo()
        {
            Homography = new Mat();
            Points2d = new MatOfPoint2f();
            Pose3d = new Matrix4x4();
        }

        public void ComputePose(Pattern pattern, Mat camMatrix, MatOfDouble distCoeff)
        {
            Mat Rvec = new Mat();
            Mat Tvec = new Mat();
            Mat raux = new Mat();
            Mat taux = new Mat();

            Cv2.SolvePnP(pattern.points3d, Points2d, camMatrix, distCoeff, raux, taux);
            raux.ConvertTo(Rvec,  MatType.CV_32F);
            taux.ConvertTo(Tvec, MatType.CV_32F);

            Mat rotMat = new Mat(3, 3, MatType.CV_64FC1);
            Cv2.Rodrigues(Rvec, rotMat);

            Pose3d.SetRow(0, new Vector4((float)rotMat.GetArray(0,0)[0], (float)rotMat.GetArray(0, 1)[0], (float)rotMat.GetArray(0, 2)[0], (float)Tvec.GetArray(0, 0)[0]));
            Pose3d.SetRow(1, new Vector4((float)rotMat.GetArray(1, 0)[0], (float)rotMat.GetArray(1, 1)[0], (float)rotMat.GetArray(1, 2)[0], (float)Tvec.GetArray(1, 0)[0]));
            Pose3d.SetRow(2, new Vector4((float)rotMat.GetArray(2, 0)[0], (float)rotMat.GetArray(2, 1)[0], (float)rotMat.GetArray(2, 2)[0], (float)Tvec.GetArray(2, 0)[0]));
            Pose3d.SetRow(3, new Vector4(0, 0, 0, 1));

            Rvec.Dispose();
            Tvec.Dispose();
            raux.Dispose();
            taux.Dispose();
            rotMat.Dispose();
        }

        public void Draw2dContour(Mat image, Scalar color)
        {
            Point2f[] points2dArray = Points2d.ToArray();

            for (int i = 0; i < points2dArray.Length; i++)
            {
                Cv2.Line(image, points2dArray[i], points2dArray[(i + 1) % points2dArray.Length], color, 2, LineTypes.AntiAlias, 0);
            }
        }
    }
}