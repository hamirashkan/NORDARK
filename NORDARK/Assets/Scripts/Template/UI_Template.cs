using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Template : MonoBehaviour
{
    [SerializeField] private Toggle sampleToggle;
    [SerializeField] private Slider sampleTimeSlider;
    [SerializeField] private TMP_Text sampleTimeLabel;
    private FUN_Template templateFun;

    void Start() {
        templateFun = GameObject.Find("Prefab_FUN_Template").GetComponent<FUN_Template>();

        sampleToggle.onValueChanged.AddListener(delegate {OnToggleChanged(); });

        sampleTimeSlider.minValue = 0;
        sampleTimeSlider.maxValue = 100;
        sampleTimeSlider.wholeNumbers = true;
        sampleTimeSlider.onValueChanged.AddListener(delegate {OnTimeSliderChanged(); });
        sampleTimeSlider.interactable = false;
    }

    public void ActivateIndicator() {
        sampleToggle.interactable = true;
    }

    private void OnToggleChanged() {
        if (sampleToggle.isOn) {
            sampleTimeSlider.interactable = true;
            OnTimeSliderChanged();
        } else {
            sampleTimeSlider.interactable = false;
            templateFun.RemoveModifier();
        }
    }
    private void OnTimeSliderChanged() {
        templateFun.UpdateData((int)sampleTimeSlider.value);
        
        int beginningHour = (int)sampleTimeSlider.value;
        string firstHour = beginningHour.ToString();
        if (firstHour.Length < 2) {
            firstHour = "0" + firstHour;
        }
        string secondHour = (beginningHour + (int)sampleTimeSlider.value).ToString();
        if (secondHour.Length < 2) {
            secondHour = "0" + secondHour;
        }

        sampleTimeLabel.text = firstHour + ":00 - " + secondHour + ":00";
    }
}
