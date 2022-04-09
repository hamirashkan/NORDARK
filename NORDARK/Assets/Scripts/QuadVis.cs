using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadVis : MonoBehaviour
{
    public GameObject Container;
    public GameObject AssetQuad;
    public Vector3 StartPosition;
    public float x_Margin;//x
    public float z_Margin;//z
    public int x_cols;
    public int z_rows;
    public float[] value;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Redraw()
    {
        // check the length of array
        if ((value != null) && (value.Length == x_cols * z_rows))
        {
            // Judge to delete and redraw
            DestroyChildren(Container.name);

            Vector3[] verticesC;
            for (int z = z_rows; z > 0 + 1; z--)
                for (int x = 0; x < x_cols - 1; x++)
                {
                    int i = (z_rows - z) * x_cols + x;
                    GameObject NewQuad = Instantiate(AssetQuad);
                    NewQuad.name = "Quad_" + x.ToString() +"_" + (z_rows - z).ToString();
                    NewQuad.transform.parent = Container.transform;
                    
                    verticesC = NewQuad.GetComponent<MeshFilter>().mesh.vertices;
                    Vector3 v0 = new Vector3(StartPosition.x + x * x_Margin, StartPosition.y + value[i], StartPosition.z + z * z_Margin);
                    Vector3 v1 = new Vector3(StartPosition.x + (x + 1) * x_Margin, StartPosition.y + value[i + 1], StartPosition.z + z * z_Margin);
                    Vector3 v2 = new Vector3(StartPosition.x + x * x_Margin, StartPosition.y + value[i + x_cols], StartPosition.z + (z + 1) * z_Margin);
                    Vector3 v3 = new Vector3(StartPosition.x + (x + 1) * x_Margin, StartPosition.y + value[i + 1 + x_cols], StartPosition.z + (z + 1) * z_Margin);

                    verticesC[0] = v0;
                    verticesC[1] = v1;
                    verticesC[2] = v2;
                    verticesC[3] = v3;

                    NewQuad.GetComponent<MeshFilter>().mesh.vertices = verticesC;
                }
        }
    }

    public void DestroyChildren(string parentName)
    {
        Transform[] children = GameObject.Find(parentName).GetComponentsInChildren<Transform>();
        for (int i = 1; i < children.Length; i++)
        {
            Destroy(children[i].gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
