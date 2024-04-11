using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    public GameObject panel;
    public GameObject canvas;

    public void OpenPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
        if (panel.name == "PopUp Menu")
        {
            canvas.transform.GetChild(2).gameObject.SetActive(false);
        }

    }

    public void isPaused()
    {
        if (panel.name == "PopUp Menu")
        {
            GameManager.IsPaused = true;
        }
    }

    public void Restarter()
    {
        GameManager.IsRestarted = true;
    }
}
