using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class ErrorManager : MonoBehaviourPunCallbacks
{
    public GameObject[] ErrorMessages;
    public GameObject canvas;

    public override void OnJoinRoomFailed(short returnCode, string Message)
    {
        if (returnCode == 32765)
        {
            GameObject err1 = Instantiate(ErrorMessages[0],canvas.transform);
            Destroy(err1, 5f);
        }
        if (returnCode == 32758)
        {
            GameObject err1 = Instantiate(ErrorMessages[1],canvas.transform);
            Destroy(err1, 5f);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Room non creata:" + message, this);
        GameObject err1 = Instantiate(ErrorMessages[2], canvas.transform);
        Destroy(err1, 5f);
    }


}
