using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickDetectionSystem : MonoBehaviour
{
    public static ClickDetectionSystem? Instance { get; private set; }

    private Camera mainCamera = default!;
    [SerializeField] private LayerMask clickableLayerMask = default!;  // Inspector에서 설정

    //private bool isDraggingDiamond = false;
    //private DiamondMask currentDiamondMask;

    private bool isTutorialMode = false;
    private string expectedButtonName = string.Empty;

    // 이미 실행된 UI가 있는 경우, 이 스크립트가 동작하지 않아도 되게 함
    public bool buttonClickedThisFrame = false;
    private bool shouldSkipHandleClick = false;

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
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseDown();
            shouldSkipHandleClick = false; // 매 프레임 초기화
        }
        if (Input.GetMouseButtonUp(0))
        {
            // UI 클릭이 없었을 때에만 HandleClick 동작
            if (!shouldSkipHandleClick)
            {
                HandleClick();
            }

            // 다음 프레임을 위한 초기화
            buttonClickedThisFrame = false;
            shouldSkipHandleClick = false;
        }
    }

    public void OnButtonClicked()
    {
        buttonClickedThisFrame = true;
        shouldSkipHandleClick = true; // 즉시 HandleClick이 호출되는 것을 방지
    }

    private void HandleMouseDown()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);

        // UI 요소에 대한 레이캐스트
        List<RaycastResult> results = PerformScreenRaycast();
        foreach (var result in results)
        {
            // ButtonDown 동작 1. 다이아몬드 내부 클릭 시 방향 설정
            DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
            if (diamondMask != null)
            {
                if (diamondMask.IsPointInsideDiamond(Input.mousePosition))
                {
                    //Debug.LogWarning("HandleUIClick : 다이아몬드 내부 ");
                    DeployableManager.Instance!.IsMousePressed = true;
                    return;
                }
            }

            // ButtonDown 동작 2. 오퍼레이터 박스 드래그 동작 시작
            DeployableBox deployableBox = result.gameObject.GetComponent<DeployableBox>();
            if (deployableBox != null)
            {
                deployableBox.OnPointerDown(pointerData);
                return;
            }
        }
    }



    // 클릭한 지점에 UI 요소(GrpahicRayCaster가 있는 Canvas)가 있다면 먼저 반응함(여기서의 처리가 아님)
    // 마우스 버튼을 "뗀" 시점에 동작함
    private void HandleClick()
    {
        List<RaycastResult> results = PerformScreenRaycast();

        // UI 요소를 감지했고 그 UI가 클릭 이벤트를 처리했으면 멈춘다.
        if (results.Count > 0 && ProcessClickUI(results))
        {
            return;
        }

        // UI가 없었거나 있어도 클릭을 처리하지 않았을 때 맵 오브젝트를 처리
        ProcessClickMapObject();
    }

    // UI 요소 처리: GraphicRaycaster 모듈이 있는 결과만 필터링
    private bool ProcessClickUI(List<RaycastResult> results)
    {
        var uiResults = results.Where(r => r.module is GraphicRaycaster).ToList();
        if (uiResults.Count > 0)
        {
            return HandleUIClick(uiResults);
        }
        return false; // 처리할 UI가 없음
    }

    private bool HandleUIClick(List<RaycastResult> uiResults)
    {
        foreach (var result in uiResults)
        {
            // 1. 다이아몬드 외부 클릭 시 상태 해제
            DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
            if (diamondMask != null)
            {
                if (!diamondMask.IsPointInsideDiamond(Input.mousePosition))
                {
                    Debug.Log("HandleUIClick : 다이아몬드 외부 클릭");

                    // 마름모 외부 클릭 처리
                    DeployableManager.Instance!.CancelCurrentAction();
                    return true;
                }
            }

            // 2. OperatorUI 관련 요소 클릭 처리 - Deployable.OnClick이 동작하도록 수정
            DeployableUnitEntity? associatedDeployable = GetAssociatedDeployableUnitEntity(result.gameObject);
            if (associatedDeployable != null )
            {
                associatedDeployable.OnClick();
                return true;
            }
        }

        return false;
    }

    private void HandleDiamondInteriorClick(RaycastResult result)
    {
        // 마름모 내부 클릭 시 내부 동작 유지 - ActionUI나 DeployingUI 상태 유지
    }

    private void ProcessClickMapObject()
    {
        // 1. 배치 중 드래깅 혹은 방향 선택 상태라면 클릭 처리 중단
        // 꼭 필요하지 않은 부분일 수 있음 - Click이니까 마우스 떼는 시점에만 이 스크립트가 동작하는데
        // 최소한 IsDraggingDeployable은 떼는 시점에 이미 false로 바뀌어 있음
        if (DeployableManager.Instance!.IsSelectingDirection ||
            DeployableManager.Instance!.IsDraggingDeployable)
        {
            Debug.Log("HandleClick : 배치 중 드래깅 혹은 방향 선택 상태 - 클릭 처리 중단");
            return;
        }

        // 2. 3D 오브젝트 클릭 처리
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit clickableHit, Mathf.Infinity, clickableLayerMask))
        {
            HandleObjectClick(clickableHit);
        }
        else
        {
            HandleEmptySpaceClick();
        }
    }

    private void HandleObjectClick(RaycastHit hit)
    {
        DeployableUnitEntity? clickable = hit.collider.GetComponent<DeployableUnitEntity>();

        if (clickable != null && !DeployableManager.Instance!.IsClickingPrevented)
        {
            clickable.OnClick();
        }

        else
        {
            Tile? clickedTile = hit.collider.GetComponent<Tile>();
            if (clickedTile != null)
            {
                DeployableUnitEntity? clickedDeployable = clickedTile.OccupyingDeployable;
                if (clickedDeployable != null)
                {
                    if (clickedDeployable is Operator op)
                    {
                        op.OnClick();
                    }

                    else
                    {
                        clickedDeployable.OnClick();
                    }
                    // Operator가 아닐 때에도 퇴각 버튼은 나타나야 함 
                }
                else
                {
                    // clickedTile이 null일 때도 현재 액션 취소
                    Debug.Log("클릭된 배치 요소 없음 - CancelCurrentAction 동작");

                    DeployableManager.Instance!.CancelCurrentAction();
                }
            }
        }
    }
    
    private void HandleEmptySpaceClick()
    {
        DeployableManager.Instance!.CancelCurrentAction();
    }

    /// <summary>
    /// 마우스 포인터 클릭 시 닿는 모든 레이캐스트 대상을 반환함
    /// </summary>
    private List<RaycastResult> PerformScreenRaycast()
    {
        // UI 요소를 클릭했는지 점검
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results;
    }

    /// <summary>
    /// 클릭된 오브젝트로부터 상위 오브젝트에 DeployableUnitEntity가 있는지 검사함
    /// </summary>
    private DeployableUnitEntity? GetAssociatedDeployableUnitEntity(GameObject clickedObject)
    {
        Transform? current = clickedObject.transform;
        while (current != null)
        {
            DeployableUnitEntity deployable = current.GetComponent<DeployableUnitEntity>();
            if (deployable != null)
            {
                return deployable;
            }
            current = current.parent;
        }

        return null;
    }
}
