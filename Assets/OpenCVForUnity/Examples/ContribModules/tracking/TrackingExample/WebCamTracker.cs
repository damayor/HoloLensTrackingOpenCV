using UnityEngine;
using UnityEngine.EventSystems;

using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Skeleton to start developing on OpenCVForUnity
    ///OpenCV program for testing different trackers.
    ///  It is based on code from [https://www.learnopencv.com/object-tracking-using-opencv-cpp-python/]
    /// 
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class WebCamTracker : MonoBehaviour
    // It depends of is used the webcam (WebCamTextureToMatHelper) or a video (VideoCapture
    {
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;


        /// <summary> 
        /// The stored touch point. 6N
        /// </summary>
        Point storedTouchPoint;


        string[] tracker_types = { "BOOSTING", "MIL", "KCF", "TLD", "MEDIANFLOW", "GOTURN", "MOSSE", "CSRT" };

        Tracker monotracker;

        Rect2d bbox;

        Rect2d previousBox;


        /// <summary>
        /// The selected point list.
        /// </summary>
        List<Point> selectedPointList;

        public TrackerType tracker_type;

        bool trackerInitialized;


#if UNITY_ANDROID && !UNITY_EDITOR
                float rearCameraRequestedFPS;
#endif

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, pixel 2)
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            rearCameraRequestedFPS = webCamTextureToMatHelper.requestedFPS;
            if (webCamTextureToMatHelper.requestedIsFrontFacing) {                
                webCamTextureToMatHelper.requestedFPS = 15;
                webCamTextureToMatHelper.Initialize ();
            } else {
                webCamTextureToMatHelper.Initialize ();
            }
#else
            webCamTextureToMatHelper.Initialize();
#endif
            //SelectTracker(tracker_type);
         
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);

            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", webCamTextureMat.width().ToString());
                fpsMonitor.Add("height", webCamTextureMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }

            monotracker = TrackerKCF.create(); //8n
            bbox = new Rect2d();
            previousBox = new Rect2d();
            selectedPointList = new List<Point>();

            grayMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");
            //ToCheck
            if (grayMat != null)
                grayMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update()
        {

           
            /// Mouse interaction, not developed yet
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            //Touch
            int touchCount = Input.touchCount;
            if (touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                if(t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject(t.fingerId)){
                    storedTouchPoint = new Point (t.position.x, t.position.y);
                    //Debug.Log ("touch X " + t.position.x);
                    //Debug.Log ("touch Y " + t.position.y);
                }
            }
#else
            //Mouse
            if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
                //Debug.Log ("mouse X " + Input.mousePosition.x);
                //Debug.Log ("mouse Y " + Input.mousePosition.y);
            }
        #endif


            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();
                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                
                //Here starts the OpenCV script 

                //onTouch para el 1er clic
                if (selectedPointList.Count == 1)
                {
                    if (storedTouchPoint != null)
                    {
                        ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, rgbaMat.cols(), rgbaMat.rows());
                        
                        OnTouch(rgbaMat, storedTouchPoint); //rgab o gray¡
                        Debug.Log("primer clic por "+storedTouchPoint.x +";"+ storedTouchPoint.y);
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
                            //TODO 13n cambiar essa region por varias regiones, que le pasara
                            OpenCVForUnity.Rect region = Imgproc.boundingRect(selectedPointMat); //si se necesita al fin y al cabo                                                                   

                            SelectTracker(tracker_type);   //16n monotracker = TrackerKCF.create(); //8n
                            trackerInitialized = monotracker.init(grayMat, new Rect2d(region.x, region.y, region.width, region.height));
                        }
                        selectedPointList.Clear(); 
                    }


                    // aca ya no lo inicializa, sino que lo actualiza
                    if (trackerInitialized)
                    {
                        bool updated = monotracker.update(grayMat, bbox);//8n //pero nunca le pasa la bbox ni las coordenadas, unicamente al momento de tracker.init
                        if (bbox.width != 0 && bbox.height != 0)
                        {    
                            Debug.Log("tracking por aca:" + bbox.x + ";" + bbox.y);
                            Imgproc.rectangle(rgbaMat, bbox.tl(), bbox.br(), new Scalar(255, 255, 255, 255), 2, 1, 0);
                            previousBox = new Rect2d(bbox.x, bbox.y, bbox.width, bbox.height);
                        }
                        else
                        {
                            Debug.Log("Se perdio en:" + previousBox.x + ";" + previousBox.y);
                            Imgproc.rectangle(rgbaMat, previousBox.tl(), previousBox.br(), new Scalar(255, 0, 0, 255), 2, 1, 0); //8n


                        }
                    }
                    //     bool updated = trackers.update (rgbaMat, objects);
                    //     Debug.Log ("updated " + updated);
                    //     if (!updated && bbox.width > 0)
                    //         OnResetTrackerButtonClick ();
                    //     }


                    if (selectedPointList.Count != 1)
                    {
                        //Imgproc.putText (rgbaMat, "Please touch the screen, and select tracking regions.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        if (fpsMonitor != null)
                        {
                            fpsMonitor.consoleText = "Touch the screen to select a new tracking region.";
                        }
                    }
                    else
                    {
                        //Imgproc.putText (rgbaMat, "Please select the end point of the new tracking region.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        if (fpsMonitor != null)
                        {
                            fpsMonitor.consoleText = "Please select the end point of the new tracking region.";
                        }
                    }

                    Utils.matToTexture2D(rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors());

                    //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                }
            }
        }


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


        /**
        ******  Buttons 
        **/


        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
