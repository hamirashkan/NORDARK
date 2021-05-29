
using UnityEngine;

public class Texture : MonoBehaviour
{
    public GameObject plane;
    private float[] perA;

    private void Start()
    {
        perA = plane.GetComponent<SkyExposure>().percentArray;
    }

    private void Update()
    {
        
        Vector3 mapSize = transform.GetComponent<Renderer>().bounds.size;
        Texture2D texture = new Texture2D((int)mapSize.x, (int)mapSize.z);
        GetComponent<Renderer>().material.mainTexture = texture;
        GetComponent<Renderer>().material.mainTexture.filterMode = FilterMode.Trilinear;

        //int times = 10;
        //if (texture.height * texture.width <= ((int)mapSize.x * (int)mapSize.z))
        //    for (int x = 0; x < texture.width; x += 1)
        //    {
        //        for (int y = 0; y < texture.height; y+=1)
        //        {
        //            Color color = new Color(perA[x * texture.height/ times + y/ times] / 100, perA[x * texture.height / times + y / times] / 100, perA[x * texture.height / times + y / times] / 100);
        //            Debug.Log(x * texture.height / times + y / times);
        //            texture.SetPixel(x , y , color);
        //        }
        //    }
        //timer
        int width = 100, height = 100;
        int times = 10;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int index = i * height + j;
                Color color = new Color(perA[index] / 100, perA[index] / 100, perA[index] / 100);
                Debug.Log(index);
                Color[] colors = new Color[100];
                for (int k = 0; k < colors.Length; k++)
                    colors[k] = color;
                texture.SetPixels(i * times, j * times, times, times, colors);
            }
        }
        texture.Apply();
    }  

}

