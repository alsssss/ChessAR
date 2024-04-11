using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class CreateAndJoinsRooms : MonoBehaviourPunCallbacks
{
    public InputField createInput;
    public InputField joinInput;
    

    public void CreateRoom(){
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        string roomName = createInput.text.ToUpper();
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room creata", this);
    }

    public void JoinRoom(){
        string roomName = joinInput.text.ToUpper();
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinedRoom(){
       PhotonNetwork.LoadLevel("PlayScene");
    }

    
    public void OnBackButtonClick(){
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("Menu");
    }
}
