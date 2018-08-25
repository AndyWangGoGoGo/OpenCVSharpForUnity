using OpenCvSharp;
using System.Collections.Generic;

namespace VideoDetectorExample
{
    public class PatternDetector
    {
        public bool enableRatioTest;
        public bool enableHomographyRefinement;
        public float homographyReprojectionThreshold;

        KeyPoint[] m_queryKeypoints;
        Mat m_queryDescriptors;
        Mat m_grayImg;
        Mat m_warpedImg;
        Mat m_roughHomography;
        Mat m_refinedHomography;
        Pattern m_pattern;
        ORB m_detector;
        ORB m_extractor;
        KeyPoint[] warpedKeypoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternDetector"/> class.
        /// </summary>
        /// <param name="detector">Detector.</param>
        /// <param name="extractor">Extractor.</param>
        /// <param name="matcher">Matcher.</param>
        /// <param name="ratioTest">If set to <c>true</c> ratio test.</param>
        public PatternDetector(ORB detector, ORB extractor, bool ratioTest = false)
        {
            if (detector == null)
            {
                detector = ORB.Create();
                detector.MaxFeatures = 1000;
            }
            if (extractor == null)
            {
                extractor = ORB.Create();
                extractor.MaxFeatures = 1000;
            }

            m_detector = detector;
            m_extractor = extractor;

            enableRatioTest = ratioTest;
            enableHomographyRefinement = true;
            homographyReprojectionThreshold = 3;

            m_queryKeypoints = new KeyPoint[] { };
            m_queryDescriptors = new Mat();

            m_grayImg = new Mat();
            m_warpedImg = new Mat();
            m_roughHomography = new Mat();
            m_refinedHomography = new Mat();

            warpedKeypoints = new KeyPoint[] { };
        }

        /// <summary>
        /// Then we add vector of descriptors (each descriptors matrix describe one image). 
        /// This allows us to perform search across multiple images.
        /// </summary>
        private void Train(Pattern pattern)
        {
            pattern.bfMatcher.Clear();

            List<Mat> descriptors = new List<Mat>(1);
            descriptors.Add(pattern.descriptors.Clone());
            pattern.bfMatcher.Add(descriptors);
            pattern.bfMatcher.Train();
        }

        public void BuildPatternFromImage(Mat image, Pattern pattern)
        {
            // Store original image in pattern structure
            pattern.size = new Size(image.Cols, image.Rows);
            pattern.frame = image.Clone();
            GetGray(image, pattern.grayImg);

            // Build 2d and 3d contours (3d contour lie in XY plane since it's planar)
            List<Point2f> points2dList = new List<Point2f>(4);
            List<Point3f> points3dList = new List<Point3f>(4);

            // Image dimensions
            float w = image.Cols;
            float h = image.Rows;

            // Normalized dimensions:
            points2dList.Add(new Point2f(0, 0));
            points2dList.Add(new Point2f(w, 0));
            points2dList.Add(new Point2f(w, h));
            points2dList.Add(new Point2f(0, h));

            pattern.points2d = MatOfPoint2f.FromArray(points2dList);

            points3dList.Add(new Point3f(-0.5f, -0.5f, 0));
            points3dList.Add(new Point3f(+0.5f, -0.5f, 0));
            points3dList.Add(new Point3f(+0.5f, +0.5f, 0));
            points3dList.Add(new Point3f(-0.5f, +0.5f, 0));

            pattern.points3d = MatOfPoint3f.FromArray(points3dList);

            ExtractFeatures(pattern.grayImg, ref pattern.keypoints, pattern.descriptors);
            Train(pattern);
        }

        public bool FindPattern(Mat image, Pattern targetPattern)
        {
            GetGray(image, m_grayImg);
            ExtractFeatures(m_grayImg, ref m_queryKeypoints, m_queryDescriptors);

            List<DMatch> matches = GetMatches(targetPattern.bfMatcher, m_queryDescriptors, targetPattern.descriptors);

            bool homographyFound = RefineMatchesWithHomography
                (
                m_queryKeypoints, 
                targetPattern.keypoints,
                homographyReprojectionThreshold,
                matches.ToArray(),
                m_roughHomography
                );

            if (homographyFound)
            {
                if (enableHomographyRefinement)
                {
                    Cv2.WarpPerspective(m_grayImg, m_warpedImg, m_roughHomography, targetPattern.size, InterpolationFlags.WarpInverseMap | InterpolationFlags.Cubic);
                    ExtractFeatures(m_warpedImg, ref warpedKeypoints, m_queryDescriptors);
                    List<DMatch> reMatches = GetMatches(targetPattern.bfMatcher, m_queryDescriptors, targetPattern.descriptors);
                    homographyFound = RefineMatchesWithHomography
                        (
                        warpedKeypoints,
                        targetPattern.keypoints,
                        homographyReprojectionThreshold,
                        reMatches.ToArray(),
                        m_refinedHomography
                        );
                }
            }
            return homographyFound;
        }

