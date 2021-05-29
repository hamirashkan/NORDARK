using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMap : MonoBehaviour
{
    public TerrainData td;
    // Start is called before the first frame update
    void Start()
    {
        td = gameObject.GetComponent<Terrain>().terrainData;
        float[,] HeightMap = new float[td.heightmapResolution, td.heightmapResolution];

        for (int x = 0; x < td.heightmapResolution; x+=5)
        {
            for (int y = 0; y < td.heightmapResolution; y+=5)
            {
                HeightMap[x, y] = 0.02f;
            }
        }

        td.SetHeights(0, 0, HeightMap);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
