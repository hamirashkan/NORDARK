using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Can change this to get the different images in the script without public prefabs like this
    public GameObject StartWindow;
    public GameObject BusIndicatorUI;
    public GameObject BusIndicator;
    public GameObject SkyExposureUI;
    public GameObject SkyExposure;
    public GameObject LandmarkUI;
    public GameObject Landmark;
    public GameObject ShadowHeatmapUI;
    public GameObject ShadowHeatmap;
    public GameObject SaveLoad;
    public GameObject EmptyPanel;
    public TMPro.TMP_Dropdown UIdropdown;
    private GameObject currentUI;
    private GameObject currentProgram;
    // Start is called before the first frame update
    void Start()
    {
        currentUI = new GameObject();
        currentProgram = new GameObject();
        UIdropdown.onValueChanged.AddListener(delegate
        {
            changeUI(UIdropdown.value);
        });
        hideAll();
    }

    // Update is called once per frame
    void Update()
    {

    }

    GameObject getUI(int ui)
    {
        var uiImage = currentUI;

        switch (ui)
        {
            case 0:
                uiImage = null;
                break;
            case 1:
                uiImage = SkyExposureUI;
                break;
            case 2:
                uiImage = ShadowHeatmapUI;
                break;
            case 3:
                uiImage = LandmarkUI;
                break;
            case 4:
                uiImage = BusIndicatorUI;
                break;
        }
        return uiImage;
    }

    GameObject getProgram(int ui)
    {
        var program = currentProgram;

        switch (ui)
        {
            case 0:
                currentProgram = null;
                break;
            case 1:
                program = SkyExposure;
                break;
            case 2:
                program = ShadowHeatmap;
                break;
            case 3:
                program = Landmark;
                break;
            case 4:
                program = BusIndicator;
                break;
        }
        return program;
    }

    void hideAll()
    {
        BusIndicatorUI.SetActive(false);
        SkyExposureUI.SetActive(false);
        ShadowHeatmapUI.SetActive(false);
        LandmarkUI.SetActive(false);
        BusIndicator.SetActive(false);
        SkyExposure.SetActive(false);
        ShadowHeatmap.SetActive(false);
        Landmark.SetActive(false);
        SaveLoad.SetActive(false);
    }

    void toggleActive(GameObject g, bool b)
    {
        if (g)
        {
            for (int i = 0; i < g.transform.childCount; i++)
            {
                var child = g.transform.GetChild(i).gameObject;
                if (child.name == "Ground" && child.transform.childCount > 0)
                {
                    var heatmap = child.GetComponent<Heatmap>();
                    if (heatmap) Destroy(heatmap);
                    {
                        if (child) child.SetActive(b);
                    }
                }
            }
            g.gameObject.SetActive(b);
        }

    }

    void changeUI(int ui)
    {
        var newUI = getUI(ui);
        var newProgram = getProgram(ui);
        if (!newUI || !newProgram) hideAll();
        else
        {
            toggleActive(currentUI, false);
            toggleActive(newUI, true);
            currentUI = newUI;
            toggleActive(currentProgram, false);
            toggleActive(newProgram, true);
            currentProgram = newProgram;
            if (ui == 2)
            {
                if (!SaveLoad.activeSelf) toggleActive(SaveLoad, true);
                if (EmptyPanel.activeSelf) EmptyPanel.SetActive(false);
            }
            else
            {
                if (SaveLoad.activeSelf) toggleActive(SaveLoad, false);
                if (!EmptyPanel.activeSelf) EmptyPanel.SetActive(true);
            }
        }
        if (ui == 0)
            StartWindow.SetActive(true);
        else
            StartWindow.SetActive(false);
    }
}
