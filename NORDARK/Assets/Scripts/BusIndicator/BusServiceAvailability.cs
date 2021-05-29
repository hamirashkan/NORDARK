using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;

public class BusServiceAvailability: MonoBehaviour
{
    public int hourStep;

    [Tooltip("Distance from which we do not consider a bus stop (in meters).")]
    public float maxDistance;

    [Tooltip("Reflects the fact that actual wait times can be longer because services do not arrive in an entirely regular manner (in minutes).")]
    public float reliabilityFactor;

    private List<Stop> stops;
    private int currentStep;
    private GameObjectModifier busModifier;
    private VectorSubLayerProperties buildingLayer;
    private BusUIManager busUIManager;


    void Awake() {
        currentStep = 0;
        busModifier = ScriptableObject.CreateInstance<BusIndicatorModifier>();
        buildingLayer = GameObject.Find("CitySimulatorMap").GetComponent<AbstractMap>().VectorData.GetFeatureSubLayerAtIndex(0);
        busUIManager = GameObject.Find("BusIndicatorUI").GetComponent<BusUIManager>();
    }

    void OnEnable() {
        busUIManager.OnOKButtonClicked();
    }

    void OnDisable() {
        RemoveModifier();
    }

    public float[] GetPTAL(float lat, float lon) {
        Point point = new Point(new Tuple<float, float>(lat, lon), stops, maxDistance);

        point.ComputeAWTs(this.reliabilityFactor);
        point.ComputeEDFs();
        float[] accessIndexes = point.GetAccessIndex();

        int numberOfIndexes;
        try {
            numberOfIndexes = accessIndexes.Length;
            float[] PTAL = new float[numberOfIndexes];

            if (accessIndexes == null) {
                return PTAL;
            }

            for (int i=0; i<accessIndexes.Length; i++) {
                if (accessIndexes[i] > 40) {
                    PTAL[i] = 1;
                } else {
                    PTAL[i] = accessIndexes[i] / 40;
                }   
            }

            return PTAL;
        } catch (NullReferenceException) {
            return new float[0];
        }
        
    }

    public int GetHourStep() {
        return hourStep;
    }
    public int GetNbOfSteps() {
        return 24 / hourStep;
    }
    public int GetCurrentStep() {
        return currentStep;
    }
    public void SetCurrentStep(int currentStep) {
        this.currentStep = currentStep;

        RemoveModifier();
        AddModifier();
    }
    public void AddModifier() {
        buildingLayer.BehaviorModifiers.AddGameObjectModifier(busModifier);
    }
    public void RemoveModifier() {
        buildingLayer.BehaviorModifiers.RemoveGameObjectModifier(busModifier);
    }

    public void SetStops() {
        SetStopsFromFile();
    }
    public void SetStopsFromSavedFile(BusData busData) {
        this.stops = busData.GetStops();
        this.hourStep = busData.GetHourStep();
        this.maxDistance = busData.GetMaxDistance();
        this.reliabilityFactor = busData.GetReliabilityFactor();
    }
    public List<Stop> GetStops() {
        return this.stops;
    }

    private void SetStopsFromFile() {
        string text = loadFile("BusIndicator/stops");
        string[] lines = Regex.Split(text, "\n");

        int nbStops = lines.Length - 2;
        this.stops = new List<Stop>();

        for (int i=0; i < nbStops; i++) {
            string line = lines[i+1];

            if (!line.Contains("NSR:Quay")) {
                continue;
            }

            string[] quotes = Regex.Split(line, "\"");
            if (quotes.Length > 1) {
                line = quotes[0] + quotes[2];
            }

            string[] values = Regex.Split(line, ",");
            string id = values[0];
            float lat = float.Parse(values[4], System.Globalization.CultureInfo.InvariantCulture);
            float lon = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture);

            this.stops.Add(new Stop(id, new Tuple<float, float>(lat, lon), 24/hourStep));
        }

        SetNumberOfStops();
    }
    private void SetNumberOfStops() {
        string text = loadFile("BusIndicator/stop_times");
        string[] lines = Regex.Split(text, "\n");

        int nbStopsTimes = lines.Length - 2;

        for (int i=0; i < nbStopsTimes; i++) {
            string line = lines[i+1];

            string[] values = Regex.Split(line, ",");
            string id = values[1];
            string time = values[4];
            int hour = int.Parse(Regex.Split(time, ":")[0]);
            hour %= 24;
            int indexStep = hour / hourStep;

            foreach (Stop stop in this.stops) {
                if (stop.id == id) {
                    stop.IncreaseNbOfStops(indexStep);
                    break;
                }
            }
        }
    }

    private string loadFile(string filename) {
        TextAsset file = Resources.Load<TextAsset>(filename);
        if (file == null) {
            throw new Exception(filename + " not found");
        }
        return file.text;
    }
}