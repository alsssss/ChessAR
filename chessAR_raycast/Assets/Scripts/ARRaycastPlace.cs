using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;
using Photon.Pun;
using UnityEngine.UIElements;
using ExitGames.Client.Photon;
using Photon.Realtime;
using System.Data.Common;

[RequireComponent(typeof(ARRaycastManager), typeof(ARPlaneManager))]
public class ARRaycastPlace : MonoBehaviour
{
    [HideInInspector] public Vector3 p;
    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    [HideInInspector] public GameObject spawnedOne;
    

    [SerializeField] private GameObject chessboard;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();
    }

    private void OnEnable()
    {
        EnhancedTouch.TouchSimulation.Enable();
        EnhancedTouch.EnhancedTouchSupport.Enable();
        EnhancedTouch.Touch.onFingerDown += FingerDown;
    }

    private void OnDisable()
    {
        EnhancedTouch.TouchSimulation.Disable();
        EnhancedTouch.EnhancedTouchSupport.Disable();
        EnhancedTouch.Touch.onFingerDown -= FingerDown;
    }

    public void Restart()
    {
        spawnedOne = null;
    }

    public void FingerDown(EnhancedTouch.Finger finger)
    {
        if (finger.index != 0) return;

        if (spawnedOne == null)
        {
            if (raycastManager.Raycast(finger.currentTouch.screenPosition, hits, TrackableType.Planes))
            {
                foreach (ARRaycastHit hit in hits)
                {
                    Pose pose = hit.pose;
                    p = pose.position;

                    spawnedOne = Instantiate(chessboard, p, pose.rotation);
                    PhotonView photonView = spawnedOne.GetComponent<PhotonView>();

                    if (PhotonNetwork.AllocateViewID(photonView))
                    {
                        int data = photonView.ViewID;
                        if (PhotonNetwork.IsMasterClient) { GameManager.IdMaster = data; }
                        else { GameManager.IdClient = data; }
                        Debug.LogWarning(data);
                    }

                    else // Solo in caso di errore
                    {
                        Debug.LogError("Impossibile allocare il ViewId");

                        Destroy(spawnedOne);
                    }

                    if (planeManager.GetPlane(hit.trackableId).alignment == PlaneAlignment.HorizontalUp)
                    {
                        Vector3 position = spawnedOne.transform.position;
                        Vector3 cameraPosition = Camera.main.transform.position;
                        Vector3 direction = cameraPosition - position;
                        Vector3 targetRotationEuler = Quaternion.LookRotation(direction).eulerAngles;
                        Vector3 scaledEuler = Vector3.Scale(targetRotationEuler, spawnedOne.transform.up.normalized);
                        Quaternion targetRotation = Quaternion.Euler(scaledEuler);
                        spawnedOne.transform.rotation = targetRotation;
                        planeManager.SetTrackablesActive(false);
                        planeManager.enabled = false;
                        GameObject.Find("ARRaycastPlace").GetComponent<ARPlaneManager>().SetTrackablesActive(false);
                        GameObject.Find("ARRaycastPlace").GetComponent<ARPlaneManager>().enabled = false;
                    }

                }
            }
        }
    }
}

