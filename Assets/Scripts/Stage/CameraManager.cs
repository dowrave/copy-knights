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
        // map�� ī�޶� position, rotation�� �̿��� ī�޶� ��ġ ����
        // ī�޶� ��ġ ���� ��ü�� �����ڰ� ���� ���鼭 �����ϰ� ����
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
        // ���۷����� ���� �ʿ��� ���) ī�޶� ���������� �̵�, ũ�� 3/4 ���� ���
        if (show)
        {
            // ī�޶� ���������� �̵�
            Vector3 newPosition = originalPosition + Vector3.right * (Screen.width / 8f);
            StartCoroutine(LerpPosition(mainCamera.transform, newPosition, animationDuration));

            // ī�޶� ũ�� ���� (3/4)
            float newSize = originalSize * 0.75f;
            StartCoroutine(LerpOrthoSize(mainCamera, newSize, animationDuration));
        }
        else
        {
            // ����ġ�� ũ��� ����
            StartCoroutine(LerpPosition(mainCamera.transform, originalPosition, animationDuration));
            StartCoroutine(LerpOrthoSize(mainCamera, originalSize, animationDuration));
        }
    }

    // ī�޶��� ��ġ�� �ε巴�� ���� : ���� -> ��ǥ ��ġ�� ������ duration ���� ���� ������ ����
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

    // orthographic ī�޶��� ũ�⸦ �ε巴�� ����
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
