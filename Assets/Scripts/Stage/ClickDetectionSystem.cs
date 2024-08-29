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
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    /// <summary>
    /// 전체적인 클릭 동작을 담당함
    /// 1. 클릭한 지점에 UI 요소(GrpahicRayCaster가 있는 Canvas)가 있다면 먼저 반응
    /// 2. 없다면 3D 오브젝트가 IClickable일 경우 해당 클릭에 대한 동작
    /// </summary>
    private void HandleClick()
    {
        // 1. UI 요소를 클릭했는지 점검
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        List<RaycastResult> allResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, allResults);

        // UI 요소만 필터링(GraphicRaycaster를 갖고 있는 것들만)
        List<RaycastResult> uiResults = allResults.Where(r => r.module is GraphicRaycaster).ToList();

        // 2. UI 요소가 클릭되었다면 해당 UI 요소 실행
        // 일단 버튼에 대해서만 구현
        if (uiResults.Count > 0)
        {            
            HandleUIClick(uiResults);
            return;
        }

        // 3. UI 요소가 클릭되지 않은 상태에서 다른 Clickable 요소 처리
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

            DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
            if (diamondMask != null)
            {
                if (diamondMask.IsPointInsideDiamond(Input.mousePosition))
                {
                    Debug.LogWarning("HandleUIClick : 다이아몬드 내부 ");

                    // 마름모 내부 클릭 처리
                    HandleDiamondInteriorClick(result);
                    return;
                }
                else
                {
                    Debug.LogWarning("HandleUIClick : 다이아몬드 외부");

                    // 마름모 외부 클릭 처리
                    OperatorManager.Instance.CancelCurrentAction();
                    return;
                }
            }

            Button button = result.gameObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.Invoke();
                return;
            }
        }

        // 클릭될 요소가 없으면 현재 진행중인 동작을 멈추게 함
        //OperatorManager.Instance.CancelCurrentAction();
    }

    private void HandleDiamondInteriorClick(RaycastResult result)
    {
        // 마름모 내부 클릭 시 내부 동작 유지 - ActionUI나 DeployingUI 상태 유지

    }

    private void HandleObjectClick(RaycastHit hit)
    {
        Debug.LogWarning("HandleObjectClick");
        IClickable clickable = hit.collider.GetComponent<IClickable>();
        if (clickable != null)
        {
            clickable.OnClick();
        }
        else
        {
            Tile clickedTile = hit.collider.GetComponent<Tile>();
            if (clickedTile != null && clickedTile.OccupyingOperator != null)
            {
                clickedTile.OccupyingOperator.ShowActionUI();
            }
            else
            {
                OperatorManager.Instance.CancelCurrentAction();
            }
        }
    }
    
    private void HandleEmptySpaceClick()
    {
        OperatorManager.Instance.CancelCurrentAction();
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

        IClickable clickable = hit.collider.GetComponent<IClickable>();
        if (clickable != null)
        {
            Debug.Log("Hit 오브젝트는 IClickable 인터페이스를 구현했습니다.");
        }
        else
        {
            Debug.Log("Hit 오브젝트는 IClickable 인터페이스를 구현하지 않았습니다.");
        }
    }

}

public interface IClickable
{
    void OnClick();
}