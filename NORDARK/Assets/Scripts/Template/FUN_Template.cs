using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;

public class FUN_Template: MonoBehaviour
{
    [SerializeField] private int sample_data_i1 = 2;

    [Tooltip("tips")]
    [SerializeField] private float sample_data_f1 = 640f;

    private int sample_data_i2;
    private GameObjectModifier sampleModifier;
    private VectorSubLayerProperties buildingLayer;


    void Start() {
        sample_data_i2 = 0;
        sampleModifier = ScriptableObject.CreateInstance<Modifier_Template>();
        buildingLayer = GameObject.Find("Map").GetComponent<AbstractMap>().VectorData.GetFeatureSubLayerAtIndex(0);
    }

    public void UpdateData(int step) {
        // change the modifier according to some parameters such as step
        RemoveModifier();
        AddModifier();
    }
    public void AddModifier() {
        buildingLayer.BehaviorModifiers.AddGameObjectModifier(sampleModifier);
    }
    public void RemoveModifier() {
        buildingLayer.BehaviorModifiers.RemoveGameObjectModifier(sampleModifier);
    }

    private void SetStops() {
        // load your data array from dat file
        UrbanScene urbandata = Scene.Load();

        // activate your UI indicator
        GameObject.Find("Prefab_UI_Template").GetComponent<UI_Template>().ActivateIndicator();
    }

    private void ReadRawDataFromFile() {
        string text = loadFile("Assets/Resources/Template/datasource.txt");
        string[] lines = Regex.Split(text, "\n");

        int nbStops = lines.Length - 2;

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

            //add information to your data array
        }
        // save your data array to dat file

    }

    private string loadFile(string filename) {
        TextAsset file = Resources.Load<TextAsset>(filename);
        if (file == null) {
            throw new Exception(filename + " not found");
        }
        return file.text;
    }
}