        static void GetGray(Mat image, Mat gray)
        {
            if (image.Channels() == 3)
                Cv2.CvtColor(image, gray, ColorConversionCodes.RGB2GRAY);
            else if (image.Channels() == 4)
                Cv2.CvtColor(image, gray, ColorConversionCodes.RGBA2GRAY);
            else if (image.Channels() == 1)
                image.CopyTo(gray);
        }

        /// <summary>
        /// Extracts the features.
        /// </summary>
        /// <returns><c>true</c>, if features was extracted, <c>false</c> otherwise.</returns>
        bool ExtractFeatures(Mat image, ref KeyPoint[] keypoints, Mat descriptors)
        {
            if (image.Total() == 0)
            {
                return false;
            }
            if (image.Channels() != 1)
            {
                return false;
            }

            keypoints = m_detector.Detect(image);
            if (keypoints.Length == 0)
                return false;

            m_extractor.Compute(image, ref keypoints, descriptors);
            if (keypoints.Length == 0)
                return false;

            return true;
        }

        /// <summary>
        /// To avoid NaN's when best match has zero distance we will use inversed ratio.
        /// KNN match will return 2 nearest matches for each query descriptor
        /// </summary>
        List<DMatch> GetMatches(BFMatcher matcher, Mat queryDescriptors, Mat trainDescriptors)
        {
            List<DMatch> matchesList = new List<DMatch>();
            if (enableRatioTest)
            {
                 
                float minRatio = 1.0f / 1.5f;
                DMatch[][] dm = matcher.KnnMatch(queryDescriptors, trainDescriptors, 2);

                for (int i = 0; i < dm.Length; i++)
                {
                    DMatch bestMatch = dm[i][0];
                    DMatch betterMatch = dm[i][1];

                    float distanceRatio = bestMatch.Distance / betterMatch.Distance;

                    if (distanceRatio < minRatio)
                    {
                        matchesList.Add(bestMatch);
                    }
                }
            }
            else
            {
                matchesList.AddRange(matcher.Match(queryDescriptors, trainDescriptors));
            }
            return matchesList;
        }

        static bool RefineMatchesWithHomography(
            KeyPoint[] queryKeypoints,
            KeyPoint[] trainKeypoints,
            float reprojectionThreshold,
            DMatch[] matches,
            Mat homography
        )
        {
            int minNumberMatchesAllowed = 8;

            if (matches.Length < minNumberMatchesAllowed)
                return false;

            List<Point2f> srcPointsList = new List<Point2f>(matches.Length);
            List<Point2f> dstPointsList = new List<Point2f>(matches.Length);

            for (int i = 0; i < matches.Length; i++)
            {
                srcPointsList.Add(trainKeypoints[matches[i].TrainIdx].Pt);
                dstPointsList.Add(queryKeypoints[matches[i].QueryIdx].Pt);
            }

            using (MatOfPoint2f srcPoints = MatOfPoint2f.FromArray(srcPointsList))
            using (MatOfPoint2f dstPoints = MatOfPoint2f.FromArray(dstPointsList))
            using (MatOfByte inliersMask = new MatOfByte())
            {
                Cv2.FindHomography(srcPoints, dstPoints, HomographyMethods.Ransac, reprojectionThreshold, inliersMask).CopyTo(homography);

                if (homography.Rows != 3 || homography.Cols != 3)
                    return false;

                byte[] inliersMaskList = inliersMask.ToArray();

                List<DMatch> inliers = new List<DMatch>();
                for (int i = 0; i < inliersMaskList.Length; i++)
                {
                    if (inliersMaskList[i] == 1)
                        inliers.Add(matches[i]);
                }

                matches = inliers.ToArray();
            }
            return matches.Length > minNumberMatchesAllowed;
        }

        public virtual void Dispose()
        {
            if (m_queryDescriptors != null)
            {
                m_queryDescriptors.Dispose();
            }
            if (m_grayImg != null)
            {
                m_grayImg.Dispose();
            }
            if (m_warpedImg != null)
            {
                m_grayImg.Dispose();
            }
            if (m_roughHomography != null)
            {
                m_roughHomography.Dispose();
            }
            if (m_refinedHomography != null)
            {
                m_roughHomography.Dispose();
            }

            if (m_detector != null)
            {
                m_detector.Dispose();
            }
            if (m_extractor != null)
            {
                m_extractor.Dispose();
            }
        }
    }
}