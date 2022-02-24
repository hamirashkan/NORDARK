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
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class ShowMap : MonoBehaviour
{
    Vector3[] coords = new Vector3[40];
    string[] nodesNames = new string[40];
    string[] edgesNames;
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
    // CFH
    public string FeatureString = "010";

    float scale = 1.0f;
    bool recalculateNormals = false;
    private Vector3[] baseVertices;

    public Material SurfaceMat;
    // Start is called before the first frame update
    public GameObject BackgroundMap;
    public Boolean IsBGMap;
    public List<int> NodeIndexArray;
    public List<int>[,] NodeIndexArrayS;
    public List<Node> NodeArray;
    public List<Node>[,] NodeArrayS;
    public List<int>[] CFHArrayS;
    public int timeIndex;
    public int tileIndex;
    public Slider slrTimeLine;
    public int SliderStartTimeValue = 1; // current start value
    public int SliderStopTimeValue = 10; // current stop value

    private AbstractMap bg_Mapbox;
    public Dropdown dropdown_graphop;
    private bool c_flag_last = false;
    private Slider slrStartTime;
    private Slider slrStopTime;
    private Text txtStartTime;
    private Text txtStopTime;
    private InputField IptFeatureString;

    private GameObject Nodes;
    private GameObject Edges;

    private Vector3[][] ArrayV3 = new Vector3[12][];
    private int[][] ArrayTriangles = new int[12][];

    private bool bReadyForCFH = false;
    // Build 0009, add IFT for Graph 1
    IntPtr intPtrImage, intPtrRMImage;
    int nrows = 3, ncols = 4;
    int[] testImage;
    int[] testRMImage;
    //
    // Build 0010, high scale for the vertices interpolation
    int vertices_scale = 10;// 4;// scale parameters
    const int vertices_max = 10;
    int vmax;
    //
    // Build 0012
    public List<AuxLine> AuxLines;
    //

    void Start()
    {
        bReadyForCFH = false;
        // gameObject.transform.childCount. 13 (static) or 14 (dynamic)
        Nodes = GameObject.Find("Nodes");
        Edges = GameObject.Find("Edges");

        slrTimeLine = GameObject.Find("SlrTimeLine").GetComponent<Slider>();
        dropdown_graphop = GameObject.Find("Dropdown").GetComponent<Dropdown>();
        bg_Mapbox = GameObject.Find("BG_Mapbox").GetComponent<AbstractMap>();
        slrStartTime = GameObject.Find("SlrStartTime").GetComponent<Slider>();
        slrStopTime = GameObject.Find("SlrStopTime").GetComponent<Slider>();
        IptFeatureString = GameObject.Find("IptFeatureString").GetComponent<InputField>();

        slrStartTime.onValueChanged.AddListener(delegate { StartTimeValueChangeCheck(); });
        txtStartTime = GameObject.Find("TxtStartTime").GetComponent<Text>();

        slrStopTime.onValueChanged.AddListener(delegate { StopTimeValueChangeCheck(); });
        txtStopTime = GameObject.Find("TxtStopTime").GetComponent<Text>();

        IptFeatureString.onValueChanged.AddListener(delegate { FeatureStringValueChangeCheck(); });


        UIButton.bg = bg_Mapbox.gameObject;

        // Build 0005
        if (gameObject.transform.GetChild(12).name == "TileProvider")
            c_flag_last = true;
        else
            c_flag_last = false;
        //

        for (int c = 0; c < 12; c++)
        {
            Transform tile;
            if (c_flag_last)
                tile = gameObject.transform.GetChild(c);
            else
                tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile
            ArrayV3[c] = new Vector3[tile.gameObject.GetComponent<MeshFilter>().mesh.vertices.Length];
            Array.Copy(tile.gameObject.GetComponent<MeshFilter>().mesh.vertices, ArrayV3[c], ArrayV3[c].Length);
            ArrayTriangles[c] = new int[tile.gameObject.GetComponent<MeshFilter>().mesh.triangles.Length];
            Array.Copy(tile.gameObject.GetComponent<MeshFilter>().mesh.triangles, ArrayTriangles[c], ArrayTriangles[c].Length);
        }

        // Build 0009, add IFT for Graph 1
        vmax = vertices_max + (vertices_scale - 1) * (vertices_max - 1);
        nrows = vmax * nrows;
        ncols = vmax * ncols;
        initImageArray(nrows * ncols);
        //

        if (!IsBGMap)// if GraphSet1
        {
            StartCoroutine(CreateMap(0.01f, 1));
        }        
    }

    // Build 0009, add IFT for graph 1 
    public void initImageArray(int length)
    {
        testImage = new int[length];
        testRMImage = new int[length];
        unsafe
        {
            fixed (int* pArray = testImage)
            {
                intPtrImage = new IntPtr((void*)pArray);
            }
            fixed (int* pArrayRM = testRMImage)
            {
                intPtrRMImage = new IntPtr((void*)pArrayRM);
            }
        }
    }
    //

    // Build 0014, IFT opt test
    public IntPtr GetImagePtr(int[] imageIntArray,int length)
    {
        testImage = new int[length];
        unsafe
        {
            fixed (int* pArray = imageIntArray)
            {
                intPtrImage = new IntPtr((void*)pArray);
                return intPtrImage;
            }
        }
    }
    //

    public IEnumerator CreateMap(float time, int graph_op = 0)
    {
        yield return new WaitForSeconds(time);
        bReadyForCFH = false;
        map = gameObject.GetComponent<AbstractMap>();// GameObject.Find("Mapbox").GetComponent<AbstractMap>();
        graph_op = dropdown_graphop.value;
        if (graph_op == 0)
        {
            GraphSet1("RoadGraph1");
            slrStartTime.minValue = 1;
            slrStartTime.maxValue = 4;
            slrStartTime.value = slrStartTime.minValue;
            slrStopTime.minValue = slrStartTime.minValue;
            slrStopTime.maxValue = slrStartTime.maxValue;
            slrStopTime.value = slrStopTime.maxValue;
        }
        else if (graph_op == 2)
        {
            //map.ResetMap();           
            //map.SetZoom(17);
            //map.SetCenterLatitudeLongitude(new Mapbox.Utils.Vector2d(62.4750425, 6.1914948));
            map.Initialize(new Mapbox.Utils.Vector2d(62.4750425, 6.1914948), 17);
            bg_Mapbox.Initialize(new Mapbox.Utils.Vector2d(62.4750425, 6.1914948), 17);
            //map.UpdateMap();
            GraphSet3("RoadGraph3");
            slrStartTime.minValue = 1;
            slrStartTime.maxValue = 20;
            slrStartTime.value = slrStartTime.minValue;
            slrStopTime.minValue = slrStartTime.minValue;
            slrStopTime.maxValue = slrStartTime.maxValue;
            slrStopTime.value = slrStopTime.maxValue;
        }
        else if (graph_op == 3)
        {
            map.Initialize(new Mapbox.Utils.Vector2d(62.4750425, 6.1914948), 17);
            bg_Mapbox.Initialize(new Mapbox.Utils.Vector2d(62.4750425, 6.1914948), 17);
            GraphSet4("RoadGraph4");
            slrStartTime.minValue = 1;
            slrStartTime.maxValue = 20;
            slrStartTime.value = slrStartTime.minValue;
            slrStopTime.minValue = slrStartTime.minValue;
            slrStopTime.maxValue = slrStartTime.maxValue;
            slrStopTime.value = slrStopTime.maxValue;
        }
        // Build 0013, alesund graph
        else if (graph_op == 4)
        {
            map.Initialize(new Mapbox.Utils.Vector2d(62.49, 6.3), 12);
            bg_Mapbox.Initialize(new Mapbox.Utils.Vector2d(62.49, 6.3), 12);
            GraphSet5("RoadGraph5");
            slrStartTime.minValue = 1;
            slrStartTime.maxValue = 4;
            slrStartTime.value = slrStartTime.minValue;
            slrStopTime.minValue = slrStartTime.minValue;
            slrStopTime.maxValue = slrStartTime.maxValue;
            slrStopTime.value = slrStopTime.maxValue;
        }
        //
        else
        {
            //map.ResetMap();
            //map.SetZoom(8);
            //map.SetCenterLatitudeLongitude(new Mapbox.Utils.Vector2d(62.7233, 7.51087));
            map.Initialize(new Mapbox.Utils.Vector2d(62.7233, 7.51087), 8);
            bg_Mapbox.Initialize(new Mapbox.Utils.Vector2d(62.7233, 7.51087), 8);
            //map.UpdateMap();
            GraphSet2("RoadGraph2");//GraphSet2
            slrStartTime.minValue = 1;
            slrStartTime.maxValue = 10;
            slrStartTime.value = slrStartTime.minValue;
            slrStopTime.minValue = slrStartTime.minValue;
            slrStopTime.maxValue = slrStartTime.maxValue;
            slrStopTime.value = slrStopTime.maxValue;
        }

        bg_Mapbox.gameObject.SetActive(!UIButton.isOn);

        // Build 0013, alesund graph 
        if (graph_op != 4)
        {
            foreach (Node node in graph.Nodes)
            {
                Color AccessColor = GameObject.Find(node.MostAccessPOI.name).GetComponent<Renderer>().material.color;
                float AccessDist = node.LeastCost;
                costs.Add(AccessDist);
                GameObject.Find(node.name).GetComponent<Renderer>().material.SetColor("_Color", AccessColor);
                node.objTransform.GetComponent<Lines>().nColor = AccessColor;
                node.objTransform.GetComponent<Lines>().dist = AccessDist;
                Debug.Log("Res: name=" + node.name + ",MostAccessPOI=" + node.MostAccessPOI);
            }
            // Build 0005
            if (gameObject.transform.GetChild(0).name == "TileProvider")
                c_flag_last = false;
            else
                c_flag_last = true;
            //

            minW = costs.Min();
            maxW = costs.Max();

            // Build 0004
            lambdaMap = new List<float>();
            colorMap = new List<Color>();

            //creating textures and colormap
            for (int c = 0; c < 12; c++)
            {
                Transform tile;
                if (c_flag_last)
                    tile = gameObject.transform.GetChild(c);
                else
                    tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile
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
            }
            // TimeLine Initilization
            NodeIndexArrayS = new List<int>[12, timeSteps];
            NodeArrayS = new List<Node>[12, timeSteps];
            slrTimeLine.maxValue = timeSteps;
            slrTimeLine.minValue = 1;
            //
            for (int c = 0; c < 12; c++)
            {
                Transform tile;
                if (c_flag_last)
                    tile = gameObject.transform.GetChild(c);
                else
                    tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile

                //Texture2D texture = textMap[c];
                float lambda;

                float mindist;
                Node bestNode = null;
                int step = 10;

                Mesh mesh = tile.gameObject.GetComponent<MeshFilter>().mesh;

                //baseVertices = mesh.vertices;
                baseVertices = ArrayV3[c];//
                int vertices_scalemax = vertices_max + (vertices_scale - 1) * (vertices_max - 1);//vertices_max * vertices_scale; // max index for the row or column
                                                                                                 // Build 0006
                hs = vertices_scalemax;
                ws = vertices_scalemax;
                //
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
                mesh.vertices = baseVertices;// Build 0004

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

                // TimeLine 
                // calculate the POI value array for every points/vertics
                for (int k = 0; k < timeSteps; k++)
                {
                    NodeIndexArray = new List<int>();
                    NodeArray = new List<Node>();
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
                        NodeIndexArray.Add(bestNode.POIList[k].index);
                        NodeArray.Add(bestNode.POIList[k]);
                    }
                    NodeIndexArrayS[c, k] = NodeIndexArray;
                    NodeArrayS[c, k] = NodeArray;
                }

                //Debug.Log(baseVertices.Length);

                mesh.vertices = vertices;//felando, decrease the mesh vertices size to normal 100

                // set triangles
                //int[] baseTriangles = mesh.triangles;
                int[] baseTriangles = ArrayTriangles[c];// Build 0004
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

            for (int c = 0; c < 12; c++)
            {
                Transform tile;
                if (c_flag_last)
                    tile = gameObject.transform.GetChild(c);
                else
                    tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile

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

            // Build 0003, timeline update issue
            UISlirTimeLine.label.text = timeIndex + "/" + timeSteps;
            StartTimeValueChangeCheck();
            StopTimeValueChangeCheck();
            bReadyForCFH = true;
            // Build 0009, add IFT for Graph 1
            if (UIButton.isIFT)
            {
                // Build 0015, IFT opt map
                //IFTImageTest();
                IFToptImageTest();
                // Build 0014, IFT opt test
                //IFToptTest();
            }
        }
        else
        {
            // Build 0013, alesund graph
            foreach (Node node in graph.Nodes)
            {
                try
                {
                    Color AccessColor = Color.blue;// GameObject.Find(node.MostAccessPOI.name).GetComponent<Renderer>().material.color;
                    float AccessDist = node.LeastCost;
                    costs.Add(AccessDist);
                    //GameObject.Find(node.name).GetComponent<Renderer>().material.SetColor("_Color", AccessColor);
                    node.objTransform.GetComponent<Renderer>().material.SetColor("_Color", AccessColor);
                    node.objTransform.localScale = node.objTransform.localScale / 10f;
                    node.objTransform.GetComponent<Lines>().nColor = Color.blue;
                    node.objTransform.GetComponent<Lines>().dist = AccessDist;
                    Debug.Log("Res: name=" + node.name + ",MostAccessPOI=" + node.MostAccessPOI);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }
            c_flag_last = true;
            for (int c = 0; c < 12; c++)
            {
                Transform tile;
                if (c_flag_last)
                    tile = gameObject.transform.GetChild(c);
                else
                    tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile

                Mesh mesh = tile.gameObject.GetComponent<MeshFilter>().mesh;
                var vertices = mesh.vertices;
                Color[] colors = new Color[vertices.Length];

                for (var i = 0; i < vertices.Length; i++)
                {
                    colors[i] = Color.black;
                    colors[i].a = 0;
                }

                Shader shader; shader = Shader.Find("Particles/Standard Unlit");

                //tile.gameObject.GetComponent<Renderer>().material.shader = shader;
                tile.gameObject.GetComponent<Renderer>().material = SurfaceMat;
                mesh.colors = colors;
            }
        }
        //
    }

    // Build 0015, IFT opt map
    public void IFTImageTest()
    {
        //IFT
        // Prepare the position for the top left tile. top right is the minimum for graph, top left is the start for image
        float x_min = float.MaxValue;
        float z_max = float.MinValue;
        int x_index = 0;
        int z_index = 0;
        int adj_type = 1;// 1;
        for (int c = 0; c < 12; c++)
        {
            Transform tile;
            if (c_flag_last)
                tile = gameObject.transform.GetChild(c);
            else
                tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile

            Vector3 center = tile.position;
            if (center.x < x_min)
                x_min = center.x;
            if (center.z > z_max)
                z_max = center.z;
        }
        x_min = x_min - 50;
        z_max = z_max + 50;
        // Step 1, set pixel value to 1 if POI nodes belongs to this pixel
        Array.Clear(testImage, 0, testImage.Length);
        // Adjacent region 1, 4 or more points
        foreach (Node node in graph.POInodes)
        {
            //node.vec
            x_index = (int)((node.vec.x - x_min) / (100.0 / (vmax - 1)));
            z_index = (int)((z_max - node.vec.z) / (100.0 / (vmax - 1)));
            if (adj_type == 1)
            {
                // set test Image pixel as 1
                // Adjacent region 1, top left point
                SetImageValue(x_index, z_index);
            }
            else
            {
                // Adjacent region 4 points
                SetImageValue(x_index, z_index);
                SetImageValue(x_index + 1, z_index);
                SetImageValue(x_index, z_index + 1);
                SetImageValue(x_index + 1, z_index + 1);
            }
        }
        //testImage[0] = 1;
        //testImage[ncols * 10 + 20] = 1;
        //testImage[ncols * 10 + 21] = 1;
        //testImage[ncols * 15 + 20] = 1;
        //testImage[ncols * 15 + 21] = 1;

        DateTime dateTime1 = DateTime.Now;
        IntPtr intPtrEdt = DllInterface.IFT(intPtrImage, nrows, ncols);
        DateTime dateTime2 = DateTime.Now;
        var diffInSeconds = (dateTime2 - dateTime1).TotalMilliseconds;
        Debug.Log("IFT:" + diffInSeconds + " millisec");

        int[] edtImage = new int[nrows * ncols];
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        //Debug.Log(edtImage);

        DllInterface.ExportFile(intPtrImage, nrows, ncols, Marshal.StringToHGlobalAnsi("raw.pgm"));
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("edit.pgm"));
        intPtrEdt = DllInterface.GetImage('P');
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("P.pgm"));
        intPtrEdt = DllInterface.GetImage('V');
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("V.pgm"));
        intPtrEdt = DllInterface.GetImage('R');
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("R.pgm"));
    }
    //

    // Build 0015, IFT opt map
    public void IFToptImageTest()
    {
        //IFT opt
        // Prepare the position for the top left tile. top right is the minimum for graph, top left is the start for image
        float x_min = float.MaxValue;
        float x_max = float.MinValue;
        float z_min = float.MaxValue; 
        float z_max = float.MinValue;
        
        int x_index = 0;
        int z_index = 0;
        int adj_type = 1;// 1;
        for (int c = 0; c < 12; c++)
        {
            Transform tile;
            if (c_flag_last)
                tile = gameObject.transform.GetChild(c);
            else
                tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile

            Vector3 center = tile.position;
            if (center.x < x_min)
                x_min = center.x;
            if (center.x > x_max)
                x_max = center.x;
            if (center.z < z_min)
                z_min = center.z;
            if (center.z > z_max)
                z_max = center.z;
        }
        x_min = x_min - 50;
        x_max = x_max - 50;
        z_min = z_min + 50;
        z_max = z_max + 50;
        // Step 1, set pixel value to 1 if POI nodes belongs to this pixel
        Array.Clear(testImage, 0, testImage.Length);
        Array.Clear(testRMImage, 0, testRMImage.Length);
        //int[] rmImage = new int[nrows * ncols];
        //Array.Clear(rmImage, 0, rmImage.Length);
        //IntPtr ipRMImage = GetImagePtr(rmImage, rmImage.Length);

        // Adjacent region 1, 4 or more points
        foreach (Node node in graph.Nodes)
        //foreach (Node node in graph.POInodes)
        {
            //node.vec
            x_index = (int)((node.vec.x - x_min) / (100.0 / (vmax - 1)));
            z_index = (int)((z_max - node.vec.z) / (100.0 / (vmax - 1)));
            if (adj_type == 1)
            {
                // set test Image pixel as 1
                // Adjacent region 1, top left point
                SetImageValue(x_index, z_index, node.index + 1);
                SetRMImageValue(x_index, z_index, (int)(node.LeastCost / 10));
            }
            else
            {
                // Adjacent region 4 points
                SetImageValue(x_index, z_index, node.index + 1);
                SetImageValue(x_index + 1, z_index, node.index + 1);
                SetImageValue(x_index, z_index + 1, node.index + 1);
                SetImageValue(x_index + 1, z_index + 1, node.index + 1);
                // set rmMatrix value also
                SetRMImageValue(x_index, z_index, (int)(node.LeastCost));
                SetRMImageValue(x_index + 1, z_index, (int)(node.LeastCost));
                SetRMImageValue(x_index, z_index + 1, (int)(node.LeastCost));
                SetRMImageValue(x_index + 1, z_index + 1, (int)(node.LeastCost));
            }
        }

        // Build 0016, draw lines
        int x1 = 0;
        int y1 = 0;
        int x2 = 0;
        int y2 = 0;
        float x, y;
        float dy, dx, m, dy_inc;
        foreach (AuxLine lineX in AuxLines)
        {
            //var posv = new Vector3[x.AuxNodes.Count + 2];
            //posv[0] = transform.position + offset;
            ////Debug.Log("Find nodes by index[" + nodeIndex.ToString() + "]: " + x.name);
            //for (int j = 0; j < x.AuxNodes.Count; j++)
            //    posv[j + 1] = x.AuxNodes[j];
            //posv[x.AuxNodes.Count + 1] = Neighbors[i].vec + offset;
            //l.positionCount = posv.Length;
            //l.SetPositions(posv);
            if (lineX.AuxNodes.Count == 0)
            {
                x1 = (int)((lineX.startNodePosition.x - x_min) / (100.0 / (vmax - 1)));
                y1 = (int)((lineX.startNodePosition.z - z_max) / (100.0 / (vmax - 1)));
                x2 = (int)((lineX.stopNodePosition.x - x_min) / (100.0 / (vmax - 1)));
                y2 = (int)((lineX.stopNodePosition.z - z_max) / (100.0 / (vmax - 1)));
                DrawLine(x1, y1, x2, y2);
            }
            else
            {
                x1 = (int)((lineX.startNodePosition.x - x_min) / (100.0 / (vmax - 1)));
                y1 = (int)((lineX.startNodePosition.z - z_max) / (100.0 / (vmax - 1)));
                x2 = (int)((lineX.AuxNodes[0].x - x_min) / (100.0 / (vmax - 1)));
                y2 = (int)((lineX.AuxNodes[0].z - z_max) / (100.0 / (vmax - 1)));
                DrawLine(x1, y1, x2, y2); // don't draw start

                for (int j = 1; j < lineX.AuxNodes.Count; j++)
                {
                    x1 = (int)((lineX.AuxNodes[j - 1].x - x_min) / (100.0 / (vmax - 1)));
                    y1 = (int)((lineX.AuxNodes[j - 1].z - z_max) / (100.0 / (vmax - 1)));
                    x2 = (int)((lineX.AuxNodes[j].x - x_min) / (100.0 / (vmax - 1)));
                    y2 = (int)((lineX.AuxNodes[j].z - z_max) / (100.0 / (vmax - 1)));
                    DrawLine(x1, y1, x2, y2, true); // draw start
                }

                x1 = (int)((lineX.AuxNodes[lineX.AuxNodes.Count - 1].x - x_min) / (100.0 / (vmax - 1)));
                y1 = (int)((lineX.AuxNodes[lineX.AuxNodes.Count - 1].z - z_max) / (100.0 / (vmax - 1)));
                x2 = (int)((lineX.stopNodePosition.x - x_min) / (100.0 / (vmax - 1)));
                y2 = (int)((lineX.stopNodePosition.z - z_max) / (100.0 / (vmax - 1)));
                DrawLine(x1, y1, x2, y2, true); //  draw start
            }
        }
        //

        //testImage[0] = 1;
        //testImage[ncols * 10 + 20] = 1;
        //testImage[ncols * 10 + 21] = 1;
        //testImage[ncols * 15 + 20] = 1;
        //testImage[ncols * 15 + 21] = 1;

        DllInterface.ExportFile(intPtrImage, nrows, ncols, Marshal.StringToHGlobalAnsi("raw.pgm"));
        DllInterface.ExportFile(intPtrRMImage, nrows, ncols, Marshal.StringToHGlobalAnsi("risk.pgm"));

        DateTime dateTime1 = DateTime.Now;
        IntPtr intPtrEdt = DllInterface.IFTopt(intPtrImage, intPtrRMImage, nrows, ncols);
        DateTime dateTime2 = DateTime.Now;
        var diffInSeconds = (dateTime2 - dateTime1).TotalMilliseconds;
        Debug.Log("IFT opt:" + diffInSeconds + " millisec");

        int[] edtImage = new int[nrows * ncols];
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        //Debug.Log(edtImage);
        // 
        
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("edit.pgm"));
        intPtrEdt = DllInterface.GetImage('P');
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("P.pgm"));
        intPtrEdt = DllInterface.GetImage('V');
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("V.pgm"));
        intPtrEdt = DllInterface.GetImage('R');
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("R.pgm"));
    }
    //

    // Build 0017, draw auxlines
    public void DrawLine(int x1, int y1, int x2, int y2, bool drawStart = false)
    {
        float x, y;
        float dy, dx, m, dy_inc;

        dy = y2 - y1;
        dx = x2 - x1;
        m = dy / dx;
        dy_inc = 1;

        if (dy < 0)
            dy = -1;

        float dx_inc = 1;
        if (dx < 0)
            dx = -1;

        if (Mathf.Abs(dy) > Mathf.Abs(dx))
        {
            for (y = y1; y < y2; y += dy_inc)
            {
                x = x1 + (y - y1) / m;
                if(drawStart || (x != x1))
                {
                    Debug.Log("Setting Pixel at: x=" + x + ", y=" + y);
                    SetImageValue((int)(x), -(int)(y), 50);
                    SetRMImageValue((int)(x), -(int)(y), 2);
                    //MyTexture.SetPixel((int)(x), (int)(y), Color.black);
                }
            }
        }
        else
        {
            //if (y1 != y2)
            for (x = x1; x < x2; x += dx_inc)
            {
                y = y1 + (x - x1) * m;
                if (drawStart || (y != y1))
                {
                    Debug.Log("Setting Pixel at: x=" + x + ", y=" + y);
                    SetImageValue((int)(x), -(int)(y), 50);
                    SetRMImageValue((int)(x), -(int)(y), 2);
                }
            }
        }
    }

    // Build 0014, IFT opt test
    public void IFToptTest()
    {
        int tRows = 5;
        int tCols = 5;
        int tlen = tRows * tCols;
        int[] rawImage = new int[tlen];
        int[] rmImage = new int[tlen];
        Array.Clear(rawImage, 0, rawImage.Length);
        Array.Clear(rmImage, 0, rmImage.Length);
        IntPtr ipRawImage = GetImagePtr(rawImage, tlen);
        IntPtr ipRMImage = GetImagePtr(rmImage, tlen);
        // init, set values
        rawImage[6] = 1;
        rawImage[18] = 2;
        rmImage[6] = 10;
        rmImage[18] = 12;//100;
                         // 
        IntPtr intPtrEdt = DllInterface.IFTopt(ipRawImage, ipRMImage, tRows, tCols);
        int[] edtImage = new int[tlen];
        Marshal.Copy(intPtrEdt, edtImage, 0, tlen);
        // 
        DllInterface.ExportFile(ipRawImage, tRows, tCols, Marshal.StringToHGlobalAnsi("raw.pgm"));
        DllInterface.ExportFile(ipRMImage, tRows, tCols, Marshal.StringToHGlobalAnsi("risk.pgm"));
        DllInterface.ExportFile(intPtrEdt, tRows, tCols, Marshal.StringToHGlobalAnsi("edit.pgm"));
        intPtrEdt = DllInterface.GetImage('P');
        Marshal.Copy(intPtrEdt, edtImage, 0, tRows * tCols);
        DllInterface.ExportFile(intPtrEdt, tRows, tCols, Marshal.StringToHGlobalAnsi("P.pgm"));
        intPtrEdt = DllInterface.GetImage('V');
        Marshal.Copy(intPtrEdt, edtImage, 0, tRows * tCols);
        DllInterface.ExportFile(intPtrEdt, tRows, tCols, Marshal.StringToHGlobalAnsi("V.pgm"));
        intPtrEdt = DllInterface.GetImage('R');
        Marshal.Copy(intPtrEdt, edtImage, 0, tRows * tCols);
        DllInterface.ExportFile(intPtrEdt, tRows, tCols, Marshal.StringToHGlobalAnsi("R.pgm"));
    }

    public void SetImageValue(int x = 0, int z = 0, int value = 1)
    {
        if ((x >= 0) && (x < ncols) && (z >= 0) && (z < nrows))
        {
            // set test Image pixel as 1
            // Adjacent region 1, top left point
            testImage[ncols * z + x] = value;
            Debug.Log("M0001:set rawdata x_index =" + x.ToString() + ", z_index = " + z.ToString() + " is value " + value.ToString());
        }
        else
            Debug.Log("E0001:x_index =" + x.ToString() + ", z_index = " + z.ToString() + " are out of the bound");
    }

    public void SetRMImageValue(int x = 0, int z = 0, int value = 1)
    {
        if ((x >= 0) && (x < ncols) && (z >= 0) && (z < nrows))
        {
            // set test Image pixel as 1
            // Adjacent region 1, top left point
            testRMImage[ncols * z + x] = value;
            Debug.Log("M0002:set riskdata x_index =" + x.ToString() + ", z_index = " + z.ToString() + " is value " + value.ToString());
        }
        else
            Debug.Log("E0002:x_index =" + x.ToString() + ", z_index = " + z.ToString() + " are out of the bound");
    }

    public void ComputeTDM()
    {
        float lMin = lambdaMap.Min();
        float lMax = lambdaMap.Max();
        int iter = 0;

        for (int c = 0; c < 12; c++)
        {
            Transform tile;
            if (c_flag_last)
                tile = gameObject.transform.GetChild(c);
            else
                tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile

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
    }

    public void UpdateTexture()
    {
        int iter = 0;
        if ((NodeArrayS != null) && (NodeArrayS[0,0]!=null))// Build 0004, null bug
        {
            for (int c = 0; c < 12; c++)
            {
                Transform tile;
                if (c_flag_last)
                    tile = gameObject.transform.GetChild(c);
                else
                    tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile

                Mesh mesh = tile.gameObject.GetComponent<MeshFilter>().mesh;
                var vertices = mesh.vertices;
                Color[] colors = new Color[vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                {
                    colors[i] = NodeArrayS[c, timeIndex-1][i].clr;//colorMap[iter];
                    colors[i].a = 0.5f;
                    iter += 1;
                }

                Shader shader; shader = Shader.Find("Particles/Standard Unlit");

                tile.gameObject.GetComponent<Renderer>().material = SurfaceMat;
                mesh.colors = colors;
            }

            foreach (Node node in graph.restNodes)
            {
                Color AccessColor = GameObject.Find(node.POIList[timeIndex-1].name).GetComponent<Renderer>().material.color;
                float AccessDist = node.LeastCost;
                costs.Add(AccessDist);
                GameObject.Find(node.name).GetComponent<Renderer>().material.SetColor("_Color", AccessColor);
                node.objTransform.GetComponent<Lines>().nColor = AccessColor;
                node.objTransform.GetComponent<Lines>().dist = AccessDist;
                //Debug.Log("Res: " + node.name + node.MostAccessPOI);
            }
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
            nodeX.objTransform.parent = Nodes.transform;

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

        float[][,] temporalRoad = Enumerable.Range(0, timeSteps).Select(_ => new float[nodesNames.Length, nodesNames.Length]).ToArray();

        float[,] roads = new float[nodesNames.Length, nodesNames.Length];

        graph.roadcosts = roads;
        graph.roadTemporal = temporalRoad;

        System.Random rnd = new System.Random();

        for (int k = 0; k < timeSteps; k++)
        {
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
                double weight = roads[i, j];
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

    public void GraphSet3(string graphName)
    {
        graph = Graph.Create(graphName, false);
        float y = 0.25f;// xOz plane is the map 2D coordinates

        string text = loadFile("Assets/Resources/nodes.csv");
        string[] lines = Regex.Split(text, "\n");

        int nodesNum = lines.Length - 2; //25;//lines.Length - 2;//nbStops
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

            string[] values = Regex.Split(rowdata, ",");
            //string id = values[0];
            float lat = float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
            float lon = float.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture);

            nodesNames[i] = values[0];
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
            nodeX.objTransform.parent = Nodes.transform;

            nodeX.obj.GetComponent<Lines>().index = i;
            nodeX.obj.GetComponent<Lines>().Neighbors = graph.Nodes[i].Neighbors;
            nodeX.obj.GetComponent<Lines>().Weights = graph.Nodes[i].Weights;
            nodeX.obj.GetComponent<Lines>().currentNode = graph.Nodes[i];
            nodeX.obj.GetComponent<Lines>().line = line;

            if (i % 50 == 0)
                Debug.Log(DateTime.Now.ToString() + ", inited " + i + "_th nodes");
        }

        // Load raw edges
        float[,] t0Road = new float[nodesNames.Length, nodesNames.Length];
        text = loadFile("Assets/Resources/edges.csv");
        string[] edges_data = Regex.Split(text, "\n");

        int edgesNum = edges_data.Length - 2;
        edgesNames = new string[edgesNum];

        for (int i = 0; i < edgesNames.Length; i++)
        {
            string rowdata = edges_data[i + 1];

            string[] values = Regex.Split(rowdata, ",");
            //string id = values[0];
            int startindex = graph.FindFirstNode(values[1]).index;
            int stopindex = graph.FindFirstNode(values[2]).index;
            if (values[6] == "nan")
                t0Road[startindex, stopindex] = 300 / 300;// 1e-4f;
            else
                t0Road[startindex, stopindex] = 300 / float.Parse(values[6], System.Globalization.CultureInfo.InvariantCulture);
            //Linename values[6]
        }

        timeSteps = 20;
        graph.timeSteps = timeSteps;

        float[][,] temporalRoad = Enumerable.Range(0, timeSteps).Select(_ => new float[nodesNames.Length, nodesNames.Length]).ToArray();

        float[,] roads = new float[nodesNames.Length, nodesNames.Length];

        graph.roadcosts = roads;
        graph.roadTemporal = temporalRoad;

        System.Random rnd = new System.Random();

        for (int k = 0; k < timeSteps; k++)
        {
            int high = 100;//500;//0
            int low = 0;

            //temporalRoad[k][0, 13] = rnd.Next(low, high) + 40;//Line 1 (1<=>14) (40, 39)
            
            for (int i = 0; i < nodesNames.Length; i++)
            {
                for (int j = 0; j < nodesNames.Length; j++)
                {
                    if (t0Road[i, j] != 0)
                        temporalRoad[k][i, j] = t0Road[i, j] + rnd.Next(low, high);
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
        string[] strPOIs = { "7379970941", "7379971169", "8745416901" };
        Color[] clrPOIs = { Color.blue, Color.red, Color.green };

        //string[] strPOIs = { "7379970941", "8745416892" };
        //Color[] clrPOIs = { Color.blue, Color.red };

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

    public void GraphSet4(string graphName)
    {
        //pass C#'s delegate to C++
        //DllInterface.InitCSharpDelegate(DllInterface.LogMessageFromCpp);

        //IntPtr ptr = DllInterface.fnwrapper_intarr();
        //int[] result = new int[3];
        //Marshal.Copy(ptr, result, 0, 3);
        //Debug.Log(result);

        //IntPtr intPtr;
        //unsafe
        //{
        //    fixed (int* pArray = result)
        //    {
        //        intPtr = new IntPtr((void*)pArray);
        //    }
        //}

        //IntPtr ptr1 = DllInterface.add(intPtr);
        //int[] result1 = new int[3];
        //Marshal.Copy(ptr1, result1, 0, 3);
        //Debug.Log(result1);

        ////IFT
        //int nrows = 50, ncols = 50;
        //int[] testImage = new int[nrows * ncols];
        //testImage[0] = 1;
        //testImage[ncols * 20 + 20] = 1;
        //testImage[ncols * 20 + 21] = 1;
        //testImage[ncols * 30 + 20] = 1;
        //testImage[ncols * 30 + 21] = 1;
        //IntPtr intPtrImage;
        //unsafe
        //{
        //    fixed (int* pArray = testImage)
        //    {
        //        intPtrImage = new IntPtr((void*)pArray);
        //    }
        //}

        //DateTime dateTime1 = DateTime.Now;
        //IntPtr intPtrEdt = DllInterface.IFT(intPtrImage, nrows, ncols);
        //DateTime dateTime2 = DateTime.Now;
        //var diffInSeconds = (dateTime2 - dateTime1).TotalMilliseconds;
        //Debug.Log("IFT:" + diffInSeconds + " millisec");

        //int[] edtImage = new int[nrows * ncols];
        //Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        //Debug.Log(edtImage);

        //DllInterface.ExportFile(intPtrImage, nrows, ncols, Marshal.StringToHGlobalAnsi("raw.pgm"));
        //DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("edit.pgm"));

        //

        graph = Graph.Create(graphName);
        float y = 0.25f;// xOz plane is the map 2D coordinates

        string text = loadFile("Assets/Resources/NodeSet.csv");
        string[] lines = Regex.Split(text, "\n");

        int nodesNum = lines.Length - 2; //25;//lines.Length - 2;//nbStops
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

            string[] values = Regex.Split(rowdata, ",");
            //string id = values[0];
            float lat = float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
            float lon = float.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture);

            nodesNames[i] = values[0];
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
            nodeX.objTransform.parent = Nodes.transform;

            nodeX.obj.GetComponent<Lines>().index = i;
            nodeX.obj.GetComponent<Lines>().Neighbors = graph.Nodes[i].Neighbors;
            nodeX.obj.GetComponent<Lines>().Weights = graph.Nodes[i].Weights;
            nodeX.obj.GetComponent<Lines>().currentNode = graph.Nodes[i];
            nodeX.obj.GetComponent<Lines>().line = line;

            if (i % 50 == 0)
                Debug.Log(DateTime.Now.ToString() + ", inited " + i + "_th nodes");
        }

        // Load raw edges
        float[,] t0Road = new float[nodesNames.Length, nodesNames.Length];
        // Build 0012, more nodes for edges
        AuxLines = new List<AuxLine>();
        text = loadFile("Assets/Resources/EdgeSet.csv");
        string[] edges_data = Regex.Split(text, "\n");

        int edgesNum = edges_data.Length - 2;
        edgesNames = new string[edgesNum];

        for (int i = 0; i < edgesNames.Length; i++)
        {
            string rowdata = edges_data[i + 1];

            string[] values = Regex.Split(rowdata, ",");
            //string id = values[0];
            int startindex = graph.FindFirstNode(values[1]).index;
            int stopindex = graph.FindFirstNode(values[2]).index;
            if ((values[6] == "nan") || (values[6] == "nan\r"))
                t0Road[startindex, stopindex] = 300 / 300;// 1e-4f;
            else
                t0Road[startindex, stopindex] = 300 / float.Parse(values[6], System.Globalization.CultureInfo.InvariantCulture);
            //Linename values[6]
            // Build 0012, more nodes for edges
            // create list based on indexes

            AuxLine AuxLineX = new AuxLine();
            AuxLineX.LineName = startindex.ToString() + "_" + stopindex.ToString();
            string[] lonSet= Regex.Split(values[7], "@");
            string[] latSet = Regex.Split(values[8].Replace("\r", ""), "@");
            if ((lonSet[0] != "") && (lonSet.Length == latSet.Length))
            {
                for (int j = 0; j < lonSet.Length; j++)
                {
                    float lon = float.Parse(lonSet[j], System.Globalization.CultureInfo.InvariantCulture);
                    float lat = float.Parse(latSet[j], System.Globalization.CultureInfo.InvariantCulture);
                    Vector2 latlong = new Vector2(lat, lon);
                    Vector3 pos = latlong.AsUnityPosition(map.CenterMercator, map.WorldRelativeScale);
                    AuxLineX.AuxNodes.Add(pos);
                }
            }
            // Build 0016, draw lines
            AuxLineX.startNodeIndex = startindex;
            AuxLineX.startNodePosition = graph.FindNode(startindex).vec;
            AuxLineX.stopNodeIndex = stopindex;
            AuxLineX.stopNodePosition = graph.FindNode(stopindex).vec;
            //
            //AuxLineX.Add()
            AuxLines.Add(AuxLineX);
            //
        }

        timeSteps = 20;
        graph.timeSteps = timeSteps;

        float[][,] temporalRoad = Enumerable.Range(0, timeSteps).Select(_ => new float[nodesNames.Length, nodesNames.Length]).ToArray();

        float[,] roads = new float[nodesNames.Length, nodesNames.Length];

        graph.roadcosts = roads;
        graph.roadTemporal = temporalRoad;

        System.Random rnd = new System.Random();

        for (int k = 0; k < timeSteps; k++)
        {
            int high = 100;//500;//0
            int low = 0;

            //temporalRoad[k][0, 13] = rnd.Next(low, high) + 40;//Line 1 (1<=>14) (40, 39)

            for (int i = 0; i < nodesNames.Length; i++)
            {
                for (int j = 0; j < nodesNames.Length; j++)
                {
                    if (t0Road[i, j] != 0)
                        temporalRoad[k][i, j] = t0Road[i, j] + rnd.Next(low, high);
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
        string[] strPOIs = { "7379970941", "7379971169", "8745416901" };
        Color[] clrPOIs = { Color.blue, Color.red, Color.green };

        //string[] strPOIs = { "7379970941", "8745416892" };
        //Color[] clrPOIs = { Color.blue, Color.red };

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

    public void GraphSet5(string graphName)
    {
        graph = Graph.Create(graphName);
        float y = 0.25f;// xOz plane is the map 2D coordinates

        string text = loadFile("Assets/Resources/Graph5/NodeSet.csv");
        string[] lines = Regex.Split(text, "\n");

        int nodesNum = lines.Length - 2; //25;//lines.Length - 2;//nbStops
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

            string[] values = Regex.Split(rowdata, ",");
            //string id = values[0];
            float lat = float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
            float lon = float.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture);

            nodesNames[i] = values[0];
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
            nodeX.objTransform.parent = Nodes.transform;

            nodeX.obj.GetComponent<Lines>().index = i;
            nodeX.obj.GetComponent<Lines>().Neighbors = graph.Nodes[i].Neighbors;
            nodeX.obj.GetComponent<Lines>().Weights = graph.Nodes[i].Weights;
            nodeX.obj.GetComponent<Lines>().currentNode = graph.Nodes[i];
            nodeX.obj.GetComponent<Lines>().line = line;

            if (i % 50 == 0)
                Debug.Log(DateTime.Now.ToString() + ", inited " + i + "_th nodes");
        }

        // Load raw edges
        float[,] t0Road = new float[nodesNames.Length, nodesNames.Length];
        // Build 0012, more nodes for edges
        AuxLines = new List<AuxLine>();
        text = loadFile("Assets/Resources/Graph5/EdgeSet.csv");
        string[] edges_data = Regex.Split(text, "\n");

        int edgesNum = edges_data.Length - 2;
        edgesNames = new string[edgesNum];

        for (int i = 0; i < edgesNames.Length; i++)
        {
            try
            {
                string rowdata = edges_data[i + 1];

                string[] values = Regex.Split(rowdata, ",");
                //string id = values[0];
                int startindex = graph.FindFirstNode(values[1]).index;
                int stopindex = graph.FindFirstNode(values[2]).index;
                if ((values[6] == "nan") || (values[6] == "nan\r"))
                    t0Road[startindex, stopindex] = 300 / 300;// 1e-4f;
                else
                    t0Road[startindex, stopindex] = 300 / float.Parse(values[6], System.Globalization.CultureInfo.InvariantCulture);
                //Linename values[6]
                // Build 0012, more nodes for edges
                // create list based on indexes

                AuxLine AuxLineX = new AuxLine();
                AuxLineX.LineName = startindex.ToString() + "_" + stopindex.ToString();
                string[] lonSet = Regex.Split(values[7], "@");
                string[] latSet = Regex.Split(values[8].Replace("\r", ""), "@");
                if ((lonSet[0] != "") && (lonSet.Length == latSet.Length))
                {
                    for (int j = 0; j < lonSet.Length; j++)
                    {
                        float lon = float.Parse(lonSet[j], System.Globalization.CultureInfo.InvariantCulture);
                        float lat = float.Parse(latSet[j], System.Globalization.CultureInfo.InvariantCulture);
                        Vector2 latlong = new Vector2(lat, lon);
                        Vector3 pos = latlong.AsUnityPosition(map.CenterMercator, map.WorldRelativeScale);
                        AuxLineX.AuxNodes.Add(pos);
                    }
                }
                //AuxLineX.Add()
                AuxLines.Add(AuxLineX);
                //
            }
            catch (Exception e)
            {
                Debug.Log(i);
            }
        }

        timeSteps = 4;
        graph.timeSteps = timeSteps;

        float[][,] temporalRoad = Enumerable.Range(0, timeSteps).Select(_ => new float[nodesNames.Length, nodesNames.Length]).ToArray();

        float[,] roads = new float[nodesNames.Length, nodesNames.Length];

        graph.roadcosts = roads;
        graph.roadTemporal = temporalRoad;

        System.Random rnd = new System.Random();

        for (int k = 0; k < timeSteps; k++)
        {
            int high = 100;//500;//0
            int low = 0;

            //temporalRoad[k][0, 13] = rnd.Next(low, high) + 40;//Line 1 (1<=>14) (40, 39)

            for (int i = 0; i < nodesNames.Length; i++)
            {
                for (int j = 0; j < nodesNames.Length; j++)
                {
                    if (t0Road[i, j] != 0)
                        temporalRoad[k][i, j] = t0Road[i, j] + rnd.Next(low, high);
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




        //// set Brekke, Vegtun as the nodes of POI nodes
        //string[] strPOIs = { "277918686", "7389961142", "5086249142" };
        //Color[] clrPOIs = { Color.blue, Color.red, Color.green };

        ////string[] strPOIs = { "7379970941", "8745416892" };
        ////Color[] clrPOIs = { Color.blue, Color.red };

        //graph.CreatePOInodes(strPOIs, clrPOIs);

        //for (int i = 0; i < strPOIs.Length; i++)
        //{
        //    GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.SetColor("_Color", clrPOIs[i]);

        //}

        //for (int i = 0; i < strPOIs.Length; i++)
        //{
        //    Color AccessColor = GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.color;
        //    GameObject.Find(strPOIs[i]).GetComponent<Lines>().nColor = AccessColor;
        //}

        //graph.printNodes();
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
            nodeX.objTransform.parent = Nodes.transform;

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
        float[][,] temporalRoad = Enumerable.Range(0, timeSteps).Select(_ => new float[nodesNames.Length, nodesNames.Length]).ToArray();

        float[,] roads = new float[nodesNames.Length, nodesNames.Length];

        graph.roadcosts = roads;
        graph.roadTemporal = temporalRoad;

        System.Random rnd = new System.Random();

        for (int k = 0; k < timeSteps; k++)
        {
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
                double weight = roads[i, j];
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

    public bool TestMode = false;

    private List<int> MatrixA_Array = new List<int>();
    private List<int> tMatrixA_Array;
    private string strFeatureString;
    private int patternMax = 1;
    private int hs;
    private int ws;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stept">the step size, min 1</param>
    /// <param name="start_time">the index of start time</param>
    /// <param name="stop_time">the index of stop time</param>
    /// <returns></returns>
    public List<int> ComputeCFH(int stept, int start_time, int stop_time)
    {
        CFHArrayS = new List<int>[12];

        for (int c = 0; c < 12; c++)
        {
            Transform tile;
            if (c_flag_last)
                tile = gameObject.transform.GetChild(c);
            else
                tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile

            tileIndex = c;

            MatrixA_Array = new List<int>();
            int t_count = 0;
            for (int i = start_time; i <= stop_time; i = i + stept)
            {
                t_count++;
                tMatrixA_Array = ComputeMatrixA(i);
                MatrixA_Array.AddRange(tMatrixA_Array);
            }

            Mesh mesh = tile.gameObject.GetComponent<MeshFilter>().mesh;
            var vertices = mesh.vertices;
            Color[] colors = new Color[vertices.Length];

            CFHArrayS[c] = new List<int>();
            for (var i = 0; i < vertices.Length; i++)
            {
                List<int> CountParkShadowsCFH = new List<int>();
                if (t_count >= 1)
                {
                    // t count = frame size of t
                    // ws = account size of width
                    // hs = account size of height
                    int[] sub;// = new int[] { 0, 1 };//{ 1, 1, 1 }
                    sub = Fun_FeatureStrToInt(FeatureString);
                    int i_count = hs;
                    int j_count = ws;
                    int[] data = new int[t_count - 1];
                    int max = 1;
                    if (t_count >= 2)
                    {
                        for (int t = 0; t < t_count - 1; t++)
                        {
                            // ComputeMatrixD, binary pattern
                            int binaryMapData = MatrixA_Array[t * vertices.Length + i] !=
                                MatrixA_Array[(t + 1) * vertices.Length + i] ? 1 : 0;
                            data[t] = binaryMapData;
                        }
                        List<int> a = Fun_SubFeatureForData(data, sub);
                        if (a.Count > max)
                            max = a.Count;
                        CountParkShadowsCFH.Add(a.Count);
                        CFHArrayS[c].Add(a.Count);
                    }
                    patternMax = max;
                }
                else
                {
                    patternMax = 0;
                    CountParkShadowsCFH = null;
                }

                //new Color(resultCFM[y * ParkTexture.width + x] / (float)patternMax, 0, 1 - resultCFM[y * ParkTexture.width + x] / (float)patternMax)
                colors[i] = new Color(CFHArrayS[c][i] / (float)patternMax, 0, 1 - CFHArrayS[c][i] / (float)patternMax);// CFHArrayS[i] ;//colorMap[iter];
                colors[i].a = 0.5f;
            }

            Shader shader; shader = Shader.Find("Particles/Standard Unlit");

            tile.gameObject.GetComponent<Renderer>().material = SurfaceMat;
            mesh.colors = colors;
        }
        Debug.Log("PatternMax = " + patternMax.ToString());
        return null;
    }

    //Function ComputeMatrixA
    public List<int> ComputeMatrixA(int i)
    {
        tMatrixA_Array = NodeIndexArrayS[tileIndex, i];
        return tMatrixA_Array;
    }

    public int[] Fun_FeatureStrToInt(string feature)
    {
        char[] c_feature;
        int[] result;
        if (feature.Length > 0)
        {
            c_feature = feature.ToCharArray();
            result = new int[c_feature.Length];
            for (int i = 0; i < c_feature.Length; i++)
            {
                if (c_feature[i] == '0')
                    result[i] = 0;
                else if (c_feature[i] == '1')
                    result[i] = 1;
                else
                    return null;
            }
            return result;
        }
        return null;
    }

    public List<int> Fun_SubFeatureForData(int[] data, int[] sub)
    {
        //int[] data = new int[] { 1, 1, 1, 0, 1, 0, 1 };
        //int[] sub = new int[] { 1, 0, 1 };
        List<int> result = new List<int>();
        for (int i = 0; i < data.Length - sub.Length + 1; i++)
            for (int j = 0; j < sub.Length; j++)
            {
                if (data[i + j] == sub[j])
                {
                    if (j + 1 == sub.Length)
                    {
                        result.Add(i);
                    }
                }
                else
                    break;
            }
        // return [2,4] for List
        return result;
    }

    public void DestroyChildren(string parentName)
    {
        Transform[] children = GameObject.Find(parentName).GetComponentsInChildren<Transform>();
        for (int i = 1; i < children.Length; i++)
        {
            Destroy(children[i].gameObject);
        }
    }

    public void StartTimeValueChangeCheck()
    {
        if (SliderStartTimeValue != (int)slrStartTime.value)
        {
            SliderStartTimeValue = (int)slrStartTime.value;
            if (bReadyForCFH && (SliderStartTimeValue < SliderStopTimeValue))
                ComputeCFH(1, SliderStartTimeValue - 1, SliderStopTimeValue - 1);
        }
        txtStartTime.text = "Start Time:" + SliderStartTimeValue + "/" + slrStartTime.maxValue;
    }

    public void StopTimeValueChangeCheck()
    {
        if (SliderStopTimeValue != (int)slrStopTime.value)
        {
            SliderStopTimeValue = (int)slrStopTime.value;
            if (bReadyForCFH && (SliderStartTimeValue < SliderStopTimeValue))
                ComputeCFH(1, SliderStartTimeValue - 1, SliderStopTimeValue - 1);
        }
        txtStopTime.text = "Stop Time:" + SliderStopTimeValue + "/" + slrStopTime.maxValue;
    }

    public void FeatureStringValueChangeCheck()
    {
        FeatureString = IptFeatureString.text;
        if (bReadyForCFH && (SliderStartTimeValue < SliderStopTimeValue))
            ComputeCFH(1, SliderStartTimeValue - 1, SliderStopTimeValue - 1);
    }
}







