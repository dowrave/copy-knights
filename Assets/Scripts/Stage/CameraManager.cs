using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    private Camera mainCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    public void SetupForMap(Map map)
    {
        if (map != null)
        {
            SetCameraPosition(map.CameraPosition, map.CameraRotation);
        }
    }

    private void SetCameraPosition(Vector3 position, Vector3 rotation)
    {
        mainCamera.transform.position = position;
        mainCamera.transform.rotation = Quaternion.Euler(rotation);
    }

    public void FocusOnPosition(Vector3 targetPosition)
    {
        Vector3 offset = targetPosition - mainCamera.transform.position;
        mainCamera.transform.position += new Vector3(offset.x, 0, offset.z);
    }

    //private void OnValidate()
    //{
    //    if (mainCamera != null)
    //    {
    //        ApplyCameraSettings();
    //    }
    //}

}
