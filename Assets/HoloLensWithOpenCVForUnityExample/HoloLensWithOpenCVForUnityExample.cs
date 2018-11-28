using UnityEngine;
using System.Collections;

namespace HoloLensWithOpenCVForUnityExample
{
    public class HoloLensWithOpenCVForUnityExample : ExampleSceneBase
    {
        // Use this for initialization
        protected override void Start ()
        {
            base.Start ();
        }
        
        // Update is called once per frame
        void Update ()
        {
            
        }

        public void OnShowLicenseButtonClick ()
        {
            LoadScene ("ShowLicense");
        }

        public void OnHoloLensPhotoCaptureExampleButtonClick ()
        {
            LoadScene ("HoloLensPhotoCaptureExample");
        }

        public void OnHoloLensComicFilterExampleButtonClick ()
        {
            LoadScene ("HoloLensComicFilterExample");
        }
        

        public void OnHoloLensTrackingExampleButtonClick()
        {
            LoadScene("HoloLensTrackerExample");
        }

        public void OnHoloLensTrackingCSRTButtonClick()
        {
            LoadScene("HoloLensCSRTTracker");
        }

        public void OnHoloLensVideoWriterButtonClick()
        {
            LoadScene("HoloLensVideoWriter");
        }

        public void OnHoloLensSpatialMappingExample()
        {
            LoadScene("SpatialMapping");
        }

        public void OnHoloLensNormalCamExampleButtonClick()
        {
            LoadScene("HoloLensNormalCamExample");
        }
    }
}