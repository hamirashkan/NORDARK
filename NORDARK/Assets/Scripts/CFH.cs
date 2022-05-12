using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFH : MonoBehaviour
{
    public float[][] inputValues;
    public int[][] inputValuesInt;
    public float[] outputValues;
    public string FeatureString = "0";
    public float patternMax;
    public int stept = 1;
    public int start_time = 0;
    public int stop_time = 10;
    //public string 
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ComputeCFH_Int()
    {
        int t_count = 0;
        int i_count = 0;
        patternMax = 0;
        t_count = inputValuesInt.Length;
        i_count = inputValuesInt[0].Length;
        outputValues = new float[i_count];
        float Threshold = 1;
        for (var i = 0; i < i_count; i++)
        {
            if (t_count >= 2)
            {
                // t count = frame size of t
                int[] sub;// = new int[] { 0, 1 };//{ 1, 1, 1 }
                sub = Fun_FeatureStrToInt(FeatureString);
                int[] data = new int[t_count - 1];
                int histogram = 0;
                
                for (int t = start_time; t < stop_time - 1; t+= stept)
                {
                    // ComputeMatrixD, binary pattern
                    //// ITDM_Label change or not
                    //int binaryMapData = inputValues[t][i] != inputValues[t + 1][i] ? 1 : 0;
                    //data[t] = binaryMapData;
                    //// ITDM_Cost change or not
                    int binaryMapData = Mathf.Abs(inputValuesInt[t][i] - inputValuesInt[t + 1][i]) >= Threshold ? 1 : 0;
                    data[t] = binaryMapData;
                }
                List<int> a = Fun_SubFeatureForData(data, sub);
                histogram = a.Count;
                if (histogram > patternMax)
                    patternMax = histogram;
                outputValues[i] = histogram;// * 2;
            }
            else
            {
                patternMax = 0;
            }

        }
    }

    public void ComputeCFH(float Threshold = 1)
    {
        int t_count = 0;
        int i_count = 0;
        patternMax = 0;
        t_count = inputValues.Length;
        i_count = inputValues[0].Length;
        outputValues = new float[i_count];
        for (var i = 0; i < i_count; i++)
        {
            if (t_count >= 2)
            {
                // t count = frame size of t
                int[] sub;// = new int[] { 0, 1 };//{ 1, 1, 1 }
                sub = Fun_FeatureStrToInt(FeatureString);
                int[] data = new int[t_count - 1];
                int histogram = 0;

                for (int t = start_time; t < stop_time - 1; t += stept)
                {
                    // ComputeMatrixD, binary pattern
                    //// ITDM_Label change or not
                    //int binaryMapData = inputValues[t][i] != inputValues[t + 1][i] ? 1 : 0;
                    //data[t] = binaryMapData;
                    //// ITDM_Cost change or not
                    int binaryMapData = Mathf.Abs(inputValues[t][i] - inputValues[t + 1][i]) >= Threshold ? 1 : 0;
                    data[t] = binaryMapData;
                }
                List<int> a = Fun_SubFeatureForData(data, sub);
                histogram = a.Count;
                if (histogram > patternMax)
                    patternMax = histogram;
                outputValues[i] = histogram;
            }
            else
            {
                patternMax = 0;
            }

        }
    }


    public int[] Fun_FeatureStrToInt(string feature)
    {
        char[] c_feature;
        int[] result;
        if (feature.Length > 0)
        {
            c_feature = feature.ToCharArray();
            result = new int[c_feature.Length];
            for (int i = 0; i < c_feature.Length; i++)
            {
                if (c_feature[i] == '0')
                    result[i] = 0;
                else if (c_feature[i] == '1')
                    result[i] = 1;
                else
                    return null;
            }
            return result;
        }
        return null;
    }

    public List<int> Fun_SubFeatureForData(int[] data, int[] sub)
    {
        //int[] data = new int[] { 1, 1, 1, 0, 1, 0, 1 };
        //int[] sub = new int[] { 1, 0, 1 };
        List<int> result = new List<int>();
        for (int i = 0; i < data.Length - sub.Length + 1; i++)
            for (int j = 0; j < sub.Length; j++)
            {
                if (data[i + j] == sub[j])
                {
                    if (j + 1 == sub.Length)
                    {
                        result.Add(i);
                    }
                }
                else
                    break;
            }
        // return [2,4] for List
        return result;
    }
}
