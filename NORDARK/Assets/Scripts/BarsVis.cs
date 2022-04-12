using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarsVis : MonoBehaviour
{
    public GameObject Container;
    public GameObject AssetBar;
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
            for (int z = z_rows; z > 0; z--)
                for (int x = 0; x < x_cols; x++)
                {
                    int i = (z_rows - z) * x_cols + x;
                    GameObject NewBar = Instantiate(AssetBar);
                    NewBar.name = "Vertex_" + i.ToString();
                    NewBar.transform.parent = Container.transform;// local position, ignore the parent. Position, global position
                    NewBar.transform.localPosition = new Vector3(StartPosition.x + x * x_Margin, Mathf.Min(StartPosition.y + value[i], 100), StartPosition.z + z * z_Margin);
                    NewBar.transform.localScale = new Vector3(1, Mathf.Min((StartPosition.y + value[i]) * 2, 200), 1);
                    //NewBar.transform.position = new Vector3(StartPosition.x + x * x_Margin, StartPosition.y, StartPosition.z + z * z_Margin);
                    //NewBar.GetComponent<BarIndication>().BarValue = value[i];
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
