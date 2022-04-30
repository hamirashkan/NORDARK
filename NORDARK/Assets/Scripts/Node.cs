using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GeoJSON.Net.Geometry;

public class Node : ScriptableObject
{
    public Vector3 vec;
    // Build 0026
    public Vector2 GeoVec;
    //
    public Transform objTransform;
    public GameObject obj;
    public Color clr;
    public float LeastCost = Mathf.Infinity;// the least access time
    public Node MostAccessPOI = null;// the most accessible POI
    public int index;// index for calculating the shortest path easlier
    public string stop_id; // stop_id from raw data file Value[0]
    public List<string> visited = new List<string>(); //List of nodes already visited by this node
    public Node[] POIList = null; //List of POIs for risk calculation
    public float[] LeastCostList; //List of costs for different POIs for risk calculation
    public float riskFactor; //riskFactor calculation happens in Graph.cs

    public int x_index;
    public int z_index;

    public Vector3 globalposition;

    public int indexOfPOI = -1;

    [SerializeField]
    private List<string> neighborNames;
    public List<string> NeighborNames
    {
        get
        {
            if (neighborNames == null)
            {
                neighborNames = new List<string>();
            }

            return neighborNames;
        }
    }


    [SerializeField]
    private List<Node> neighbors;
    public List<Node> Neighbors
    {
        get
        {
            if (neighbors == null)
            {
                neighbors = new List<Node>();
            }

            return neighbors;
        }
    }

    [SerializeField]
    private List<float> weights;
    public List<float> Weights
    {
        get
        {
            if (weights == null)
            {
                weights = new List<float>();
            }

            return weights;
        }
    }

    public static Node Create(string name, Vector3 pos)
    {
        Node node = new Node();// CreateInstance<Node>();
        //string path = string.Format("Assets/{0}.asset", name);
        //AssetDatabase.CreateAsset(node, path);

        return node;
    }

    public static T Create<T>(string name, Vector3 pos)
    where T : Node
    {
        T node = CreateInstance<T>();
        node.name = name;
        node.vec = pos;
        return node;
    }
}

public class AuxLine //: ScriptableObject
{
    public string LineName;

    [SerializeField]
    private List<Vector3> auxNodes;
    public List<Vector3> AuxNodes
    {
        get
        {
            if (auxNodes == null)
            {
                auxNodes = new List<Vector3>();
            }

            return auxNodes;
        }
    }
    public int startNodeIndex;
    public Vector3 startNodePosition;
    public int stopNodeIndex;
    public Vector3 stopNodePosition;
    public Dictionary<string, object> properties;
    // Build 0045, output edges
    public List<IPosition> geoPointList;
}
