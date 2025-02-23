using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Burst.CompilerServices;

public class ClickDetectionSystem : MonoBehaviour
{
    public static ClickDetectionSystem Instance { get; private set; }

    private Camera mainCamera;
    [SerializeField] private LayerMask clickableLayerMask;  // Inspector에서 설정

    private bool isMouseDown = false;
    private bool isDraggingDiamond = false;
    private DiamondMask currentDiamondMask; 

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
        }
        if (Input.GetMouseButtonUp(0))
        {
            HandleClick();
        }
    }

    private void HandleMouseDown()
    {
        isMouseDown = true;
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        List<RaycastResult> results = PerformScreenRaycast();
        foreach (var result in results)
        {
            //Debug.Log($"MouseButtonDown Raycast hit: {result.gameObject.name}");

            // ButtonDown 동작 1. 다이아몬드 내부 클릭 시 방향 설정
            DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
            if (diamondMask != null)
            {
                if (diamondMask.IsPointInsideDiamond(Input.mousePosition))
                {
                    //Debug.LogWarning("HandleUIClick : 다이아몬드 내부 ");
                    DeployableManager.Instance.IsMousePressed = true;
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


    /// <summary>
    /// 전체적인 클릭 동작을 담당함
    /// 1. 클릭한 지점에 UI 요소(GrpahicRayCaster가 있는 Canvas)가 있다면 먼저 반응
    /// 2. 없다면 3D 오브젝트가 IClickable일 경우 해당 클릭에 대한 동작
    /// </summary>
    private void HandleClick()
    {
        List<RaycastResult> results = PerformScreenRaycast();

        foreach (var result in results)
        {
            // 다이아몬드와 DeployableUnitentity가 겹치면 deployable의 동작이 우선되어야 함
            // 임시)
            Debug.Log($"Raycast hit: {result.gameObject.name}");
            Tile clickedTile = result.gameObject.GetComponent<Tile>();
            if (clickedTile != null)
            {
                DeployableUnitEntity clickedDeployable = clickedTile.OccupyingDeployable;
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

                    return;
                    // Operator가 아닐 때에도 퇴각 버튼은 나타나야 함 
                }
            }
        }

        ProcessClickPriority(results);
    }

    private void ProcessClickPriority(List<RaycastResult> results)
    {
        List<RaycastResult> uiResults = results.Where(r => r.module is GraphicRaycaster).ToList();
        // UI 요소가 클릭되었다면 해당 UI 요소 실행
        if (uiResults.Count > 0)
        {
            bool handleUI = HandleUIClick(uiResults);
            // UI 처리가 된 경우에는 더 이상의 처리를 하지 않음
            if (handleUI) return;
        }

        // 방향 선택 중이거나 드래깅 중일 때는 DeployableManager에서 처리함
        if (DeployableManager.Instance.IsSelectingDirection || DeployableManager.Instance.IsDraggingDeployable) return;

        // UI 요소가 클릭되지 않은 상태에서 다른 Clickable 요소 처리
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit clickableHit;

        // 배치 중인 상황이 아닐 때
        if (Physics.Raycast(ray, out clickableHit, Mathf.Infinity, clickableLayerMask))
        {
            HandleObjectClick(clickableHit);
        }
        else
        {
            HandleEmptySpaceClick();
        }
    }

    private bool HandleUIClick(List<RaycastResult> uiResults)
    {
        foreach (var result in uiResults)
        {
            Debug.Log($"Raycast hit: {result.gameObject.name}");

            // 1. 다이아몬드 외부 클릭 시 상태 해제
            DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
            if (diamondMask != null)
            {
                if (!diamondMask.IsPointInsideDiamond(Input.mousePosition))
                {
                    //Debug.LogWarning("HandleUIClick : 다이아몬드 외부 클릭");

                    // 마름모 외부 클릭 처리
                    DeployableManager.Instance.CancelCurrentAction();
                    return true;
                }
            }

            // 2. OperatorUI 관련 요소 클릭 처리 - Deployable.OnClick이 동작하도록 수정
            DeployableUnitEntity associatedDeployable = GetAssociatedDeployableUnitEntity(result.gameObject);
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

    private void HandleObjectClick(RaycastHit hit)
    {
        DeployableUnitEntity clickable = hit.collider.GetComponent<DeployableUnitEntity>();
        if (clickable != null && !DeployableManager.Instance.IsClickingPrevented)
        {
            clickable.OnClick();
        }

        else
        {
            Tile clickedTile = hit.collider.GetComponent<Tile>();
            if (clickedTile != null)
            {
                DeployableUnitEntity clickedDeployable = clickedTile.OccupyingDeployable;
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
                    DeployableManager.Instance.CancelCurrentAction();
                }
            }
        }
    }
    
    private void HandleEmptySpaceClick()
    {
        DeployableManager.Instance.CancelCurrentAction();
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
    private DeployableUnitEntity GetAssociatedDeployableUnitEntity(GameObject clickedObject)
    {
        Transform current = clickedObject.transform;
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
