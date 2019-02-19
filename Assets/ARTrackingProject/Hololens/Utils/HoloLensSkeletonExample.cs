using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using OpenCVForUnity;

using System.Linq;

namespace HoloLensWithOpenCVForUnityExample
{
    /// <summary>
    /// HoloLens Skeleton example
    /// An example of image on the Hololens. Shows what the cam sees
    /// </summary>
    [RequireComponent(typeof(HololensCameraStreamToMatHelper))]
    public class HoloLensSkeletonExample : ExampleSceneBase
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


       // public TextMesh textMesh;


        // Use this for initialization
        protected override void Start ()
        {
            base.Start ();

            webCamTextureToMatHelper = gameObject.GetComponent<HololensCameraStreamToMatHelper> ();
            #if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.frameMatAcquired += OnFrameMatAcquired;
            #endif
            webCamTextureToMatHelper.Initialize ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");
        
            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();


            #if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            // HololensCameraStream always returns image data in BGRA format.
            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.BGRA32, false);
            #else
            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);
            #endif

            texture.wrapMode = TextureWrapMode.Clamp;

            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
        

            processingAreaRect = new OpenCVForUnity.Rect ((int)(webCamTextureMat.cols ()*(outsideClippingRatio.x - clippingOffset.x)), (int)(webCamTextureMat.rows ()*(outsideClippingRatio.y + clippingOffset.y)),
                (int)(webCamTextureMat.cols ()*(1f-outsideClippingRatio.x*2)), (int)(webCamTextureMat.rows ()*(1f-outsideClippingRatio.y*2)));
            processingAreaRect = processingAreaRect.intersect (new OpenCVForUnity.Rect(0,0,webCamTextureMat.cols (),webCamTextureMat.rows ()));


            grayMat =  new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);
          
            //

            //HL
            quad_renderer = gameObject.GetComponent<Renderer> () as Renderer;
            quad_renderer.sharedMaterial.SetTexture ("_MainTex", texture);
            quad_renderer.sharedMaterial.SetVector ("_VignetteOffset", new Vector4(clippingOffset.x, clippingOffset.y));

            Matrix4x4 projectionMatrix;
#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            projectionMatrix = webCamTextureToMatHelper.GetProjectionMatrix ();
            quad_renderer.sharedMaterial.SetMatrix ("_CameraProjectionMatrix", projectionMatrix);
#else



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
            quad_renderer.sharedMaterial.SetMatrix ("_CameraProjectionMatrix", projectionMatrix);
            #endif

            quad_renderer.sharedMaterial.SetFloat ("_VignetteScale", vignetteScale);


            float halfOfVerticalFov = Mathf.Atan (1.0f / projectionMatrix.m11);
            float aspectRatio = (1.0f / Mathf.Tan (halfOfVerticalFov)) / projectionMatrix.m00;
            Debug.Log ("halfOfVerticalFov " + halfOfVerticalFov);
            Debug.Log ("aspectRatio " + aspectRatio);

            //
            //Imgproc.rectangle (dstMat, new Point (0, 0), new Point (webCamTextureMat.width (), webCamTextureMat.height ()), new Scalar (126, 126, 126, 255), -1);
            //
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

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
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode){
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        //Update for the Hololens
        /*9n */
#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
        public void OnFrameMatAcquired (Mat bgraMat, Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix)
        {
            Mat bgraMatClipROI = new Mat(bgraMat, processingAreaRect);

            Imgproc.cvtColor (bgraMat /*bgraMatClipROI*/, grayMat, Imgproc.COLOR_BGRA2GRAY);

           //// Here goes the code


            //new HL
            UnityEngine.WSA.Application.InvokeOnAppThread(() => {

                if (!webCamTextureToMatHelper.IsPlaying ()) return;

                Utils.fastMatToTexture2D(bgraMat, texture);
                bgraMat.Dispose ();

                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix ("_WorldToCameraMatrix", worldToCameraMatrix);

                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn (3) - cameraToWorldMatrix.GetColumn (2);
                position *= 1.2f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation (-cameraToWorldMatrix.GetColumn (2), cameraToWorldMatrix.GetColumn (1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;

            }, false);
        }

#else

        // Update for unity and holographic emulation
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                //Mat rgbaMatClipROI = new Mat(rgbaMat, processingAreaRect);

                Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                // todo lo nuevo

                

              //9n  Imgproc.cvtColor(dstMat, rgbaMat, Imgproc.COLOR_GRAY2RGBA);

                //
                //Imgproc.rectangle (rgbaMat, new Point (0, 0), new Point (rgbaMat.width (), rgbaMat.height ()), new Scalar (255, 0, 0, 255), 4);
                //Imgproc.rectangle (rgbaMat, processingAreaRect.tl(), processingAreaRect.br(), new Scalar (255, 0, 0, 255), 4);
                //

                //
                Utils.fastMatToTexture2D(rgbaMat, texture);

               // rgbaMatClipROI.Dispose ();
            }


            if (webCamTextureToMatHelper.IsPlaying ()) {

                Matrix4x4 cameraToWorldMatrix = webCamTextureToMatHelper.GetCameraToWorldMatrix();
                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix ("_WorldToCameraMatrix", worldToCameraMatrix);

                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn (3) - cameraToWorldMatrix.GetColumn (2);
                position *= 1.2f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation (-cameraToWorldMatrix.GetColumn (2), cameraToWorldMatrix.GetColumn (1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
            }

        }
        #endif

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            #if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.frameMatAcquired -= OnFrameMatAcquired;
            #endif
            webCamTextureToMatHelper.Dispose ();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            LoadScene ("HoloLensTrackingProject");
        }
/// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            webCamTextureToMatHelper.Initialize (null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
        }

        /// <summary>
        /// TakeScreenshot and save
        /// </summary>
        public void OnTakeScreenshotButtonClick()
        {
           // webCamTextureToMatHelper.Initialize(null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
            ScreenCapture.CaptureScreenshot("Screenshots/Holog-" + System.DateTime.Now.ToString("dMHmm") + ".png");

        }




    }
}