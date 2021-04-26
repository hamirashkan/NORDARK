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


        if (Neighbors.Count != 0)
        {
            for (int i = 0; i < Neighbors.Count; i++)
            {
                Color Nclr;
                if(dist < Neighbors[i].LeastCost)
                    
                    {
                        Nclr = nColor;
                        Instantiate(line);
                        l = line.GetComponent<LineRenderer>();

                        List<Vector3> pos = new List<Vector3>();
                        pos.Add(transform.position);
                        pos.Add(Neighbors[i].vec);

                        l.startWidth = 1;
                        l.endWidth = 0.2f;
                        l.startColor = Nclr;
                        l.endColor = Nclr;

                        l.SetPositions(pos.ToArray());
                        //l.useWorldSpace = true;
                }
                else {
                    if (!Neighbors[i].NeighborNames.Contains(gameObject.name))
                    {
                        Nclr = nColor;
                        Instantiate(line);
                        l = line.GetComponent<LineRenderer>();

                        List<Vector3> pos = new List<Vector3>();
                        pos.Add(transform.position);
                        pos.Add(Neighbors[i].vec);

                        l.startWidth = 1;
                        l.endWidth = 0.2f;
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
