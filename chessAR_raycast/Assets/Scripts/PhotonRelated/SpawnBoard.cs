using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Photon.Pun;

public class SpawnBoard : MonoBehaviour
{
public GameObject chessboard;
public Camera arCamera;


    private void Start(){

        Vector3 position = new Vector3(0, 0, 0);
        GameObject chessboardInstance = PhotonNetwork.InstantiateRoomObject(chessboard.name, position, Quaternion.identity);

    }

}
