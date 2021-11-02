using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;

public class points_Scene1 : MonoBehaviour
{
    Vector3[] coords = new Vector3[40];
    string[] nodesNames = new string[40];
    public int timeSteps;
    public Transform point;
    public float minW;
    public float maxW;
    public GameObject line;
    public Graph graph;
    public List<float> lambdaMap;
    public List<Color> colorMap;
    public List<float> costs;
    public AbstractMap map;

    // Start is called before the first frame update

    public void GraphSet1(string graphName)
    {
        graph = Graph.Create(graphName);
        float y = 0.25f;// xOz plane is the map 2D coordinates
        int nodesNum = 6;
        nodesNames = new string[nodesNum];
        coords = new Vector3[nodesNum];
        nodesNames[0] = "H1";   coords[0] = new Vector3(-40, y, 0);
        nodesNames[1] = "A";    coords[1] = new Vector3(-20, y, 0);
        nodesNames[2] = "B";    coords[2] = new Vector3(-4, y, 12);
        nodesNames[3] = "C";    coords[3] = new Vector3(16, y, 0);
        nodesNames[4] = "D";    coords[4] = new Vector3(10, y, -14);
        nodesNames[5] = "H2";   coords[5] = new Vector3(36, y, 0);
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
        map = GameObject.Find("Mapbox").GetComponent<AbstractMap>();

        GraphSet2("RoadGraph1");
        foreach (Node node in graph.Nodes)
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
        float r = 0.005f;
        float alpha = 2f;
        Node bestNode = null;
        for (int z = 0; z <= texture.height; z++)
        {
            for (int x = 0; x <= texture.width; x++)
            {
                mindist = Mathf.Infinity;
                bestNode = null;
                foreach (Node node in graph.RestNodes)
                {
                    Vector3 pos = new Vector3(x - texture.width / 2, 0.25f, z - texture.height / 2);
                    float dist = (pos - node.vec).magnitude;
                    if (dist < mindist)
                    {
                        mindist = dist;
                        bestNode = node;
                    }
                }

                //lambda = (1 / (r * Mathf.Sqrt(2 * Mathf.PI))) * Mathf.Exp(-0.5f * Mathf.Pow((Mathf.Pow((mindist + bestNode.LeastCost), -alpha) / r), 2));  //Gaussian
                lambda = (1 / r) * 1 / (1 + Mathf.Exp(Mathf.Pow((mindist + bestNode.LeastCost), -alpha) / r));  //Sigmoid

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
                col.a = 1 - (lambdaMap[iter] - lMin) / (lMax - lMin);
                texture.SetPixel(-x, -z, col);
                iter += 1;
            }
        }




        texture.Apply();



    }

    public void GraphSet2(string graphName)
    {
        graph = Graph.Create(graphName);
        float y = 0.25f;// xOz plane is the map 2D coordinates

        string text = loadFile("Assets/Resources/stops.txt");
        string[] lines = Regex.Split(text, "\n");

        int nodesNum = 25;//lines.Length - 2;//nbStops
        nodesNames = new string[nodesNum];
        coords = new Vector3[nodesNum];

        Debug.Log(DateTime.Now.ToString() + ", init started");
        //float center_lat = 62f;
        //float center_lon = 6f;
        //float scale = 100f;
        // test1. very fast, 1 sec could do 5K times or more
        //for (int i = 0; i < nodesNames.Length*100; i++)
        //{
        //    Instantiate(point);
        //    if (i % 5000 == 0)
        //        Debug.Log(DateTime.Now.ToString() + ", inited " + i + "_th nodes");
        //}
        for (int i = 0; i < nodesNames.Length; i++)
        {
            string rowdata = lines[i + 1];

            //if (!rowdata.Contains("NSR:Quay"))
            //{
            //    continue;
            //}

            string[] quotes = Regex.Split(rowdata, "\"");
            if (quotes.Length > 1)
            {
                rowdata = quotes[0] + quotes[2];
            }

            string[] values = Regex.Split(rowdata, ",");
            //string id = values[0];
            float lat = float.Parse(values[4], System.Globalization.CultureInfo.InvariantCulture);
            float lon = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture);

            nodesNames[i] = values[2];
            Vector2 latlong = new Vector2(lat, lon);
            Vector3 pos = latlong.AsUnityPosition(map.CenterMercator, map.WorldRelativeScale);
            //coords[i] = new Vector3((lat - center_lat) * scale, y, (lon - center_lon) * scale);
            coords[i] = pos;

            Node nodeX = Node.Create<Node>(nodesNames[i], coords[i]);
            nodeX.index = i;
            nodeX.stop_id = values[0];
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

            if (i % 50 == 0)
                Debug.Log(DateTime.Now.ToString() + ", inited " + i + "_th nodes");
        }

