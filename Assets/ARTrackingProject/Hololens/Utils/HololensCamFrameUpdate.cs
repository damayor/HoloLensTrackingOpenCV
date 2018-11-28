using HoloLensWithOpenCVForUnityExample;
using OpenCVForUnitySample;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HololensCamFrameUpdate : MonoBehaviour
{
    
    public HololensVideoWriterCV writer;

    bool rightEye;

    // Use this for initialization
    void Start()
    {
        rightEye = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnPostRender()
    {
        if (!rightEye)
        {
            writer.postRenderCalled();
            rightEye = true;
        }
        else
            rightEye = false;
    }


}
