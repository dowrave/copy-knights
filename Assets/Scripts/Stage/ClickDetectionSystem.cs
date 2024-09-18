using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ClickDetectionSystem : MonoBehaviour
{
    public static ClickDetectionSystem Instance { get; private set; }

    private Camera mainCamera;
    [SerializeField] private LayerMask clickableLayerMask;  // Inspector에서 설정

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
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        List<RaycastResult> results = PerformScreenRaycast();
        foreach (var result in results)
        {
            Debug.Log($"MouseButtonDown Raycast hit: {result.gameObject.name}");

            // ButtonDown 동작 1. 다이아몬드 내부 클릭 시 방향 설정
            DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
            if (diamondMask != null)
            {
                if (diamondMask.IsPointInsideDiamond(Input.mousePosition))
                {
                    Debug.LogWarning("HandleUIClick : 다이아몬드 내부 ");
                    DeployableManager.Instance.IsMousePressed = true;
                    return;
                }
            }

            // ButtonDown 동작 2. 오퍼레이터 박스 드래그 동작 시작
            BottomPanelDeployableBox deployableBox = result.gameObject.GetComponent<BottomPanelDeployableBox>();
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
        List<RaycastResult> uiResults = results.Where(r => r.module is GraphicRaycaster).ToList();

        // UI 요소가 클릭되었다면 해당 UI 요소 실행
        if (uiResults.Count > 0)
        {            
            HandleUIClick(uiResults);
            return;
        }

        // UI 요소가 클릭되지 않은 상태에서 다른 Clickable 요소 처리
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit clickableHit;
        if (Physics.Raycast(ray, out clickableHit, Mathf.Infinity, clickableLayerMask))
        {
            HandleObjectClick(clickableHit);
        }
        else
        {
            HandleEmptySpaceClick();
        }
    }

    private void HandleUIClick(List<RaycastResult> uiResults)
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
                    Debug.LogWarning("HandleUIClick : 다이아몬드 외부 클릭");

                    // 마름모 외부 클릭 처리
                    DeployableManager.Instance.CancelCurrentAction();
                    return;
                }
            }

            // Button 관련 로직은 굳이 구현 안해도 되는 듯 - 이거 있으면 중복실행됨
            // 1. 유니티의 UI 시스템은 OnClick을 등록하지 않더라도 자동으로 실행시킨다고 함
            // 2. 여기서 구현한 MouseButtonUp이 true가 돼서 HandleClick이 동작함
        }
    }

    private void HandleDiamondInteriorClick(RaycastResult result)
    {
        // 마름모 내부 클릭 시 내부 동작 유지 - ActionUI나 DeployingUI 상태 유지

    }

    private void HandleObjectClick(RaycastHit hit)
    {
        DeployableUnitEntity clickable = hit.collider.GetComponent<DeployableUnitEntity>();
        if (clickable != null)
        {
            clickable.OnClick();
        }

        else
        {
            Tile clickedTile = hit.collider.GetComponent<Tile>();
            DeployableUnitEntity clickedDeployable = clickedTile.OccupyingDeployable;

            if (clickedTile != null && clickedDeployable != null)
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
                DeployableManager.Instance.CancelCurrentAction();
            }
        }
    }
    
    private void HandleEmptySpaceClick()
    {
        DeployableManager.Instance.CancelCurrentAction();
    }

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

        IDeployable deployable = hit.collider.GetComponent<IDeployable>();
        if (deployable != null)
        {
            Debug.Log("Hit 오브젝트는 IDeployable 인터페이스를 구현했습니다.");
        }
        else
        {
            Debug.Log("Hit 오브젝트는 IDeployable 인터페이스를 구현하지 않았습니다.");
        }
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

}
