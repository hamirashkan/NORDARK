using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using GeoJSON.Net.Feature;
using System.Threading;

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
    private GameObject VerticesNodes; // Build 0029

    private Vector3[][] ArrayV3 = new Vector3[12][];
    private int[][] ArrayTriangles = new int[12][];

    private bool bReadyForCFH = false;
    // Build 0009, add IFT for Graph 1
    IntPtr intPtrImage, intPtrRMImage;
    int nrows = 3, ncols = 4;
    int[] testImage;
    int[] testRMImage;
    // Build 0018, change the mindist, bestNode to IFT calculation
    int[] rootImage;
    float tx_min = float.MaxValue;
    float tx_max = float.MinValue;
    float tz_min = float.MaxValue;
    float tz_max = float.MinValue;
    //
    // Build 0022, node merge by image mapping
    float ttx_min, ttx_max, ttz_min, ttz_max;
    // Build 0019, cost image matrix
    int[] costImage;
    int[] edtcostImage;
    float[] distImage;//sqrt(cost), sqrt(V)
    // Build 0010, high scale for the vertices interpolation
    int vertices_scale = 1;// 4;// scale parameters
    const int vertices_max = 10;
    int vmax;
    //
    // Build 0012
    public List<AuxLine> AuxLines;
    //
    public bool bImageMapping = true;
    // Build 0027
    public float KMh2MSEC = 3.6f;
    // Build 0029
    public List<Node> VerticesNodeArray;

    void Start()
    {
        bReadyForCFH = false;
        // gameObject.transform.childCount. 13 (static) or 14 (dynamic)
        Nodes = GameObject.Find("Nodes");
        Edges = GameObject.Find("Edges");
        VerticesNodes = GameObject.Find("Vertices");

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

        // Build 0029
        // Build 0009, add IFT for Graph 1
        vmax = vertices_max + (vertices_scale - 1) * (vertices_max - 1) - 1;
        nrows = vmax * nrows + 1;
        ncols = vmax * ncols + 1;
        initImageArray(nrows * ncols);
        VerticesNodes.SetActive(true);
        DestroyChildren(VerticesNodes.name);
        VerticesNodeArray = new List<Node>();
        for (int i = 0; i < nrows * ncols; i++)
        {
            Node verticeNodeX = Node.Create<Node>("Vertex_" + i.ToString(), new Vector3(0, -1, 0));
            verticeNodeX.index = i;
            VerticesNodeArray.Add(verticeNodeX);
            verticeNodeX.objTransform = Instantiate(point);
            verticeNodeX.obj = verticeNodeX.objTransform.gameObject;
            verticeNodeX.objTransform.name = verticeNodeX.name;
            verticeNodeX.objTransform.position = verticeNodeX.vec;
            verticeNodeX.objTransform.parent = VerticesNodes.transform;
            verticeNodeX.objTransform.localScale = new Vector3(1, 1, 1);

            //Transform verticeNodeXtra = Instantiate(point);
            //verticeNodeXtra.name =  + (c * vertices.Length + i).ToString();
            //verticeNodeXtra.position = vertices[i] + tile.position;
            //verticeNodeXtra.parent = VerticesNodes.transform;
            //verticeNodeXtra.localScale = new Vector3(1, 1, 1);
        }
        
        
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
        rootImage = new int[length];
        costImage = new int[length];
        distImage = new float[length];
        edtcostImage = new int[length];
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

    // Build 0020, save ST data to csv file
    public float Clamp(float value, float min, float max)
    {
        return (value < min) ? min : (value > max) ? max : value;
    }

    public IEnumerator CreateMap(float time, int graph_op = 0)
    {
        yield return new WaitForSeconds(time);
        DateTime dt1 = DateTime.Now;
        bReadyForCFH = false;
        
        map = gameObject.GetComponent<AbstractMap>();// GameObject.Find("Mapbox").GetComponent<AbstractMap>();
        graph_op = dropdown_graphop.value;
        // Build 0029, draw vertices nodes
        VerticesNodes.SetActive(UIButton.isShowVertics);
        //
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
            timeSteps = 59;
            GraphSet5("RoadGraph5");
            slrStartTime.minValue = 1;
            slrStartTime.maxValue = timeSteps;
            slrStartTime.value = slrStartTime.minValue;
            slrStopTime.minValue = slrStartTime.minValue;
            slrStopTime.maxValue = slrStartTime.maxValue;
            slrStopTime.value = slrStopTime.maxValue;
        }
        //
        // Build 0021, alesund road05 graph
        else if (graph_op == 5)
        {
            map.Initialize(new Mapbox.Utils.Vector2d(62.54, 6.4), 10);//(62.64, 6.4), 10)
            //map.SetCenterLatitudeLongitude(new Mapbox.Utils.Vector2d(62.6138851, 6.5737325));
            //map.SetZoom(10.7f);
            //map.UpdateMap();
            bg_Mapbox.Initialize(new Mapbox.Utils.Vector2d(62.54, 6.4), 10);
            //bg_Mapbox.SetCenterLatitudeLongitude(new Mapbox.Utils.Vector2d(62.6138851, 6.5737325));
            //bg_Mapbox.SetZoom(10.7f);
            //bg_Mapbox.UpdateMap();
            timeSteps = 59;// 5;//59
            GraphSet6("RoadGraph6");
            slrStartTime.minValue = 1;
            slrStartTime.maxValue = timeSteps;
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
        // Build 0030, after the maps update, then calculate the offset for x and z
        CalculateMinMax();
        // Build 0013, alesund graph 
        if (graph_op < 6)
        {
            // Get the MostAccessPOI and LeastCost for all nodes
            foreach (Node node in graph.Nodes)
            {
                Color AccessColor;
                float AccessDist;
                try
                {
                    AccessColor = GameObject.Find(node.MostAccessPOI.name).GetComponent<Renderer>().material.color;
                    AccessDist = node.LeastCost;
                    costs.Add(AccessDist);
                    GameObject.Find(node.name).GetComponent<Renderer>().material.SetColor("_Color", AccessColor);
                    node.objTransform.GetComponent<Lines>().nColor = AccessColor;
                    node.objTransform.GetComponent<Lines>().dist = AccessDist;
                    Debug.Log("Res: name=" + node.name + ",MostAccessPOI=" + node.MostAccessPOI);
                }
                catch (Exception e)
                {
                    Debug.Log("E0005:" + node.name + ",MostAccessPOI error=");
                }
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

            // Build 0018, change the mindist, bestNode to IFT calculation
            if (UIButton.isIFT)
            {
                //CalculateMinMax();
                // Get the IFT result
                IFTindexImageTest();
            }

            // TimeLine Initilization
            NodeIndexArrayS = new List<int>[12, timeSteps];
            NodeArrayS = new List<Node>[12, timeSteps];
            slrTimeLine.maxValue = timeSteps;// maybe put it to the end of the function
            slrTimeLine.minValue = 1;
            //
            for (int c = 0; c < 12; c++)
            {

                Transform tile;

                if (c_flag_last)
                    tile = gameObject.transform.GetChild(c);
                else
                    tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile

                // Build 0030
                while (!(tile.gameObject.GetComponent<UnityTile>().HeightDataState == Mapbox.Unity.MeshGeneration.Enums.TilePropertyState.Loaded))
                {

                }

                //Texture2D texture = textMap[c];
                float lambda;

                float mindist;
                Node bestNode = null;
                int step = 10;
                
                Mesh mesh = tile.gameObject.GetComponent<MeshFilter>().mesh;

                baseVertices = mesh.vertices;// Build 0030
                //baseVertices = ArrayV3[c];
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

                    //newValue.y = tile.gameObject.GetComponent<UnityTile>().QueryHeightDataNonclamped((newValue.x + 50) / 100.0f, (newValue.z + 50) / 100.0f);
                    newValue.y = newValue.y;
                    scale_vertices[i] = newValue;
                }
                baseVertices = scale_vertices;
                mesh.vertices = baseVertices;// Build 0004

                if (recalculateNormals)//felando
                    mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                var vertices = new Vector3[baseVertices.Length];

                // Build 0018, change the mindist, bestNode to IFT calculation
                int x_offset = 0;
                int z_offset = 0;
                //if (UIButton.isIFT)
                {
                    // tile position to offset tile
                    x_offset = (int)(Math.Round(tile.position.x - tx_min)) / 100;
                    z_offset = (int)(Math.Round(tz_max - tile.position.z)) / 100;
                }

                for (var i = 0; i < vertices.Length; i++)
                {
                    //int x = i % 10;
                    //int z = i / 10;
                    mindist = Mathf.Infinity;
                    bestNode = null;


                    var vertex = baseVertices[i];
                    vertex.x = vertex.x * scale;
                    vertex.z = vertex.z * scale;

                    // Build 0031, not calculate the sea
                    if (baseVertices[i].y < 0.00002f)
                    {
                        vertices[i] = vertex;
                        lambda = (1 / r) * 1 / (1 + Mathf.Exp(Mathf.Pow((0 + 0), -alpha) / r));
                        lambdaMap.Add((lambdaMap.Min() + lambdaMap.Max())/ 2);
                        Color col = Color.black;
                        colorMap.Add(col);
                    }
                    else
                    { 
                        //// Build 0030
                        //int hi= (int)Math.Floor((vertex.x + 50) * 2.55f) + (int)Math.Floor((50 - vertex.z) * 2.55f * 256);
                        //try
                        //{
                        //    vertex.y = tile.gameObject.GetComponent<UnityTile>().HeightData[hi];
                        //}
                        //catch (Exception e)
                        //{ }
                        //vertex.y =tile.gameObject.GetComponent<UnityTile>().QueryHeightDataNonclamped((vertex.x + 50) / 100.0f, (vertex.z + 50) / 100.0f);

                        // Build 0020, save ST data to csv file
                        float distIFT = 0;
                        float distTDM = 0;

                        if(((c * vertices.Length + i) == 498) || ((c * vertices.Length + i) == 508))
                        {
                            Debug.Log("");
                        }

                        // map to get the value of rootimage
                        int x = i % vertices_scalemax;
                        int z = i / vertices_scalemax;

                        x = x + x_offset * (vertices_scalemax - 1);
                        z = z + z_offset * (vertices_scalemax - 1);
                        int i_new = z * ncols + x;

                        //// Build 0018, change the mindist, bestNode to IFT calculation
                        if (UIButton.isIFT)
                        {
                            Node node = graph.FindNode(0);
                            try
                            {
                                node = graph.FindNode(rootImage[i_new] - 1);//graph.Nodes[rootImage[i_new] - 1];
                            }
                            catch (Exception e)
                            { }
                            float posX = vertex.x;
                            float posZ = vertex.z;
                            Vector3 pos = new Vector3(posX + tile.position.x, node.vec.y, posZ + tile.position.z);

                            // Get the pos for node.vec
                            //Vector3 nodeVec = node.vec;
                            //int nx_i = (int)((node.vec.x - (tx_min - 50)) / (100.0 / (vmax - 1)));
                            //int nz_i = (int)((node.vec.z - (tz_min - 50)) / (100.0 / (vmax - 1)));
                            //float nx = (float)(nx_i * (100.0 / (vmax - 1)) + (tx_min - 50));
                            //float nz = (float)(nz_i * (100.0 / (vmax - 1)) + (tz_min - 50));

                            //int pnx_i = (int)((pos.x - (tx_min - 50)) / (100.0 / (vmax - 1)));
                            //int pnz_i = (int)((pos.z - (tz_min - 50)) / (100.0 / (vmax - 1)));
                            //float pnx = (float)(pnx_i * (100.0 / (vmax - 1)) + (tx_min - 50));
                            //float pnz = (float)(pnz_i * (100.0 / (vmax - 1)) + (tz_min - 50));

                            //float dist = (pos - node.vec).magnitude;
                            //nodeVec = new Vector3(nx, node.vec.y, nz);
                            //Vector3 posVec = new Vector3(pnx, pos.y, pnz);
                            //float dist1 = (posVec - nodeVec).magnitude;

                            // method 1, return dist and node
                            if (UIButton.isIFTCost)
                                mindist = distImage[i_new]; //dist;// 10;// dist;
                            else
                            {
                                float dist = (pos - node.vec).magnitude;
                                mindist = dist;
                            }
                            bestNode = node; //graph.FindNode(2);// node;
                            // method 2, directly use the the cost matrix to avoid calculate magnitude twice

                            // Build 0020, save ST data to csv file
                            if (UIButton.isCostDiff)
                            {
                                distIFT = distImage[i_new];
                                distTDM = (pos - node.vec).magnitude;//dist1;// (pos - node.vec).magnitude;
                            }
                            //
                        }
                        else
                        {
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
                        }
                        // equation, K * Tv / (1 + d(P,Nr)), 
                        // Tv = bestNode.riskFactor
                        // K = scale_dis
                        // d(P,Nr) = mindist
                        float dis_new = bestNode.riskFactor * scale_dis / (1 + mindist);

                        // Build 0020, save ST data to csv file
                        if (UIButton.isCostDiff)
                            dis_new = 0;// disable mesh
                        //

                        //vertex.y = vertex.y * 50;// vertex.y + i;
                        vertex.y = dis_new;// vertex.y + i;

                        vertices[i] = vertex;

                        // Build 0029, mesh interval issue
                        if (UIButton.isShowVertics)
                        {
                        
                            //Debug.Log(baseVertices[i].y);
                            VerticesNodeArray[i_new].vec = vertices[i] + tile.position;

                            if (baseVertices[i].y < 0.00002f)//0.0001
                                VerticesNodeArray[i_new].vec.y = -100f;
                            else
                                VerticesNodeArray[i_new].vec.y = vertex.y;

                            VerticesNodeArray[i_new].objTransform.position = VerticesNodeArray[i_new].vec;
                        }
                        //

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

                        Color col = Color.black;
                        // Build 0020, save ST data to csv file
                        if (UIButton.isCostDiff)
                        {
                            if (distTDM != 0)
                                col.a = Clamp(0, Math.Abs((distIFT - distTDM) / distTDM), 1);
                        }
                        else
                            col = bestNode.clr;
                        //
                        colorMap.Add(col);//felando
                    }
                }

                // TimeLine 
                // calculate the POI value array for every points/vertics
                for (int k = 0; k < timeSteps; k++)
                {
                    // test
                    //if ((c == 0) && (UIButton.isIFT))
                    //{
                    //    CalculateMinMax();
                    //    // Get the IFT result
                    //    IFTindexImageTest();
                    //}

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

                        // Build 0031, not calculate the sea
                        //if (baseVertices[i].y < 0.00002f)
                        {
                        }
                        //else
                        { 
                            // Build 0018, change the mindist, bestNode to IFT calculation
                            if (UIButton.isIFT)
                            {
                                // map to get the value of rootimage
                                int x = i % vertices_scalemax;
                                int z = i / vertices_scalemax;
                                x = x + x_offset * (vertices_scalemax - 1);
                                z = z + z_offset * (vertices_scalemax - 1);
                                int i_new = z * ncols + x;

                                try
                                {
                                    Node node = graph.FindNode(rootImage[i_new] - 1);//Nodes[rootImage[i_new] - 1]; 

                                    float posX = vertex.x;
                                    float posZ = vertex.z;
                                    Vector3 pos = new Vector3(posX + tile.position.x, node.vec.y, posZ + tile.position.z);

                                    // method 2, directly use the the cost matrix to avoid calculate magnitude twice
                                    if (UIButton.isIFTCost)
                                        mindist = distImage[i_new]; //dist;// 10;// dist;
                                    else
                                    {
                                        // method 1, return dist and node
                                        float dist = (pos - node.vec).magnitude;
                                        mindist = dist;
                                    }

                                    bestNode = node; // graph.FindNode(3);// node;
                                }
                                catch (Exception e)
                                {
                                    Debug.Log(rootImage[i_new]);
                                }
                            }
                            else
                            {
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
                            }

                            try 
                            {
                                // if it is POI node
                                if (bestNode.POIList == null)
                                {
                                    NodeIndexArray.Add(bestNode.index);
                                    NodeArray.Add(bestNode);
                                }
                                else
                                {
                                    NodeIndexArray.Add(bestNode.POIList[k].index);
                                    NodeArray.Add(bestNode.POIList[k]);
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.Log(bestNode.POIList[k].index);
                            }
                        }
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
                    // Build 0020, save ST data to csv file
                    if (!UIButton.isCostDiff)
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
                //IFToptImageTest();
                // Build 0014, IFT opt test
                //IFToptTest();
                // Build 0018, change the mindist, bestNode to IFT calculation
                //IFTindexImageTest();
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
        DateTime dt2 = DateTime.Now;
        var diffInSeconds = (dt2 - dt1).TotalMilliseconds;
        if (UIButton.isIFT)
            if (UIButton.isIFTCost)
                Debug.Log("Fast IFT total cost:" + diffInSeconds + " millisec");
            else
                Debug.Log("IFT total cost:" + diffInSeconds + " millisec");
        else
            Debug.Log("TDM total cost:" + diffInSeconds + " millisec");
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
        float x, y, r1, r2;
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
            //if (lineX.AuxNodes.Count == 0)
            {
                x1 = (int)((lineX.startNodePosition.x - x_min) / (100.0 / (vmax - 1)));
                y1 = (int)((lineX.startNodePosition.z - z_max) / (100.0 / (vmax - 1)));
                x2 = (int)((lineX.stopNodePosition.x - x_min) / (100.0 / (vmax - 1)));
                y2 = (int)((lineX.stopNodePosition.z - z_max) / (100.0 / (vmax - 1)));
                r1 = graph.Nodes[lineX.startNodeIndex].LeastCost;
                r2 = graph.Nodes[lineX.stopNodeIndex].LeastCost;
                DrawLine(x1, y1, x2, y2, r1, r2);
            }
            //else
            //{
            //    x1 = (int)((lineX.startNodePosition.x - x_min) / (100.0 / (vmax - 1)));
            //    y1 = (int)((lineX.startNodePosition.z - z_max) / (100.0 / (vmax - 1)));
            //    x2 = (int)((lineX.AuxNodes[0].x - x_min) / (100.0 / (vmax - 1)));
            //    y2 = (int)((lineX.AuxNodes[0].z - z_max) / (100.0 / (vmax - 1)));
            //    DrawLine(x1, y1, x2, y2); // don't draw start

            //    for (int j = 1; j < lineX.AuxNodes.Count; j++)
            //    {
            //        x1 = (int)((lineX.AuxNodes[j - 1].x - x_min) / (100.0 / (vmax - 1)));
            //        y1 = (int)((lineX.AuxNodes[j - 1].z - z_max) / (100.0 / (vmax - 1)));
            //        x2 = (int)((lineX.AuxNodes[j].x - x_min) / (100.0 / (vmax - 1)));
            //        y2 = (int)((lineX.AuxNodes[j].z - z_max) / (100.0 / (vmax - 1)));
            //        DrawLine(x1, y1, x2, y2, true); // draw start
            //    }

            //    x1 = (int)((lineX.AuxNodes[lineX.AuxNodes.Count - 1].x - x_min) / (100.0 / (vmax - 1)));
            //    y1 = (int)((lineX.AuxNodes[lineX.AuxNodes.Count - 1].z - z_max) / (100.0 / (vmax - 1)));
            //    x2 = (int)((lineX.stopNodePosition.x - x_min) / (100.0 / (vmax - 1)));
            //    y2 = (int)((lineX.stopNodePosition.z - z_max) / (100.0 / (vmax - 1)));
            //    DrawLine(x1, y1, x2, y2, true); //  draw start
            //}
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

    // Build 0018, change the mindist, bestNode to IFT calculation
    public void CalculateMinMax()
    {
        // Build 0026, async not finished the map load
        if (gameObject.transform.GetChild(11) != null)
        {
            tx_min = float.MaxValue;
            tx_max = float.MinValue;
            tz_min = float.MaxValue;
            tz_max = float.MinValue;

            int x_index = 0;
            int z_index = 0;
            for (int c = 0; c < 12; c++)
            {
                Transform tile;
                if (c_flag_last)
                    tile = gameObject.transform.GetChild(c);
                else
                    tile = gameObject.transform.GetChild(c + 1); //ignoring first child that is not a tile

                Vector3 center = tile.position;
                if (center.x < tx_min)
                    tx_min = center.x;
                if (center.x > tx_max)
                    tx_max = center.x;
                if (center.z < tz_min)
                    tz_min = center.z;
                if (center.z > tz_max)
                    tz_max = center.z;
            }

            // Build 0022, node merge by image mapping
            ttx_min = tx_min - 50;
            ttx_max = tx_max - 50;
            ttz_min = tz_min + 50;
            ttz_max = tz_max + 50;
            //
        }
        else
            Debug.Log("F0009:Not ready");
    }

    // Build 0017, draw auxlines
    public void DrawLine(int x1, int y1, int x2, int y2, float r1, float r2, bool drawStart = false)
    {
        float x, y;
        float dy, dx, m, dy_inc;
        float dr;

        dy = y2 - y1;
        dx = x2 - x1;
        m = dy / dx;
        dy_inc = 1;
        dr = r2 - r1;

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
                r = r1 + dr * (y - y1) / (y2 - y1);
                if(drawStart || (x != x1))
                {
                    Debug.Log("Setting Pixel at: x=" + x + ", y=" + y);
                    SetImageValue((int)(x), -(int)(y), 50);
                    SetRMImageValue((int)(x), -(int)(y), (int)r);// 2);
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
                r = r1 + dr * (x - x1) / (x2 - x1);
                if (drawStart || (y != y1))
                {
                    Debug.Log("Setting Pixel at: x=" + x + ", y=" + y);
                    SetImageValue((int)(x), -(int)(y), 50);
                    SetRMImageValue((int)(x), -(int)(y), (int)r);// 2);
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

    // Build 0018, change the mindist, bestNode to IFT calculation
    public void IFTindexImageTest()
    {
        //IFT index
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
        foreach (Node node in graph.RestNodes)// Nodes)
        {
            //node.vec
            x_index = (int)((node.vec.x - x_min) / (100.0 / (vmax - 1)));
            z_index = (int)((z_max - node.vec.z) / (100.0 / (vmax - 1)));
            if (adj_type == 1)
            {
                // set test Image pixel as 1
                // Adjacent region 1, top left point
                SetImageValue(x_index, z_index, node.index + 1);
            }
            else
            {
                // Adjacent region 4 points
                SetImageValue(x_index, z_index, node.index + 1);
                SetImageValue(x_index + 1, z_index, node.index + 1);
                SetImageValue(x_index, z_index + 1, node.index + 1);
                SetImageValue(x_index + 1, z_index + 1, node.index + 1);
            }
        }
        //testImage[0] = 1;
        //testImage[ncols * 10 + 20] = 1;
        //testImage[ncols * 10 + 21] = 1;
        //testImage[ncols * 15 + 20] = 1;
        //testImage[ncols * 15 + 21] = 1;

        DateTime dateTime1 = DateTime.Now;
        IntPtr intPtrEdt = DllInterface.IFTindex(intPtrImage, nrows, ncols);
        DateTime dateTime2 = DateTime.Now;
        var diffInSeconds = (dateTime2 - dateTime1).TotalMilliseconds;
        Debug.Log("IFT index:" + diffInSeconds + " millisec");

        int[] edtImage = new int[nrows * ncols];
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        //Debug.Log(edtImage);

        DllInterface.ExportFile(intPtrImage, nrows, ncols, Marshal.StringToHGlobalAnsi("raw.pgm"));
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("edit.pgm"));
        // Build 0019, cost image matrix
        Marshal.Copy(intPtrEdt, edtcostImage, 0, nrows * ncols);
        //

        intPtrEdt = DllInterface.GetImage('P');
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("P.pgm"));
        intPtrEdt = DllInterface.GetImage('V');
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("V.pgm"));
        // Build 0019, cost image matrix
        Marshal.Copy(intPtrEdt, costImage, 0, nrows * ncols);
        for (int i = 0; i < nrows * ncols; i++)
            distImage[i] = (float)Math.Sqrt(costImage[i]);
        //
        intPtrEdt = DllInterface.GetImage('R');
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("R.pgm"));
        // Build 0018, change the mindist, bestNode to IFT calculation
        Marshal.Copy(intPtrEdt, rootImage, 0, nrows * ncols);
        //

    }
    //

    // Build 0022, node merge by image mapping
    public void SetImageMappingValue(int x = 0, int z = 0, int value = 1)
    {
        if ((x >= 0) && (x < ncols) && (z >= 0) && (z < nrows))
        {
            // set test Image pixel as 1
            // Adjacent region 1, top left point
            testImage[ncols * z + x] = value;
            Debug.Log("M0003:set rawdata x_index =" + x.ToString() + ", z_index = " + z.ToString() + " is value " + value.ToString());
        }
        else
            Debug.Log("E0003:x_index =" + x.ToString() + ", z_index = " + z.ToString() + " are out of the bound");
    }

    public int GetImageMappingValue(int x = 0, int z = 0)
    {
        int val = -1;
        if ((x >= 0) && (x < ncols) && (z >= 0) && (z < nrows))
            // set test Image pixel as 1
            // Adjacent region 1, top left point
            val = testImage[ncols * z + x];
        return val;
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
                // debug, mesh missed when click it
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

    // Build 0020, save ST data to csv file
    public void saveSTdataToCSV(string filename, float[][,] st_data)
    {
        FileStream stream = null;
        try 
        { 
            // Create a FileStream with mode CreateNew  
            stream = new FileStream(filename, FileMode.Create,
                                       FileAccess.ReadWrite,
                                       FileShare.None);
            // Create a StreamWriter from FileStream  
            using (var w = new StreamWriter(stream, Encoding.UTF8))
            {
                int timeSteps = st_data.Count();
                w.WriteLine("i,j,t,r");
                if(timeSteps > 0)
                { 
                    int len0 = st_data[0].GetLength(0);
                    int len1 = st_data[0].GetLength(1);
                    for (int i = 0; i < len0; i++)
                    {
                        for (int j = 0; j < len1; j++)
                        {
                            for (int k = 0; k < timeSteps; k++)
                            {
                                if (st_data[k][i, j] != 0)
                                {
                                    var line = string.Format("{0},{1},{2},{3}", i, j, k, st_data[k][i, j]);
                                    w.WriteLine(line);
                                }
                            }
                        }
                    }
                }
                //w.WriteLine("haha");
            }
        }
        finally
        {
            if (stream != null)
                stream.Dispose();
        }
    }

    // Build 0020, load to csv file to ST data
    public void loadCSVtoSTdata(string filename, ref float[][,] st_data)
    {
        try
        {
            using (var rd = new StreamReader(filename))
            {
                rd.ReadLine();
                while (!rd.EndOfStream)
                {
                    string[] values = rd.ReadLine().Split(',');
                    //string rowdata = lines[i + 1];
                    //string[] values = Regex.Split(rowdata, ",");
                    int ni = int.Parse(values[0]);
                    int nj = int.Parse(values[1]);
                    int nt = int.Parse(values[2]);
                    float nr = float.Parse(values[3], System.Globalization.CultureInfo.InvariantCulture);

                    st_data[nt][ni, nj] = nr;
                }
            }

            //string text = loadFile(filename);
            //string[] lines = Regex.Split(text, "\n");

            //int rowsNum = lines.Length - 2;

            //for (int i = 0; i < rowsNum; i++)
            //{
            //    string rowdata = lines[i + 1];
            //    string[] values = Regex.Split(rowdata, ",");
            //    int ni = int.Parse(values[0]);
            //    int nj = int.Parse(values[1]);
            //    int nt = int.Parse(values[2]);
            //    float nr = float.Parse(values[3], System.Globalization.CultureInfo.InvariantCulture);

            //    st_data[nt][ni, nj] = nr;
            //}
        }
        finally
        {
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

        // Build 0020, save ST data to csv file
        var defaultfile = "Graph2_Sample1.csv";
        if (File.Exists(defaultfile))
        {
            loadCSVtoSTdata(defaultfile, ref temporalRoad);

            for (int k = 0; k < timeSteps; k++)
            {
                for (int i = 0; i < nodesNames.Length; i++)
                {
                    for (int j = 0; j < nodesNames.Length; j++)
                    {
                        roads[i, j] += temporalRoad[k][i, j];
                    }
                }
            }
        }
        else
        {
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

            // Build 0020, save ST data to csv file
            saveSTdataToCSV("Graph2.csv", temporalRoad);
        }

        for (int i = 0; i < roads.GetLength(0); i++)
        {
            for (int j = 0; j < roads.GetLength(1); j++)
            {
                roads[i, j] = roads[i, j] / timeSteps;
                double weight = roads[i, j];
                if (weight != 0)
                {
                    Node nodeX = graph.Nodes[i];
                    if (nodeX != null)
                    {
                        nodeX.Neighbors.Add(graph.Nodes[j]);
                        nodeX.NeighborNames.Add(graph.Nodes[j].name);
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
            // risk = time
            if ((values[6] == "nan") || (values[6] == "nan\r"))
                t0Road[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture) / 70 * KMh2MSEC;// 1e-4f;
            else
                t0Road[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture) / float.Parse(values[6], System.Globalization.CultureInfo.InvariantCulture) * KMh2MSEC;
            //Linename values[6]
        }

        timeSteps = 20;
        graph.timeSteps = timeSteps;

        float[][,] temporalRoad = Enumerable.Range(0, timeSteps).Select(_ => new float[nodesNames.Length, nodesNames.Length]).ToArray();

        float[,] roads = new float[nodesNames.Length, nodesNames.Length];

        graph.roadcosts = roads;
        graph.roadTemporal = temporalRoad;

        // Build 0020, save ST data to csv file
        var defaultfile = "Graph3_Sample1.csv";
        if (File.Exists(defaultfile))
        {
            loadCSVtoSTdata(defaultfile, ref temporalRoad);

            for (int k = 0; k < timeSteps; k++)
            {
                for (int i = 0; i < nodesNames.Length; i++)
                {
                    for (int j = 0; j < nodesNames.Length; j++)
                    {
                        roads[i, j] += temporalRoad[k][i, j];
                    }
                }
            }
        }
        else 
        { 
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
                            temporalRoad[k][i, j] = t0Road[i, j]  + rnd.Next(low, high);
                        roads[i, j] += temporalRoad[k][i, j];
                    }
                }
            }
            saveSTdataToCSV("Graph3.csv", temporalRoad);
        }

        for (int i = 0; i < roads.GetLength(0); i++)
        {
            for (int j = 0; j < roads.GetLength(1); j++)
            {
                roads[i, j] = roads[i, j] / timeSteps;
                float weight = roads[i, j];
                if (weight != 0)
                {
                    Node nodeX = graph.Nodes[i];
                    if (nodeX != null)
                    {
                        nodeX.Neighbors.Add(graph.Nodes[j]);
                        nodeX.NeighborNames.Add(graph.Nodes[j].name);
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
            // Build 0023
            int ii = 0;
            ImageMapping(ref pos, ref ii);

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
            /*if ((values[6] == "nan") || (values[6] == "nan\r"))
                t0Road[startindex, stopindex] = 300 / 300;// 1e-4f;
            else
                t0Road[startindex, stopindex] = 300 / float.Parse(values[6], System.Globalization.CultureInfo.InvariantCulture);
            */
            // risk = time
            //if ((values[6] == "nan") || (values[6] == "nan\r"))
            //    t0Road[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture) / 70 * KMh2MSEC;// 1e-4f;
            //else
            //    t0Road[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture) / float.Parse(values[6], System.Globalization.CultureInfo.InvariantCulture) * KMh2MSEC;
            // risk = distance
            if ((values[6] == "nan") || (values[6] == "nan\r"))
                t0Road[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture) / 5 * KMh2MSEC;// 1e-4f;
            else
                t0Road[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture) / 5 * KMh2MSEC;


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
            AuxLineX.startNodePosition = graph.Nodes[startindex].vec;
            AuxLineX.stopNodeIndex = stopindex;
            AuxLineX.stopNodePosition = graph.Nodes[stopindex].vec;
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

        var defaultfile = "Graph4_Sample1.csv";
        if (File.Exists(defaultfile))
        {
            loadCSVtoSTdata(defaultfile, ref temporalRoad);

            for (int k = 0; k < timeSteps; k++)
            {
                for (int i = 0; i < nodesNames.Length; i++)
                {
                    for (int j = 0; j < nodesNames.Length; j++)
                    {
                        roads[i, j] += temporalRoad[k][i, j];
                    }
                }
            }
        }
        else
        {

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
            saveSTdataToCSV("Graph4.csv", temporalRoad);
        }

        for (int i = 0; i < roads.GetLength(0); i++)
        {
            for (int j = 0; j < roads.GetLength(1); j++)
            {
                roads[i, j] = roads[i, j] / timeSteps;
                float weight = roads[i, j];
                if (weight != 0)
                {
                    Node nodeX = graph.Nodes[i];
                    if (nodeX != null)
                    {
                        nodeX.Neighbors.Add(graph.Nodes[j]);
                        nodeX.NeighborNames.Add(graph.Nodes[j].name);
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
        //nodesNames = new string[nodesNum];
        //coords = new Vector3[nodesNum];

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
        int node_i = 0;
        int i;
        bool bImageMapping = true;
        Dictionary<string, int> nodeDict = new Dictionary<string, int>();
        if (bImageMapping)
            Array.Clear(testImage, 0, testImage.Length);
        for (i = 0; i < nodesNum; i++)
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

            //nodesNames[i] = values[0];
            Vector2 latlong = new Vector2(lat, lon);
            Vector3 pos = latlong.AsUnityPosition(map.CenterMercator, map.WorldRelativeScale);

            if (bImageMapping)
                ImageMapping(ref pos, ref node_i);

            nodeDict.Add(values[0], node_i);

            // 1 0
            if (node_i > graph.Nodes.Count - 1)
            {
                Node nodeX = Node.Create<Node>(values[0], pos);
                nodeX.GeoVec = latlong;
                nodeX.index = node_i;
                nodeX.stop_id = node_i.ToString();
                graph.AddNode(nodeX);
                nodeX.objTransform = Instantiate(point);
                nodeX.obj = nodeX.objTransform.gameObject;
                nodeX.objTransform.name = nodeX.name;
                nodeX.objTransform.position = nodeX.vec;
                nodeX.objTransform.parent = Nodes.transform;

                int ii = graph.Nodes.Count - 1;
                nodeX.obj.GetComponent<Lines>().index = ii;
                nodeX.obj.GetComponent<Lines>().Neighbors = graph.Nodes[ii].Neighbors;
                nodeX.obj.GetComponent<Lines>().Weights = graph.Nodes[ii].Weights;
                nodeX.obj.GetComponent<Lines>().currentNode = graph.Nodes[ii];
                nodeX.obj.GetComponent<Lines>().line = line;
            }

            // Build 0024, auto adjust to closest nodes
            Node nodeR = Node.Create<Node>(values[0], pos);
            graph.AddRawNode(nodeR);
            //

            if (graph.Nodes.Count % 50 == 0)
                Debug.Log(DateTime.Now.ToString() + ", inited " + graph.Nodes.Count + "_th nodes");
        }

        float maxRisk = 300;

        // Load raw edges
        float[,] t0Road = new float[graph.Nodes.Count, graph.Nodes.Count];

        float[,] distanceRoad = new float[graph.Nodes.Count, graph.Nodes.Count];
        float[,] speedlimitRoad = new float[graph.Nodes.Count, graph.Nodes.Count];
        // Build 0012, more nodes for edges
        AuxLines = new List<AuxLine>();
        text = loadFile("Assets/Resources/Graph5/EdgeSet.csv");
        string[] edges_data = Regex.Split(text, "\n");

        int edgesNum = edges_data.Length - 2;
        edgesNames = new string[edgesNum];

        for (i = 0; i < edgesNames.Length; i++)
        {
            try
            {
                string rowdata = edges_data[i + 1];

                string[] values = Regex.Split(rowdata, ",");
                //string id = values[0];
                int startindex = nodeDict[values[1]]; //graph.FindFirstNode(values[1]).index;
                int stopindex = nodeDict[values[2]]; //graph.FindFirstNode(values[2]).index;
                if(startindex != stopindex)
                {
                    // risk = time
                    //if ((values[6] == "nan") || (values[6] == "nan\r"))
                    //    t0Road[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture) / 70 * KMh2MSEC;// 1e-4f;
                    //else
                    //    t0Road[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture) / float.Parse(values[6], System.Globalization.CultureInfo.InvariantCulture) * KMh2MSEC;
                    // risk = distance
                    //if ((values[6] == "nan") || (values[6] == "nan\r"))
                    //    t0Road[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture) / 5 * KMh2MSEC;// 1e-4f;
                    //else
                    //    t0Road[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture) / 5 * KMh2MSEC;
                    //Linename values[6]
                    // Build 0012, more nodes for edges
                    // create list based on indexes

                    // Build 0028, adjust distance and time calculation
                    distanceRoad[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture);
                    if ((values[6] == "nan") || (values[6] == "nan\r"))
                        speedlimitRoad[startindex, stopindex] = 70;
                    else
                        speedlimitRoad[startindex, stopindex] = float.Parse(values[6], System.Globalization.CultureInfo.InvariantCulture);
                    //    t0Road[startindex, stopindex] = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture)


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
            }
            catch (Exception e)
            {
                Debug.Log(i);
            }
        }

        graph.timeSteps = timeSteps;

        float[][,] temporalRoad = Enumerable.Range(0, timeSteps).Select(_ => new float[graph.Nodes.Count, graph.Nodes.Count]).ToArray();

        float[,] roads = new float[graph.Nodes.Count, graph.Nodes.Count];

        graph.roadcosts = roads;
        graph.roadTemporal = temporalRoad;

        var defaultfile = "Graph5_Sample1.csv";
        if (File.Exists(defaultfile))
        {
            loadCSVtoSTdata(defaultfile, ref temporalRoad);

            for (int k = 0; k < timeSteps; k++)
            {
                for (i = 0; i < nodesNames.Length; i++)
                {
                    for (int j = 0; j < nodesNames.Length; j++)
                    {
                        roads[i, j] += temporalRoad[k][i, j];
                    }
                }
            }
        }
        else
        {
            // Build 0026
            int method_type = 1;

            if (method_type == 1)
            {
                // Method 1, single weather station, distance < 20km
                // average start and stop points, >20cm, speed = 0
                float[] snowdata = new float[timeSteps];
                Dictionary<int, string> timedata = new Dictionary<int, string>();
                LoadSnowData(ref snowdata, ref timedata, "Graph6_snow.csv");

                for (int k = 0; k < timeSteps; k++)
                {
                    for (i = 0; i < roads.GetLength(0); i++)
                    {
                        for (int j = 0; j < roads.GetLength(1); j++)
                        {
                            // Build 0028, adjust distance and time calculation
                            if (distanceRoad[i, j] != 0) // toRoad
                            {
                                float sfactor = 1;
                                double dist1, dist2;
                                Vector2 wStationGeoVec = new Vector2(62.4775f, 6.8167f);// Ørskog
                                // calculation i,j node distance to weather station
                                dist1 = GetDistance(graph.Nodes[i].GeoVec, wStationGeoVec);
                                dist2 = GetDistance(graph.Nodes[j].GeoVec, wStationGeoVec);
                                double avgDist = (dist1 + dist2) / 2;
                                // linear interpolation
                                double maxDis = 50;
                                float newspeed = Clamp((float)(speedlimitRoad[i, j] * (1 - avgDist / maxDis * snowdata[k] / 25)), 5, maxRisk);
                                // y = -2.5x+50, x is snow depth (cm), y is speed(km/h)
                                // equation to get the parameters
                                // big region, drive, calculate
                                temporalRoad[k][i, j] = distanceRoad[i, j] / newspeed * KMh2MSEC;
                                // small region, walk
                                //float walknewspeed = Clamp((float)(5 - avgDist / maxDis * snowdata[k] / 10.0), 2f, 5); // not snowdata[k], will be zero
                                //temporalRoad[k][i, j] = distanceRoad[i, j] / walknewspeed * KMh2MSEC;
                            }
                            roads[i, j] += temporalRoad[k][i, j];
                        }
                    }
                }
            }
            else
            {
                System.Random rnd = new System.Random();

                for (int k = 0; k < timeSteps; k++)
                {
                    int high = 100;//500;//0
                    int low = 0;

                    //temporalRoad[k][0, 13] = rnd.Next(low, high) + 40;//Line 1 (1<=>14) (40, 39)

                    for (i = 0; i < roads.GetLength(0); i++)
                    {
                        for (int j = 0; j < roads.GetLength(1); j++)
                        {
                            if (t0Road[i, j] != 0)
                                temporalRoad[k][i, j] = t0Road[i, j] + rnd.Next(low, high);
                            roads[i, j] += temporalRoad[k][i, j];
                        }
                    }
                }
            }

            for (i = 0; i < roads.GetLength(0); i++)
            {
                for (int j = 0; j < roads.GetLength(1); j++)
                {
                    roads[i, j] = roads[i, j] / timeSteps;
                    float weight = roads[i, j];
                    if (weight != 0)
                    {
                        Node nodeX = graph.Nodes[i];
                        if (nodeX != null)
                        {
                            nodeX.Neighbors.Add(graph.Nodes[j]);
                            nodeX.NeighborNames.Add(graph.Nodes[j].name);
                            nodeX.Weights.Add(roads[i, j]);
                        }
                    }
                }
            }

            saveSTdataToCSV("Graph5.csv", temporalRoad);
        }

        // set Brekke, Vegtun as the nodes of POI nodes
        string[] strPOIs = { "7379971301", "7379970095", "419659911" }; 
        //string[] strPOIs = { "2390101122", "847431215", "3966732182" };
        //string[] strPOIs = { graph.RawNodes[10].name, graph.RawNodes[40].name, "7389961142" };
        Color[] clrPOIs = { Color.blue, Color.red, Color.green };

        //string[] strPOIs = { "7379970941", "8745416892" };
        //Color[] clrPOIs = { Color.blue, Color.red };

        graph.CreatePOInodes(strPOIs, clrPOIs);

        for (i = 0; i < strPOIs.Length; i++)
        {
            GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.SetColor("_Color", clrPOIs[i]);

        }

        for (i = 0; i < strPOIs.Length; i++)
        {
            Color AccessColor = GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.color;
            GameObject.Find(strPOIs[i]).GetComponent<Lines>().nColor = AccessColor;
        }

        graph.printNodes();
    }

    public FeatureCollection Can_Deserialize()
    {
        var rd = new StreamReader("Road_class_012345.geojson");// ("viktig.Geojson");

        string json = rd.ReadToEnd();

        var featureCollection = JsonConvert.DeserializeObject<FeatureCollection>(json);

        return featureCollection;
    }

    // Build 0022, node merge by image mapping
    public void ImageMapping(ref Vector3 position, ref int index)
    {
        try 
        {
            //node.vec
            int x_index = (int)((position.x - ttx_min) / (100.0 / (vmax - 1)));
            int z_index = (int)((ttz_max - position.z) / (100.0 / (vmax - 1)));

            int node_index = GetImageMappingValue(x_index, z_index);
            if (node_index <= 0)
            {
                node_index = graph.Nodes.Count + 1;
                SetImageMappingValue(x_index, z_index, node_index);
            }
            // Calculate the position
            position.x = (float)((x_index + 0.5f) * (100.0 / (vmax - 1)) + ttx_min);
            position.z = (float)(ttz_max - (z_index + 0.5f) * (100.0 / (vmax - 1)));
            // return the position and index
            index = node_index - 1;
        }
        catch (Exception e) 
        { }
    }

    public void GraphSet6(string graphName)
    {
        FeatureCollection fCollection = Can_Deserialize();

        graph = Graph.Create(graphName);
        float y = 0.25f;// xOz plane is the map 2D coordinates

        //List<Vector3> nodesT = new List<Vector3>();
        AuxLines = new List<AuxLine>();

        // Build 0022, node merge by image mapping
        int node_i = 0;
        int i, ii;
        if (bImageMapping)
            Array.Clear(testImage, 0, testImage.Length);
        // Create nodes if not exist
        for (i = 0; i < fCollection.Features.Count; i++)
        {
            GeoJSON.Net.Geometry.MultiLineString multilines = fCollection.Features[i].Geometry as GeoJSON.Net.Geometry.MultiLineString;
            var coords = multilines.Coordinates[0].Coordinates;
            if (coords.Count >= 2)
            {
                var index = 0;
                Vector2 latlong;
                Vector3 pos;
                Node nodeX;
                int start_i = 0;
                int stop_i = 0;
                Vector3 start_pos;
                Vector3 stop_pos;
                // Search and Add start node, return start index
                latlong = new Vector2((float)(coords[index].Latitude), (float)(coords[index].Longitude));
                pos = latlong.AsUnityPosition(map.CenterMercator, map.WorldRelativeScale);
                // Build 0024, auto adjust to closest nodes
                Node nodeR = Node.Create<Node>("node" + graph.RawNodes.Count.ToString(), pos);
                graph.AddRawNode(nodeR);
                //
                //coords[i] = pos;
                if (bImageMapping)
                    ImageMapping(ref pos, ref node_i);
                // 1 0
                if(node_i > graph.Nodes.Count - 1)
                { 
                    nodeX = Node.Create<Node>(nodeR.name, pos);
                    nodeX.GeoVec = latlong;
                    nodeX.index = node_i;
                    nodeX.stop_id = node_i.ToString();
                    graph.AddNode(nodeX);
                    nodeX.objTransform = Instantiate(point);
                    nodeX.obj = nodeX.objTransform.gameObject;
                    nodeX.objTransform.name = nodeX.name;
                    nodeX.objTransform.position = nodeX.vec;
                    nodeX.objTransform.parent = Nodes.transform;

                    ii = graph.Nodes.Count - 1;
                    nodeX.obj.GetComponent<Lines>().index = ii;
                    nodeX.obj.GetComponent<Lines>().Neighbors = graph.Nodes[ii].Neighbors;
                    nodeX.obj.GetComponent<Lines>().Weights = graph.Nodes[ii].Weights;
                    nodeX.obj.GetComponent<Lines>().currentNode = graph.Nodes[ii];
                    nodeX.obj.GetComponent<Lines>().line = line;
                }
                start_i = node_i;
                start_pos = pos;// nodeX.vec;
                //node_i++;

                // Search and Add stop node, return stop index
                latlong = new Vector2((float)(coords[coords.Count - 1].Latitude), (float)(coords[coords.Count - 1].Longitude));
                pos = latlong.AsUnityPosition(map.CenterMercator, map.WorldRelativeScale);
                // Build 0024, auto adjust to closest nodes
                nodeR = Node.Create<Node>("node" + graph.RawNodes.Count.ToString(), pos);
                graph.AddRawNode(nodeR);
                
                //coords[i] = pos;
                if (bImageMapping)
                    ImageMapping(ref pos, ref node_i);
                // 1 0
                if (node_i > graph.Nodes.Count - 1)
                {
                    nodeX = Node.Create<Node>(nodeR.name, pos);
                    // Build 0026
                    // pos to geo
                    nodeX.GeoVec = latlong;
                    //
                    nodeX.index = node_i;
                    nodeX.stop_id = node_i.ToString();
                    graph.AddNode(nodeX);
                    nodeX.objTransform = Instantiate(point);
                    nodeX.obj = nodeX.objTransform.gameObject;
                    nodeX.objTransform.name = nodeX.name;
                    nodeX.objTransform.position = nodeX.vec;
                    nodeX.objTransform.parent = Nodes.transform;

                    ii = graph.Nodes.Count - 1;
                    nodeX.obj.GetComponent<Lines>().index = ii;
                    nodeX.obj.GetComponent<Lines>().Neighbors = graph.Nodes[ii].Neighbors;
                    nodeX.obj.GetComponent<Lines>().Weights = graph.Nodes[ii].Weights;
                    nodeX.obj.GetComponent<Lines>().currentNode = graph.Nodes[ii];
                    nodeX.obj.GetComponent<Lines>().line = line;
                }
                stop_i = node_i;
                stop_pos = pos;// nodeX.vec;
                //node_i++;

                // AuxLines
                try
                {
                    //string id = values[0];
                    int startindex = start_i;
                    int stopindex = stop_i;
                    // cost load
                    //if ((values[6] == "nan") || (values[6] == "nan\r"))
                    //    t0Road[startindex, stopindex] = 300 / 300;// 1e-4f;
                    //else
                    //    t0Road[startindex, stopindex] = 300 / float.Parse(values[6], System.Globalization.CultureInfo.InvariantCulture);
                    
                    // Build 0023, Optimize the network
                    // Step 1, consider the different start and stop index
                    if (startindex != stopindex)
                    { 
                        AuxLine AuxLineX = new AuxLine();
                        AuxLineX.LineName = startindex.ToString() + "_" + stopindex.ToString();
                        for (int j = 1; j < coords.Count - 1; j++)
                        {
                            latlong = new Vector2((float)(coords[j].Latitude), (float)(coords[j].Longitude));
                            pos = latlong.AsUnityPosition(map.CenterMercator, map.WorldRelativeScale);
                            AuxLineX.AuxNodes.Add(pos);
                        }
                    
                        AuxLineX.startNodeIndex = startindex;
                        AuxLineX.startNodePosition = start_pos;// graph.Nodes[startindex].vec;
                        AuxLineX.stopNodeIndex = stopindex;
                        AuxLineX.stopNodePosition = stop_pos;// graph.Nodes[stopindex].vec;
                        AuxLineX.properties = fCollection.Features[i].Properties;
                        //AuxLineX.Add()
                        AuxLines.Add(AuxLineX);
                        //
                        // Build 0023, Optimize the network
                        // Step 2, consider two ways for each road
                        AuxLineX = new AuxLine();
                        int ix = startindex;
                        startindex = stopindex;
                        stopindex = ix;
                        Vector3 ipos = start_pos;
                        start_pos = stop_pos;
                        stop_pos = ipos;
                        AuxLineX.LineName = startindex.ToString() + "_" + stopindex.ToString();
                        for (int j = coords.Count - 2; j >= 1; j--)
                        {
                            latlong = new Vector2((float)(coords[j].Latitude), (float)(coords[j].Longitude));
                            pos = latlong.AsUnityPosition(map.CenterMercator, map.WorldRelativeScale);
                            AuxLineX.AuxNodes.Add(pos);
                        }

                        AuxLineX.startNodeIndex = startindex;
                        AuxLineX.startNodePosition = start_pos;// graph.Nodes[startindex].vec;
                        AuxLineX.stopNodeIndex = stopindex;
                        AuxLineX.stopNodePosition = stop_pos;// graph.Nodes[stopindex].vec;

                        AuxLineX.properties = fCollection.Features[i].Properties;
                        //AuxLineX.Add()
                        AuxLines.Add(AuxLineX);

                        // Build 0023, Optimize the network
                        // Step 3, retain the only least weighted risk value road
                        // TBD
                        //
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(i);
                }
            }
            //fCollection.Features[i].Geometry as GeoJSON.Net.Geometry.LineString;
            //var coords = fCollection.Features[i].Geometry.Coordinates[0].Coordinates;
        }

        // Build 0025, add lines to separated graph 
        //// 411, 362
        //AddAuxlines("node411", "node362");
        //AddAuxlines("node362", "node411");
        ////406, 404
        //AddAuxlines("node406", "node404");
        //AddAuxlines("node404", "node406");
        ////406, 710
        //AddAuxlines("node406", "node710");
        //AddAuxlines("node710", "node406");
        ////367, 411
        //AddAuxlines("node367", "node411");
        //AddAuxlines("node411", "node367");
        ////369, 391
        //AddAuxlines("node369", "node391");
        //AddAuxlines("node391", "node369");
        //////1120, 600
        //AddAuxlines("node1120", "node600");
        //AddAuxlines("node600", "node1120");
        //AuxLine AuxLineN = new AuxLine();
        //string start_str = "node411";
        //string stop_str = "node362";
        //Node start_n = graph.FindFirstNode(start_str);
        //Node stop_n = graph.FindFirstNode(stop_str);
        //AuxLineN.LineName = start_n.index.ToString() + "_" + stop_n.index.ToString();
        //AuxLineN.startNodeIndex = start_n.index;
        //AuxLineN.startNodePosition = start_n.vec;
        //AuxLineN.stopNodeIndex = stop_n.index;
        //AuxLineN.stopNodePosition = start_n.vec;
        //AuxLineN.properties = fCollection.Features[0].Properties;
        //AuxLineN.properties["SPEEDLIMIT"] = 20;
        ////AuxLineX.Add()
        //AuxLines.Add(AuxLineN);
        //AuxLineN = new AuxLine();
        //start_str = "node362";
        //stop_str = "node411";
        //start_n = graph.FindFirstNode(start_str);
        //stop_n = graph.FindFirstNode(stop_str);
        //AuxLineN.LineName = start_n.index.ToString() + "_" + stop_n.index.ToString();
        //AuxLineN.startNodeIndex = start_n.index;
        //AuxLineN.startNodePosition = start_n.vec;
        //AuxLineN.stopNodeIndex = stop_n.index;
        //AuxLineN.stopNodePosition = start_n.vec;
        //AuxLineN.properties = fCollection.Features[0].Properties;
        //AuxLineN.properties["SPEEDLIMIT"] = 20;
        ////AuxLineX.Add()
        //AuxLines.Add(AuxLineN);
        //406, 404
        //406, 710
        //367, 411
        //369, 391
        //1120, 600

        float maxRisk = 300; 
        // Load raw edges
        float[,] t0Road = new float[graph.Nodes.Count, graph.Nodes.Count];
        // Build 0023, Optimize the network
        for (i = 0; i < AuxLines.Count; i++)
        {
            try
            {
                //float speed = (float)(AuxLines[i].properties["SPEEDLIMIT"]);
                string dir = (string)(AuxLines[i].properties["DIRECCTION"]);
                // Build 0026
                // fix bug, failed to get maxspeed when convert object to float
                float speed = Convert.ToSingle(AuxLines[i].properties["SPEEDLIMIT"]);
                t0Road[AuxLines[i].startNodeIndex, AuxLines[i].stopNodeIndex] = speed;
                if (dir != "med")
                    Debug.Log("E0007:wrong DIRECCTION value=" + AuxLines[i].properties.ToString());
            }
            catch (Exception e)
            {
                t0Road[AuxLines[i].startNodeIndex, AuxLines[i].stopNodeIndex] = maxRisk;
                Debug.Log("E0004:wrong edge cost value=" + AuxLines[i].properties.ToString() + " " + e.ToString());
            }
        }
        
        graph.timeSteps = timeSteps;

        float[][,] temporalRoad = Enumerable.Range(0, timeSteps).Select(_ => new float[graph.Nodes.Count, graph.Nodes.Count]).ToArray();

        float[,] roads = new float[graph.Nodes.Count, graph.Nodes.Count];

        graph.roadcosts = roads;
        graph.roadTemporal = temporalRoad;

        var defaultfile = "Graph6_Sample1.csv";
        if (File.Exists(defaultfile))
        {
            loadCSVtoSTdata(defaultfile, ref temporalRoad);

            for (int k = 0; k < timeSteps; k++)
            {
                for (i = 0; i < nodesNames.Length; i++)
                {
                    for (int j = 0; j < nodesNames.Length; j++)
                    {
                        roads[i, j] += temporalRoad[k][i, j];
                    }
                }
            }
        }
        else
        {
            // Build 0026
            int method_type = 1;

            if (method_type == 1)
            {
                // Method 1, single weather station, distance < 20km
                // average start and stop points, >20cm, speed = 0
                float[] snowdata = new float[timeSteps];
                Dictionary<int, string> timedata = new Dictionary<int, string>();
                LoadSnowData(ref snowdata, ref timedata, "Graph6_snow.csv");

                for (int k = 0; k < timeSteps; k++)
                {
                    for (i = 0; i < roads.GetLength(0); i++)
                    {
                        for (int j = 0; j < roads.GetLength(1); j++)
                        {
                            if (t0Road[i, j] != 0)
                            {
                                float sfactor = 1;
                                double dist1, dist2;
                                Vector2 wStationGeoVec = new Vector2(62.4775f, 6.8167f);// Ørskog
                                // calculation i,j node distance to weather station
                                dist1 = GetDistance(graph.Nodes[i].GeoVec, wStationGeoVec);
                                dist2 = GetDistance(graph.Nodes[j].GeoVec, wStationGeoVec);
                                double avgDist = (dist1 + dist2) / 2;
                                // linear interpolation
                                double maxDis = 50;
                                float risk = Clamp((float)(t0Road[i, j] *( 1 - avgDist / maxDis * snowdata[k] / 25)), 5, maxRisk);
                                // y = -2.5x+50, x is snow depth (cm), y is speed(km/h)
                                // equation to get the parameters
                                // calculate
                                temporalRoad[k][i, j] = maxRisk / risk;
                            }
                            roads[i, j] += temporalRoad[k][i, j];
                        }
                    }
                }
            }
            else
            {
                // Default method, random value
                System.Random rnd = new System.Random();

                for (int k = 0; k < timeSteps; k++)
                {
                    int high = 100;//500;//0
                    int low = 0;

                    //temporalRoad[k][0, 13] = rnd.Next(low, high) + 40;//Line 1 (1<=>14) (40, 39)

                    for (i = 0; i < roads.GetLength(0); i++)
                    {
                        for (int j = 0; j < roads.GetLength(1); j++)
                        {
                            if (t0Road[i, j] != 0)
                                temporalRoad[k][i, j] = t0Road[i, j] + rnd.Next(low, high);
                            roads[i, j] += temporalRoad[k][i, j];
                        }
                    }
                }
            }

            for (i = 0; i < roads.GetLength(0); i++)
            {
                for (int j = 0; j < roads.GetLength(1); j++)
                {
                    roads[i, j] = roads[i, j] / timeSteps; // use average value to calculate
                    float weight = roads[i, j];
                    if (weight != 0)
                    {
                        Node nodeX = graph.Nodes[i];
                        if (nodeX != null)
                        {
                            nodeX.Neighbors.Add(graph.Nodes[j]);
                            nodeX.NeighborNames.Add(graph.Nodes[j].name);
                            nodeX.Weights.Add(roads[i, j]);
                        }
                    }
                }
            }

            saveSTdataToCSV("Graph6.csv", temporalRoad);
        }

        // set Brekke, Vegtun as the nodes of POI nodes
        //string[] strPOIs = { "3", "10", "20" };
        string[] strPOIs = { graph.RawNodes[369].name, graph.RawNodes[544].name, graph.RawNodes[412].name };
        Color[] clrPOIs = { Color.blue, Color.red, Color.green };

        //string[] strPOIs = { "7379970941", "8745416892" };
        //Color[] clrPOIs = { Color.blue, Color.red };
        //
        DateTime dt1 = DateTime.Now;
        graph.CreatePOInodes(strPOIs, clrPOIs);
        DateTime dt2 = DateTime.Now;
        var diffInSeconds = (dt2 - dt1).TotalMilliseconds;
        Debug.Log("E1001:CreatePOInodes cost:" + diffInSeconds + " millisec");

        for (i = 0; i < strPOIs.Length; i++)
        {
            GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.SetColor("_Color", clrPOIs[i]);

        }

        for (i = 0; i < strPOIs.Length; i++)
        {
            Color AccessColor = GameObject.Find(strPOIs[i]).GetComponent<Renderer>().material.color;
            GameObject.Find(strPOIs[i]).GetComponent<Lines>().nColor = AccessColor;
        }

        graph.printNodes();
    }

    // Build 0025, add lines to separated graph 
    public void AddAuxlines(string start_str, string stop_str, float value = 15) // ferry speed
    {
        AuxLine AuxLineN = new AuxLine();
        //string start_str = "node411";
        //string stop_str = "node362";
        if (bImageMapping)
        {
            int start_i = 0, stop_i = 0;
            Node start_n = graph.FindFirstRawNode(start_str);
            ImageMapping(ref start_n.vec, ref start_i);
            Node stop_n = graph.FindFirstRawNode(stop_str);
            ImageMapping(ref stop_n.vec, ref stop_i);

            if (start_i != stop_i)
            {
                try
                {
                    AuxLineN.LineName = start_i.ToString() + "_" + stop_i.ToString();
                    AuxLineN.startNodeIndex = start_i;
                    AuxLineN.startNodePosition = graph.Nodes[start_i].vec;
                    AuxLineN.stopNodeIndex = stop_i;
                    AuxLineN.stopNodePosition = graph.Nodes[stop_i].vec;

                    AuxLineN.properties = new Dictionary<string, object>();
                    AuxLineN.properties["DIRECCTION"] = "med";
                    AuxLineN.properties["SPEEDLIMIT"] = value;
                    AuxLines.Add(AuxLineN);
                }
                catch (Exception e)
                {
                    Debug.Log("E0008");
                    Debug.Log(e);
                }
            }
        }
        else
        {
            //Node start_n = graph.FindFirstRawNode(start_str);
            //Node stop_n = graph.FindFirstRawNode(stop_str);

            //AuxLineN.LineName = start_n.index.ToString() + "_" + stop_n.index.ToString();
            //AuxLineN.startNodeIndex = start_n.index;
            //AuxLineN.startNodePosition = start_n.vec;
            //AuxLineN.stopNodeIndex = stop_n.index;
            //AuxLineN.stopNodePosition = stop_n.vec;
        }
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

        // Build 0020, save ST data to csv file
        var defaultfile = "Graph1_Sample1.csv";
        if (File.Exists(defaultfile))
        {
            loadCSVtoSTdata(defaultfile, ref temporalRoad);

            for (int k = 0; k < timeSteps; k++)
            {
                for (int i = 0; i < nodesNames.Length; i++)
                {
                    for (int j = 0; j < nodesNames.Length; j++)
                    {
                        roads[i, j] += temporalRoad[k][i, j];
                    }
                }
            }
        }
        else
        {

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

            // Build 0020, save ST data to csv file
            saveSTdataToCSV("Graph1.csv", temporalRoad);
        }

        for (int i = 0; i < roads.GetLength(0); i++)
        {
            for (int j = 0; j < roads.GetLength(1); j++)
            {
                roads[i, j] = roads[i, j] / timeSteps;
                double weight = roads[i, j];
                if (weight != 0)
                {
                    Node nodeX = graph.Nodes[i];
                    if (nodeX != null)
                    {
                        nodeX.Neighbors.Add(graph.Nodes[j]);
                        nodeX.NeighborNames.Add(graph.Nodes[j].name);
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

    // Build 0026, snow data load
    public void LoadSnowData(ref float[] sdata, ref Dictionary<int, string> time, string filename)
    {
        try
        {
            using (var rd = new StreamReader(filename))
            {
                rd.ReadLine();
                int i = 0;
                while (!rd.EndOfStream)
                {
                    string[] values = rd.ReadLine().Split(';');
                    if(i < sdata.Length)
                    { 
                        time.Add(i, values[2]);
                        sdata[i] = int.Parse(values[6], System.Globalization.CultureInfo.InvariantCulture);
                        i++;
                    }
                }
            }
        }
        finally
        {
        }
    }

    private const double EARTH_RADIUS = 6378.137; 
    private static double rad(double d) { return d * Math.PI / 180.0; }
    public static double GetDistance(double lat1, double lng1, double lat2, double lng2)
    {
        double radLat1 = rad(lat1); 
        double radLat2 = rad(lat2); 
        double a = radLat1 - radLat2; 
        double b = rad(lng1) - rad(lng2); 
        double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2))); 
        s = s * EARTH_RADIUS; 
        s = Math.Round(s * 10000) / 10000; 
        return s;//km
    }

    public static double GetDistance(Vector2 latlng1, Vector2 latlng2)
    {
        return GetDistance(latlng1.x, latlng1.y, latlng2.x, latlng2.y);
    }
    //
}







