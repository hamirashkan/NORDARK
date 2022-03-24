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

    public GameObject newline;
    private ShowMap sm;

    public void init()
    {
        Start();
    }
    void Start()
    {

        //float minW = GameObject.Find("Map").GetComponent<points_Scene1>().minW;//points
        //float maxW = GameObject.Find("Map").GetComponent<points_Scene1>().maxW;//points
        float minW = GameObject.Find("Mapbox").GetComponent<ShowMap>().minW;//points
        float maxW = GameObject.Find("Mapbox").GetComponent<ShowMap>().maxW;//points
        // Build 0012
        sm = GameObject.Find("Mapbox").GetComponent<ShowMap>();
        //

        if (Neighbors.Count != 0)
        {
            for (int i = 0; i < Neighbors.Count; i++)
            {
                Color Nclr;
                Vector3 offset = new Vector3(0, 0, 0);
                int flag = 0;

                if (!currentNode.visited.Contains(Neighbors[i].name))
                {
                    if (dist <= Neighbors[i].LeastCost)
                    {
                        offset = new Vector3(0, 0, 0.5f);
                        flag = 1;
                    }
                    else
                    {
                        if (!Neighbors[i].NeighborNames.Contains(gameObject.name))
                        {
                            offset = new Vector3(0, 0, 0.5f);
                            flag = 1;
                        }
                        else
                        {
                            offset = new Vector3(0, 0, -0.5f);
                            flag = 2;

                        }
                    }
                }
                else
                {
                    offset = new Vector3(0, 0, -0.5f);
                    flag = 2;
                }

                // Build 0022, node merge by image mapping
                if (sm.dropdown_graphop.value >= 4)
                    offset = new Vector3(0, 0, 0);

                float sWidth = 1 - (dist - minW) / (maxW - minW);
                float eWidth = 1 - (Neighbors[i].LeastCost - minW) / (maxW - minW);

                //if (dist == Neighbors[i].LeastCost)
                //{
                Neighbors[i].visited.Add(currentNode.name);
                //   eWidth = sWidth;
                //}

                Nclr = nColor;
                newline = Instantiate(line);
                l = newline.GetComponent<LineRenderer>();
                newline.transform.parent = GameObject.Find("Edges").transform;
                Graph.LinesNum = Graph.LinesNum + 1;
                newline.transform.name = "Line" + Graph.LinesNum.ToString() + "(" + currentNode.name + "," + Neighbors[i].name + ")";

                List<Vector3> pos = new List<Vector3>();
                pos.Add(currentNode.vec + offset); // Build 0021, transform.position 
                pos.Add(Neighbors[i].vec + offset);

                // Build 0013, alesund graph
                if (sm.dropdown_graphop.value < 6)
                {
                    l.startWidth = sWidth;
                    l.endWidth = eWidth;

                    if (flag == 2)
                    {
                        if (nColor != Neighbors[i].clr)
                        {
                            l.startWidth = eWidth;
                            l.endWidth = sWidth;
                        }
                    }
                    l.startColor = Nclr;
                    l.endColor = Nclr;
                }
                else
                {
                    l.startWidth = 0.1f;
                    l.endWidth = 0.1f;

                    l.startColor = Color.red;
                    l.endColor = Color.red;
                }   
                //

                
                l.SetPositions(pos.ToArray());
                //Build 0012, more nodes for edges, search LinesNum
                if (sm.dropdown_graphop.value >= 3)
                {
                    foreach (AuxLine x in sm.AuxLines.FindAll(element => element.LineName == (currentNode.index + "_" + Neighbors[i].index)))
                    {
                        var posv = new Vector3[x.AuxNodes.Count + 2];
                        posv[0] = currentNode.vec + offset; // Build 0021, transform.position
                        //Debug.Log("Find nodes by index[" + nodeIndex.ToString() + "]: " + x.name);
                        for (int j = 0; j < x.AuxNodes.Count; j++)
                            posv[j + 1] = x.AuxNodes[j];
                        posv[x.AuxNodes.Count + 1] = Neighbors[i].vec + offset;
                        l.positionCount = posv.Length;
                        l.SetPositions(posv);
                    }
                }
                //l.useWorldSpace = true;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {


    }
}
