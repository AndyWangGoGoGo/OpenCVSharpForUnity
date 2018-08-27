# OpenCVSharpForUnity
Mainly demonstrated how opencvsharp's image detection function is used in Unity.
There are plugins in the Unity Asset Store which do this  and more,but here is a free solution.
## OpenCVSharp
OpenCvSharp is modeled on the native OpenCV C/C++ API style as much as possible. 
See [opencvsharp](https://github.com/shimat/opencvsharp) for more details

Currently supports windows x86, x64 standalone and Unity editor. If you want to support more platforms, Please consider using [OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088)
## How To
Just open the Unity project in Unity2018+ and try the SampleScene. 
The Utils script provides interconversion between Texture2d and Mat.
## Feature
1、CapturePattern scene demonstrates the ability to capture and save the current image of the camera in the Screenshot folder(Application.persistentDataPath + "/Screenshot/").

2、WebCamDetector scene demonstrates live detection of images stored in the Screenshot directory in camera view.Display matching images in the upper right corner of the window. 
## License
Licensed under the [BSD 3-Clause License](https://github.com/AndyWangGoGoGo/OpenCVSharpForUnity/blob/master/LICENSE).