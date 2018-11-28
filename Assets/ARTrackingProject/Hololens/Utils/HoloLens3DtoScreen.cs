using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using OpenCVForUnity;

using System.Linq;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.XR.WSA.Input;
using HoloToolkit.Unity.InputModule;

namespace HoloLensWithOpenCVForUnityExample
{
    /// <summary>
    /// HoloLens Comic Filter Example
    /// An example of image processing (comic filter) using OpenCVForUnity on Hololens.
    /// Referring to http://dev.classmethod.jp/smartphone/opencv-manga-2/.
    /// </summary>
    [RequireComponent(typeof(HololensCameraStreamToMatHelper))]
    public class HoloLens3DtoScreen : ExampleSceneBase
    {
        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The gray pixels.
        /// </summary>
        byte[] grayPixels;

        /// <summary>
        /// The mask pixels.
        /// </summary>
        byte[] maskPixels;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The quad renderer.
        /// </summary>
        /// //new hololens
        Renderer quad_renderer;

        /// <summary>
        /// The web cam texture to mat helper.
        /// </summary>
        HololensCameraStreamToMatHelper webCamTextureToMatHelper;

        OpenCVForUnity.Rect processingAreaRect;

        public Vector2 outsideClippingRatio = new Vector2(0.17f, 0.19f);
        public Vector2 clippingOffset = new Vector2(0.043f, -0.025f);
        public float vignetteScale = 1.5f;



        /// <summary> 
        /// The stored touch point. 6N
        /// </summary>
        Point storedTouchPoint;

        /// <summary>
        /// The selected point list.
        /// </summary>
        List<Point> selectedPointList;

        Rect2d bbox;

        //Tap Event
        GestureRecognizer m_GestureRecognizer;

        bool tap;

        public TextMesh textMesh;
        public TextMesh textScreenGazePos;



        // Use this for initialization
        protected override void Start()
        {
            base.Start();

            webCamTextureToMatHelper = gameObject.GetComponent<HololensCameraStreamToMatHelper>();
#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.frameMatAcquired += OnFrameMatAcquired;
#endif
            webCamTextureToMatHelper.Initialize();

        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();


#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            // HololensCameraStream always returns image data in BGRA format.
            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.BGRA32, false);
#else
            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
#endif

            texture.wrapMode = TextureWrapMode.Clamp;

            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            processingAreaRect = new OpenCVForUnity.Rect((int)(webCamTextureMat.cols() * (outsideClippingRatio.x - clippingOffset.x)), (int)(webCamTextureMat.rows() * (outsideClippingRatio.y + clippingOffset.y)),
                (int)(webCamTextureMat.cols() * (1f - outsideClippingRatio.x * 2)), (int)(webCamTextureMat.rows() * (1f - outsideClippingRatio.y * 2)));
            processingAreaRect = processingAreaRect.intersect(new OpenCVForUnity.Rect(0, 0, webCamTextureMat.cols(), webCamTextureMat.rows()));


            grayMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);

            //

            //HL
            quad_renderer = gameObject.GetComponent<Renderer>() as Renderer;
            quad_renderer.sharedMaterial.SetTexture("_MainTex", texture);
            quad_renderer.sharedMaterial.SetVector("_VignetteOffset", new Vector4(clippingOffset.x, clippingOffset.y));

            Matrix4x4 projectionMatrix;
#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            projectionMatrix = webCamTextureToMatHelper.GetProjectionMatrix ();
            quad_renderer.sharedMaterial.SetMatrix ("_CameraProjectionMatrix", projectionMatrix);
#else

            //19n
            //Matrix4x4 projectionMatrix2 = webCamTextureToMatHelper.GetProjectionMatrix();
            //Matrix4x4 camera2WorldMatrix = webCamTextureToMatHelper.GetCameraToWorldMatrix();

            //HoloLensCameraStream.Resolution _resolution = CameraStreamHelper.Instance.GetLowestResolution();

            //Vector3 imageCenterDirection = LocatableCameraUtils.PixelCoordToWorldCoord(camera2WorldMatrix, projectionMatrix2, _resolution, new Vector2(_resolution.width / 2, _resolution.height / 2));
            //Vector3 imageBotRightDirection = LocatableCameraUtils.PixelCoordToWorldCoord(camera2WorldMatrix, projectionMatrix2, _resolution, new Vector2(_resolution.width, _resolution.height));
            ////_laser.ShootLaserFrom(camera2WorldMatrix.GetColumn(3), imageBotRightDirection, 10f, _botRightMaterial);
            //Debug.Log(imageBotRightDirection);



