using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformCoordinates : MonoBehaviour {

    public Vector3 positionTracked;

    public Vector2 terrain;

    public GameObject ARcamera;

    private static float rateZX =  0.09211f/ 0.2008f;  //x0/z0

    private static float rateZY =  0.0644f/ 0.2008f;  //y0/z0

    public TextMesh consoleUIText;

    
    // Use this for initialization
    void Start () {
       // positionTracked = transform.position;
	}
	
	// Update is called once per frame
	void Update () {

        float x02 = transform.position.z * rateZX;
        float y02 = transform.position.z * rateZY;

        Vector3 zeroSistemaScreen = new Vector3(x02, y02, 0);

        //P01->P02 + P02 -> Pp2

        //Vector3 newCoord =  zeroSistemaScreen + transform.position;

        // Debug.Log(positionTracked.x - x02 + ","+ - y02 + positionTracked.y);
        // Debug.Log(newCoord.ToString("F4"));
        //28n consoleUIText.text = /*newCoord.ToString("F4") + "\n"+*/ ARcamera.transform.position.ToString("F3");
        Debug.Log("TC - ARCam parent pos " + ARcamera.transform.position.ToString("F2"));


        //Vector3 currentPos = GetComponent<GlobalZeroLocation>().globalZeroTo0;



    }

    public Vector2 getPosInTerrein()
    {
        Vector2 truePos = new Vector2(0,0);

        return truePos;
    }
}
