using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

public class Graph// : ScriptableObject
{
    // Build 0009, add IFT for Graph 1
    [SerializeField]
    public List<Node> pOInodes;
    public List<Node> POInodes
    {
        get
        {
            if (pOInodes == null)
            {
                pOInodes = new List<Node>();
            }

            return pOInodes;
        }
    }
    //
    private List<GameObject> lines;
    public float[,] roadcosts;//int
    public float[][,] roadTemporal;//int
    private float[] dist;//int
    public int timeSteps;
    public static int LinesNum;

    [SerializeField]
    public List<Node> restNodes;
    public List<Node> RestNodes
    {
        get
        {
            if (restNodes == null)
            {
                restNodes = new List<Node>();
            }

            return restNodes;
        }
    }


    [SerializeField]
    private List<Node> nodes;
    public List<Node> Nodes
    {
        get
        {
            if (nodes == null)
            {
                nodes = new List<Node>();
            }

            return nodes;
        }
    }

    // Build 0024, auto adjust to closest nodes
    [SerializeField]
    private List<Node> rawnodes;
    public List<Node> RawNodes
    {
        get
        {
            if (rawnodes == null)
            {
                rawnodes = new List<Node>();
            }

            return rawnodes;
        }
    }

    public void AddRawNode(Node node)
    {
        RawNodes.Add(node);
    }

    public Node FindFirstRawNode(string nodeName)
    {
        foreach (Node x in rawnodes.FindAll(element => element.name == nodeName))
        {
            Debug.Log("Find raw nodes: " + x.name);
            return x;
        }
        return null;
    }

    public Node GetNearestNode(Node node)
    {
        Node nodeR = node;
        float minDist = float.PositiveInfinity;
        foreach (Node x in nodes)
        {
            float dist = (x.vec - node.vec).magnitude;
            if (dist < minDist)
            {
                minDist = dist;
                nodeR = x;
            }
            
        }
        Debug.Log("E0006: Find closest nodes raw node: " + nodeR.name + ",pos=" + nodeR.vec.ToString()
            + " request node:" + node.name + ",pos=" + node.vec.ToString());
        return nodeR;
    }
    //

    public static Graph Create(string name, bool enableAsset = true)
    {
        Graph graph = new Graph();// CreateInstance<Graph>();

        //// Build 0013, alesund graph
        //if(enableAsset)
        //{ 
        //    string path = string.Format("Assets/{0}.asset", name);
        //    AssetDatabase.CreateAsset(graph, path);
        //}

        LinesNum = 0;
        return graph;
    }

    public void printNodes()
    {
        Debug.Log("Print nodes\n");
        for (int i = 0; i < Nodes.Count; i++)
            Debug.Log(Nodes[i].name + " \t\t " + Nodes[i].MostAccessPOI + " \t\t " + Nodes[i].LeastCost + "\n");
    }

    public void AddNode(Node node)
    {
        Nodes.Add(node);
        //AssetDatabase.AddObjectToAsset(node, this);
        //AssetDatabase.SaveAssets();
    }

    public Node FindFirstNode(string nodeName)
    {
        foreach (Node x in nodes.FindAll(element => element.name == nodeName))
        {
            Debug.Log("Find nodes: " + x.name);
            return x;
        }
        return null;
    }

    public Node FindNode(int nodeIndex)
    {
        if(nodeIndex < nodes.Count)
        {
            foreach (Node x in nodes.FindAll(element => element.index == nodeIndex))
            {
                //Debug.Log("Find nodes by index[" + nodeIndex.ToString() + "]: " + x.name);
                return x;
            }
            return null;
            //Debug.Log("Find nodes by index[" + nodeIndex.ToString() + "]: " + nodes[nodeIndex].name);
            //return nodes[nodeIndex];
        }
        return null;
    }

