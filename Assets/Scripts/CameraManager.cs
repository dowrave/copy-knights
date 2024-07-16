using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    // 카메라 설정
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraHeight = 8f;
    [SerializeField] private float cameraAngle = 75f;
    [SerializeField] private float cameraOffsetZ = 2f;

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
    }

    public void AdjustCameraToMap(int mapWidth, int mapHeight)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 mapCenter = new Vector3(mapWidth / 2f - 0.5f, 0f, mapHeight / 2f - 0.5f);
            mainCamera.transform.rotation = Quaternion.Euler(cameraAngle, 0f, 0f);

            float zOffset = cameraOffsetZ * Mathf.Tan((90f - cameraAngle) * Mathf.Deg2Rad);
            Vector3 cameraPosition = new Vector3(
                mapCenter.x,
                cameraHeight,
                mapCenter.z - zOffset
            );
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.LookAt(mapCenter);

            float mapSize = Mathf.Max(mapWidth, mapHeight);
            mainCamera.fieldOfView = 2f * Mathf.Atan(mapSize / (2f * cameraHeight)) * Mathf.Rad2Deg;
        }
    }
}
