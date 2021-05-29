using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heatmap : MonoBehaviour
{
    //setting up parameters
    [HideInInspector]
    public int epoch = 0;
    [HideInInspector]
    public int steps;
    //public int[,,] ShadowData;
    //public int[,] ShadowHM;
    //public int[,] ChangeHM;
    public GameObject hmPoint;
    public Vector3 origin;

    // Start is called before the first frame update
    void Start()
    {
        //initialization
        origin = gameObject.GetComponent<HeatmapIns>().objPos;
        steps = (int)gameObject.GetComponent<HeatmapIns>().HeatmapSteps; //square
        //ShadowData = new int[steps + 1, steps + 1, 1000]; //xsteps,zsteps,timesteps (should be user dependent)
        //ShadowHM = new int[steps + 1, steps + 1];
        //ChangeHM = new int[steps + 1, steps + 1];

        InvokeRepeating("ShadowMap", 0, 0.2f);
    }

    void ShadowMap()
    {
        float pX = origin.x - (steps / 2);
        float pZ = origin.z - (steps / 2);
        GameObject tempPoint;
        GameObject Sun = GameObject.Find("RealSun");
        Vector3 SunPos = Sun.transform.position;


        //resetting the heatmap for each time step
        if(gameObject.transform.childCount > 0)
        {
            foreach (Transform child in gameObject.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        //calculating the heatmap
        for (int i = 0; i <= steps; i++)
        {
            for (int j = 0; j <= steps; j++)
            {
                //terrain height
                RaycastHit hit;
                Vector3 RayOrigin0 = new Vector3(pX + i, 1000, pZ + j);
                Vector3 RayDir0 = Vector3.down;
                int layerMask = 1 << 8;
                if (Physics.Raycast(RayOrigin0, RayDir0, out hit, Mathf.Infinity, layerMask))
                {
                    RaycastHit hit2;
                    Vector3 RayOrigin = new Vector3(pX + i, hit.point.y, pZ + j);
                    Vector3 RayDir = SunPos - RayOrigin;
                    if (Physics.Raycast(RayOrigin, RayDir, out hit2, Mathf.Infinity))
                    {
                        //ShadowData[i, j, epoch] = 1;
                        tempPoint = Instantiate(hmPoint);
                        tempPoint.transform.position = new Vector3(pX + i, hit.point.y, pZ + j);
                        tempPoint.GetComponent<Renderer>().material.color = Color.red;
                        tempPoint.transform.SetParent(gameObject.transform);
                    }
                    else
                    {
                        //ShadowData[i, j, epoch] = 0;
                        tempPoint = Instantiate(hmPoint);
                        tempPoint.transform.position = new Vector3(pX + i, hit.point.y, pZ + j);
                        tempPoint.GetComponent<Renderer>().material.color = Color.green;
                        tempPoint.transform.SetParent(gameObject.transform);
                    }

                    //ShadowHM[i, j] += ShadowData[i, j, epoch];

                    //if (epoch > 0 && ShadowData[i, j, epoch] != ShadowData[i, j, epoch - 1])
                    //{
                    //    ChangeHM[i, j] += 1;
                    //}
                }

            }
        }
        epoch += 1;
    }
}
