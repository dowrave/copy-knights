using UnityEngine;

public class CameraManager : MonoBehaviour
{
    // 싱글톤 패턴을 위한 인스턴스
    public static CameraManager Instance { get; private set; }

    // 카메라 설정
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraHeight = 8f;
    [SerializeField] private float cameraAngle = 75f;
    [SerializeField] private float cameraOffsetZ = 2f;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AdjustCameraToMap(int mapWidth, int mapHeight)
    {
        // 메인 카메라가 없으면 찾아서 할당
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            // 맵의 중심점 계산
            Vector3 mapCenter = new Vector3(mapWidth / 2f - 0.5f, 0f, mapHeight / 2f - 0.5f);

            // 카메라 회전 설정
            mainCamera.transform.rotation = Quaternion.Euler(cameraAngle, 0f, 0f);

            // 카메라 위치 계산 및 설정
            float zOffset = cameraOffsetZ * Mathf.Tan((90f - cameraAngle) * Mathf.Deg2Rad);
            Vector3 cameraPosition = new Vector3(
                mapCenter.x,
                cameraHeight,
                mapCenter.z - zOffset
            );
            mainCamera.transform.position = cameraPosition;

            // 카메라가 맵의 중심을 바라보도록 설정
            mainCamera.transform.LookAt(mapCenter);

            // 카메라의 시야각 조정
            float mapSize = Mathf.Max(mapWidth, mapHeight);
            mainCamera.fieldOfView = 2f * Mathf.Atan(mapSize / (2f * cameraHeight)) * Mathf.Rad2Deg;
        }
    }
}