#if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene("OpenCVForUnityExample");
#else
            Application.LoadLevel ("OpenCVForUnityExample");
#endif
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
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!webCamTextureToMatHelper.IsFrontFacing ()) {
                rearCameraRequestedFPS = webCamTextureToMatHelper.requestedFPS;
                webCamTextureToMatHelper.Initialize (!webCamTextureToMatHelper.IsFrontFacing (), 15, webCamTextureToMatHelper.rotate90Degree);
            } else {                
                webCamTextureToMatHelper.Initialize (!webCamTextureToMatHelper.IsFrontFacing (), rearCameraRequestedFPS, webCamTextureToMatHelper.rotate90Degree);
            }
#else
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
#endif
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

        public enum TrackerType
        {
            Boosting = 1, MIL = 2, KCF = 3, TLD = 4, MedianFlow = 5, CSRT = 6, GOTURN = 7, MOSSE = 8
        }

        
#if UNITY_2017_2_OR_NEWER
       // void OnTappedEvent(TappedEventArgs args)
#else
        void OnTappedEvent (InteractionSourceKind source, int tapCount, Ray headRay)

        {
            // Determine if a Gaze pointer is over a GUI.
            if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.transform.name == "Text")
            {
                return;
            }

            if (sphere == null)
            {
                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }

            sphere.transform.position = GazeManager.Instance.HitPosition;
            storedTouchPoint = WorldCoordsToScreen(webCamTextureToMatHelper.GetCameraToWorldMatrix(), webCamTextureToMatHelper.GetProjectionMatrix(), sphere.transform.position);//19N

            //Debug.Log(point3D +"punto en el espacio");

            /*
            if (selectedPointList.Count < 2)
            {
                selectedPointList.Add(storedTouchPoint);
                /*if (!(new OpenCVForUnity.Rect(0, 0, img.cols(), img.rows()).contains(selectedPointList[selectedPointList.Count - 1])))
                {
                    selectedPointList.RemoveAt(selectedPointList.Count - 1);
                }
            }*/

            //Debug.Log(selectedPointList.Count/*"Le dii"*//*"pos pegada"+GazeManager.Instance.HitPosition.x +";"+ GazeManager.Instance.HitPosition.y*/);
            //15n textMesh.text = selectedPointList.Count+1 + "";

        }
#endif
    }

}
