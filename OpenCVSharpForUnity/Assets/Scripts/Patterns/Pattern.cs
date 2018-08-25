using OpenCvSharp;

namespace VideoDetectorExample
{
    public class Pattern
    {
        public BFMatcher bfMatcher;
        public Size size;
        public Mat frame;
        public Mat grayImg;
        public KeyPoint[] keypoints;
        public Mat descriptors;
        public MatOfPoint2f points2d;
        public MatOfPoint3f points3d;
        /// <summary>
        /// Initializes a new instance of the <see cref="Pattern"/> class.
        /// </summary>
        public Pattern()
        {
            bfMatcher = new BFMatcher();
            size = new Size();
            frame = new Mat();
            grayImg = new Mat();
            keypoints = new KeyPoint[] { };
            descriptors = new Mat();
            points2d = new MatOfPoint2f();
            points3d = new MatOfPoint3f();
        }
    }
}