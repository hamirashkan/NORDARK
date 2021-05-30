using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;

public class ShowMap : MonoBehaviour
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
    public string Kernel = "S"; //defaults to sigmoid, "G" for gaussian
    public float r = 0.005f;
    public float alpha = 2;

    // Start is called before the first frame update


    void Start()
    {

        StartCoroutine(CreateMap(0.01f));
    }

    public IEnumerator CreateMap(float time)
    {
        yield return new WaitForSeconds(time);
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
        
        //creating textures and colormap
        for(int c = 0; c < gameObject.transform.childCount - 1; c++)
        {
            Transform tile = transform.GetChild(c + 1);
            Vector3 mapSize = tile.gameObject.GetComponent<Renderer>().bounds.size;
            Texture2D texture = new Texture2D((int)mapSize.x, (int)mapSize.z);
            tile.gameObject.GetComponent<Renderer>().material.mainTexture = texture;
            tile.gameObject.GetComponent<Renderer>().material.mainTexture.filterMode = FilterMode.Trilinear;


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

                    //choice of kernel function for density estimation
                    if (Kernel == "G")
                    {
                        lambda = (1 / (r * Mathf.Sqrt(2 * Mathf.PI))) * Mathf.Exp(-0.5f * Mathf.Pow((Mathf.Pow((mindist + bestNode.LeastCost), -alpha) / r), 2));  //Gaussian
                    }
                    else
                    {
                        lambda = (1 / r) * 1 / (1 + Mathf.Exp(Mathf.Pow((mindist + bestNode.LeastCost), -alpha) / r));  //Sigmoid
                    }


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

        timeSteps = 10;
        graph.timeSteps = timeSteps;

        int[][,] temporalRoad = Enumerable.Range(0, timeSteps).Select(_ => new int[nodesNames.Length, nodesNames.Length]).ToArray();

        int[,] roads = new int[nodesNames.Length, nodesNames.Length];

        graph.roadcosts = roads;
        graph.roadTemporal = temporalRoad;

        System.Random rnd = new System.Random();

        for (int k = 0; k < timeSteps; k++)
        {
            for (int i = 0; i < nodesNames.Length; i++)
            {
                for (int j = 0; j < nodesNames.Length; j++)
                {
                    if (rnd.Next(1, 10) >= 8) // 70% = 0
                        temporalRoad[k][i, j] = rnd.Next(1, 20);
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
        string[] strPOIs = { "Brekke", "Vegtun" };
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
