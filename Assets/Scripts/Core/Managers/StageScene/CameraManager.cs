using System.Collections;
using UnityEngine;
using System; 

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; } = null!; // null이 아님을 보장, 경고문구 무시

    private float animationDuration = 0.05f;

    // OperatorInfo가 나타날 때 카메라의 위치 / 높이 변화
    private float cameraShiftAmount = -0.1f;
    private float cameraHeightAmount = 1f;
    private float clickedOperatorZShiftAmount = 2f;

    private Vector3 operatorInfoRotation = new Vector3(0, -15, -15);

    [SerializeField] Camera? mainCamera;
    public Camera MainCamera 
    { 
        get => mainCamera!;
        private set { mainCamera = value; }
    }

    private Coroutine _currentCoroutine; 

    private Vector3 originalPosition;

    // 카메라 각도 - Quaternion 타입은 transform.rotation에 할당
    public Quaternion baseRotation = Quaternion.Euler(70f, 0f, 0f);
    public Quaternion infoRotation = Quaternion.Euler(70f, -15f, -15f);
    // 만약 Vector3을 쓴다면 transform.eulerAngles에 할당

    private float originalSize;

    public System.Action OnCameraMovementFinished = delegate { };

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

    public void SetupForMap(Map? map)
    {
        // map의 카메라 position, rotation을 이용해 카메라 위치 설정
        // 카메라 위치 설정 자체는 개발자가 직접 보면서 실험하고 있음
        if (map != null)
        {
            SetCameraTransform(map.CameraPosition);
            SaveOriginalCameraSettings();
        }
    }

    private void SetCameraTransform(Vector3 position)
    {
        MainCamera.transform.position = position;
        MainCamera.transform.rotation = baseRotation;
    }

    public void FocusOnPosition(Vector3 targetPosition)
    {
        Vector3 offset = targetPosition - MainCamera.transform.position;
        MainCamera.transform.position += new Vector3(offset.x, 0, offset.z);
    }

    private void SaveOriginalCameraSettings()
    {
        originalPosition = MainCamera.transform.position;
        originalSize = MainCamera.orthographicSize;
    }

    /// <summary>
    /// UI를 클릭하거나 배치된 오퍼레이터 클릭 시 카메라 이동 / 회전 변경
    /// </summary>
    /// <param name="show">보여주는지 여부</param>
    /// <param name="deployable">배치된 deployable(배치되지 않은 경우는 null)</param>
    public void AdjustForDeployableInfo(bool show, DeployableUnitEntity? deployable = null)
    {

        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }

        // ShowDeployableInfo
        if (show)
        {
            Vector3 newPosition;
            if (MapManager.Instance == null)
            {
                Logger.LogError("맵 매니저 인스턴스가 초기화되지 않았음");
                return;
            }
            float mapWidth = MapManager.Instance.GetCurrentMapWidth();

            // 배치된 Deployable 클릭
            if (deployable != null && deployable.IsDeployed)
            {
                Vector3 operatorPosition = deployable.transform.position;

                float cameraOffset = mapWidth * (1 - 0.75f) * 0.5f;

                newPosition = new Vector3(
                    operatorPosition.x,
                    originalPosition.y,
                    operatorPosition.z - clickedOperatorZShiftAmount
                );
            }

            // 하단 패널의 Deployable 클릭
            else
            {
                newPosition = (originalPosition +
                                Vector3.up * cameraHeightAmount +
                                (Vector3.right * mapWidth * cameraShiftAmount)
                                );
            }


            // 카메라 이동 및 회전 (+ 후에 Slow 설정)
            _currentCoroutine = StartCoroutine(LerpPositionAndRotation(MainCamera.transform, newPosition, infoRotation, animationDuration, 
                onComplete: () => StageManager.Instance.SlowState = true));
        }
        // 해제 상황 : HideDeployableInfo
        else
        {
            // 카메라 이동 및 회전 (시작 시에 Slow 해제)
            _currentCoroutine = StartCoroutine(LerpPositionAndRotation(MainCamera.transform, originalPosition, baseRotation, animationDuration,
                onStart: () => StageManager.Instance.SlowState = false ));
        }
    }

    private IEnumerator LerpPositionAndRotation(Transform transform, Vector3 targetPosition, Quaternion targetRotation, float duration, Action onStart = null, Action onComplete = null)
    {
        onStart?.Invoke();

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        float time = 0;
        while (time < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, time / duration);

            time += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        onComplete?.Invoke();
    }
}