            //HL
            //This value is obtained from PhotoCapture's TryGetProjectionMatrix() method.I do not know whether this method is good.
            //Please see the discussion of this thread.Https://forums.hololens.com/discussion/782/live-stream-of-locatable-camera-webcam-in-unity
            projectionMatrix = Matrix4x4.identity;
            projectionMatrix.m00 = 2.31029f;
            projectionMatrix.m01 = 0.00000f;
            projectionMatrix.m02 = 0.09614f;
            projectionMatrix.m03 = 0.00000f;
            projectionMatrix.m10 = 0.00000f;
            projectionMatrix.m11 = 4.10427f;
            projectionMatrix.m12 = -0.06231f;
            projectionMatrix.m13 = 0.00000f;
            projectionMatrix.m20 = 0.00000f;
            projectionMatrix.m21 = 0.00000f;
            projectionMatrix.m22 = -1.00000f;
            projectionMatrix.m23 = 0.00000f;
            projectionMatrix.m30 = 0.00000f;
            projectionMatrix.m31 = 0.00000f;
            projectionMatrix.m32 = -1.00000f;
            projectionMatrix.m33 = 0.00000f;
            quad_renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
#endif

            quad_renderer.sharedMaterial.SetFloat("_VignetteScale", vignetteScale);


            float halfOfVerticalFov = Mathf.Atan(1.0f / projectionMatrix.m11);
            float aspectRatio = (1.0f / Mathf.Tan(halfOfVerticalFov)) / projectionMatrix.m00;
            Debug.Log("halfOfVerticalFov " + halfOfVerticalFov);
            Debug.Log("aspectRatio " + aspectRatio);

            //13n
            bbox = new Rect2d();
            selectedPointList = new List<Point>();



            SetupGestureRecognizer();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (grayMat != null)
                grayMat.Dispose();


            //bgMat.Dispose ();
            //dstMat.Dispose ();
            //dstMatClippingROI.Dispose ();

            grayPixels = null;
            maskPixels = null;
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        //alo? cual si se corre?
        //OnFrameMatAcquired == update pero para el HL?
        /*ToDo en Hololens 13n*/
#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
        public void OnFrameMatAcquired (Mat bgraMat, Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix)
        {
            // Not used. If wanted, reverse commit to 16.11

        }

#else

        // Update is called once per frame, in webcam
        void Update()
        {

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                //19n
                //Vector3 posWorld = GazeManager.Instance.HitPosition;
                //textMesh.text = posWorld + "";
                //Vector2 posWorld2Screen = WorldCoordsToScreen(webCamTextureToMatHelper.GetCameraToWorldMatrix(), webCamTextureToMatHelper.GetProjectionMatrix(), posWorld /*not used because of gaze*/);
                //textScreenGazePos.text = posWorld2Screen + "";



                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                //textMesh.text = selectedPointList.Count+"";



                //Here starts the OpenCV script 

                Vector2 posWorld2Screen = WorldCoordsToScreen(webCamTextureToMatHelper.GetCameraToWorldMatrix(), webCamTextureToMatHelper.GetProjectionMatrix(), sphere.transform.position);//19N

                storedTouchPoint = new Point(posWorld2Screen.x, posWorld2Screen.y);
                textMesh.text = storedTouchPoint.x + ";" + storedTouchPoint.y;




                //pintemelo en la screen
                /*
                if (selectedPointList.Count < 2)
                {
                    foreach (var point in selectedPointList)
                    {
                        Imgproc.circle(rgbaMat, point, 6, new Scalar(0, 0, 255), 2);
                    }
                }*/

                Utils.fastMatToTexture2D(rgbaMat, texture);


            }



            if (webCamTextureToMatHelper.IsPlaying())
            {

                Matrix4x4 cameraToWorldMatrix = webCamTextureToMatHelper.GetCameraToWorldMatrix();
                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);

                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
                position *= 1.2f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
            }


            
        }

