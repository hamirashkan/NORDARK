using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;

public static class Scene
{
    private const string FILENAME = "Scene";

    public static void Save() {
        UrbanScene data = new UrbanScene();
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Path.Combine(Application.persistentDataPath, FILENAME + ".dat"));
        bf.Serialize(file, data);
        file.Close();
    }

    public static UrbanScene Load() {
        string path = Path.Combine(Application.persistentDataPath, FILENAME + ".dat");

        if (File.Exists(path)) {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(path, FileMode.Open);
            UrbanScene data = (UrbanScene)bf.Deserialize(file);
            file.Close();

            return data;
        } else {
            return null;
        }
    }
}

[Serializable]
public class UrbanScene
{
    // points
    public List<InspectPoint> inspectPoint = new List<InspectPoint>();
    // other scene information
    // common
    public string sceneName;
    public float latitude;
    public float longitude;
    private DateTime lastSavedDateTime;//save to the .dat file
    // busStopIndicator
    private int hourStep;
    // sky exposure
    private int sky_exposure_steps;
    private int sky_exposure_rays_w_number;
    private int sky_exposure_rays_h_number;
    public UrbanScene()
    {
        // init values, or read value from input parameters
    }
}


[Serializable]
public class InspectPoint
{
    // busStopIndicator
    private string id;
    private float latitude;
    private float longitude;
    private int[] nbOfStopsPerHourStep;
    // common
    private float altitude;//y_value of currentPosition
    private SerializableVector3 currentPosition;
    private SerializableVector3 lastPosition;
    private DateTime lastEditDateTime;
    private DateTime lastUpdateDateTime;
    // sky exposure
    private float sky_exposure_percents;
    private SerializableVector3 sky_exposure_refPosition;

    public InspectPoint()
    {
        // init values, or read value from input parameters
    }

    [Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(float rX, float rY, float rZ)
        {
            x = rX;
            y = rY;
            z = rZ;
        }

        public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}]", x, y, z);
        }

        public static implicit operator Vector3(SerializableVector3 rValue)
        {
            return new Vector3(rValue.x, rValue.y, rValue.z);
        }

        public static implicit operator SerializableVector3(Vector3 rValue)
        {
            return new SerializableVector3(rValue.x, rValue.y, rValue.z);
        }
    }

    [Serializable]
    public struct SerializableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SerializableQuaternion(float rX, float rY, float rZ, float rW)
        {
            x = rX;
            y = rY;
            z = rZ;
            w = rW;
        }
        public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}, {3}]", x, y, z, w);
        }

        public static implicit operator Quaternion(SerializableQuaternion rValue)
        {
            return new Quaternion(rValue.x, rValue.y, rValue.z, rValue.w);
        }

        public static implicit operator SerializableQuaternion(Quaternion rValue)
        {
            return new SerializableQuaternion(rValue.x, rValue.y, rValue.z, rValue.w);
        }
    }
}