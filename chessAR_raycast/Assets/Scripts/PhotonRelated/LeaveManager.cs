using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class LeaveManager : MonoBehaviourPunCallbacks
{
    public GameObject Warning;
    public GameObject canvas;


    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        GameObject err1 = Instantiate(Warning,canvas.transform);
        Destroy(err1, 5f);
    }
}