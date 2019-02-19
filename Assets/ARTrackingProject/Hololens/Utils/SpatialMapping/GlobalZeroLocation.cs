using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalZeroLocation : MonoBehaviour {

    //public TextMesh zeroLayer;
    public TextMesh headGlobalPosLayer;

    public Vector3 globalPosition;

    //public Vector3 globalZeroTo0; //offset anchor //toDo

    public GameObject arCamera;



    // Use this for initialization
    void Start () {

    }

    // Update is called once per frame
    void Update () {

        //getcomponent<Placeable>
        //que el primero que sea movido, ps see sea el cero

        // GlobalZeroLocation zero = GetComponent<GlobalZeroLocation>();
        //headGlobalPosLayer.text = "Global:" + globalPosition.ToString("F2");

        //PosQfrom0
        Transform currentPos = arCamera.transform;

        ////PosQ from 1
        globalPosition = currentPos.position /*- globalZeroTo0*/; //Vector from Origin0 to Origin1, o1-o0
        Debug.Log("GZL global position: " + globalPosition.ToString("F2"));
        headGlobalPosLayer.text = "Global:" + globalPosition.ToString("F2");



    }
}