#endif


        private void OnTouch(Mat img, Point touchPoint)
        {
            if (selectedPointList.Count < 2)
            {
                selectedPointList.Add(touchPoint);
                if (!(new OpenCVForUnity.Rect(0, 0, img.cols(), img.rows()).contains(selectedPointList[selectedPointList.Count - 1])))
                {
                    selectedPointList.RemoveAt(selectedPointList.Count - 1);
                }
            }
        }

        /// <summary>
        /// Converts the screen point to texture point.
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="dstPoint">Dst point.</param>
        /// <param name="texturQuad">Texture quad.</param>
        /// <param name="textureWidth">Texture width.</param>
        /// <param name="textureHeight">Texture height.</param>
        /// <param name="camera">Camera.</param>
        private void ConvertScreenPointToTexturePoint(Point screenPoint, Point dstPoint, GameObject textureQuad, int textureWidth = -1, int textureHeight = -1, Camera camera = null)
        {
            if (textureWidth < 0 || textureHeight < 0)
            {
                Renderer r = textureQuad.GetComponent<Renderer>();
                if (r != null && r.material != null && r.material.mainTexture != null)
                {
                    textureWidth = r.material.mainTexture.width;
                    textureHeight = r.material.mainTexture.height;
                }
                else
                {
                    textureWidth = (int)textureQuad.transform.localScale.x;
                    textureHeight = (int)textureQuad.transform.localScale.y;
                }
            }

            if (camera == null)
                camera = Camera.main;

            Vector3 quadPosition = textureQuad.transform.localPosition;
            Vector3 quadScale = textureQuad.transform.localScale;

            Vector2 tl = camera.WorldToScreenPoint(new Vector3(quadPosition.x - quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
            Vector2 tr = camera.WorldToScreenPoint(new Vector3(quadPosition.x + quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
            Vector2 br = camera.WorldToScreenPoint(new Vector3(quadPosition.x + quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));
            Vector2 bl = camera.WorldToScreenPoint(new Vector3(quadPosition.x - quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));

            using (Mat srcRectMat = new Mat(4, 1, CvType.CV_32FC2))
            using (Mat dstRectMat = new Mat(4, 1, CvType.CV_32FC2))
            {
                srcRectMat.put(0, 0, tl.x, tl.y, tr.x, tr.y, br.x, br.y, bl.x, bl.y);
                dstRectMat.put(0, 0, 0, 0, quadScale.x, 0, quadScale.x, quadScale.y, 0, quadScale.y);

                using (Mat perspectiveTransform = Imgproc.getPerspectiveTransform(srcRectMat, dstRectMat))
                using (MatOfPoint2f srcPointMat = new MatOfPoint2f(screenPoint))
                using (MatOfPoint2f dstPointMat = new MatOfPoint2f())
                {
                    Core.perspectiveTransform(srcPointMat, dstPointMat, perspectiveTransform);

                    dstPoint.x = dstPointMat.get(0, 0)[0] * textureWidth / quadScale.x;
                    dstPoint.y = dstPointMat.get(0, 0)[1] * textureHeight / quadScale.y;
                }
            }
        }


        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.frameMatAcquired -= OnFrameMatAcquired;
#endif
            webCamTextureToMatHelper.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            LoadScene("HoloLensWithOpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.Initialize(null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
        }

        //Tap Events

        GameObject sphere;

        void SetupGestureRecognizer()
        {
            m_GestureRecognizer = new GestureRecognizer();
            m_GestureRecognizer.SetRecognizableGestures(GestureSettings.Tap);
#if UNITY_2017_2_OR_NEWER
            m_GestureRecognizer.Tapped += OnTappedEvent;
#else
            m_GestureRecognizer.TappedEvent += OnTappedEvent;
#endif
            m_GestureRecognizer.StartCapturingGestures();

            //m_CapturingPhoto = false;
        }

#if UNITY_2017_2_OR_NEWER
        void OnTappedEvent(TappedEventArgs args)
#else
        void OnTappedEvent (InteractionSourceKind source, int tapCount, Ray headRay)
#endif
        {
            // Determine if a Gaze pointer is over a GUI.
            if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.transform.name == "Text")
            {
                return;
            }

            if(sphere == null)
            {
                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }

            sphere.transform.position = GazeManager.Instance.HitPosition;


            /*
            Vector2 posWorld2Screen = WorldCoordsToScreen(webCamTextureToMatHelper.GetCameraToWorldMatrix(), webCamTextureToMatHelper.GetProjectionMatrix(), sphere.transform.position);//19N

            storedTouchPoint = new Point(posWorld2Screen.x, posWorld2Screen.y);
            textMesh.text = storedTouchPoint.x + ";" + storedTouchPoint.y;*/

            //Debug.Log(point3D +"punto en el espacio");


            if (selectedPointList.Count < 2)
            {
                selectedPointList.Add(storedTouchPoint);
                /*if (!(new OpenCVForUnity.Rect(0, 0, img.cols(), img.rows()).contains(selectedPointList[selectedPointList.Count - 1])))
                {
                    selectedPointList.RemoveAt(selectedPointList.Count - 1);
                }*/
            }


            Debug.Log(selectedPointList.Count/*"Le dii"*//*"pos pegada"+GazeManager.Instance.HitPosition.x +";"+ GazeManager.Instance.HitPosition.y*/);
            //15n textMesh.text = selectedPointList.Count+1 + "";

        }

        /// <summary>
        /// Helper method for pixel projection into Unity3D world space.
        /// This method return a Vector3 with direction: optical center of the camera to the pixel coordinate
        /// The method is based on: https://developer.microsoft.com/en-us/windows/mixed-reality/locatable_camera#pixel_to_application-specified_coordinate_system
        /// </summary>
        /// <param name="cameraToWorldMatrix">The camera to Unity world matrix.</param>
        /// <param name="projectionMatrix">Projection Matrix.</param>
        /// <param name="pixelCoordinates">The coordinate of the pixel that IS ALREADY converted to world-space.</param>
        /// <param name="cameraResolution">The resolution of the image that the pixel came from.</param>
        /// <returns>Vector3 with direction: optical center to camera world-space coordinates</returns>
        public static Vector3 PixelCoordToWorldCoords(Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix, /*HoloLensCameraStream.Resolution cameraResolution,*/ Vector2 pixelCoordinates)
        {
            //pixelCoordinates = ConvertPixelCoordsToScaledCoords(pixelCoordinates, cameraResolution); // -1 to 1 coords

            float focalLengthX = projectionMatrix.GetColumn(0).x;
            float focalLengthY = projectionMatrix.GetColumn(1).y;
            float centerX = projectionMatrix.GetColumn(2).x;
            float centerY = projectionMatrix.GetColumn(2).y;

            // On Microsoft Webpage the centers are normalized 
            float normFactor = projectionMatrix.GetColumn(2).z;
            centerX = centerX / normFactor;
            centerY = centerY / normFactor;

            Vector3 dirRay = new Vector3((pixelCoordinates.x - centerX) / focalLengthX, (pixelCoordinates.y - centerY) / focalLengthY, 1.0f / normFactor); //Direction is in camera space
            Vector3 direction = new Vector3(Vector3.Dot(cameraToWorldMatrix.GetRow(0), dirRay), Vector3.Dot(cameraToWorldMatrix.GetRow(1), dirRay), Vector3.Dot(cameraToWorldMatrix.GetRow(2), dirRay));

            return direction;
        }


        /*19n From https://docs.microsoft.com/es-es/windows/mixed-reality/locatable-camera#application-specified-coordinate-system-to-pixel-coordinates 
         float4 ImagePosUnnormalized = mul(CameraProjection, float4(CameraSpacePos.xyz, 1)); // use 1 as the W component
         float2 ImagePosProjected = ImagePosUnnormalized.xy / ImagePosUnnormalized.w; // normalize by W, gives -1 to 1 space
         float2 ImagePosZeroToOne = (ImagePosProjected * 0.5) + float2(0.5, 0.5); // good for GPU textures
         int2 PixelPos = int2(ImagePosZeroToOne.x * ImageWidth, (1 - ImagePosZeroToOne.y) * ImageHeight); // good for CPU textures*/
        public static Vector3 WorldCoordsToScreen(Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix, /*HoloLensCameraStream.Resolution cameraResolution,*/ Vector3 worldSpacePos /*not used because of gaze*/)
        {

            Matrix4x4 worldToCamera = Matrix4x4.Inverse(cameraToWorldMatrix);
            Vector4 cameraSpacePos = worldToCamera * new Vector4(worldSpacePos.x, worldSpacePos.y, worldSpacePos.z, 1);
            Debug.Log("cameraSpacePos, si las 2 primeras salen en 0, hice clic al frente");

            Vector4 ImagePosUnnormalized = projectionMatrix * new Vector4(cameraSpacePos.x, cameraSpacePos.y, cameraSpacePos.z, 1); // use 1 as the W component
            Vector2 ImagePosProjected = new Vector2(ImagePosUnnormalized.x / ImagePosUnnormalized.w, ImagePosUnnormalized.y / ImagePosUnnormalized.w); // normalize by W, gives -1 to 1 space
            Vector2 ImagePosZeroToOne = (ImagePosProjected * 0.5f) + new Vector2(0.5f, 0.5f); // good for GPU textures
            Vector2 pixelPos = new Vector2(ImagePosZeroToOne.x * Screen.width, (1 - ImagePosZeroToOne.y) * Screen.height); // good for CPU textures

            return pixelPos;
        }



    }
    

}

