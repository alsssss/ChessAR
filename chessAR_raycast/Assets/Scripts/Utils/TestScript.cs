using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    private int i = 1;
    [SerializeField] private TextMeshProUGUI _text;
    void Start()
    {
        InvokeRepeating("SetNewText",0f,1f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetNewText(){
        if(i == 1){
            _text.text = "Loading .  ";
        }
        else if(i == 2){
            _text.text = "Loading .. ";
        }else if(i == 3){
            _text.text = "Loading ...";
            i = 0;
        }
        i = i+1;
    }
}
