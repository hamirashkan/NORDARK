using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISlirTimeLine : MonoBehaviour
{
    ShowMap other;
    Slider slider;
    static Text label;
    // Start is called before the first frame update
    void Start()
    {
        //Adds a listener to the main slider and invokes a method when the value changes.
        slider = gameObject.GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate { ValueChangeCheck(); });
        label = GameObject.Find("TxtTimeLine").GetComponent<Text>();
        other = GameObject.Find("Mapbox").GetComponent<ShowMap>();
    }

    public void ValueChangeCheck()
    {
        if (other.timeIndex != (int)slider.value)
        {
            other.timeIndex = (int)slider.value;
            other.UpdateTexture();
        }
        other.timeIndex = (int)slider.value;
        label.text = other.timeIndex + "/" + other.timeSteps;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
