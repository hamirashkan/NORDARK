using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;

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
    public List<Texture2D> textMap;
    public List<float> costs;
    public AbstractMap map;
    public string Kernel = "S"; //defaults to sigmoid, "G" for gaussian
    public float r = 0.005f;
    public float alpha = 2;
    public float scale_dis = 1000;
    public float scale_risk = 10;

    float scale = 1.0f;
    bool recalculateNormals = false;
    private Vector3[] baseVertices;

    public Material SurfaceMat;
    // Start is called before the first frame update
    public GameObject BackgroundMap;
    public Boolean IsBGMap;

    void Start()
    {
        if (!IsBGMap)// if GraphSet1
            StartCoroutine(CreateMap(0.01f));
    }

    public IEnumerator CreateMap(float time)
    {
        yield return new WaitForSeconds(time);
        map = GameObject.Find("Mapbox").GetComponent<AbstractMap>();

        GraphSet1("RoadGraph1");//GraphSet1 or 2
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
        for (int c = 0; c < gameObject.transform.childCount - 1; c++)
        {
            Transform tile = transform.GetChild(c + 1); //ignoring first child that is not a tile
            //Vector3 mapSize = tile.gameObject.GetComponent<Renderer>().bounds.size;
            //Texture2D texture = new Texture2D((int)mapSize.x, (int)mapSize.z);
            //textMap.Add(texture);
            //tile.gameObject.GetComponent<Renderer>().material.mainTexture = texture;
            //tile.gameObject.GetComponent<Renderer>().material.mainTexture.filterMode = FilterMode.Trilinear;


            float mindist;
            float lambda;
            float r = 0.005f;
            float alpha = 2f;
            Node bestNode = null;
            //float posStepX = mapSize.x / texture.width;
            //float posStepZ = mapSize.z / texture.height;
            /*       for (int z = 0; z < texture.height; z++)
                   {
                       for (int x = 0; x < texture.width; x++)
                       {
                           mindist = Mathf.Infinity;
                           bestNode = null;
                           foreach (Node node in graph.RestNodes)
                           {
                               float posX = (tile.position.x - mapSize.x/2) + x;
                               float posZ = (tile.position.z - mapSize.z/2) + z;
                               Vector3 pos = new Vector3(posX , 0.25f, posZ);
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

               }
               float lMin = lambdaMap.Min();
               float lMax = lambdaMap.Max();
               int iter = 0;

                   */
        }
        for (int c = 0; c < gameObject.transform.childCount - 1; c++)
        {
            Transform tile = transform.GetChild(c + 1);
            //Texture2D texture = textMap[c];
            float lambda;

            float mindist;
            Node bestNode = null;
            int step = 10;

            Mesh mesh = tile.gameObject.GetComponent<MeshFilter>().mesh;

            baseVertices = mesh.vertices;
            // high scale for the vertices interpolation
            int vertices_scale = 4;// scale parameters
            int vertices_max = 10;
            int vertices_scalemax = vertices_max + (vertices_scale - 1) * (vertices_max - 1);//vertices_max * vertices_scale; // max index for the row or column
            int row = 0, column = 0;
            int p1_row = 0, p1_column = 0; // point 1
            int p2_row = 0, p2_column = 0; // point 2
            Vector3 p1 = Vector3.zero;
            Vector3 p2 = Vector3.zero;
            Vector3 p3 = Vector3.zero;
            Vector3 p4 = Vector3.zero;
            Vector3 newValue = Vector3.zero;
            float k_row, k_column, k_height = 0;
            // interpolation the vertices
            var scale_vertices = new Vector3[vertices_scalemax * vertices_scalemax];
            if (c == 0)
                c = 0;
            for (var i = 0; i < scale_vertices.Length; i++)
            {
                row = (int)Math.Floor((double)i / vertices_scalemax);
                column = i % vertices_scalemax;
                p1_row = (int)Math.Floor((double)row / vertices_scale);
                p2_row = (p1_row + 1) >= vertices_max ? p1_row : (p1_row + 1);
                p1_column = (int)Math.Floor((double)column / vertices_scale);
                p2_column = (p1_column + 1) >= vertices_max ? p1_column : (p1_column + 1);
                p1 = baseVertices[p1_row * vertices_max + p1_column];
                //p1,  p2
                //   p
                //p4,  p3
                p2 = baseVertices[p1_row * vertices_max + p2_column];
                p3 = baseVertices[p2_row * vertices_max + p2_column];
                p4 = baseVertices[p2_row * vertices_max + p1_column];
                k_row = (float)((row - p1_row * vertices_scale) / (float)vertices_scale);
                k_column = (float)((column - p1_column * vertices_scale) / (float)vertices_scale);
                k_height = (float)((Math.Sqrt((row - p1_row * vertices_scale) * (row - p1_row * vertices_scale)
                    + (column - p1_column * vertices_scale) * (column - p1_column * vertices_scale)))
                    / ((float)vertices_scale * Math.Sqrt(2)));
                newValue.z = p1.z + (p4.z - p1.z) * k_row;
                newValue.x = p1.x + (p2.x - p1.x) * k_column;
                newValue.y = p1.y + (p3.y - p1.y) * k_height;
                scale_vertices[i] = newValue;
            }
            baseVertices = scale_vertices;

            if (recalculateNormals)//felando
                mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var vertices = new Vector3[baseVertices.Length];

            for (var i = 0; i < vertices.Length; i++)
            {
                //int x = i % 10;
                //int z = i / 10;
                mindist = Mathf.Infinity;
                bestNode = null;


                var vertex = baseVertices[i];
                vertex.x = vertex.x * scale;
                vertex.z = vertex.z * scale;

                foreach (Node node in graph.RestNodes)
                    {
                        float posX = vertex.x;
                        float posZ = vertex.z;
                        Vector3 pos = new Vector3(posX + tile.position.x, node.vec.y, posZ + tile.position.z);

                        float dist = (pos - node.vec).magnitude;

                        if (dist < mindist)
                        {
                            mindist = dist;
                            bestNode = node;
                        }

                    }

                    float dis_new = bestNode.riskFactor * scale_dis / (1 + mindist);

                    vertex.y = dis_new;// vertex.y + i;
                    vertices[i] = vertex;

                    //Debug.Log(dis_new);


                
                    //choice of kernel function for density estimation
                    if (Kernel == "G")
                    {
                        lambda = (1 / (r * Mathf.Sqrt(2 * Mathf.PI))) * Mathf.Exp(-0.5f * Mathf.Pow((Mathf.Pow((mindist + bestNode.LeastCost), -alpha) / r), 2));  //Gaussian
                    }
                    else
                    {
                        lambda = (1 / r) * 1 / (1 + Mathf.Exp(Mathf.Pow((mindist + bestNode.LeastCost), -alpha) / r));  //Sigmoid
                    }


                    lambdaMap.Add(lambda);//Felando   
                    Color col = bestNode.clr;
                    colorMap.Add(col);//felando
                }

            //Debug.Log(baseVertices.Length);

            mesh.vertices = vertices;//felando, decrease the mesh vertices size to normal 100

            

                // set triangles
            int[] baseTriangles = mesh.triangles;
            int[] triangles = new int[(vertices_scalemax - 1) * (vertices_scalemax - 1) * 6];
            int index = 0;
            for (int row_i = 0; row_i < vertices_scalemax - 1; row_i++)
            {
                for (int column_j = 0; column_j < vertices_scalemax - 1; column_j++)
                {
                    triangles[index] = row_i * vertices_scalemax + column_j;
                    triangles[index + 1] = (row_i + 1) * vertices_scalemax + column_j + 1;
                    triangles[index + 2] = (row_i + 1) * vertices_scalemax + column_j;
                    triangles[index + 3] = row_i * vertices_scalemax + column_j;
                    triangles[index + 4] = row_i * vertices_scalemax + column_j + 1;
                    triangles[index + 5] = (row_i + 1) * vertices_scalemax + column_j + 1;
                    index += 6;
                    //Debug.Log(index);
                }
            }
            mesh.triangles = triangles;
            //

            /*tile.gameObject.GetComponent<Renderer>().material.mainTexture = texture;
            tile.gameObject.GetComponent<Renderer>().material.mainTexture.filterMode = FilterMode.Trilinear;
            Shader shader; shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            tile.gameObject.GetComponent<Renderer>().material.shader = shader;
            for (int z = 0; z < texture.height; z++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    Color col = colorMap[iter];
                    col.a = 1 - (lambdaMap[iter] - lMin) / (lMax - lMin);
                    texture.SetPixel(x, z, col);
                    iter += 1;
                }
            }

            texture.Apply();*/
        }
        
        float lMin = lambdaMap.Min();
        float lMax = lambdaMap.Max();
        int iter = 0;

        for (int c = 0; c < gameObject.transform.childCount - 1; c++)
        {
            Transform tile = transform.GetChild(c + 1);
            Mesh mesh = tile.gameObject.GetComponent<MeshFilter>().mesh;
            var vertices = mesh.vertices;
            Color[] colors = new Color[vertices.Length];

            for (var i = 0; i < vertices.Length; i++)
            {
                colors[i] = colorMap[iter];
                colors[i].a = 1 - (lambdaMap[iter] - lMin) / (lMax - lMin);
                iter += 1;
            }

            Shader shader; shader = Shader.Find("Particles/Standard Unlit");

            //tile.gameObject.GetComponent<Renderer>().material.shader = shader;
            tile.gameObject.GetComponent<Renderer>().material = SurfaceMat;
            mesh.colors = colors;
        }


        //Transform tile1 = transform.GetChild(2);
        //Mesh mesh1 = tile1.gameObject.GetComponent<MeshFilter>().mesh;
        //baseVertices = mesh1.vertices;
        //for (var i = 0; i < baseVertices.Length; i++)
        //{
        //    Transform objectX;
        //    objectX = Instantiate(point);
        //    objectX.position = baseVertices[i];
        //    objectX.name = "node_" + i;
        //}
        //string tileName = map.AbsoluteZoom + "/133/70";
        //UnityTile tilex = GameObject.Find(tileName).GetComponent<UnityTile>();
        //tilex.HeightData[0] = 100000;
        //map.TileProvider.UpdateTileProvider(); 
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
            temporalRoad[k][13, 10] = rnd.Next(low, high) + 10;//Line 2 (14<=>11) (10, 11)
            temporalRoad[k][10, 13] = rnd.Next(low, high) + 11;
            temporalRoad[k][13, 22] = rnd.Next(low, high) + 100;//Line 3 (14<=>23) (100, 110)
            temporalRoad[k][22, 13] = rnd.Next(low, high) + 110;
            temporalRoad[k][6, 3] = rnd.Next(low, high) + 8;//Line 4 (7<=>4) (8, 11)
            temporalRoad[k][3, 6] = rnd.Next(low, high) + 11;
            temporalRoad[k][3, 22] = rnd.Next(low, high) + 80;//Line 5(4 <=> 23)(80, 85)
            temporalRoad[k][22, 3] = rnd.Next(low, high) + 85;

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

    public void GraphSet1(string graphName)
    {
        graph = Graph.Create(graphName);
        float y = 0.25f;// xOz plane is the map 2D coordinates
        int nodesNum = 6;
        nodesNames = new string[nodesNum];
        coords = new Vector3[nodesNum];

        Debug.Log(DateTime.Now.ToString() + ", init started");
        nodesNames[0] = "H1"; coords[0] = new Vector3(-40, y, 0);
        nodesNames[1] = "A"; coords[1] = new Vector3(-20, y, 0);
        nodesNames[2] = "B"; coords[2] = new Vector3(-4, y, 12);
        nodesNames[3] = "C"; coords[3] = new Vector3(16, y, 0);
        nodesNames[4] = "D"; coords[4] = new Vector3(10, y, -14);
        nodesNames[5] = "H2"; coords[5] = new Vector3(36, y, 0);
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

        timeSteps = 4;
        graph.timeSteps = timeSteps;

        ////H1,  A,  B,  C,  D, H2
        //int[,] roads = new int[,] { {  0,  6,  0,  0,  0,  0},//H1
        //                            {  5,  0,  8,  0,  0,  0},//A
        //                            {  0, 5,  0, 6,  0,  0},//B
        //                            {  0,  0, 7,  0, 7, 10},//C
        //                            {  0, 8,  0, 7,  0,  0},//D
        //                            {  0,  0,  0, 8,  0,  0}};//H2
        int[][,] temporalRoad = Enumerable.Range(0, timeSteps).Select(_ => new int[nodesNames.Length, nodesNames.Length]).ToArray();

        int[,] roads = new int[nodesNames.Length, nodesNames.Length];

        graph.roadcosts = roads;
        graph.roadTemporal = temporalRoad;

        System.Random rnd = new System.Random();

        for (int k = 0; k < timeSteps; k++)
        {
            // random value
            /*
            int high = 10;//500;//0
            int low = 0;
            temporalRoad[k][0, 1] = rnd.Next(low, high) + 6;//Line 1 (H1<=>A) (6, 5)
            temporalRoad[k][1, 0] = rnd.Next(low, high) + 5;
            temporalRoad[k][1, 2] = rnd.Next(low, high) + 8;//Line 2 (A<=>B) (8, 10)
            temporalRoad[k][2, 1] = rnd.Next(low, high) + 10;
            temporalRoad[k][2, 3] = rnd.Next(low, high) + 21;//Line 3 (B<=>C) (21, 25)
            temporalRoad[k][3, 2] = rnd.Next(low, high) + 25;
            temporalRoad[k][3, 4] = rnd.Next(low, high) + 15;//Line 4 (C<=>D) (15, 14)
            temporalRoad[k][4, 3] = rnd.Next(low, high) + 14;
            temporalRoad[k][3, 5] = rnd.Next(low, high) + 12;//Line 5 (C<=>H2) (12, 11)
            temporalRoad[k][5, 3] = rnd.Next(low, high) + 11;
            temporalRoad[k][4, 1] = rnd.Next(low, high) + 20;//Line 6 (D=>A) (20, 0)
            */
            // T=4 test demo
            if (k == 0)
            {
                temporalRoad[k][0, 1] = 6;//Line 1 (H1<=>A) (6, 5)
                temporalRoad[k][1, 0] = 5;
                temporalRoad[k][1, 2] = 8;//Line 2 (A<=>B) (8, 10)
                temporalRoad[k][2, 1] = 10;
                temporalRoad[k][2, 3] = 21;//Line 3 (B<=>C) (21, 25)
                temporalRoad[k][3, 2] = 25;
                temporalRoad[k][3, 4] = 15;//Line 4 (C<=>D) (15, 14)
                temporalRoad[k][4, 3] = 14;
                temporalRoad[k][3, 5] = 12;//Line 5 (C<=>H2) (12, 11)
                temporalRoad[k][5, 3] = 11;
                temporalRoad[k][4, 1] = 20;//Line 6 (D=>A) (20, 0)
            }
            else if (k == 1)
            {
                temporalRoad[k][0, 1] = 6;//Line 1 (H1<=>A) (6, 5)
                temporalRoad[k][1, 0] = 5;
                temporalRoad[k][1, 2] = 8;//Line 2 (A<=>B) (8, 30)
                temporalRoad[k][2, 1] = 30;
                temporalRoad[k][2, 3] = 21;//Line 3 (B<=>C) (21, 10)
                temporalRoad[k][3, 2] = 10;
                temporalRoad[k][3, 4] = 15;//Line 4 (C<=>D) (15, 14)
                temporalRoad[k][4, 3] = 14;
                temporalRoad[k][3, 5] = 12;//Line 5 (C<=>H2) (12, 11)
                temporalRoad[k][5, 3] = 11;
                temporalRoad[k][4, 1] = 20;//Line 6 (D=>A) (20, 0)
            }
            else if (k == 2)
            {
                temporalRoad[k][0, 1] = 6;//Line 1 (H1<=>A) (6, 5)
                temporalRoad[k][1, 0] = 5;
                temporalRoad[k][1, 2] = 8;//Line 2 (A<=>B) (8, 10)
                temporalRoad[k][2, 1] = 10;
                temporalRoad[k][2, 3] = 21;//Line 3 (B<=>C) (21, 24)
                temporalRoad[k][3, 2] = 24;
                temporalRoad[k][3, 4] = 15;//Line 4 (C<=>D) (15, 14)
                temporalRoad[k][4, 3] = 14;
                temporalRoad[k][3, 5] = 12;//Line 5 (C<=>H2) (12, 11)
                temporalRoad[k][5, 3] = 11;
                temporalRoad[k][4, 1] = 20;//Line 6 (D=>A) (20, 0)
            }
            else if (k == 3)
            {
                temporalRoad[k][0, 1] = 6;//Line 1 (H1<=>A) (6, 5)
                temporalRoad[k][1, 0] = 5;
                temporalRoad[k][1, 2] = 15;//Line 2 (A<=>B) (15, 10)
                temporalRoad[k][2, 1] = 10;
                temporalRoad[k][2, 3] = 21;//Line 3 (B<=>C) (21, 25)
                temporalRoad[k][3, 2] = 25;
                temporalRoad[k][3, 4] = 20;//Line 4 (C<=>D) (20, 14)
                temporalRoad[k][4, 3] = 14;
                temporalRoad[k][3, 5] = 12;//Line 5 (C<=>H2) (12, 11)
                temporalRoad[k][5, 3] = 11;
                temporalRoad[k][4, 1] = 30;//Line 6 (D=>A) (30, 0)
            }
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

        // set H1, H2 as the nodes of POI nodes
        string[] strPOIs = { "H1", "H2" };
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

    // comment UvTo3D, not used
    //public Vector3 UvTo3D(Vector2 uv)
    //{
    //    Mesh mesh = GetComponent<MeshFilter>().mesh;
    //    int[] tris = mesh.triangles;
    //    Vector2[] uvs = mesh.uv;
    //    Vector3[] verts = mesh.vertices;
    //    for (int i = 0; i < tris.Length; i += 3){
    //        Vector2 u1 = uvs[tris[i]]; // get the triangle UVs
    //        Vector2 u2 = uvs[tris[i + 1]];
    //        Vector2 u3 = uvs[tris[i + 2]];
    //        // calculate triangle area - if zero, skip it
    //        float a = Area(u1, u2, u3); if (a == 0) continue;
    //        // calculate barycentric coordinates of u1, u2 and u3
    //        // if anyone is negative, point is outside the triangle: skip it
    //        float a1 = Area(u2, u3, uv) / a; if (a1 < 0) continue;
    //        float a2 = Area(u3, u1, uv) / a; if (a2 < 0) continue;
    //        float a3 = Area(u1, u2, uv) / a; if (a3 < 0) continue;
    //        // point inside the triangle - find mesh position by interpolation...
    //        Vector3 p3D = a1 * verts[tris[i]] + a2 * verts[tris[i + 1]] + a3 * verts[tris[i + 2]];
    //        // and return it in world coordinates:
    //        return transform.TransformPoint(p3D);
    //    }
    //    // point outside any uv triangle: return Vector3.zero
    //    return Vector3.zero;
    //}

    //// calculate signed triangle area using a kind of "2D cross product":
    //float Area(Vector2 p1, Vector2 p2, Vector2 p3)
    //{
    //Vector2 v1 = p1 - p3;
    //Vector2 v2= p2 - p3;
    //return (v1.x* v2.y - v1.y* v2.x) / 2;
    //}

    //public void GraphSet1(string graphName)
    //{
    //    graph = Graph.Create(graphName);
    //    float y = 0.25f;// xOz plane is the map 2D coordinates
    //    int nodesNum = 6;
    //    nodesNames = new string[nodesNum];
    //    coords = new Vector3[nodesNum];
    //    nodesNames[0] = "H1"; coords[0] = new Vector3(-40, y, 0);
    //    nodesNames[1] = "A"; coords[1] = new Vector3(-20, y, 0);
    //    nodesNames[2] = "B"; coords[2] = new Vector3(-4, y, 12);
    //    nodesNames[3] = "C"; coords[3] = new Vector3(16, y, 0);
    //    nodesNames[4] = "D"; coords[4] = new Vector3(10, y, -14);
    //    nodesNames[5] = "H2"; coords[5] = new Vector3(36, y, 0);
    //    for (int i = 0; i < nodesNames.Length; i++)
    //    {
    //        Node nodeX = Node.Create<Node>(nodesNames[i], coords[i]);
    //        nodeX.index = i;
    //        graph.AddNode(nodeX);
    //        nodeX.objTransform = Instantiate(point);
    //        nodeX.obj = nodeX.objTransform.gameObject;
    //        nodeX.objTransform.name = nodeX.name;
    //        nodeX.objTransform.position = nodeX.vec;

    //        nodeX.obj.GetComponent<Lines>().index = i;
    //        nodeX.obj.GetComponent<Lines>().Neighbors = graph.Nodes[i].Neighbors;
    //        nodeX.obj.GetComponent<Lines>().Weights = graph.Nodes[i].Weights;
    //        nodeX.obj.GetComponent<Lines>().currentNode = graph.Nodes[i];
    //        nodeX.obj.GetComponent<Lines>().line = line;
    //    }
    //    //H1,  A,  B,  C,  D, H2
    //    int[,] roads = new int[,] { {  0,  6,  0,  0,  0,  0},//H1
    //                                {  5,  0,  8,  0,  0,  0},//A
    //                                {  0, 5,  0, 6,  0,  0},//B
    //                                {  0,  0, 7,  0, 7, 10},//C
    //                                {  0, 8,  0, 7,  0,  0},//D
    //                                {  0,  0,  0, 8,  0,  0}};//H2





    //    graph.roadcosts = roads;



    //    for (int i = 0; i < roads.GetLength(0); i++)
    //    {
    //        for (int j = 0; j < roads.GetLength(1); j++)
    //        {
    //            float weight = roads[i, j];
    //            if (weight != 0)
    //            {
    //                Node nodeX = graph.FindNode(i);
    //                if (nodeX != null)
    //                {
    //                    nodeX.Neighbors.Add(graph.FindNode(j));
    //                    nodeX.NeighborNames.Add(graph.FindNode(j).name);
    //                    nodeX.Weights.Add(roads[i, j]);
    //                }
    //            }
    //        }
    //    }


    //    /* for (int i = 0; i < roads.GetLength(0); i++)
    //    {
    //        for (int j = 0; j < roads.GetLength(1); j++)
    //        {
    //            //Assign current array element to max, if (arr[i,j] > max)
    //            if (roads[i, j] > maxW)
    //            {
    //                maxW = roads[i, j];
    //            }

    //            //Assign current array element to min if if (arr[i,j] < min)
    //            if (roads[i, j] < minW)
    //            {
    //                minW = roads[i, j];
    //            }

    //        }

    //    } */



    //    // set H1, H2 as the nodes of POI nodes
    //    string[] strPOIs = { "H1", "H2" };
    //    Color[] clrPOIs = { Color.blue, Color.red };

    //    graph.CreatePOInodes(strPOIs, clrPOIs);

    //    for (int i = 0; i < strPOIs.Length; i++)
    //    {
    //        GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.SetColor("_Color", clrPOIs[i]);

    //    }



    //    for (int i = 0; i < strPOIs.Length; i++)
    //    {
    //        Color AccessColor = GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.color;
    //        GameObject.Find(strPOIs[i]).GetComponent<Lines>().nColor = AccessColor;
    //    }

    //    graph.printNodes();
    //}

}