        timeSteps = 10;//200
        graph.timeSteps = timeSteps;

        int[][,] temporalRoad = Enumerable.Range(0, timeSteps).Select(_ => new int[nodesNames.Length, nodesNames.Length]).ToArray();

        int[,] roads = new int[nodesNames.Length, nodesNames.Length];

        graph.roadcosts = roads;
        graph.roadTemporal = temporalRoad;

        System.Random rnd = new System.Random();

        for (int k = 0; k < timeSteps; k++)
        {
            //for (int i = 0; i < nodesNames.Length; i++)
            //{
            //    for (int j = 0; j < nodesNames.Length; j++)
            //    {
            //        if (rnd.Next(1, 10) >= 8) // 70% = 0
            //            temporalRoad[k][i, j] = rnd.Next(1, 20);
            //            roads[i, j] += temporalRoad[k][i, j];
            //    }
            //}
            int high = 100;//500;//0
            int low = 0;
            temporalRoad[k][0, 13] = rnd.Next(low, high) + 40;//Line 1 (1<=>14) (40, 39)
            temporalRoad[k][13, 0] = rnd.Next(low, high) + 39;
            temporalRoad[k][13, 9] = rnd.Next(low, high) + 10;//Line 2 (14<=>10) (10, 11)
            temporalRoad[k][9, 13] = rnd.Next(low, high) + 11;
            temporalRoad[k][13, 22] = rnd.Next(low, high) + 100;//Line 3 (14<=>23) (100, 110)
            temporalRoad[k][22, 13] = rnd.Next(low, high) + 110;
            temporalRoad[k][6, 3] = rnd.Next(low, high) + 8;//Line 4 (7<=>4) (8, 11)
            temporalRoad[k][3, 6] = rnd.Next(low, high) + 11;
            temporalRoad[k][3, 23] = rnd.Next(low, high) + 80;//Line 5(4 <=> 23)(80, 85)
            temporalRoad[k][23, 3] = rnd.Next(low, high) + 85;

            temporalRoad[k][19, 12] = rnd.Next(low, high) + 10;//Line 6 (20=>13) (10, 0)
            temporalRoad[k][12, 4] = rnd.Next(low, high) + 10;//Line 7 (13<=>5) (10, 11)
            temporalRoad[k][4, 12] = rnd.Next(low, high) + 11;

            temporalRoad[k][4, 2] = rnd.Next(low, high) + 220;//Line 8 (5<=>3) (220, 200)
            temporalRoad[k][2, 4] = rnd.Next(low, high) + 200;
            temporalRoad[k][2, 9] = rnd.Next(low, high) + 10;//Line 9 (3<=>10) (10, 11)
            temporalRoad[k][9, 2] = rnd.Next(low, high) + 11;
            temporalRoad[k][1, 9] = rnd.Next(low, high) + 140;//Line 10 (2<=>10) (140, 150)
            temporalRoad[k][9, 1] = rnd.Next(low, high) + 150;
            temporalRoad[k][2, 22] = rnd.Next(low, high) + 280;//Line 11 (3<=>23) (280, 270)
            temporalRoad[k][22, 2] = rnd.Next(low, high) + 270;

            temporalRoad[k][22, 5] = rnd.Next(low, high) + 60;//Line 12 (23<=>6) (60,65)
            temporalRoad[k][5, 22] = rnd.Next(low, high) + 65;
            temporalRoad[k][22, 18] = rnd.Next(low, high) + 90;//Line 13 (23<=>19) (90,83)
            temporalRoad[k][18, 22] = rnd.Next(low, high) + 83;
            temporalRoad[k][5, 23] = rnd.Next(low, high) + 55;//Line 14 (6<=>24) (55,58)
            temporalRoad[k][23, 5] = rnd.Next(low, high) + 58;
            temporalRoad[k][5, 8] = rnd.Next(low, high) + 60;//Line 15 (6<=>9) (60,64)
            temporalRoad[k][8, 5] = rnd.Next(low, high) + 64;

            temporalRoad[k][24, 18] = rnd.Next(low, high) + 30;//Line 16 (25<=>19) (30,34)
            temporalRoad[k][18, 24] = rnd.Next(low, high) + 34;
            temporalRoad[k][24, 23] = rnd.Next(low, high) + 25;//Line 17 (25<=>24) (25,28)
            temporalRoad[k][23, 24] = rnd.Next(low, high) + 28;
            temporalRoad[k][24, 17] = rnd.Next(low, high) + 34;//Line 18 (25<=>18) (34,31)
            temporalRoad[k][17, 24] = rnd.Next(low, high) + 31;
            temporalRoad[k][18, 23] = rnd.Next(low, high) + 32;//Line 19 (19<=>24) (32,34)
            temporalRoad[k][23, 18] = rnd.Next(low, high) + 34;
            temporalRoad[k][17, 23] = rnd.Next(low, high) + 24;//Line 20 (18<=>24) (24,25)
            temporalRoad[k][23, 17] = rnd.Next(low, high) + 25;
            temporalRoad[k][8, 23] = rnd.Next(low, high) + 41;//Line 21 (9<=>24) (41,38)
            temporalRoad[k][23, 8] = rnd.Next(low, high) + 38;
            temporalRoad[k][8, 16] = rnd.Next(low, high) + 110;//Line 22 (9<=>17) (110,120)
            temporalRoad[k][16, 8] = rnd.Next(low, high) + 120;
            temporalRoad[k][16, 23] = rnd.Next(low, high) + 100;//Line 23 (17<=>24) (100,99)
            temporalRoad[k][23, 16] = rnd.Next(low, high) + 99;
            temporalRoad[k][8, 17] = rnd.Next(low, high) + 45;//Line 24 (9<=>18) (45,50)
            temporalRoad[k][17, 8] = rnd.Next(low, high) + 50;

            temporalRoad[k][7, 15] = rnd.Next(low, high) + 15;//Line 25 (8<=>16) (15,12)
            temporalRoad[k][15, 7] = rnd.Next(low, high) + 12;
            temporalRoad[k][7, 20] = rnd.Next(low, high) + 18;//Line 26 (8<=>21) (18,17)
            temporalRoad[k][20, 7] = rnd.Next(low, high) + 17;
            temporalRoad[k][14, 15] = rnd.Next(low, high) + 8;//Line 27 (15<=>16) (8,7)
            temporalRoad[k][15, 14] = rnd.Next(low, high) + 7;
            temporalRoad[k][15, 20] = rnd.Next(low, high) + 23;//Line 28 (16<=>21) (23,21)
            temporalRoad[k][20, 15] = rnd.Next(low, high) + 21;
            temporalRoad[k][14, 24] = rnd.Next(low, high) + 65;//Line 29 (15<=>25) (65,59)
            temporalRoad[k][24, 14] = rnd.Next(low, high) + 59;
            temporalRoad[k][17, 20] = rnd.Next(low, high) + 40;//Line 30 (18<=>21) (40,47)
            temporalRoad[k][20, 17] = rnd.Next(low, high) + 47;
            temporalRoad[k][7, 21] = rnd.Next(low, high) + 80;//Line 31 (8<=>22) (80,75)
            temporalRoad[k][21, 7] = rnd.Next(low, high) + 75;
            temporalRoad[k][7, 11] = rnd.Next(low, high) + 180;//Line 32 (8<=>12) (180,170)
            temporalRoad[k][11, 7] = rnd.Next(low, high) + 170;
            for (int i = 0; i < nodesNames.Length; i++)
            {
                for (int j = 0; j < nodesNames.Length; j++)
                {
                    roads[i, j] += temporalRoad[k][i, j];
                }
            }
        }

        for (int i = 0; i < roads.GetLength(0); i++)
        {
            for (int j = 0; j < roads.GetLength(1); j++)
            {
                roads[i, j] = roads[i, j] / timeSteps;
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




        // set Brekke, Vegtun as the nodes of POI nodes
        string[] strPOIs = { "Nyveien Tomrefjord", "Nåsbru" };//{ "Brendehaug", "Nåsbru" };//{ "Brekke", "Vegtun" };
        string[] removeNodes = { "Brendehaug" };
        Color[] clrPOIs = { Color.blue, Color.red };

        graph.CreatePOInodes(strPOIs, clrPOIs);

        for (int i = 0; i < strPOIs.Length; i++)
        {
            GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.SetColor("_Color", clrPOIs[i]);

        }

        for (int i = 0; i < strPOIs.Length; i++)
        {
            Color AccessColor = GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.color;
            GameObject.Find(strPOIs[i]).GetComponent<Lines>().nColor = AccessColor;
        }

        graph.printNodes();
    }

    private string loadFile(string filename)
    {
        TextAsset file = (TextAsset)AssetDatabase.LoadAssetAtPath(filename, typeof(TextAsset));
        if (file == null)
        {
            throw new Exception(filename + " not found");
        }
        return file.text;
    }
}
