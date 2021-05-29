using System;
using System.Linq;
using UnityEngine;

public class Stop
{
    public string id { get; }
    public Tuple<float, float> coordinates { get; }
    public int[] nbOfStopsPerHourStep { get; private set; }
    public float[] AWTs { get; private set; }


    public Stop(string id, Tuple<float, float> coordinates, int nbSteps) {
        this.id = id;
        this.coordinates = coordinates;
        this.nbOfStopsPerHourStep = new int[nbSteps];
        this.AWTs = new float[nbSteps];
    }
    public Stop(string id, Tuple<float, float> coordinates, int[] nbOfStopsPerHourStep) {
        this.id = id;
        this.coordinates = coordinates;
        this.nbOfStopsPerHourStep = nbOfStopsPerHourStep;
        this.AWTs = new float[nbOfStopsPerHourStep.Length];
    }

    public void IncreaseNbOfStops(int indexStep) {
        this.nbOfStopsPerHourStep[indexStep]++;
    }

    public void ComputeAWTs(float reliabilityFactor) {
        int n = AWTs.Length;
    
        for (int i=0; i<n; i++) {
            if (nbOfStopsPerHourStep[i] == 0) {
                AWTs[i] = Mathf.Infinity;
            } else {
                float SWT = 720 / (nbOfStopsPerHourStep[i] * n);
                AWTs[i] = SWT + reliabilityFactor;
            }
        }
    }
}
