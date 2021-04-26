using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class points : MonoBehaviour
{
    Vector3[] coords = new Vector3[40];
    string[] nodesNames = new string[40];
    public Transform point;
    public int minW;
    public int maxW;
    public GameObject line;
    public Graph graph;
    // Start is called before the first frame update

    public void GraphSet1(string graphName)
    {
        graph = Graph.Create(graphName);
        float y = 0.25f;// xOz plane is the map 2D coordinates
        int nodesNum = 6;
        nodesNames = new string[nodesNum];
        coords = new Vector3[nodesNum];
        nodesNames[0] = "H1";   coords[0] = new Vector3(-20, y, 0);
        nodesNames[1] = "A";    coords[1] = new Vector3(-10, y, 0);
        nodesNames[2] = "B";    coords[2] = new Vector3(-2, y, 6);
        nodesNames[3] = "C";    coords[3] = new Vector3(8, y, 0);
        nodesNames[4] = "D";    coords[4] = new Vector3(5, y, -7);
        nodesNames[5] = "H2";   coords[5] = new Vector3(18, y, 0);
        for (int i = 0; i < nodesNames.Length; i++)
        {
            Node nodeX = Node.Create<Node>(nodesNames[i], coords[i]);
            nodeX.index = i;
            graph.AddNode(nodeX);
            nodeX.objTransform = Instantiate(point);
            nodeX.obj = nodeX.objTransform.gameObject;
            nodeX.objTransform.name = nodeX.name;
            nodeX.objTransform.position = nodeX.vec;

            nodeX.obj.GetComponent<Lines>().index = i;
            nodeX.obj.GetComponent<Lines>().Neighbors = graph.Nodes[i].Neighbors;
            nodeX.obj.GetComponent<Lines>().Weights = graph.Nodes[i].Weights;
            nodeX.obj.GetComponent<Lines>().line = line;
        }
        //H1,  A,  B,  C,  D, H2
        int[,] roads = new int[,] { {  0,  6,  0,  0,  0,  0},//H1
                                    {  5,  0,  8,  0,  20,  0},//A
                                    {  0, 10,  0, 21,  0,  0},//B
                                    {  0,  0, 25,  0, 15, 12},//C
                                    {  0, 0,  0, 14,  0,  0},//D
                                    {  0,  0,  0, 11,  0,  0}};//H2
     




        graph.roadcosts = roads;
        


        for (int i = 0; i < roads.GetLength(0); i++)
        {
            for (int j = 0; j < roads.GetLength(1); j++)
            {
                float weight = roads[i, j];
                if (weight != 0)
                {
                    Node nodeX = graph.FindNode(i);
                    if (nodeX != null)
                    {
                        nodeX.Neighbors.Add(graph.FindNode(j));
                        nodeX.NeighborNames.Add(graph.FindNode(j).name);
                        nodeX.Weights.Add(roads[i, j]);
                    }
                }
            }
        }


        minW = 0;
        maxW = 0;
        for (int i = 0; i < roads.GetLength(0); i++)
        {
            for (int j = 0; j < roads.GetLength(1); j++)
            {
                //Assign current array element to max, if (arr[i,j] > max)
                if (roads[i, j] > maxW)
                {
                    maxW = roads[i, j];
                }

                //Assign current array element to min if if (arr[i,j] < min)
                if (roads[i, j] < minW)
                {
                    minW = roads[i, j];
                }

            }

        }



        // set H1, H2 as the nodes of POI nodes
        string[] strPOIs = { "H1", "H2"};
        Color[] clrPOIs = { Color.blue, Color.red };
        graph.CreatePOInodes(strPOIs, clrPOIs);


        for (int i= 0; i < strPOIs.Length; i++)
        {
            GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.SetColor("_Color", clrPOIs[i]);
        }

        graph.printNodes();
    }

    void Start()
    {
        GraphSet1("RoadGraph1");
        //foreach(Node node in graph.Nodes)
        //{
        //    if (node.MostAccessPOI != null)
        //    {
        //        Color AccessColor = GameObject.Find(node.MostAccessPOI.name).GetComponent<Renderer>().material.color;
        //        float AccessDist = node.LeastCost;
        //        GameObject.Find(node.name).GetComponent<Renderer>().material.SetColor("_Color", AccessColor);
        //        node.objTransform.GetComponent<Lines>().nColor = AccessColor;
        //        node.objTransform.GetComponent<Lines>().dist = AccessDist;
        //        Debug.Log("Res: " + node.name + node.MostAccessPOI);
        //    }
        //}

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
