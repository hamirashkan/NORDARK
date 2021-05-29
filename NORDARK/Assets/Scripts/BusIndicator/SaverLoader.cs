using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;

public static class SaverLoader
{
    public static void Save(string filename, BusServiceAvailability busServiceAvailability) {
        BusData data = new BusData(busServiceAvailability.GetStops(), busServiceAvailability.GetHourStep(), busServiceAvailability.maxDistance, busServiceAvailability.reliabilityFactor);
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Path.Combine(Application.persistentDataPath, filename + ".dat"));
        bf.Serialize(file, data);
        file.Close();
    }

    public static BusData Load(string filename) {
        string path = Path.Combine(Application.persistentDataPath, filename + ".dat");

        if (File.Exists(path)) {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(path, FileMode.Open);
            BusData data = (BusData)bf.Deserialize(file);
            file.Close();

            return data;
        } else {
            return null;
        }
    }
}


[Serializable]
public class BusData
{
    private int hourStep;
    private float maxDistance;
    private float reliabilityFactor;
    private string[] id;
    private float[] latitude;
    private float[] longitude;
    private int[][] nbOfStopsPerHourStep;


    public BusData(List<Stop> stops, int hourStep, float maxDistance, float reliabilityFactor)
    {
        this.hourStep = hourStep;
        this.maxDistance = maxDistance;
        this.reliabilityFactor = reliabilityFactor;
        int n = stops.Count;
        this.id = new string[n];
        this.latitude = new float[n];
        this.longitude = new float[n];
        this.nbOfStopsPerHourStep = new int[n][];

        for (int i=0; i<n; i++) {
            id[i] = stops[i].id;
            this.latitude[i] = stops[i].coordinates.Item1;
            this.longitude[i] = stops[i].coordinates.Item2;

            int m = stops[i].nbOfStopsPerHourStep.Length;
            this.nbOfStopsPerHourStep[i] = new int[m];
            for (int j=0; j<m; j++) {
                this.nbOfStopsPerHourStep[i][j] = stops[i].nbOfStopsPerHourStep[j];
            }
        }
    }

    public List<Stop> GetStops() {
        List<Stop> stops = new List<Stop>();

        int n = id.Length;
        for (int i=0; i<n; i++) {
            stops.Add(new Stop(id[i], new Tuple<float, float>(latitude[i], longitude[i]), nbOfStopsPerHourStep[i]));
        }

        return stops;
    }
    public int GetHourStep() {
        return hourStep;
    }
    public float GetMaxDistance() {
        return maxDistance;
    }
    public float GetReliabilityFactor() {
        return reliabilityFactor;
    }
}