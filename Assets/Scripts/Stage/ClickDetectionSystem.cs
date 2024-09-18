using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ClickDetectionSystem : MonoBehaviour
{
    public static ClickDetectionSystem Instance { get; private set; }

    private Camera mainCamera;
    [SerializeField] private LayerMask clickableLayerMask;  // Inspector���� ����

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

            // ButtonDown ���� 1. ���̾Ƹ�� ���� Ŭ�� �� ���� ����
            DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
            if (diamondMask != null)
            {
                if (diamondMask.IsPointInsideDiamond(Input.mousePosition))
                {
                    Debug.LogWarning("HandleUIClick : ���̾Ƹ�� ���� ");
                    DeployableManager.Instance.IsMousePressed = true;
                    return;
                }
            }

            // ButtonDown ���� 2. ���۷����� �ڽ� �巡�� ���� ����
            BottomPanelDeployableBox deployableBox = result.gameObject.GetComponent<BottomPanelDeployableBox>();
            if (deployableBox != null)
            {
                deployableBox.OnPointerDown(pointerData);
                return;
            }
        }
    }

    /// <summary>
    /// ��ü���� Ŭ�� ������ �����
    /// 1. Ŭ���� ������ UI ���(GrpahicRayCaster�� �ִ� Canvas)�� �ִٸ� ���� ����
    /// 2. ���ٸ� 3D ������Ʈ�� IClickable�� ��� �ش� Ŭ���� ���� ����
    /// </summary>
    private void HandleClick()
    {
        List<RaycastResult> results = PerformScreenRaycast();
        List<RaycastResult> uiResults = results.Where(r => r.module is GraphicRaycaster).ToList();

        // UI ��Ұ� Ŭ���Ǿ��ٸ� �ش� UI ��� ����
        if (uiResults.Count > 0)
        {            
            HandleUIClick(uiResults);
            return;
        }

        // UI ��Ұ� Ŭ������ ���� ���¿��� �ٸ� Clickable ��� ó��
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

            // 1. ���̾Ƹ�� �ܺ� Ŭ�� �� ���� ����
            DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
            if (diamondMask != null)
            {
                if (!diamondMask.IsPointInsideDiamond(Input.mousePosition))
                {
                    Debug.LogWarning("HandleUIClick : ���̾Ƹ�� �ܺ� Ŭ��");

                    // ������ �ܺ� Ŭ�� ó��
                    DeployableManager.Instance.CancelCurrentAction();
                    return;
                }
            }

            // Button ���� ������ ���� ���� ���ص� �Ǵ� �� - �̰� ������ �ߺ������
            // 1. ����Ƽ�� UI �ý����� OnClick�� ������� �ʴ��� �ڵ����� �����Ų�ٰ� ��
            // 2. ���⼭ ������ MouseButtonUp�� true�� �ż� HandleClick�� ������
        }
    }

    private void HandleDiamondInteriorClick(RaycastResult result)
    {
        // ������ ���� Ŭ�� �� ���� ���� ���� - ActionUI�� DeployingUI ���� ����

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
                // Operator�� �ƴ� ������ �� ��ư�� ��Ÿ���� �� 

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
    /// Ŭ�� ������ - �ذ��
    /// </summary>
    private void DebugClick(RaycastHit hit)
    {
        Debug.Log($"Hit ������Ʈ: {hit.collider.gameObject.name}");
        Debug.Log($"Hit ��ġ: {hit.point}");
        Debug.Log($"Hit ������Ʈ �±�: {hit.collider.gameObject.tag}");
        Debug.Log($"Hit ������Ʈ ���̾�: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

        Component[] components = hit.collider.gameObject.GetComponents<Component>();
        Debug.Log("Hit ������Ʈ�� ������Ʈ ���:");
        foreach (Component component in components)
        {
            Debug.Log($"- {component.GetType().Name}");
        }

        IDeployable deployable = hit.collider.GetComponent<IDeployable>();
        if (deployable != null)
        {
            Debug.Log("Hit ������Ʈ�� IDeployable �������̽��� �����߽��ϴ�.");
        }
        else
        {
            Debug.Log("Hit ������Ʈ�� IDeployable �������̽��� �������� �ʾҽ��ϴ�.");
        }
    }

    /// <summary>
    /// ���콺 ������ Ŭ�� �� ��� ��� ����ĳ��Ʈ ����� ��ȯ��
    /// </summary>
    private List<RaycastResult> PerformScreenRaycast()
    {
        // UI ��Ҹ� Ŭ���ߴ��� ����
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results;
    }

}
