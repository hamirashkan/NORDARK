using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class points : MonoBehaviour
{
    Vector3[] coords = new Vector3[40];
    string[] nodesNames = new string[40];
    public Transform point;
    public float minW;
    public float maxW;
    public GameObject line;
    public Graph graph;
    public List<float> lambdaMap;
    public List<Color> colorMap;
    public List<float> costs;

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
            nodeX.obj.GetComponent<Lines>().currentNode = graph.Nodes[i];
            nodeX.obj.GetComponent<Lines>().line = line;
        }
        //H1,  A,  B,  C,  D, H2
        int[,] roads = new int[,] { {  0,  6,  0,  0,  0,  0},//H1
                                    {  5,  0,  8,  0,  0,  0},//A
                                    {  0, 5,  0, 6,  0,  0},//B
                                    {  0,  0, 7,  0, 7, 10},//C
                                    {  0, 8,  0, 7,  0,  0},//D
                                    {  0,  0,  0, 8,  0,  0}};//H2
     




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


        /* for (int i = 0; i < roads.GetLength(0); i++)
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

        } */



        // set H1, H2 as the nodes of POI nodes
        string[] strPOIs = { "H1", "H2"};
        Color[] clrPOIs = { Color.blue, Color.red };

        graph.CreatePOInodes(strPOIs, clrPOIs);

        for (int i =0; i < strPOIs.Length; i++)
        {
            GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.SetColor("_Color", clrPOIs[i]);

        }



        for (int i= 0; i < strPOIs.Length; i++)
        {
            Color AccessColor = GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.color;
            GameObject.Find(strPOIs[i]).GetComponent<Lines>().nColor = AccessColor;
        }

        graph.printNodes();
    }

    void Start()
    {
        GraphSet1("RoadGraph1");
        foreach(Node node in graph.Nodes)
        {
            Color AccessColor = GameObject.Find(node.MostAccessPOI.name).GetComponent<Renderer>().material.color;
            float AccessDist = node.LeastCost;
            costs.Add(AccessDist);
            GameObject.Find(node.name).GetComponent<Renderer>().material.SetColor("_Color", AccessColor);
            node.objTransform.GetComponent<Lines>().nColor = AccessColor;
            node.objTransform.GetComponent<Lines>().dist = AccessDist;
            Debug.Log("Res: " + node.name + node.MostAccessPOI);
        }

        minW = costs.Min();
        maxW = costs.Max();

        Vector3 mapSize = transform.GetComponent<Renderer>().bounds.size;
        Texture2D texture = new Texture2D((int)mapSize.x, (int)mapSize.z);
        GetComponent<Renderer>().material.mainTexture = texture;
        GetComponent<Renderer>().material.mainTexture.filterMode = FilterMode.Trilinear;


        float mindist;
        float lambda;
        float r = 0.003f;
        float alpha = 2f;
        Node bestNode = null;
        for (int z = 0; z <= texture.height; z++)
        {
            for (int x = 0; x <= texture.width; x++)
            {
                mindist = Mathf.Infinity;
                bestNode = null;
                foreach(Node node in graph.RestNodes)
                {
                    Vector3 pos = new Vector3(x-texture.width/2, 0.25f, z-texture.height/2);
                    float dist = (pos - node.vec).magnitude;
                    if(dist < mindist)
                    {
                        mindist = dist;
                        bestNode = node;
                    }
                }

                lambda = (1 / (r * Mathf.Sqrt(2 * Mathf.PI))) * Mathf.Exp(-0.5f * Mathf.Pow((Mathf.Pow((mindist + bestNode.LeastCost), -alpha) / r), 2));  //Gaussian
                //lambda = (1 / r) * 1/(1+Mathf.Exp(Mathf.Pow((mindist + bestNode.LeastCost), -alpha)/r));  //Sigmoid

                lambdaMap.Add(lambda);
                Color col = bestNode.clr;
                colorMap.Add(col);
            }
        }


        float lMin = lambdaMap.Min();
        float lMax = lambdaMap.Max();
        int iter = 0;

        for (int z = 0; z <= texture.height; z++)
        {
            for (int x = 0; x <= texture.width; x++)
            {
                Color col = colorMap[iter];
                col.a = 1 - (lambdaMap[iter] - lMin)/(lMax - lMin);
                texture.SetPixel(-x, -z, col);
                iter += 1;
            }
        }




        texture.Apply();



    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
