using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using TMPro; //UI(BRV)

public class HeatmapIns : MonoBehaviour
{
    public GameObject pointObj;
    //public GameObject UIobj; //Interface with UI (BRV)
    public Vector3 objPos;
    public float HeatmapSteps = 10; 


    // Update is called once per frame
    void Update()
    {
        //activate with mouse click

        if (Input.GetMouseButtonDown(1))
        {
            HeatmapSteps = GameObject.Find("ShadowMapUI").GetComponent<UIScript>().heatmapSize;

            if (gameObject.transform.childCount > 0)
            {
                Destroy(gameObject.GetComponent<Heatmap>());
                foreach (Transform child in gameObject.transform)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }

            RaycastHit hit;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int layerMask = 1 << 8;
            if (Physics.Raycast(ray, out hit, layerMask))
            {
                objPos = hit.point;
                gameObject.AddComponent<Heatmap>().hmPoint = pointObj;
            }
        }


        //variable update form UI
       // HeatmapSteps = UIobj.GetComponent<UIScript>().heatmapSize;
        //Debug.Log(HeatmapSteps);
    }
}
