using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaSelector : MonoBehaviour
{
    public GameObject projObject;
    GameObject clone;

    // GameObject plane;

    void Start()
    {
        // currently clones at scene origin. Can be changed to spawn only when 'SHADOW MAP UI' is clicked.
        clone = Instantiate(projObject, new Vector3(0,0,0),Quaternion.Euler(90,0,0));

    }


    void Update()
    {
       Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
       RaycastHit hit;
       if(Physics.Raycast(ray, out hit)){
           clone.transform.position = new Vector3(hit.point.x,50.0f,hit.point.z);
       }
    }
}
