using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class Graph : ScriptableObject
{
    private List<Node> POInodes;
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

    public static Graph Create(string name)
    {
        Graph graph = CreateInstance<Graph>();

        string path = string.Format("Assets/{0}.asset", name);
        AssetDatabase.CreateAsset(graph, path);

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
        AssetDatabase.AddObjectToAsset(node, this);
        AssetDatabase.SaveAssets();
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
            // update color
            for (int i = 0; i < name.Length; i++)
            {
                Node nodeX = FindFirstNode(name[i]);
                nodeX.clr = colors[i];
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


            for(int i =0; i < POINodes_list.Count; i++)
            {
                Node nodeX = FindNode(POINodes_list[i]);
                if (nodeX != null)
                {
                    if (dist[i] <= nodeX.LeastCost)
                    {
                        nodeX.LeastCost = dist[i];
                        nodeX.MostAccessPOI = nodeX;
                    }
                }
            }

            // calculate risk
            risk(roadTemporal, restNodes_list, POINodes_list);

            // calculate the minium distances to POIs for each rest node
            for (int i = 0; i < restNodes_list.Count; i++)
            {
                Node nodeX = FindNode(restNodes_list[i]);
                RestNodes.Add(nodeX); //keeping track of nodes in between
                if (nodeX != null)
                {
                    Debug.Log("Calculate " + nodeX.name + " nodes");
                    dijkstra(roadcosts, nodeX.index);

                    for (int j = 0; j < POINodes_list.Count; j++)
                    {
                        if (dist[POINodes_list[j]] <= nodeX.LeastCost)
                        {
                            nodeX.LeastCost = dist[POINodes_list[j]];
                            nodeX.MostAccessPOI = FindNode(POINodes_list[j]);
                            // update color as the same of the MostAccessPOI
                            nodeX.clr = nodeX.MostAccessPOI.clr;
                        }
                    }

                }
            }
            // Print the result
            Debug.Log("POI     calculation result " + "from Source\n");
            for (int i = 0; i < restNodes_list.Count; i++)
            {
                Node nodeX = FindNode(restNodes_list[i]);
                if (nodeX != null)
                    Debug.Log(nodeX.name + " \t\t " + nodeX.MostAccessPOI + " \t\t " + nodeX.LeastCost + "\t\t RISK FACTOR: " + nodeX.riskFactor + "\n");
            }
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
    

    void risk(float[][,] temporalCost,List<int> rests,List<int> POIs)
    {
        // calculate the minium distances to POIs for each rest node
        for (int i = 0; i < rests.Count; i++)
        {
            Node nodeX = FindNode(rests[i]);
            nodeX.riskFactor = 0;
            nodeX.POIList = new Node[timeSteps];
            nodeX.LeastCostList = new float[timeSteps];
            Node tempAccess;
            if (nodeX != null)
            {
                for (int k = 0; k < timeSteps; k++)
                {
                    float tempCost = Mathf.Infinity;
                    dijkstra(temporalCost[k], nodeX.index);
                    for (int j = 0; j < POIs.Count; j++)
                    {
                        if (dist[POIs[j]] <= tempCost)
                        {
                            tempCost = dist[POIs[j]];
                            tempAccess = FindNode(POIs[j]);
                            nodeX.LeastCostList[k] = tempCost;
                            nodeX.POIList[k] = tempAccess;
                        }
                    }

                    if (k > 0 && nodeX.POIList[k] != nodeX.POIList[k - 1])
                    {
                        nodeX.riskFactor += 1;
                    }
                }

            }
        }
    }


}