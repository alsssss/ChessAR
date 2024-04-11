using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class ReloadScene : MonoBehaviourPunCallbacks
{
    public void OnRestartButton() {

        PhotonNetwork.LoadLevel("PlayScene");
    }
}
