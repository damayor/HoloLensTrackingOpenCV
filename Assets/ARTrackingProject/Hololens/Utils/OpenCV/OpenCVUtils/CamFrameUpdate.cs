using OpenCVForUnitySample;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFrameUpdate : MonoBehaviour
{
    
    public CamWriterExample writer;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnPostRender()
    {
        writer.postRenderCalled();
    }


}
