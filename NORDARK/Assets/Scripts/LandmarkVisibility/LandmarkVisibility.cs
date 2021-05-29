using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.UI;

public class LandmarkVisibility : MonoBehaviour
{

    public Button button;
    public Color wantedColor;
    public Text buttonText;

    public TextMeshProUGUI diplayLandmarkScore;

    public Slider heightSlider;
    public TextMeshProUGUI levelValue;

    public Slider circleSlider;
    public TextMeshProUGUI circleValue;

    public Slider angleSlider;
    public TextMeshProUGUI angleValue;

    public TextMeshProUGUI totalNofBuildings;
    public TextMeshProUGUI heightSelected;

    private float turnAngle = 0.0175f;
    public static bool computeLandmark = false;
    public static float visibilityHeight = 1.0f;
    public int heightLevels = 4;
    

    private int totalBuildings;
    private int noOfHittedBuildings;
    private float temp;
    private float angleIncrement;
    private float changeHeight;

    // private Dictionary<string, int> buildingCounters = new Dictionary<string, int>();
    private Dictionary<Vector3, int> buildingCounters = new Dictionary<Vector3, int>();
    private float minIntensity;
    private float maxIntensity;
    Color ourColor;
    public Camera myCamera;

    public static List<GameObject> Buildingsno = new List<GameObject>();
    public static GameObject[] objs;
    private List<GameObject> hittedBuildings = new List<GameObject>();
    private List<float>  visibilityScores = new List<float>();

    public Transform markerPosition;
    public GameObject marker;
    private GameObject markerInstance;


    void Start()
    {
        
    }

    void Update()
    {
        temp = Mathf.Round(heightSlider.value  * 10f);
        levelValue.text = temp.ToString() + "%";
        circleValue.text = circleSlider.value.ToString();
        angleValue.text = angleSlider.value.ToString();
        if (computeLandmark)
        {
            LandmarkComputation();
        }
    }

