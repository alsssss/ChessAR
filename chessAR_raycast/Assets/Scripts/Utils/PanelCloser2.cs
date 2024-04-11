using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;


public class PanelCloser2 : MonoBehaviour
{
    public GameObject panel;

    // Update is called once per frame
    void Update()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            panel.SetActive(false);
        }
    }
}
