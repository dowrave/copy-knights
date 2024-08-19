using System;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEditor.Experimental.GraphView.GraphView;

public class ClickDetectionSystem : MonoBehaviour
{
    public static ClickDetectionSystem Instance { get; private set; }

    private Camera mainCamera;
    [SerializeField] private LayerMask clickableLayerMask;  // Inspector에서 설정
    [SerializeField] private LayerMask operatorUILayerMask; 

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

    private void Start()
    {
        mainCamera = Camera.main;
        //LogCameraCullingMask();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) 
            //&& !IsPointerOverUIObject()
            )
        {
            HandleClick();
        }
    }

    // 전체적인 클릭 로직을 담당함
    private void HandleClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // 카메라 -> 마우스 위치로 향하는 광선 생성
        RaycastHit hit; // 레이캐스트 결과 저장 변수

        // OperatorUI와의 충돌 검사
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, operatorUILayerMask))
        {
            // OperatorUI를 클릭한 경우, 여기서 처리하고 return
            Debug.Log("OperatorUI clicked");
            return;
        }

        // 활성화된 OperatorActionUI가 있고, 그 외의 영역을 클릭한 경우
        if (OperatorManager.Instance.CurrentActiveActionUI != null)
        {
            OperatorManager.Instance.HideAllActionUIs();
            return;
        }



        // 지정된 레이어들에 대해 레이캐스트 수행
        // - ray : 시작점과 방향을 가진 광선
        // - out hit : 레이캐스트 결과 저장 변수
        // - Mathf.Infinity : 레이캐스트 최대 거리
        // - ClickableLayerMask.value : 검사할 레이어 마스크
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickableLayerMask.value)) 
        {

            // 클릭 로직 처리
            IClickable clickable = hit.collider.GetComponent<IClickable>();

            if (clickable != null)
            {
                clickable.OnClick();
            }
            else
            {
                // Tile에 Operator가 있는지 확인 <-- 배치된 Operator의 크기가 타일을 차지해서 꼭 이렇게 하지 않아도 되기는 함
                Tile clickedTile = hit.collider.GetComponent<Tile>();
                if (clickedTile != null && clickedTile.OccupyingOperator != null)
                {
                    clickedTile.OccupyingOperator.ShowActionUI();
                }
            }
        }

        else
        {
            //Debug.Log("레이캐스트가 어떤 오브젝트에도 닿지 않았습니다.");
        }
    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = Input.mousePosition;
        System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    //private bool IsPointerOverUIObject()
    //{
    //    PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
    //    eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
    //    List<RaycastResult> results = new List<RaycastResult>();

    //    // GraphicRaycaster 컴포넌트가 있는 모든 Canvas 위의 UI 요소에 대한 레이캐스트 수행
    //    EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

    //    int operatorLayer = LayerMask.NameToLayer("Operator");
    //    int uiLayer = LayerMask.NameToLayer("UI");

    //    foreach (RaycastResult result in results)
    //    {
    //        string layerName = LayerMask.LayerToName(result.gameObject.layer);
    //        Debug.Log("Hit layer: " + layerName);

    //        // UI 레이어에 있는지 확인
    //        if (result.gameObject.layer == uiLayer)
    //        {
    //            Debug.Log("UI clicked");
    //            return true;
    //        }
    //    }

    //    // UI 레이어가 아닌 경우 false 반환
    //    return false;
    //}

    /// <summary>
    /// 클릭 디버깅용 - 해결됨
    /// </summary>
    private void DebugClick(RaycastHit hit)
    {
        Debug.Log($"Hit 오브젝트: {hit.collider.gameObject.name}");
        Debug.Log($"Hit 위치: {hit.point}");
        Debug.Log($"Hit 오브젝트 태그: {hit.collider.gameObject.tag}");
        Debug.Log($"Hit 오브젝트 레이어: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

        Component[] components = hit.collider.gameObject.GetComponents<Component>();
        Debug.Log("Hit 오브젝트의 컴포넌트 목록:");
        foreach (Component component in components)
        {
            Debug.Log($"- {component.GetType().Name}");
        }

        IClickable clickable = hit.collider.GetComponent<IClickable>();
        if (clickable != null)
        {
            Debug.Log("Hit 오브젝트는 IClickable 인터페이스를 구현했습니다.");
        }
        else
        {
            Debug.Log("Hit 오브젝트는 IClickable 인터페이스를 구현하지 않았습니다.");
        }

        Debug.Log("------------------------");
    }

    private void LogCameraCullingMask()
    {
        if (mainCamera != null)
        {
            int cullingMask = mainCamera.cullingMask;
            Debug.Log($"Camera Culling Mask: {cullingMask}");

            // 활성화된 레이어 이름 출력
            for (int i = 0; i < 32; i++)
            {
                if ((cullingMask & (1 << i)) != 0)
                {
                    string layerName = LayerMask.LayerToName(i);
                    Debug.Log($"Enabled Layer {i}: {layerName}");
                }
            }
        }
        else
        {
            Debug.LogError("Main Camera not found!");
        }
    }
}

public interface IClickable
{
    void OnClick();
}