    public void LandmarkComputation()
    {

        InitiateCounter();
        hittedBuildings.Clear();
        float radius;
        float initial_x = 0f;
        float initial_z = 0f;
        

        if (Input.GetMouseButtonDown(0))
        {
            if (markerInstance!= null)
            {
                Destroy(markerInstance);
            }
            RaycastHit hitInfo;
            bool hit = Physics.Raycast(myCamera.ScreenPointToRay(Input.mousePosition), out hitInfo);
            float counter = 0f;
            float totalRays = 0f;
            if (hit)
            {
                if ((hitInfo.transform.gameObject.tag == "Buildings"))
                {   
                    var selection = hitInfo.transform;
                    selection.GetComponent<Renderer>().material.color = Color.blue;
                 

                    Vector3 selectionPosition = selection.position;
                    selectionPosition.y = selection.GetComponent<MeshFilter>().mesh.bounds.center.y;
                    Vector3 selectionScale = selection.GetComponent<MeshFilter>().mesh.bounds.extents;
                    heightSelected.text = "Height of Landmark: " + Mathf.Round(selectionScale.y*2.73f).ToString() + "m";


                        if (selectionScale.x < selectionScale.z)
                        {
                            radius = selectionScale.x / 3;
                            transform.position = selectionPosition;

                            initial_x = transform.position.x + radius;
                            initial_z = transform.position.z;
                        }
                        
                        else
                        {
                            radius = selectionScale.z / 3;
                            transform.position = selectionPosition;

                            initial_x = transform.position.x;
                            initial_z = transform.position.z - radius;
                        }

                    float yError = -0.523330f;

                    transform.position = selectionPosition;

                    selection.GetComponent<MeshCollider>().enabled = false;
                    
                    
                    changeHeight = (float) 2*selectionScale.y/heightLevels;

                    float circleDivisions = (float) 360/circleSlider.value;

                    float integerY = (float) selectionPosition.y + selectionScale.y;
                    visibilityHeight = heightSlider.value/10;
                    

                    float topLevel = visibilityHeight * integerY;
                    float bottomLevel =  (float) selectionPosition.y - selectionScale.y;
                    transform.Translate(new Vector3(0, -selectionScale.y, 0));

                    angleIncrement = angleSlider.value;

                    for (float i = bottomLevel; i <= topLevel-yError/2; i+=changeHeight)
                    {
                        for (float m = 0; m <360; m+=circleDivisions)
                        {
                            for (float n = 0; n <360; n+=circleDivisions)
                            {
                                transform.RotateAround(new Vector3(initial_x, i, initial_z), Vector3.up , circleDivisions);
                                for (float k = 0; k < 360; k+=angleIncrement)
                                {

                                    for (float j = 0; j < 360; j+=angleIncrement)
                                    {
                                        RaycastHit intersect;
                                        Vector3 rayDirection = new Vector3(Mathf.Cos(k * turnAngle), Mathf.Sin(k * turnAngle) * Mathf.Cos(j * turnAngle), Mathf.Sin(k * turnAngle) * Mathf.Sin(j * turnAngle));
                                        Ray viewRay = new Ray(transform.position, rayDirection);

                                        totalRays++;
                                        // This Debug works: (Do not Delete)
                                        // Debug.DrawRay(transform.position, rayDirection * 1f , Color.red, 20);

                                        if (Physics.Raycast(viewRay, out intersect))
                                        {
                                            
                                            var newSelection = intersect.transform;
                                
                                            if (newSelection.CompareTag("Buildings"))
                                            {
                                                GameObject buildingss = intersect.collider.gameObject;
                                                if (!hittedBuildings.Contains(buildingss))
                                                {
                                                    hittedBuildings.Add(buildingss);
                                                    noOfHittedBuildings = hittedBuildings.Count;
                                                }

                                                if (buildingCounters.ContainsKey(newSelection.localPosition))
                                                {
                                                    buildingCounters[newSelection.localPosition] = buildingCounters[newSelection.localPosition] + 1;
                                                    if (buildingCounters[newSelection.localPosition] > maxIntensity)
                                                    {
                                                        maxIntensity = buildingCounters[newSelection.localPosition];
                                                    }
                                                    if (buildingCounters[newSelection.localPosition] < minIntensity)
                                                    {
                                                        minIntensity =  buildingCounters[newSelection.localPosition];
                                                    }

                                                }
                                                counter++;
                                            }

                                            else
                                            {
                                                continue;
                                            }
                                        }
                                    }
                            
                                }
                            }
                        }

                        float rayRatio = 100*  noOfHittedBuildings/ totalBuildings;
                        rayRatio = Mathf.Round(rayRatio * 100f) / 100f;
                        visibilityScores.Add(rayRatio);
                  

                        diplayLandmarkScore.text = "Landmark Visibility: " + rayRatio.ToString() + "%";

                        transform.Translate(new Vector3(0, changeHeight, 0));
                    }
                    
                    selection.GetComponent<MeshCollider>().enabled = true;
                }
                
                transform.Translate(new Vector3(0, -changeHeight, 0));

                markerInstance = Instantiate(marker, markerPosition.position, marker.transform.rotation);

                
            }
        }

        foreach (GameObject obj in hittedBuildings)
        {
            float counterValue = buildingCounters[obj.transform.localPosition];
            float maxColorIntensity = counterValue/Mathf.Sqrt(maxIntensity*maxIntensity+minIntensity*minIntensity);
            Color ourColor = new Color(maxColorIntensity, 1 - maxColorIntensity, 0);

            obj.gameObject.GetComponent<Renderer>().material.color = ourColor;
        }
    }

    public void StartLandmark()
    {
        computeLandmark = !computeLandmark;
    }

    public void InitiateCounter()
    {
        objs = GameObject.FindGameObjectsWithTag("Buildings");
        foreach (GameObject obj in objs)
        {
            try
            {
                Buildingsno.Add(obj);
                buildingCounters.Add(obj.transform.localPosition, 0);
                totalBuildings = Buildingsno.Count;
            } 
            catch
            {
                // nothing
            } 
        }
        totalNofBuildings.text = "Total Buildings: " + totalBuildings.ToString();
    }

    public void ChangeButtonColor()
    {
        ColorBlock cbOriginal = button.colors;
        
        ColorBlock cb = cbOriginal;

        if (computeLandmark)
        {
            cb.normalColor = wantedColor;
            cb.highlightedColor = wantedColor;
            cb.pressedColor = wantedColor;
            button.colors = cb;
            buttonText.text = "Stop Computing"; 
            buttonText.color = Color.red;          
        }

        else
        {
            if (markerInstance != null)
            {
                Destroy(markerInstance);
            }
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white;
            button.colors = cb;
            buttonText.text = "Landmark View";
            buttonText.color = Color.black; 

        }
    }
}
