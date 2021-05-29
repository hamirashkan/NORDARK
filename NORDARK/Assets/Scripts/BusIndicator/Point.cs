using System;
using System.Collections.Generic;
using UnityEngine;

public class Point {
    private const float EARTH_RADIUS = 6371e3f;
    public Tuple<float, float> coordinates { get; }
    List<Stop> nearestSAP { get; }
    List<float> walkingTime;
    List<float[]> EDFs;

    public Point(Tuple<float, float> coordinates, List<Stop> stops, float maxDistance) {
        this.coordinates = coordinates;
        this.nearestSAP = new List<Stop>();
        this.walkingTime = new List<float>();
        this.EDFs = new List<float[]>();
        
        foreach (Stop stop in stops) {
            float distance = Distance(this.coordinates, stop.coordinates);
            if (distance < maxDistance) {
                this.nearestSAP.Add(stop);
                this.walkingTime.Add(distance / 80f);
            }
        }
    }

    public void ComputeAWTs(float reliabilityFactor) {
        foreach (Stop SAP in this.nearestSAP) {
            SAP.ComputeAWTs(reliabilityFactor);
        }
    }

    public void ComputeEDFs() {
        for (int i=0; i<this.nearestSAP.Count; i++) {
            int n = this.nearestSAP[i].AWTs.Length;

            float[] EDF = new float[n];
            for (int j=0; j<n; j++) {
                if (this.nearestSAP[i].AWTs[j] == Mathf.Infinity) {
                    EDF[j] = 0;
                } else {
                    float TAT = this.nearestSAP[i].AWTs[j] + walkingTime[i];
                    EDF[j] = 30f / TAT;
                }
            }
            this.EDFs.Add(EDF);
        }
    }

    public float[] GetAccessIndex() {
        if (this.EDFs.Count < 1) {
            return null;
        }

        int timeNumber = this.EDFs[0].Length;
        int numberOfSAP = this.EDFs.Count;
        float[] accessIndex = new float[timeNumber];

        for (int i=0; i<timeNumber; i++) {
            int maxEDFIndex = 0;

            for (int j=0; j<numberOfSAP; j++) {
                if (this.EDFs[j][i] > this.EDFs[maxEDFIndex][i]) {
                    maxEDFIndex = j;
                }
            }
            accessIndex[i] = this.EDFs[maxEDFIndex][i];

            for (int j=0; j<numberOfSAP; j++) {
                if (j != maxEDFIndex) {
                    accessIndex[i] += 0.5f*this.EDFs[j][i];
                }
            }
        }

        return accessIndex;
    }

    public static float Distance(Tuple<float, float> from, Tuple<float, float> to) {
        float deltaLat = (to.Item1 - from.Item1) * Mathf.Deg2Rad;
        float deltaLon = (to.Item2 - from.Item2) * Mathf.Deg2Rad;

        float a = Mathf.Pow(Mathf.Sin(deltaLat/2), 2) + Mathf.Cos(from.Item1 * Mathf.Deg2Rad) * Mathf.Cos(to.Item1 * Mathf.Deg2Rad) * Mathf.Pow(Mathf.Sin(deltaLon/2), 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1-a));

        return EARTH_RADIUS * c;
    }
}
