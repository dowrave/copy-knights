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
            // UI 요소 클릭 시의 동작
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // HandleUIMouseDown();
                return;
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                Debug.Log("UI 요소 클릭되어서 동작 중단됨. 감지된 첫 번째 요소: " + results[0].gameObject.name);

                // 디버깅을 위해 모든 감지된 요소 출력
                // foreach (var result in results)
                // {
                //     Debug.Log("감지된 UI: " + result.gameObject.name);
                // }

                return;
            }

            // UI가 클릭되지 않았을 때의 로직 (예: ProcessClickMapObject())
            ProcessClickMapObject();
        }
    }
    


    public void OnButtonClicked()
    {
        buttonClickedThisFrame = true;
        shouldSkipHandleClick = true; // 즉시 HandleClick이 호출되는 것을 방지
    }

    // private void HandleUIMouseDown()
    // {
    //     // UI 요소에 대한 레이캐스트
    //     List<RaycastResult> results = PerformScreenRaycast();
    //     foreach (var result in results)
    //     {
    //         // ButtonDown 동작 1. 다이아몬드 내부 클릭 시 방향 설정
    //         DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
    //         if (diamondMask != null)
    //         {
    //             if (diamondMask.IsPointInsideDiamond(Input.mousePosition))
    //             {
    //                 DeploymentInputHandler.Instance!.SetIsMousePressed(true);
    //                 return;
    //             }
    //         }
    //     }
    // }

    private void ProcessClickMapObject()
    {
        // 1. 배치 중 드래깅 혹은 방향 선택 상태라면 클릭 처리 중단
        // 배치 상황은 DeployableManager에서 처리한다. 여기서는 다른 오브젝트의 클릭 동작을 막기 위해 남겨둠.
        if (DeploymentInputHandler.Instance!.CurrentState == DeploymentInputHandler.InputState.SelectingDirection)
        {
            Debug.Log("HandleClick : 배치 중 드래깅 혹은 방향 선택 상태 - 클릭 처리 중단");
            return;
        }

        // 2. 3D 오브젝트 클릭 처리
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, clickableLayerMask);

        if (hits.Length > 0)
        {
            HandleObjectClick(hits);
        }
        else
        {
            HandleEmptySpaceClick();
        }
    }

    // private bool HandleSpecialUIClick()
    // {
    //     List<RaycastResult> results = PerformScreenRaycast();

    //     foreach (var result in results)
    //     {
    //         // 1. 다이아몬드 외부 클릭 시 상태 해제
    //         DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
    //         if (diamondMask != null)
    //         {
    //             if (!diamondMask.IsPointInsideDiamond(Input.mousePosition))
    //             {
    //                 Debug.Log("HandleUIClick : 다이아몬드 외부 클릭");

    //                 // 마름모 외부 클릭 처리
    //                 DeployableManager.Instance!.CancelCurrentAction();
    //                 return true;
    //             }
    //         }

    //         // 2. OperatorUI 관련 요소 클릭 처리 - Deployable.OnClick이 동작하도록 수정
    //         // 체력 바 같은 걸 클릭했을 때도 해당 배치 요소를 클릭하게끔 구현한 내용이다. 지우면 안됨.
    //         DeployableUnitEntity? associatedDeployable = GetAssociatedDeployableUnitEntity(result.gameObject);
    //         if (associatedDeployable != null)
    //         {
    //             associatedDeployable.OnClick();
    //             Debug.Log($"{associatedDeployable}.OnClick 메서드가 동작함");
    //             return true;
    //         }
    //     }

    //     Debug.Log("HandleSpecialUIClick이 동작했으나 해당 요소가 없음");

    //     return false;
    // }

    private void HandleObjectClick(RaycastHit[] hits)
    {
        // 디버깅용 : 모든 오브젝트 출력
        foreach (var hit in hits)
        {
            Debug.Log($"RaycastAll hit: {hit.collider.name}");
        }

        // 1. DeployableUnitEntity를 먼저 찾음
        foreach (var hit in hits.OrderBy(h => h.distance)) // 카메라에서 가까운 순서
        {
            DeployableUnitEntity? clickable = hit.collider.GetComponentInParent<DeployableUnitEntity>();
            if (clickable != null && !DeploymentInputHandler.Instance!.IsClickingPrevented)
            {
                clickable.OnClick();
                Debug.Log("DeployableUnitEntity 콜라이더 감지 및 클릭됨");
                return;
            }
        }

        // 2. 타일에 배치된 유닛이 있는지 확인
        foreach (var hit in hits.OrderBy(h => h.distance))
        {
            Tile? clickedTile = hit.collider.GetComponent<Tile>();
            if (clickedTile != null && clickedTile.OccupyingDeployable != null)
            {
                clickedTile.OccupyingDeployable.OnClick();
                Debug.Log("타일이 클릭되어서 그 위의 Deployable을 클릭함");
                return;
            }
        }

        // 3. 타일이 있다면 빈 타일을 클릭한 것으로 간주
        if (hits.Any(h => h.collider.GetComponent<Tile>() != null))
        {
            DeployableManager.Instance!.CancelCurrentAction();
            Debug.Log("빈 타일을 클릭함");
            return;
        }

        // 4. 위의 조건에 다 해당하지 않으면 빈 공간 클릭으로 처리
        HandleEmptySpaceClick();
    }
    
    private void HandleEmptySpaceClick()
    {
        Debug.Log("빈 공간 클릭 - CancelCurrentAction 동작");
        DeployableManager.Instance!.CancelCurrentAction();
    }

    /// <summary>
    /// 마우스 포인터 클릭 시 닿는 모든 UI 레이캐스트 대상을 반환함
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
        Debug.Log($"GetAssociatedDeployableUnitEntity 동작, clickedObject : {clickedObject.name}");
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