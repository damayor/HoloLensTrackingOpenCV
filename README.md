# TrackingHololens

This is the example for the object tracking for the Hololens using OpenCV. Based on the Enox OpenCVForUnity library.

To use the project we have supported on:

* [HoloToolkit-Unity-2017.4.0.0][HoloToolkit]
* [OpenCV for Unity 2.2.9][OpenCVForUnity]
* [HoloLensCameraStream][CameraStream]	
* [HoloLens With OpenCVForUnity Example]{https://github.com/EnoxSoftware/HoloLensWithOpenCVForUnityExample}

## Build

To develop the project on the Hololens or in the Holographic Emulator we need to: 

1. Pull this Git repository.
2. Add the folders which are in the OpenCVForUnity library and are included on the .gitignore file.
3. Import all the Holotoolkit Unity package.


Capabilities needed on Unity

* WebCam
* Micophone
* VideosLibrary
* SpatialMapping

### Run Holographic emulator

1. Open the Holographic Remoting Player application on the Hollens.
2. Ensure that the ip will be an public ip not a local ip started by 127.0
3. Open the Holographic emulator located in Window Menu
4. Optional, start Debug on Visual Studio
5. Play. The emulation will show 
6. Be Careful! For a new emulation is better disconnect an re conect because Unity can fail. 

### Deploy in the Hololens

1. Build Settings -> Build -> Select a new folder named by your preference
2. Open the Visual Studio Solution
3. Change Debug and ARM with **Release** and** x86**. Select the **Device ** is it will be conncected by USB or **Reomte Machine** if will be by WiFi.
4. Select Start Debugging if you want to bew see the prints console from Unity


## Unity Project

### Scenes for the Hololens

* **Hololens Normal Cam** - just for check we're vieweing and tracking by the Holoelsn Web Cam
* **Hololens MOSSE tracking** -  tracking more confident, fast 60 fps but can lose the tracking times
* **Hololens CSRT tracking** - can track 30 fps but more dynamic	
* **Comic Filter** - A sample from the OpenCVForUnity package



### Scenes for te webcam

#### Video tracking examples
1. Select the algorithm on the Mono Tracking Example Script. 
2. Clic for the first corner of the ROI tracking
3. Second clic for complete the ROI and start tracking
4. Click again if you want another ROI
#### CamTracker


## Common bugs:

* The camera stops, but the UI works: There is something wrong on the OnFrameAquired function. There are some methods can only be called from the main thread.
"Constructors and field initializers will be executed from the loading thread when loading a scene. Don't use this function in the constructor or field initializers, instead move initialization code to the Awake or Start function. "

* Seeing the webcam image and not the hololens cam: you should build the to se the hololens cameras. If you run from the holographic emulation you will always see the webcam render. Furthermor willl run always the Update function and all the code which is under 
* Dont see any change after the unity build: Ensure you add the new scene to the Build Settings
* The holographic emulator never connects
* Release x86
* Is it on?
* Is the hololens plugged?

## Debug the builded Unity Hololens project

1. Check the C# projects checkbox before the build. All the C# files will be builded too, where you can edited and rebuild after the unity build. If you have to make an edition on the uinty project you need to buil it again from unity.
2. Start Debugging lets you see all the Debug.Log logs will be showed.



[HoloToolkit]:https://github.com/Microsoft/MixedRealityToolkit-Unity/releases
[OpenCVForUnity]:https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088
[CameraStream]:https://github.com/VulcanTechnologies/HoloLensCameraStream
[LinkTiberio]:https://sistemasacademico.uniandes.edu.co/~jhernand/dokuwiki/doku.php

2018, Universidad de los Andes - Teknische Universität Kaiserslautern
