using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class CloseGame : MonoBehaviourPunCallbacks
{
    public void OnExitButton()
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("Menu");
        GameManager.IsPaused = false ;
    }
}
