using Photon.Pun.Demo.Cockpit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetInteraction : MonoBehaviour
{
    public Button[] Buttons;
    public InputField[] InputFields;

    public void EnableInteraction()
    {
        for (int i = 0; i< Buttons.Length; i++)
        {
            Buttons[i].interactable = true;
        }
        
        for (int i = 0; i < InputFields.Length; i++)
        {
            InputFields[i].interactable = true;
        }
    }

    public void DisableInteraction()
    {
        for (int i = 0; i < Buttons.Length; i++)
        {
            Buttons[i].interactable = false;
        }
        
        for (int i = 0; i < InputFields.Length; i++)
        {
            InputFields[i].interactable = false;
        }
    }

}
