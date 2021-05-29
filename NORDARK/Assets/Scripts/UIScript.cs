using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Globalization;

public class UIScript : MonoBehaviour
{
    ////////////////UI elements/////////////////////
    public GameObject shadowMapUI; 
    public GameObject dataPlugIn;

    //////Data plugin panel//////////
    private Button Save;
    private Button Load;

    private GameObject fileBox;
    private TMP_Text option1;
    private TMP_Text option2;
    private TMP_Text option3;
    private TMP_Text option4;

    private bool isSave = false;
    private bool isLoad = false;
    //temp variables
 

    //interface variables
    [NonSerialized] public string ToFileName;  //file name where the data will be saved to
    [NonSerialized] public string FromFileName; //file name where the data will be retrieved from
    
    /////////////////////////////////

    //Simulation parameters panel////

    //OK and Cancel buttons
    // public Button SimParamOKButton;
    // public Button SimParamCancelButton;


    //User Inputs
    //public TMP_InputField TMP_IF;
    private TMP_InputField TMP_IF;
    private Slider sunSpeedSlider;
    private TMP_InputField MapSizeObj;
    private TMP_InputField startTimeObj;
    private TMP_InputField stopTimeObj;
    private TMP_Dropdown seasonObj;

    //interface variables
    [NonSerialized] public float heatmapSize = 10.0f;
    [NonSerialized] public float sunSpeed = 1;
    [NonSerialized] public float startTime = 6.00f;
    [NonSerialized] public float stopTime = 18.00f;
    [NonSerialized] public string season;

    //temp variables
    private float heatmapSizePrev;
    private float sunSpeedPrev;
    private float startTimePrev;
    private float stopTimePrev;
    private string seasonPrev;


    ////////////////////////////////////

    // Start is called before the first frame update
    void Start()
    {
        //Initialize dataPlugInUI

        //Save Load button set up
        //dataPlugIn = GameObject.Find("DataPluginPanel");
        Save = dataPlugIn.transform.Find("Save").gameObject.GetComponent<Button>();
        Load = dataPlugIn.transform.Find("Load").gameObject.GetComponent<Button>();    

        //Filebox set up
        fileBox = dataPlugIn.transform.Find("FileBox").gameObject;
        option1 = fileBox.transform.Find("Option1").gameObject.GetComponent<Button>().GetComponentInChildren<TMP_Text>();
        option2 = fileBox.transform.Find("Option2").gameObject.GetComponent<Button>().GetComponentInChildren<TMP_Text>();
        option3 = fileBox.transform.Find("Option3").gameObject.GetComponent<Button>().GetComponentInChildren<TMP_Text>();
        option4 = fileBox.transform.Find("Option4").gameObject.GetComponent<Button>().GetComponentInChildren<TMP_Text>();

        fileBox.SetActive(false);
        

        //Initialize shadowMapUI
        shadowMapUI = GameObject.Find("ShadowMapUI");
        sunSpeedSlider = shadowMapUI.transform.Find("Slider").gameObject.GetComponent<Slider>();
        MapSizeObj = shadowMapUI.transform.Find("SizeInput").gameObject.GetComponent<TMP_InputField>();
        startTimeObj = shadowMapUI.transform.Find("Start").gameObject.GetComponent<TMP_InputField>();
        stopTimeObj = shadowMapUI.transform.Find("Stop").gameObject.GetComponent<TMP_InputField>();
        seasonObj = shadowMapUI.transform.Find("SeasonOptions").gameObject.GetComponent<TMP_Dropdown>();

    }

    // Update is called once per frame
    void Update()
    {

    }

    //ShadowMapUI Panel methods

    //OK
    public void onShadowMapOK()
    {
        heatmapSizePrev = heatmapSize;
        sunSpeedPrev = sunSpeed;
        startTimePrev = startTime;
        stopTimePrev = stopTime;
        seasonPrev = season;

        sunSpeed = sunSpeedSlider.value;
        if(float.TryParse(MapSizeObj.text, out float cleanSize)){heatmapSize = cleanSize;}
        if(float.TryParse(startTimeObj.text, out float cleanStart)){startTime = cleanStart;}
        if(float.TryParse(stopTimeObj.text, out float cleanStop)){stopTime = cleanStop;}
        season = seasonObj.options[seasonObj.value].text;
    }

    //CANCEL
    public void onShadowMapCANCEL()
    {
        sunSpeedSlider.value = sunSpeedPrev;
        MapSizeObj.text = heatmapSizePrev.ToString();
        startTimeObj.text = startTimePrev.ToString();
        stopTimeObj.text = stopTimePrev.ToString();
        seasonObj.options[seasonObj.value].text = seasonPrev;
    }

    //Data plugin

    //save
    public void onSave()
    {
        isLoad = false;
        //show/hide the file box
        fileBox.SetActive(true);

        isSave = true;
    }

    public void onLoad()
    {
        isSave = false;
        //show/hide the file box
        fileBox.SetActive(true);

        isLoad = true;
    }


    public void option1OnClick()
    {
        if (isSave)
        {
            ToFileName = option1.text;
            GameObject.Find("DummySun").GetComponent<DummySun>().SaveData();
        }
        if (isLoad)
        {
            FromFileName = option1.text;
            GameObject.Find("DummySun").GetComponent<DummySun>().LoadData();
        }

        fileBox.SetActive(false);
    }

    public void option2OnClick()
    {
        if (isSave)
        {
            ToFileName = option2.text;
            GameObject.Find("DummySun").GetComponent<DummySun>().SaveData();
        }
        if (isLoad)
        {
            FromFileName = option2.text;
            GameObject.Find("DummySun").GetComponent<DummySun>().LoadData();
        }

        fileBox.SetActive(false);
    }

    public void option3OnClick()
    {
        if (isSave)
        {
            ToFileName = option3.text;
            GameObject.Find("DummySun").GetComponent<DummySun>().SaveData();
        }
        if (isLoad)
        {
            FromFileName = option3.text;
            GameObject.Find("DummySun").GetComponent<DummySun>().LoadData();
        }

        fileBox.SetActive(false);
    }

    public void option4OnClick()
    {
        if (isSave)
        {
            ToFileName = option4.text;
            GameObject.Find("DummySun").GetComponent<DummySun>().SaveData();
        }
        if (isLoad)
        {
            FromFileName = option4.text;
            GameObject.Find("DummySun").GetComponent<DummySun>().LoadData();
        }

        fileBox.SetActive(false);
    }

}