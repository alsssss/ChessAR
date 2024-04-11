using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageRecognition : MonoBehaviour
{
    [SerializeField] private GameObject chessboard;
    private ARTrackedImageManager imageManager;

    private void Awake()
    {
        imageManager = FindObjectOfType<ARTrackedImageManager>();
    }

    public void OnEnable()
    {
        imageManager.trackedImagesChanged += OnImageChanged;
    }

    public void OnDisable()
    {
        imageManager.trackedImagesChanged -= OnImageChanged;
    }

    public void OnImageChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added)
        {
            UpdateImage(trackedImage);
        }

//        foreach (var trackedImage in args.updated)
//        {
//            UpdateImage(trackedImage);
//        }
//        foreach (var trackedImage in args.removed)
//        {
//            chessboard.SetActive(false);
//        }
    }

    private void UpdateImage(ARTrackedImage trackedImage)
    {
        Debug.Log(trackedImage.name);
 //       Vector3 position = trackedImage.transform.position;
 //       chessboard.transform.position = position;
    }
    
}

