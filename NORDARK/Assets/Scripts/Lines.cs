using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lines : MonoBehaviour
{
    public GameObject line;
    LineRenderer l;
    public int index = 0;
    public List<Node> Neighbors;
    public List<float> Weights;
    public Color nColor;
    public float dist;
    void Start()
    {
        int minW = GameObject.Find("Map").GetComponent<points>().minW;
        int maxW = GameObject.Find("Map").GetComponent<points>().maxW;

        if (Neighbors.Count != 0)
        {
            for (int i = 0; i < Neighbors.Count; i++)
            {
                Color Nclr;
                if(dist < Neighbors[i].LeastCost)
                    
                    {
                        float sWidth = 1 - (dist - minW)/(maxW-minW);
                        float eWidth = 1- (Neighbors[i].LeastCost - minW) / (maxW - minW);
                        Nclr = nColor;
                        Instantiate(line);
                        l = line.GetComponent<LineRenderer>();

                        List<Vector3> pos = new List<Vector3>();
                        pos.Add(transform.position);
                        pos.Add(Neighbors[i].vec);

                        l.startWidth = sWidth;
                        l.endWidth = eWidth;
                        l.startColor = Nclr;
                        l.endColor = Nclr;

                        l.SetPositions(pos.ToArray());
                        //l.useWorldSpace = true;
                }
                else {
                    if (!Neighbors[i].NeighborNames.Contains(gameObject.name))
                    {
                        float sWidth = 1 - (dist - minW) / (maxW - minW);
                        float eWidth = 1 - (Neighbors[i].LeastCost - minW) / (maxW - minW);

                        Nclr = nColor;
                        Instantiate(line);
                        l = line.GetComponent<LineRenderer>();

                        List<Vector3> pos = new List<Vector3>();
                        pos.Add(transform.position);
                        pos.Add(Neighbors[i].vec);

                        l.startWidth = sWidth;
                        l.endWidth = eWidth;
                        l.startColor = Nclr;
                        l.endColor = Nclr;

                        l.SetPositions(pos.ToArray());
                    }

                    //Nclr = Neighbors[i].objTransform.GetComponent<Lines>().nColor;
                }
            
            }
        }
    }

    // Update is called once per frame
    void Update()
    {


    }
}
