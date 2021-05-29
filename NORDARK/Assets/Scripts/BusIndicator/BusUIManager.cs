using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Globalization;

public class BusUIManager : MonoBehaviour
{
    [SerializeField] private Slider busTimeSlider;
    [SerializeField] private TMP_Text busTimeLabel;
    [SerializeField] private TMP_InputField hourStep;
    [SerializeField] private TMP_InputField maxDistance;
    [SerializeField] private TMP_InputField reliabilityFactor;
    [SerializeField] private Button OKButton;
    [SerializeField] private Button cancelButton;

    private BusServiceAvailability busServiceAvailability;
    private const string defaultHourStep = "2";
    private const string defaultMaxDistanceStep = "640";
    private const string defaultReliabilityFactor = "2";


    public GameObject dataPlugIn;
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
    

    void Awake() {
        busTimeSlider.minValue = 0;
        busTimeSlider.wholeNumbers = true;

        hourStep.text = defaultHourStep;
        maxDistance.text = defaultMaxDistanceStep;
        reliabilityFactor.text = defaultReliabilityFactor;

        OKButton.onClick.AddListener(delegate {OnOKButtonClicked(); });
        cancelButton.onClick.AddListener(delegate {OnCancelButtonClicked(); });
        busTimeSlider.onValueChanged.AddListener(delegate {UpdateTimeLabel(); });

        Save = dataPlugIn.transform.Find("Save").gameObject.GetComponent<Button>();
        Load = dataPlugIn.transform.Find("Load").gameObject.GetComponent<Button>();    

        fileBox = dataPlugIn.transform.Find("FileBox").gameObject;
        option1 = fileBox.transform.Find("Option1").gameObject.GetComponent<Button>().GetComponentInChildren<TMP_Text>();
        option2 = fileBox.transform.Find("Option2").gameObject.GetComponent<Button>().GetComponentInChildren<TMP_Text>();
        option3 = fileBox.transform.Find("Option3").gameObject.GetComponent<Button>().GetComponentInChildren<TMP_Text>();
        option4 = fileBox.transform.Find("Option4").gameObject.GetComponent<Button>().GetComponentInChildren<TMP_Text>();

        fileBox.SetActive(false);
        
    }
    void OnEnable() {
        dataPlugIn.SetActive(true);
    }

    void OnDisable() {
        dataPlugIn.SetActive(false);
    }

    public void OnOKButtonClicked() {
        if (busServiceAvailability == null) {
            busServiceAvailability = GameObject.Find("BusIndicator").GetComponent<BusServiceAvailability>();
        }

        busServiceAvailability.hourStep = int.Parse(hourStep.text);
        busTimeSlider.maxValue = busServiceAvailability.GetNbOfSteps()-1;
        busServiceAvailability.maxDistance = float.Parse(maxDistance.text);
        busServiceAvailability.reliabilityFactor = float.Parse(reliabilityFactor.text);
        busServiceAvailability.SetStops();

        UpdateTimeLabel();
    }

    private void OnCancelButtonClicked() {
        busTimeSlider.value = 0;
        hourStep.text = defaultHourStep;
        maxDistance.text = defaultMaxDistanceStep;
        reliabilityFactor.text = defaultReliabilityFactor;
        OnOKButtonClicked();
    }

    private void UpdateTimeLabel() {
        int beginningHour = (int) busTimeSlider.value * busServiceAvailability.GetHourStep();
        string firstHour = beginningHour.ToString();
        if (firstHour.Length < 2) {
            firstHour = "0" + firstHour;
        }
        string secondHour = (beginningHour + busServiceAvailability.GetHourStep()).ToString();
        if (secondHour.Length < 2) {
            secondHour = "0" + secondHour;
        }
        busTimeLabel.text = firstHour + ":00 - " + secondHour + ":00";

        busServiceAvailability.SetCurrentStep((int) busTimeSlider.value);
    }


    public void onSave()
    {
        isLoad = false;
        //show/hide the file box
        fileBox.SetActive(true);

        //toggle state
        //fileBoxActive = !fileBoxActive;
        isSave = true;
    }

    public void onLoad()
    {
        isSave = false;
        //show/hide the file box
        fileBox.SetActive(true);

        //toggle state
        //fileBoxActive = !fileBoxActive;
        isLoad = true;
    }


    public void option1OnClick()
    {
        
        if (isSave)
        {
            SaverLoader.Save(option1.text, busServiceAvailability);
        }
        if (isLoad)
        {
            busServiceAvailability.SetStopsFromSavedFile(SaverLoader.Load(option1.text));

            hourStep.text = busServiceAvailability.hourStep.ToString();
            busTimeSlider.maxValue = busServiceAvailability.GetNbOfSteps()-1;
            maxDistance.text = busServiceAvailability.maxDistance.ToString();
            reliabilityFactor.text = busServiceAvailability.reliabilityFactor.ToString();
            busServiceAvailability.SetCurrentStep((int) busTimeSlider.value);
        }

        fileBox.SetActive(false);

    }

    public void option2OnClick()
    {
        if (isSave)
        {
            SaverLoader.Save(option2.text, busServiceAvailability);
        }
        if (isLoad)
        {
            SaverLoader.Load(option2.text);

            hourStep.text = busServiceAvailability.hourStep.ToString();
            busTimeSlider.maxValue = busServiceAvailability.GetNbOfSteps()-1;
            maxDistance.text = busServiceAvailability.maxDistance.ToString();
            reliabilityFactor.text = busServiceAvailability.reliabilityFactor.ToString();
            busServiceAvailability.SetCurrentStep((int) busTimeSlider.value);
        }

        fileBox.SetActive(false);
    }

    public void option3OnClick()
    {
        if (isSave)
        {
            SaverLoader.Save(option3.text, busServiceAvailability);
        }
        if (isLoad)
        {
            SaverLoader.Load(option3.text);

            hourStep.text = busServiceAvailability.hourStep.ToString();
            busTimeSlider.maxValue = busServiceAvailability.GetNbOfSteps()-1;
            maxDistance.text = busServiceAvailability.maxDistance.ToString();
            reliabilityFactor.text = busServiceAvailability.reliabilityFactor.ToString();
            busServiceAvailability.SetCurrentStep((int) busTimeSlider.value);
        }

        fileBox.SetActive(false);
    }

    public void option4OnClick()
    {
        if (isSave)
        {
            SaverLoader.Save(option4.text, busServiceAvailability);
        }
        if (isLoad)
        {
            SaverLoader.Load(option4.text);

            hourStep.text = busServiceAvailability.hourStep.ToString();
            busTimeSlider.maxValue = busServiceAvailability.GetNbOfSteps()-1;
            maxDistance.text = busServiceAvailability.maxDistance.ToString();
            reliabilityFactor.text = busServiceAvailability.reliabilityFactor.ToString();
            busServiceAvailability.SetCurrentStep((int) busTimeSlider.value);
        }

        fileBox.SetActive(false);
    }
}
