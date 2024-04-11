using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SliderScipt : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TextMeshProUGUI _sliderText; 

    // Start is called before the first frame update
    void Start()
    {
        _slider.onValueChanged.AddListener((v) => {
            _sliderText.text = (v*300).ToString("0");
        }
        
        );
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
