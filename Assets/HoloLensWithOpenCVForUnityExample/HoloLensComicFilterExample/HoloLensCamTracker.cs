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
    public class HoloLensCamTracker : ExampleSceneBase
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

        string[] tracker_types = { "BOOSTING", "MIL", "KCF", "TLD", "MEDIANFLOW", "GOTURN", "MOSSE", "CSRT" };

        Tracker monotracker;

        Rect2d bbox;

        public TrackerType tracker_type ;

        //Tap Event
        GestureRecognizer m_GestureRecognizer;

        bool tap;

        public TextMesh textMesh;



        // Use this for initialization
        protected override void Start()
        {
            base.Start();

            webCamTextureToMatHelper = gameObject.GetComponent<HololensCameraStreamToMatHelper>();
        #if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.frameMatAcquired += OnFrameMatAcquired;
        #endif
            webCamTextureToMatHelper.Initialize();

            //tracker_type = TrackerType.KCF;

            /*
            if (tracker_type == TrackerType.Boosting)
                monotracker = TrackerBoosting.create();
            if (tracker_type == TrackerType.MIL)
                monotracker = TrackerMIL.create();
            if (tracker_type == TrackerType.KCF)
                monotracker = TrackerKCF.create();
            if (tracker_type == TrackerType.TLD)
                monotracker = TrackerTLD.create();
            if (tracker_type == TrackerType.MedianFlow)
                monotracker = TrackerMedianFlow.create();
            if (tracker_type == TrackerType.CSRT)
                monotracker = TrackerCSRT.create();*/
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
            monotracker = TrackerKCF.create();
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
            Mat rgbaMat = webCamTextureToMatHelper.GetMat();

            Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

           // textMesh.text = selectedPointList.Count+"";
             textMesh.text = "onFrameAquired";

            //eso toca cambiarlo14n
            /*if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
                //Debug.Log ("mouse X " + Input.mousePosition.x);
                //Debug.Log ("mouse Y " + Input.mousePosition.y);
            }*/
            Debug.Log("YEEEEEEEI Entró al frame update por el HL");
            //textMesh.text = ":D";


            //14N PM DEL HL A ver si sirve
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                //Here starts the OpenCV script 

                //onTouch para el 1er clic
                if (selectedPointList.Count == 1)
                {
                    if (storedTouchPoint != null)
                    {
                        ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, rgbaMat.cols(), rgbaMat.rows());

                        // OnTouch(rgbaMat, storedTouchPoint); //rgab o gray¡
                        Debug.Log("primer clic por " + storedTouchPoint.x + ";" + storedTouchPoint.y);
                        storedTouchPoint = null;
                    }
                }

                //error PlayerLoop called recursively! on iOS.reccomend WebCamTexture.
                if (selectedPointList.Count != 1)
                {
                    //onTouch para el 2do clic
                    if (storedTouchPoint != null)
                    {
                        ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, rgbaMat.cols(), rgbaMat.rows());
                        //OnTouch(rgbaMat, storedTouchPoint);
                        storedTouchPoint = null;
                    }

                    //si ya es 2, creeme otra trackinArea
                    if (selectedPointList.Count < 2)
                    {
                        foreach (var point in selectedPointList)
                        {
                            Imgproc.circle(rgbaMat, point, 6, new Scalar(0, 0, 255), 2);
                        }
                    }
                    else
                    {
                        //key line! DM
                        using (MatOfPoint selectedPointMat = new MatOfPoint(selectedPointList.ToArray()))
                        {
                            OpenCVForUnity.Rect region = Imgproc.boundingRect(selectedPointMat); //si se necesita al fin y al cabo
                                                                                                 //8n multitracker trackers.add (TrackerKCF.create (), rgbaMat, new Rect2d (region.x, region.y, region.width, region.height)); //tocomment soon

                            monotracker = TrackerKCF.create(); //8n
                            monotracker.init(grayMat, new Rect2d(region.x, region.y, region.width, region.height));
                        }
                        selectedPointList.Clear(); //comentela pa que no resetee el init
                                                   //8n trackingColorList.Add (new Scalar (UnityEngine.Random.Range (0, 255), UnityEngine.Random.Range (0, 255), UnityEngine.Random.Range (0, 255))); //le pone color
                    }


                    // aca ya no lo inicializa, update los antesriores
                    //trackers.update (rgbaMat, objects);
                    Rect2d previousBox = bbox;
                    bool updated = monotracker.update(grayMat, bbox);//8n //pero nunca le pasa la bbox ni las coordenadas, unicamente al momento de tracker.init
                    if (updated)
                    {
                        Debug.Log("tracking por aca:" + bbox.x + ";" + bbox.y);
                        Imgproc.rectangle(rgbaMat, bbox.tl(), bbox.br(), new Scalar(255, 255, 255, 255), 2, 1, 0); //8n
                    }
                    else
                    {
                        //Debug.Log("Se perdio en:" + bbox.x + ";" + bbox.y);
                        Imgproc.rectangle(rgbaMat, previousBox.tl(), previousBox.br(), new Scalar(255, 0, 0, 255), 2, 1, 0); //8n
                    }
         

                }
            }

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

        // Update is called once per frame, in webcam
        void Update()
        {

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                //textMesh.text = selectedPointList.Count+"";

                //version con el mouse
                if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
                    Debug.Log ("mouse X " + Input.mousePosition.x);
                    Debug.Log ("mouse Y " + Input.mousePosition.y);
                }
                Debug.Log("Entró al update");

                //Here starts the OpenCV script 

                //onTouch para el 1er clic
                if (selectedPointList.Count == 1)
                {
                    if (storedTouchPoint != null)
                    {
                        ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, rgbaMat.cols(), rgbaMat.rows());

                        // OnTouch(rgbaMat, storedTouchPoint); //rgab o gray¡
                        Debug.Log("primer clic por " + storedTouchPoint.x + ";" + storedTouchPoint.y);
                        
                        storedTouchPoint = null;
                    }
                }

                //error PlayerLoop called recursively! on iOS.reccomend WebCamTexture.
                if (selectedPointList.Count != 1)
                {
                    //onTouch para el 2do clic
                    if (storedTouchPoint != null)
                    {
                        ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, rgbaMat.cols(), rgbaMat.rows());
                        //OnTouch(rgbaMat, storedTouchPoint);
                        storedTouchPoint = null;
                    }

                    //si ya es 2, creeme otra trackinArea
                    if (selectedPointList.Count < 2)
                    {
                        foreach (var point in selectedPointList)
                        {
                            Imgproc.circle(rgbaMat, point, 6, new Scalar(0, 0, 255), 2);
                        }
                    }
                    else
                    {
                        //key line! DM
                        using (MatOfPoint selectedPointMat = new MatOfPoint(selectedPointList.ToArray()))
                        {
                            OpenCVForUnity.Rect region = Imgproc.boundingRect(selectedPointMat); //si se necesita al fin y al cabo

                            SelectTracker(tracker_type);   //16n monotracker = TrackerKCF.create(); //8n
                            monotracker.init(grayMat, new Rect2d(region.x, region.y, region.width, region.height));
                        }
                        selectedPointList.Clear(); //comentela pa que no resetee el init
                                                    //8n trackingColorList.Add (new Scalar (UnityEngine.Random.Range (0, 255), UnityEngine.Random.Range (0, 255), UnityEngine.Random.Range (0, 255))); //le pone color
                    }


                    // aca ya no lo inicializa, update los antesriores
                    //trackers.update (rgbaMat, objects);
                    Rect2d previousBox = bbox;
                    bool updated = monotracker.update(grayMat, bbox);//8n //pero nunca le pasa la bbox ni las coordenadas, unicamente al momento de tracker.init
                    if (updated)
                    {
                        Debug.Log("tracking por aca:" + bbox.x + ";" + bbox.y);
                        Imgproc.rectangle(rgbaMat, bbox.tl(), bbox.br(), new Scalar(255, 255, 255, 255), 2, 1, 0); //8n
                    }
                    else
                    {
                        //Debug.Log("Se perdio en:" + bbox.x + ";" + bbox.y);
                        Imgproc.rectangle(rgbaMat, previousBox.tl(), previousBox.br(), new Scalar(255, 0, 0, 255), 2, 1, 0); //8n
                    }
                    //                bool updated = trackers.update (rgbaMat, objects);
                    //                Debug.Log ("updated " + updated);
                    //                if (!updated && objects.rows () > 0) {
                    //                    OnResetTrackerButtonClick ();
                    //                }

                }

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


            /*
            // 14n pm pa q aquired corriera
            if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
                //Debug.Log ("mouse X " + Input.mousePosition.x);
                //Debug.Log ("mouse Y " + Input.mousePosition.y);
            }

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);


                //Here starts the OpenCV script 

                //onTouch para el 1er clic
                if (selectedPointList.Count == 1)
            {
                textMesh.text = selectedPointList.Count + "";

                if (storedTouchPoint != null)
                {
                    ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, rgbaMat.cols(), rgbaMat.rows());

                    OnTouch(rgbaMat, storedTouchPoint); //rgab o gray¡
                    Debug.Log("primer clic por " + storedTouchPoint.x + ";" + storedTouchPoint.y);
                    storedTouchPoint = null;

                }
            }

            //error PlayerLoop called recursively! on iOS.reccomend WebCamTexture.
            if (selectedPointList.Count != 1)
            {
                //onTouch para el 2do clic
                if (storedTouchPoint != null)
                {
                    ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, rgbaMat.cols(), rgbaMat.rows());
                    OnTouch(rgbaMat, storedTouchPoint);
                    storedTouchPoint = null;
                }

                //si ya es 2, cree otra trackinArea
                if (selectedPointList.Count < 2)
                {
                    foreach (var point in selectedPointList)
                    {
                        Imgproc.circle(rgbaMat, point, 6, new Scalar(0, 0, 255), 2);
                    }
                }
                else
                {
                    //key line! DM
                    using (MatOfPoint selectedPointMat = new MatOfPoint(selectedPointList.ToArray()))
                    {
                        OpenCVForUnity.Rect region = Imgproc.boundingRect(selectedPointMat); //si se necesita al fin y al cabo
                                                                                             //8n multitracker trackers.add (TrackerKCF.create (), rgbaMat, new Rect2d (region.x, region.y, region.width, region.height)); //tocomment soon

                        monotracker = TrackerKCF.create(); //8n
                        monotracker.init(grayMat, new Rect2d(region.x, region.y, region.width, region.height));
                        textMesh.text = selectedPointList.Count + "track";

                    }

                    selectedPointList.Clear(); //comentela pa que no resetee el init
                }


                // aca ya no lo inicializa, update los antesriores
                //trackers.update (rgbaMat, objects);
                Rect2d previousBox = bbox;
                bool updated = monotracker.update(grayMat, bbox);//8n //pero nunca le pasa la bbox ni las coordenadas, unicamente al momento de tracker.init
                if (updated)
                {
                    //Debug.Log("tracking por aca:" + bbox.x + ";" + bbox.y);
                    Imgproc.rectangle(rgbaMat, bbox.tl(), bbox.br(), new Scalar(255, 255, 255, 255), 2, 1, 0); //8n
                }
                else
                {
                    //Debug.Log("Se perdio en:" + bbox.x + ";" + bbox.y);
                    Imgproc.rectangle(rgbaMat, previousBox.tl(), previousBox.br(), new Scalar(255, 0, 0, 255), 2, 1, 0); //8n

                }
                //                bool updated = trackers.update (rgbaMat, objects);
                //                Debug.Log ("updated " + updated);
                //                if (!updated && objects.rows () > 0) {
                //                    OnResetTrackerButtonClick ();
                //                }


                // commented by me 13n
                //if (selectedPointList.Count != 1)
                //{
                //    //Imgproc.putText (rgbaMat, "Please touch the screen, and select tracking regions.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                //    if (fpsMonitor != null)
                //    {
                //        fpsMonitor.consoleText = "Touch the screen to select a new tracking region.";
                //    }
                //}
                //else
                //{
                //    //Imgproc.putText (rgbaMat, "Please select the end point of the new tracking region.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                //    if (fpsMonitor != null)
                //    {
                //        fpsMonitor.consoleText = "Please select the end point of the new tracking region.";
                //    }
                //}

                //

                Utils.fastMatToTexture2D(rgbaMat, texture);

                // rgbaMatClipROI.Dispose ();
            }
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
            }*/
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

            storedTouchPoint = new Point(  GazeManager.Instance.HitPosition.x, GazeManager.Instance.HitPosition.y);
            textMesh.text = storedTouchPoint.x + ";" + storedTouchPoint.y;

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

        public void SelectTracker(TrackerType tracker_type)
        {
            if (tracker_type == TrackerType.Boosting)
                monotracker = TrackerBoosting.create();
            if (tracker_type == TrackerType.MIL)
                monotracker = TrackerMIL.create();
            if (tracker_type == TrackerType.KCF)
                monotracker = TrackerKCF.create();
            if (tracker_type == TrackerType.TLD)
                monotracker = TrackerTLD.create();
            if (tracker_type == TrackerType.MedianFlow)
                monotracker = TrackerMedianFlow.create();
            if (tracker_type == TrackerType.CSRT)
                monotracker = TrackerCSRT.create();
            if (tracker_type == TrackerType.MOSSE)
                monotracker = TrackerMOSSE.create();
        }

    }


    public enum TrackerType
    {
        Boosting = 1, MIL = 2, KCF = 3, TLD = 4, MedianFlow = 5, CSRT = 6, GOTURN = 7, MOSSE = 8
    }
}

