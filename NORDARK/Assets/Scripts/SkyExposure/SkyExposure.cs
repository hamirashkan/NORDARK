using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SkyExposure : MonoBehaviour
{
    private int Width;
    private int Length;
    public GameObject TexturePlane;
    public GameObject CubeMap;
    public GameObject Cube;
    public GameObject currentPlaceableObject;
    int count;
    int countRay;
    float Exposure;
    public static float Exposure_text;
    public Vector3[] posArray;
    public float[] percentArray;

    public Color X1 { get; private set; }
    public Color X2 { get; private set; }

    Vector3 center;
    int width = 210, height = 210;
    int times = 5, reftime = 10;
    int ratio;
    public Text SkyPercentage;
    public Slider ResolutionSlider;
    public Text SliderValue;
    //public Text Percentage;
    //public GameObject TextureImge; //UI reference
    //public GameObject CameraImage; //UI reference
    //static float step = 0.1f;//1
    //int stepNum = 2;// Math.Floor(1/step);//1

    void Start()
    {

        ratio = Mathf.RoundToInt(reftime / times);
        TexturePlane.transform.localScale = new Vector3(width, 1, height);
        posArray = new Vector3[width * ratio * height * ratio];//width * stepNum * height * stepNum
        percentArray = new float[posArray.Length];
        InitPos();
    }

    private void mouseLocation()
    {
        Vector3 clickPosition = -Vector3.one;
        Ray ray;

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //Plane plane = new Plane(Vector3.up, 0f);
        //float distanceToplane;
        //if (plane.Raycast(ray, out distanceToplane)) {
        //    clickPosition = ray.GetPoint(distanceToplane);
        //    var relative_Pos = clickPosition - center + new Vector3(width * reftime / 2f, 0, height * reftime / 2f);
        //    int i = Mathf.RoundToInt(relative_Pos.x / times);
        //    int j = Mathf.RoundToInt(relative_Pos.z / times);
        //    float v1 = percentArray[i * height * ratio + j];
        //    float v2 = percentArray[(i + 1) * height * ratio + j];
        //    float v3 = percentArray[i * height * ratio + j + 1];
        //    float v4 = percentArray[(i + 1) * height * ratio + j + 1];

        //    //Debug.Log("Position " + Position +
        //    //    ",i=" + (i).ToString() + 
        //    //    ",j=" + (j).ToString() +
        //    //    ",p1=" + posArray[i * height * ratio + j].ToString()  +
        //    //    ",p4=" + posArray[(i + 1) * height * ratio + j + 1].ToString() +
        //    //    ",v1(" + (i * height * ratio).ToString() + ")=" + v1.ToString() +
        //    //    ",v2(" + ((i + 1) * height * ratio + j).ToString() + ")=" + v2.ToString() +
        //    //    ",v3(" + (i * height * ratio + j + 1).ToString() + ")=" + v3.ToString() +
        //    //    ",v4(" + ((i + 1) * height * ratio + j + 1).ToString() + ")=" + v4.ToString());
        //    Cube.transform.position = clickPosition + new Vector3(width * reftime + 10, 0, 0);
        //    CubeMap.transform.position = clickPosition;
        //    Exposure = ((v1 + v2 + v3 + v4) / 4);
        //    Debug.Log("Position " + clickPosition + ",sky_percents=" + Exposure.ToString());
        //    SkyPercentage.text = (Exposure.ToString("0.00") + "%");
        //   // Percentage.text = (Exposure.ToString("0.00") + "%");

        //}

        RaycastHit hit;
        //int layerMask = 1 << 8;
        if (Physics.Raycast(ray, out hit))
        {

            var Position = hit.point;
            var relative_Pos = Position - center + new Vector3(width * reftime / 2f, 0, height * reftime / 2f);
            int i = Mathf.RoundToInt(relative_Pos.x / times);
            int j = Mathf.RoundToInt(relative_Pos.z / times);
            float v1 = percentArray[i * height * ratio + j];
            float v2 = percentArray[(i + 1) * height * ratio + j];
            float v3 = percentArray[i * height * ratio + j + 1];
            float v4 = percentArray[(i + 1) * height * ratio + j + 1];
            Cube.transform.position = Position + new Vector3(width * reftime + 10, 0, 0);
            CubeMap.transform.position = Position;
            Exposure = ((v1 + v2 + v3 + v4) / 4);
            //Debug.Log("Position " + Position + ",sky_percents=" + Exposure.ToString());
            SkyPercentage.text = (Exposure.ToString("0.00") + "%" + " "+ "                         " + Position.ToString());

        }
    }

    private void InitPos()
    {
        X1 = Color.clear;
        X2 = Color.clear;

        //GameObject ma = GameObject.Find("16/33887/18096");
        //center = ma.transform.localPosition;//.GetComponent<MeshFilter>().mesh.bounds.center; // width = height = 30
        center = new Vector3(-7.3f, 0.0f, 17.9f);

        TexturePlane.transform.localPosition = center + new Vector3(width * reftime + 10, 0, 0);// - new Vector3(times, 0, times);


        for (int i = 0; i < width * ratio; i++)
        {
            for (int j = 0; j < height * ratio; j++)
            {
                posArray[i * height * ratio + j] = center + new Vector3(i * times, 0, j * times) - new Vector3(width * reftime / 2f, 0, height * reftime / 2f);// - new Vector3(times, 0, times);

            }
        }
    }

    public void Cal_posiotion()
    {
        //Debug.Log("Start" + DateTime.Now.ToString());
        InitPos();
        //Debug.Log("InitPos" + DateTime.Now.ToString());
        for (int i = 0; i < posArray.Length; i++)
        {
            count = 0;
            Exposure = 0;
            RaycastHit casthit;
            Vector3 direct = new Vector3(0, -1, 0);
            //new Vector3(-38, 9, -10)
            if (Physics.Raycast(posArray[i], direct, out casthit, 200))//1 << 6))
            {
                //Debug.DrawRay(posArray[i], direct * 1000, Color.clear);
                posArray[i] = casthit.point;
            }
            RayCastNew(posArray[i]);
            //transform.position = posArray[i];
            //RayCast();
            percentArray[i] = (((float)countRay - (float)count) / (float)countRay) * 100;
        }
        //Debug.Log("percentArray" + DateTime.Now.ToString());
        ChangeTexture();
        //Debug.Log("ChangeTexture" + DateTime.Now.ToString());
    }

    /*private void RayCast()
    {
        countRay = 0;
        RaycastHit hit;
        for (int i = 0; i < Width * 2 - 1; i += 1)
        {
            for (int j = 0; j < Length * 2 - 1; j += 1)
            {
                Vector3 Direction = new Vector3((-Width / 2) + (0.5f * i), 9 , (-Length / 2) + (0.5f * j));
                Debug.DrawRay(transform.position, Direction * 5, X1);
                countRay += 1;

                if (Physics.Raycast(transform.position, Direction, out hit, 45f))
                {
                    Debug.DrawRay(transform.position,Direction * 5, X2);
                    count += 1;
                }
            }
        }
    }*/

    private void RayCastNew(Vector3 pos)
    {
        countRay = 0;
        RaycastHit hit;
        float ray_length = 10f;
        float error_tolorance = 1e-4f;
        float plane_w_angle = 120f * Mathf.PI / 180f; // 120 degree for width
        float plane_l_angle = 90f * Mathf.PI / 180f; // 90 degree for width
        float step_i = plane_w_angle / Width; // Width = 2, step_i = 60
        float step_j = plane_l_angle / Length;// Length = 2, step_j = 45
        float offset_w = -Mathf.PI / 2 + (Mathf.PI - plane_w_angle) / 2 + step_i / 2;//-90+30+30=-30
        float offset_l = -Mathf.PI / 2 + (Mathf.PI - plane_l_angle) / 2 + step_j / 2;//-90+45+22.5=-22.5
        for (float i = 0; i < plane_w_angle - error_tolorance; i = i + step_i)
        {
            for (float j = 0; j < plane_l_angle - error_tolorance; j = j + step_j)
            {
                Vector3 Direction = new Vector3(Mathf.Tan(i + offset_w), 1, Mathf.Tan(j + offset_l));
                Debug.DrawRay(pos, Direction * ray_length, X1);
                countRay += 1;

                if (Physics.Raycast(pos, Direction, out hit, 45f))
                {
                    Debug.DrawRay(pos, Direction * ray_length, X2);
                    count += 1;
                }
            }
        }

        /// previous code version
        //for (int i = 1; i < Width * 2; i += 1)
        //{
        //    for (int j = 1; j < Length * 2; j += 1)
        //    {
        //        Vector3 Direction = new Vector3((-Width * 2) + (2 * i), 8, (-Length * 2) + (2 * j));
        //        Debug.DrawRay(pos, Direction * 5, X1);
        //        countRay += 1;

        //        if (Physics.Raycast(pos, Direction, out hit, 45f))
        //        {
        //            Debug.DrawRay(pos, Direction * 5, X2);
        //            count += 1;
        //        }
        //    }
        //}
    }


    private void ChangeTexture()
    {
        Renderer render = TexturePlane.transform.GetComponent<Renderer>();
        Vector3 mapSize = render.bounds.size;
        Texture2D texture = new Texture2D((int)mapSize.x, (int)mapSize.z);
        render.material.mainTexture = texture;
        render.material.mainTexture.filterMode = FilterMode.Trilinear;

        //timer
        for (int i = 0; i < width * ratio; i++)
        {
            for (int j = 0; j < height * ratio; j++)
            {
                int index = i * height * ratio + j;
                Color color = new Color(percentArray[index] / 100, percentArray[index] / 100, percentArray[index] / 100);
                //Debug.Log(index);
                Color[] colors = new Color[100];
                for (int k = 0; k < colors.Length; k++)
                    colors[k] = color;
                texture.SetPixels((width * ratio - i - 1) * times, (height * ratio - j - 1) * times, times, times, colors);
            }
        }
        //TextureImge.GetComponent<RawImage>().texture = texture;
        texture.Apply();
    }

    //void OnGUI()
    //{
    //    GUI.backgroundColor = Color.magenta;
    //    GUIStyle myButtonStyle = new GUIStyle(GUI.skin.button);
    //    myButtonStyle.fontSize = 25;
    //    GUIStyle myButtonStyle1 = new GUIStyle(GUI.skin.button);
    //    myButtonStyle1.fontSize = 15;
    //    Rect myRect1 = new Rect(10, 350, 270, 40);
    //    GUI.Box(myRect1, "Sky exposure: " + SkyExposure.Exposure_text.ToString("0.0") + "%", myButtonStyle);

    //    if (GUI.Button(new Rect(280, 350, 40, 40), "Ray", myButtonStyle1))
    //    {

    //        if (isClicked)
    //        {

    //            isClicked = false;
    //            X1 = Color.clear;
    //            X2 = Color.clear;
    //        }
    //        else
    //        {

    //            isClicked = true;
    //            X1 = Color.cyan;
    //            X2 = Color.red;
    //        }
    //    }
    //    if (GUI.Button(new Rect(340, 350, 40, 40), "Calculte", myButtonStyle1))
    //    {

    //        Cal_posiotion();
    //    }
    //}



    void Update()
    {
        Width = Mathf.FloorToInt(ResolutionSlider.value);
        Length = Mathf.FloorToInt(ResolutionSlider.value);
        SliderValue.text = (ResolutionSlider.value.ToString("0"));
        mouseLocation();
        //Vector3 test = new Vector3(0, 90, 0);
        //RayCastNew(test);
        SkyObject();
    }

    public void StopCalculation()
    {
        //TexturePlane.GetComponent<Renderer>().material.mainTexture = ;
    }
    public void SkyObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Transform SkyBars = GameObject.Find("SkyBars").transform;
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo))
        {
            if (Input.GetMouseButtonDown(1))
            {
                GameObject SkyBar;
                SkyBar = Instantiate(currentPlaceableObject);
                SkyBar.name = "SkyBar" + "_" + DTtoGameObjectName(DateTime.Now);
                SkyBar.transform.parent = SkyBars;
                SkyBar.transform.position = hitInfo.point;
                BarIndication BarSample = (BarIndication)SkyBar.GetComponent("BarIndication");
                BarSample.LabelText = hitInfo.point.ToString();
                BarSample.BarValue = Exposure;
                //SkyBar.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
                //SkyBar.transform.localScale = new Vector3(SkyBar.transform.localScale.x, Exposure * 0.05f, SkyBar.transform.localScale.z);
                //currentPlaceableObject.GetComponentsInChildren<Renderer>().material.color = new Color(1-Exposure / 100f, Exposure / 100f,0 );
                //SkyPercentage.color = new Color(1 - Exposure / 100f, Exposure / 100f, 0);

                foreach (Renderer r in SkyBar.GetComponentsInChildren<Renderer>())
                {
                    if(r.gameObject.name == "Value")
                        r.material.color = new Color(1 - (Exposure * 0.8f) / 100f, (Exposure * 0.8f) / 100f, 0);
                }

            }
        }

    }
    public string DTtoGameObjectName(DateTime dt)
    {
        string sp = "-";
        string mp = "_";
        return dt.Year.ToString() + sp + dt.Month.ToString().PadLeft(2, '0') + sp + dt.Day.ToString().PadLeft(2, '0') + mp + dt.Hour.ToString().PadLeft(2, '0') + sp + dt.Minute.ToString().PadLeft(2, '0') + sp + dt.Second.ToString().PadLeft(2, '0') + sp + dt.Millisecond.ToString().PadLeft(3, '0');
    }

    public void DestroyChildren()
    {
        string parentName = "SkyBars";
        Transform[] buildingSet = GameObject.Find(parentName).GetComponentsInChildren<Transform>();
        for (int i = 1; i < buildingSet.Length; i++)
        {
            Destroy(GameObject.Find(buildingSet[i].name) as GameObject);
        }
    }
}
