using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class ConnectToServer : MonoBehaviourPunCallbacks
{
    public GameObject panel;

    public void OnClick()
    {
        PhotonNetwork.ConnectUsingSettings();
        if (panel != null)
            panel.SetActive(true);
    }

    public override void OnConnectedToMaster(){
        PhotonNetwork.JoinLobby(); 
    }

    public override void OnJoinedLobby(){
        SceneManager.LoadScene("OnlineMenu");    
    }

   
}