    public void CreatePOInodes(string[] name, Color[] colors)
    {
        /*   if (name.Length > 0)
           {
               POInodes = new List<Node>();
               for (int i = 0; i < name.Length; i++)
               {
                   Node nodeX = FindFirstNode(name[i]);
                   nodeX.clr = colors[i];
                   if (nodeX != null)
                       POInodes.Add(nodeX);
                   Debug.Log("Add " + nodeX.name + " as POI nodes");
                   dijkstra(roadcosts, nodeX.index);
               }
               return POInodes;
           }
           else
               return null; */



        dist = new float[Nodes.Count]; // The output array. dist[i]
                                       // will hold the shortest
                                       // distance from src to i
        if (name.Length > 0)
        {
            DateTime dt3 = DateTime.Now;
            // update color
            for (int i = 0; i < name.Length; i++)
            {
                Node nodeX = FindFirstNode(name[i]);
                // Build 0024, auto adjust to closest nodes
                if (nodeX == null)
                {
                    nodeX = FindFirstRawNode(name[i]);
                    nodeX = GetNearestNode(nodeX);
                    name[i] = nodeX.name;
                    Debug.Log("E0005:adjust POI name=" + name[i].ToString() + " to new name=" + nodeX.name);
                }
                nodeX.clr = colors[i];
                // Build 0024, manually add risk to new lines
                // TBD
                //
            }
            // create two lists for index
            List<int> POINodes_list = new List<int>();
            List<int> restNodes_list = new List<int>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                Node nodeX = FindNode(i);
                if (nodeX != null)
                {
                    if (Array.IndexOf(name, nodeX.name) == -1)
                        //if(Array.IndexOf())// felando
                        restNodes_list.Add(nodeX.index);
                    else
                        POINodes_list.Add(nodeX.index);
                }
            }
            Debug.Log("E10011:AddNodes:" + (DateTime.Now - dt3).TotalMilliseconds + " millisec");
            dt3 = DateTime.Now;

            for (int i = 0; i < POINodes_list.Count; i++)
            {
                Node nodeX = FindNode(POINodes_list[i]);
                POInodes.Add(nodeX);// Build 0009, add IFT for Graph 1
                if (nodeX != null)
                {
                    if (dist[i] <= nodeX.LeastCost)
                    {
                        nodeX.LeastCost = dist[i];
                        nodeX.MostAccessPOI = nodeX;
                    }
                }
            }
            // Build 0033, calculate the minium distances from POIs to each rest nodes
            for (int i = 0; i < restNodes_list.Count; i++) 
            {
                Node nodeX = FindNode(restNodes_list[i]);
                RestNodes.Add(nodeX); //keeping track of nodes in between
            }

            // calculate risk
            risk(roadTemporal, restNodes_list, POINodes_list);
            Debug.Log("E10012:Nearest POI & Risk(Dijkstra):" + (DateTime.Now - dt3).TotalMilliseconds + " millisec");
            dt3 = DateTime.Now;
            // Build 0033, calculate the minium distances from POIs to each rest nodes
            // calculate the minium distances to POIs for each rest node
            for (int i = 0; i < POINodes_list.Count; i++)
            {
                Node nodeX = FindNode(POINodes_list[i]);
                if (nodeX != null)
                {
                    dijkstra(roadcosts, nodeX.index);
                    for (int j = 0; j < restNodes_list.Count; j++)
                    {
                        Node nodeY = FindNode(restNodes_list[j]);
                        if (dist[restNodes_list[j]] <= nodeY.LeastCost)
                        {
                            nodeY.LeastCost = dist[restNodes_list[j]];
                            nodeY.MostAccessPOI = nodeX;
                            // update color as the same of the MostAccessPOI
                            nodeY.clr = nodeY.MostAccessPOI.clr;
                        }
                    }
                }
            }
            //// calculate the minium distances to POIs for each rest node
            //for (int i = 0; i < restNodes_list.Count; i++)
            //{
            //    Node nodeX = FindNode(restNodes_list[i]);
            //    RestNodes.Add(nodeX); //keeping track of nodes in between
            //    if (nodeX != null)
            //    {
            //        //Debug.Log("Calculate " + nodeX.name + " nodes");

            //        dijkstra(roadcosts, nodeX.index);
            //        for (int j = 0; j < POINodes_list.Count; j++)
            //        {
            //            if (dist[POINodes_list[j]] <= nodeX.LeastCost)
            //            {
            //                nodeX.LeastCost = dist[POINodes_list[j]];
            //                nodeX.MostAccessPOI = FindNode(POINodes_list[j]);
            //                // update color as the same of the MostAccessPOI
            //                nodeX.clr = nodeX.MostAccessPOI.clr;
            //            }
            //        }
            //    }
            //}

            // Print the result
            Debug.Log("POI     calculation result " + "from Source\n");
            for (int i = 0; i < restNodes_list.Count; i++)
            {
                Node nodeX = FindNode(restNodes_list[i]);
                if (nodeX != null)
                    Debug.Log(nodeX.name + " \t\t " + nodeX.MostAccessPOI + " \t\t " + nodeX.LeastCost + "\t\t RISK FACTOR: " + nodeX.riskFactor + "\n");
            }
            Debug.Log("E10013:Others:" + (DateTime.Now - dt3).TotalMilliseconds + " millisec");
        }




    }

    // A utility function to find the
    // vertex with minimum distance
    // value, from the set of vertices
    // not yet included in shortest
    // path tree
    int minDistance(float[] dist,
                    bool[] sptSet)
    {
        // Initialize min value
        float min = float.MaxValue;
        int min_index = -1;

        for (int v = 0; v < Nodes.Count; v++)
            if (sptSet[v] == false && dist[v] <= min)
            {
                min = dist[v];
                min_index = v;
            }

        return min_index;
    }

    // A utility function to print
    // the constructed distance array
    void printSolution(int[] dist, int n)
    {
        Debug.Log("Vertex     Distance " + "from Source\n");
        for (int i = 0; i < Nodes.Count; i++)
            Debug.Log(i + " \t\t " + dist[i] + "\n");
    }

    //// Function that implements Dijkstra's
    //// single source shortest path algorithm
    //// for a graph represented using adjacency
    //// matrix representation
    //void dijkstra(float[,] graph, int src)
    //{
    //    dist = new float[Nodes.Count]; // The output array. dist[i]
    //                             // will hold the shortest
    //                             // distance from src to i

    //    // sptSet[i] will true if vertex
    //    // i is included in shortest path
    //    // tree or shortest distance from
    //    // src to i is finalized
    //    bool[] sptSet = new bool[Nodes.Count];

    //    // Initialize all distances as
    //    // INFINITE and stpSet[] as false
    //    for (int i = 0; i < Nodes.Count; i++)
    //    {
    //        dist[i] = int.MaxValue;
    //        sptSet[i] = false;
    //    }

    //    // Distance of source vertex
    //    // from itself is always 0
    //    dist[src] = 0;

    //    // Find shortest path for all vertices
    //    for (int count = 0; count < Nodes.Count - 1; count++)
    //    {
    //        // Pick the minimum distance vertex
    //        // from the set of vertices not yet
    //        // processed. u is always equal to
    //        // src in first iteration.
    //        int u = minDistance(dist, sptSet);

    //        // Mark the picked vertex as processed
    //        sptSet[u] = true;

    //        // Update dist value of the adjacent
    //        // vertices of the picked vertex.
    //        for (int v = 0; v < Nodes.Count; v++)

    //            // Update dist[v] only if is not in
    //            // sptSet, there is an edge from u
    //            // to v, and total weight of path
    //            // from src to v through u is smaller
    //            // than current value of dist[v]
    //            if (!sptSet[v] && graph[u, v] != 0 &&
    //                 dist[u] != int.MaxValue && dist[u] + graph[u, v] < dist[v])
    //                dist[v] = dist[u] + graph[u, v];
    //    }

    // /*   Node Nodesrc = FindNode(src);
    //    if(Nodesrc != null)
    //        for (int i = 0; i < Nodes.Count; i++)
    //        {
    //            Node nodeX = FindNode(i);
    //            if (nodeX != null)
    //            {
    //                if (dist[i] <= nodeX.LeastCost)
    //                { 
    //                    nodeX.LeastCost = dist[i];
    //                    nodeX.MostAccessPOI = Nodesrc;
    //                }
    //            }
    //        }
    //    // print the constructed distance array
    //    printSolution(dist, Nodes.Count); */
    //}

    // Function that implements Dijkstra's
    // single source shortest path algorithm
    // for a graph represented using adjacency
    // matrix representation
    void dijkstra(float[,] graph, int src)
    {
        dist = new float[Nodes.Count]; // The output array. dist[i]
                                       // will hold the shortest
                                       // distance from src to i

        // sptSet[i] will true if vertex
        // i is included in shortest path
        // tree or shortest distance from
        // src to i is finalized
        bool[] sptSet = new bool[Nodes.Count];

        // Initialize all distances as
        // INFINITE and stpSet[] as false
        for (int i = 0; i < Nodes.Count; i++)
        {
            dist[i] = int.MaxValue;
            sptSet[i] = false;
        }

        // Distance of source vertex
        // from itself is always 0
        dist[src] = 0;

        // Find shortest path for all vertices
        for (int count = 0; count < Nodes.Count - 1; count++)
        {
            // Pick the minimum distance vertex
            // from the set of vertices not yet
            // processed. u is always equal to
            // src in first iteration.
            int u = minDistance(dist, sptSet);

            // Mark the picked vertex as processed
            sptSet[u] = true;

            // Update dist value of the adjacent
            // vertices of the picked vertex.
            for (int v = 0; v < Nodes.Count; v++)

                // Update dist[v] only if is not in
                // sptSet, there is an edge from u
                // to v, and total weight of path
                // from src to v through u is smaller
                // than current value of dist[v]
                if (!sptSet[v] && graph[u, v] != 0 &&
                     dist[u] != int.MaxValue && dist[u] + graph[u, v] < dist[v])
                    dist[v] = dist[u] + graph[u, v];
        }

        /*   Node Nodesrc = FindNode(src);
           if(Nodesrc != null)
               for (int i = 0; i < Nodes.Count; i++)
               {
                   Node nodeX = FindNode(i);
                   if (nodeX != null)
                   {
                       if (dist[i] <= nodeX.LeastCost)
                       { 
                           nodeX.LeastCost = dist[i];
                           nodeX.MostAccessPOI = Nodesrc;
                       }
                   }
               }
           // print the constructed distance array
           printSolution(dist, Nodes.Count); */
    }

    // Calculate the risk for timeframes, save the result to POIList and LeastCostList
    void risk(float[][,] temporalCost, List<int> rests, List<int> POIs)
    {
        // Build 0033, calculate the minium distances from POIs to each rest nodes
        // initialization
        for (int i = 0; i < rests.Count; i++)
        {
            Node nodeX = FindNode(rests[i]);//Nodes[rests[i]]
            nodeX.riskFactor = 0;
            nodeX.POIList = new Node[timeSteps];
            nodeX.LeastCostList = Enumerable.Repeat(Mathf.Infinity, timeSteps).ToArray();
        }
        // caluclation
        for (int k = 0; k < timeSteps; k++)
        {
            for (int i = 0; i < POIs.Count; i++)
            {
                Node nodeX = FindNode(POIs[i]);
                Node tempAccess;
                if (nodeX != null)
                {
                    float tempCost = Mathf.Infinity;
                    //DateTime dt3 = DateTime.Now;

                    dijkstra(temporalCost[k], nodeX.index);

                    for (int j = 0; j < rests.Count; j++)
                    {
                        Node nodeY = FindNode(rests[j]);
                        tempCost = dist[rests[j]];
                        tempAccess = nodeX;
                        if (tempCost <= nodeY.LeastCostList[k])
                        {
                            nodeY.LeastCostList[k] = tempCost;
                            nodeY.POIList[k] = tempAccess;
                        }
                    }
                }
            }
        }
        // Leastcost
        //for (int j = 0; j < rests.Count; j++)
        //{
        //    Node nodeY = FindNode(rests[j]);
        //    nodeY.LeastCost = nodeY.LeastCostList.Min();
        //}
        //
        // POI Label frequency calculation, get CFH result
        for (int k = 0; k < timeSteps; k++)
        {
            for (int j = 0; j < rests.Count; j++)
            {
                Node nodeY = FindNode(rests[j]);

                if (k > 0 && nodeY.POIList[k] != nodeY.POIList[k - 1])
                {
                    nodeY.riskFactor += 1;
                }
                //Debug.Log("E100131:dij:" + (DateTime.Now - dt3).TotalMilliseconds + " millisec");
            }
        }
            //// calculate the minium distances to POIs for each rest node
            //for (int i = 0; i < rests.Count; i++)
            //{
            //    Node nodeX = FindNode(rests[i]);//Nodes[rests[i]]
            //    nodeX.riskFactor = 0;
            //    nodeX.POIList = new Node[timeSteps];
            //    nodeX.LeastCostList = new float[timeSteps];
            //    Node tempAccess;
            //    if (nodeX != null)
            //    {

            //        for (int k = 0; k < timeSteps; k++)
            //        {
            //            float tempCost = Mathf.Infinity;
            //            //DateTime dt3 = DateTime.Now;

            //            dijkstra(temporalCost[k], nodeX.index);

            //            for (int j = 0; j < POIs.Count; j++)
            //            {
            //                if (dist[POIs[j]] <= tempCost)
            //                {
            //                    tempCost = dist[POIs[j]];
            //                    tempAccess = FindNode(POIs[j]);// Nodes[POIs[j]]
            //                    nodeX.LeastCostList[k] = tempCost;
            //                    nodeX.POIList[k] = tempAccess;
            //                }
            //            }
            //            // POI Label frequency calculation, get CFH result
            //            if (k > 0 && nodeX.POIList[k] != nodeX.POIList[k - 1])
            //            {
            //                nodeX.riskFactor += 1;
            //            }
            //            //Debug.Log("E100131:dij:" + (DateTime.Now - dt3).TotalMilliseconds + " millisec");
            //        }

            //    }
            //}
        }

    public class SparseMatrix<T>
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public long MaxSize { get; private set; }
        public long Count { get { return _cells.Count; } }

        private Dictionary<long, T> _cells = new Dictionary<long, T>();

        private Dictionary<int, Dictionary<int, T>> _rows =
            new Dictionary<int, Dictionary<int, T>>();

        private Dictionary<int, Dictionary<int, T>> _columns =
            new Dictionary<int, Dictionary<int, T>>();

        public SparseMatrix(int w, int h)
        {
            this.Width = w;
            this.Height = h;
            this.MaxSize = w * h;
        }

        public bool IsCellEmpty(int row, int col)
        {
            long index = row * Width + col;
            return _cells.ContainsKey(index);
        }

        public T this[int row, int col]
        {
            get
            {
                long index = row * Width + col;
                T result;
                _cells.TryGetValue(index, out result);
                return result;
            }
            set
            {
                long index = row * Width + col;
                _cells[index] = value;

                UpdateValue(col, row, _columns, value);
                UpdateValue(row, col, _rows, value);
            }
        }

        private void UpdateValue(int index1, int index2,
            Dictionary<int, Dictionary<int, T>> parent, T value)
        {
            Dictionary<int, T> dict;
            if (!parent.TryGetValue(index1, out dict))
            {
                parent[index2] = dict = new Dictionary<int, T>();
            }
            dict[index2] = value;
        }
    }
}