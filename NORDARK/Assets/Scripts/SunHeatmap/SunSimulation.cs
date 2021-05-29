using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;

public class SunSimulation : MonoBehaviour
{
    public Vector3 origin = new Vector3(0f, 0f, 0f);
    public float speed = 1;
    public string season;
    public Vector3 axis;
    public float seasonY;
    private string seasonVal;
    private Vector3 temp;
    private string prevSeason;
    void Start()
    {
        changeSeason();
        temp = transform.position;
        temp.y = seasonY;
        temp.x = 0;
        temp.z = 5000;

        transform.position = temp;
        axis = new Vector3(0f, Mathf.Sin(Mathf.PI / 3), Mathf.Cos(Mathf.PI / 3));

        origin.y = seasonY;

        /*temp.y and origin.y:  spring : april : -1200 ,,, summer : july : -100 ,,, fall : october : -2500 ,,, winter : january : -4000 */
    }

    void Update()
    {
        changeSeason();

        speed = GameObject.Find("ShadowMapUI").GetComponent<UIScript>().sunSpeed;

        var angle = speed * Time.deltaTime*40;

        transform.RotateAround(origin, axis, angle);
    }

    void changeSeason()
    {
        seasonVal = GameObject.Find("ShadowMapUI").GetComponent<UIScript>().season;
        if (seasonVal != prevSeason)
        {
            switch (seasonVal)
            {
                case "Spring":
                    seasonY = -1200;
                    break;
                case "Summer":
                    seasonY = -100;
                    break;
                case "Winter":
                    seasonY = -4000;
                    break;
                case "Autumn":
                    seasonY = -2500;
                    break;

            }
            //temp = transform.position;
            temp.y = seasonY;
            transform.position = temp;
            origin.y = seasonY;
        }
        prevSeason = seasonVal;
    }
}