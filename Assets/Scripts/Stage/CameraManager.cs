using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private float animationDuration = 0.5f;

    private Camera mainCamera;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float originalSize;

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
        // map의 카메라 position, rotation을 이용해 카메라 위치 설정
        // 카메라 위치 설정 자체는 개발자가 직접 보면서 실험하고 있음
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

    private void SaveOriginalCameraSettings()
    {
        originalPosition = mainCamera.transform.position;
        originalRotation = mainCamera.transform.rotation;
        originalSize = mainCamera.orthographicSize;
    }

    public void AdjustForOperatorInfo(bool show)
    {
        // 오퍼레이터 정보 필요한 경우) 카메라를 오른쪽으로 이동, 크기 3/4 으로 축소
        if (show)
        {
            // 카메라 오른쪽으로 이동
            Vector3 newPosition = originalPosition + Vector3.right * (Screen.width / 8f);
            StartCoroutine(LerpPosition(mainCamera.transform, newPosition, animationDuration));

            // 카메라 크기 조정 (3/4)
            float newSize = originalSize * 0.75f;
            StartCoroutine(LerpOrthoSize(mainCamera, newSize, animationDuration));
        }
        else
        {
            // 원위치와 크기로 복귀
            StartCoroutine(LerpPosition(mainCamera.transform, originalPosition, animationDuration));
            StartCoroutine(LerpOrthoSize(mainCamera, originalSize, animationDuration));
        }
    }

    // 카메라의 위치를 부드럽게 변경 : 시작 -> 목표 위치로 지정된 duration 동안 선형 보간을 수행
    private IEnumerator LerpPosition(Transform transform, Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = transform.position;
        float time = 0;
        while (time < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
    }

    // orthographic 카메라의 크기를 부드럽게 변경
    private IEnumerator LerpOrthoSize(Camera camera, float targetSize, float duration)
    {
        float startSize = camera.orthographicSize;
        float time = 0;
        while (time < duration)
        {
            camera.orthographicSize = Mathf.Lerp(startSize, targetSize, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        camera.orthographicSize = targetSize;
    }

    //private void OnValidate()
    //{
    //    if (mainCamera != null)
    //    {
    //        ApplyCameraSettings();
    //    }
    //}

}
