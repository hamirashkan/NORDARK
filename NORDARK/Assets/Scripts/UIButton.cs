using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;

public class UIButton : MonoBehaviour
{
    public ShowMap other;

    public static GameObject bg;
    public static bool isOn = true;
    public static bool isIFT = true;
    public static bool isIFTCost = true;

    // Start is called before the first frame update
    void Start()
    {
        other = GameObject.Find("Mapbox").GetComponent<ShowMap>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OptionValueChange()
    {
        //int op = this.GetComponent<Dropdown>().value;
        other.DestroyChildren("Nodes");
        other.DestroyChildren("Edges");
        //other.DisplayMapbox = Instantiate(GameObject.Find("Mapbox"));
        StartCoroutine(other.CreateMap(0.01f));
    }

    public void HideValueChange()
    {
        bg.SetActive(!this.GetComponent<Toggle>().isOn);
        isOn = this.GetComponent<Toggle>().isOn;
    }

    public void UseIFTChange()
    {
        isIFT = this.GetComponent<Toggle>().isOn;
        OptionValueChange();
    }

    public void UseIFTCostChange()
    {
        isIFTCost = this.GetComponent<Toggle>().isOn;
        OptionValueChange();
    }

    public void test()
    {
        double lat = 62.7233, lon = 7.51087;
        int z = 8;
        int x = long2tilex(lon, z);
        int y = lat2tiley(lat, z);
        Debug.Log("lon=" + lon + ", z=" + z + ", tile_x=" + x);
        Debug.Log("lat=" + lat + ", z=" + z + ", tile_y=" + y);
        Debug.Log("tile_x=" + x + ", z=" + z + ", lon=" + tilex2long(x, z));
        Debug.Log("tile_y=" + y + ", z=" + z + ", lat=" + tiley2lat(y, z));
        x--;
        Debug.Log("tile_x=" + x + ", z=" + z + ", lon=" + tilex2long(x, z));
        Debug.Log("tile_y=" + y + ", z=" + z + ", lat=" + tiley2lat(y, z));
        Debug.Log("lon=" + lon + ", z=" + z + ", tile_x=" + x);
        Debug.Log("lat=" + lat + ", z=" + z + ", tile_y=" + y);
        Debug.Log("difflon=" + lon + ", z=" + z + ", tile_x=" + (tilex2long(x + 1, z) - tilex2long(x, z)));
        Debug.Log("difflat=" + lat + ", z=" + z + ", tile_y=" + (tiley2lat(y + 1, z) - tiley2lat(y, z)));

        Debug.Log("timeIndex=" + other.timeIndex);
    }

    public void ComputeCFH()
    {
        other.ComputeCFH(1, other.SliderStartTimeValue - 1, other.SliderStopTimeValue - 1);
    }

    public void ComputeTDM()
    {
        other.ComputeTDM();
    }

    int long2tilex(double lon, int z)
    {
        return (int)(Math.Floor((lon + 180.0) / 360.0 * (1 << z)));
    }

    double ToRadians(double deg)
    {
        double rad = deg / 180.0 * Math.PI;
        return rad;
    }
    int lat2tiley(double lat, int z)
    {
        return (int)Math.Floor((1 - Math.Log(Math.Tan(ToRadians(lat)) + 1 / Math.Cos(ToRadians(lat))) / Math.PI) / 2 * (1 << z));
    }

    double tilex2long(int x, int z)
    {
        return x / (double)(1 << z) * 360.0 - 180;
    }

    double tiley2lat(int y, int z)
    {
        double n = Math.PI - 2.0 * Math.PI * y / (double)(1 << z);
        return 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
    }

    int lat2x(double lng, int zoom)
    {
        var x = (int)Math.Floor((lng + 180.0) / 360.0 * Math.Pow(2.0, zoom));
        return x;
    }

    int lon2y(double lat, int zoom)
    {
        var y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0)
                + 1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom));
        return y;
    }
}
