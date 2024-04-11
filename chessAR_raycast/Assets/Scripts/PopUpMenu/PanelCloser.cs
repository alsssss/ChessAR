using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelCloser : MonoBehaviour
{
    public GameObject panel;
    public GameObject canvas;


    public void ClosePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
        if (panel.name == "PopUp Menu")
        {
            canvas.transform.GetChild(2).gameObject.SetActive(true);
        }
    }
    public void unPause()
    {
        if (panel.name == "PopUp Menu")
        {
            GameManager.IsPaused = false;
        }
    }

}

