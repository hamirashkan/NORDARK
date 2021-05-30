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
    public Node currentNode;
    void Start()
    {
        //float minW = GameObject.Find("Map").GetComponent<points_Scene1>().minW;//points
        //float maxW = GameObject.Find("Map").GetComponent<points_Scene1>().maxW;//points
        float minW = GameObject.Find("Mapbox").GetComponent<ShowMap>().minW;//points
        float maxW = GameObject.Find("Mapbox").GetComponent<ShowMap>().maxW;//points

        if (Neighbors.Count != 0)
        {
            for (int i = 0; i < Neighbors.Count; i++)
            {
                Color Nclr;

                if (!currentNode.visited.Contains(Neighbors[i].name))
                {


                    if (dist <= Neighbors[i].LeastCost)
                    {
                        float sWidth = 1 - (dist - minW) / (maxW - minW);
                        float eWidth = 1 - (Neighbors[i].LeastCost - minW) / (maxW - minW);

                        //if (dist == Neighbors[i].LeastCost)
                        //{
                        Neighbors[i].visited.Add(currentNode.name);
                        //   eWidth = sWidth;
                        //}

                        Nclr = nColor;
                        Instantiate(line);
                        l = line.GetComponent<LineRenderer>();

                        List<Vector3> pos = new List<Vector3>();
                        pos.Add(transform.position + new Vector3(0, 0, 0.5f));
                        pos.Add(Neighbors[i].vec + new Vector3(0, 0, 0.5f));

                        l.startWidth = sWidth;
                        l.endWidth = eWidth;
                        l.startColor = Nclr;
                        l.endColor = Nclr;

                        l.SetPositions(pos.ToArray());
                        //l.useWorldSpace = true;
                    }
                    else
                    {
                        if (!Neighbors[i].NeighborNames.Contains(gameObject.name))
                        {
                            float sWidth = 1 - (dist - minW) / (maxW - minW);
                            float eWidth = 1 - (Neighbors[i].LeastCost - minW) / (maxW - minW);

                            Neighbors[i].visited.Add(currentNode.name);

                            Nclr = nColor;
                            Instantiate(line);
                            l = line.GetComponent<LineRenderer>();

                            List<Vector3> pos = new List<Vector3>();
                            pos.Add(transform.position + new Vector3(0, 0, 0.5f));
                            pos.Add(Neighbors[i].vec + new Vector3(0, 0, 0.5f));

                            l.startWidth = sWidth;
                            l.endWidth = eWidth;
                            l.startColor = Nclr;
                            l.endColor = Nclr;

                            l.SetPositions(pos.ToArray());
                        }
                        else
                        {
                            float sWidth = 1 - (dist - minW) / (maxW - minW);
                            float eWidth = 1 - (Neighbors[i].LeastCost - minW) / (maxW - minW);

                            Nclr = nColor;
                            Instantiate(line);
                            l = line.GetComponent<LineRenderer>();

                            List<Vector3> pos = new List<Vector3>();
                            pos.Add(transform.position + new Vector3(0, 0, -0.5f));
                            pos.Add(Neighbors[i].vec + new Vector3(0, 0, -0.5f));

                            l.startWidth = sWidth;
                            l.endWidth = eWidth;
                            if (nColor != Neighbors[i].clr)
                            {
                                l.startWidth = eWidth;
                                l.endWidth = sWidth;
                            }
                            l.startColor = Nclr;
                            l.endColor = Nclr;

                            l.SetPositions(pos.ToArray());
                        }
                    }
                }
                else
                {
                    float sWidth = 1 - (dist - minW) / (maxW - minW);
                    float eWidth = 1 - (Neighbors[i].LeastCost - minW) / (maxW - minW);

                    //if (dist == Neighbors[i].LeastCost)
                    //{
                    //Neighbors[i].visited.Add(currentNode.name);
                    //   eWidth = sWidth;
                    //}

                    Nclr = nColor;
                    Instantiate(line);
                    l = line.GetComponent<LineRenderer>();

                    List<Vector3> pos = new List<Vector3>();
                    pos.Add(transform.position + new Vector3(0,0, -0.5f));
                    pos.Add(Neighbors[i].vec + new Vector3(0, 0, -0.5f));

                    l.startWidth = sWidth;
                    l.endWidth = eWidth;
                    if (nColor != Neighbors[i].clr)
                    {
                        l.startWidth = eWidth;
                        l.endWidth = sWidth;
                    }
                    l.startColor = Nclr;
                    l.endColor = Nclr;

                    l.SetPositions(pos.ToArray());
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {


    }
}
