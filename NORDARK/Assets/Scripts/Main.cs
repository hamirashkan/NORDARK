using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class Main : MonoBehaviour {

    private GameObject cube1;
    private GameObject cube2;
	// Use this for initialization
	void Start () {
        cube1 = GameObject.Find("Cube1");
        cube2 = GameObject.Find("Cube2");
        //pass C#'s delegate to C++
        DllInterface.InitCSharpDelegate(DllInterface.LogMessageFromCpp);
        PrintDistanceViaUnity();

        IntPtr ptr = DllInterface.fnwrapper_intarr();
        int[] result = new int[3];
        Marshal.Copy(ptr, result, 0, 3);
        Debug.Log(result);

        IntPtr intPtr;
        unsafe
        {
            fixed (int* pArray = result)
            {
                intPtr = new IntPtr((void*)pArray);
            }
        }

        IntPtr ptr1 = DllInterface.add(intPtr);
        int[] result1 = new int[3];
        Marshal.Copy(ptr1, result1, 0, 3);
        Debug.Log(result1);

        //IFT
        int nrows = 50, ncols = 50;
        int[] testImage = new int[nrows * ncols];
        testImage[0] = 1;
        testImage[ncols * 20 + 20] = 1;
        testImage[ncols * 20 + 21] = 1;
        testImage[ncols * 30 + 20] = 1;
        testImage[ncols * 30 + 21] = 1;
        IntPtr intPtrImage;
        unsafe
        {
            fixed (int* pArray = testImage)
            {
                intPtrImage = new IntPtr((void*)pArray);
            }
        }

        DateTime dateTime1 = DateTime.Now;
        IntPtr intPtrEdt = DllInterface.IFT(intPtrImage, nrows, ncols);
        DateTime dateTime2 = DateTime.Now;
        var diffInSeconds = (dateTime2 - dateTime1).TotalMilliseconds;
        Debug.Log("IFT:" + diffInSeconds + " millisec");

        int[] edtImage = new int[nrows * ncols];
        Marshal.Copy(intPtrEdt, edtImage, 0, nrows * ncols);
        Debug.Log(edtImage);

        DllInterface.ExportFile(intPtrImage, nrows, ncols, Marshal.StringToHGlobalAnsi("raw.pgm"));
        DllInterface.ExportFile(intPtrEdt, nrows, ncols, Marshal.StringToHGlobalAnsi("edit.pgm"));
    }
	
	void PrintDistanceViaUnity()
    {
        var pos1 = cube1.transform.position;
        var pos2 = cube2.transform.position;
        Debug.Log("This is a log from Unity");
        Debug.Log("Distance:" + DllInterface.GetDistance(pos1.x, pos1.y, pos2.x, pos2.y));
    }
}
