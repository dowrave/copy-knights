using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickDetectionSystem : MonoBehaviour
{
    public static ClickDetectionSystem? Instance { get; private set; }

    private Camera mainCamera = default!;
    [SerializeField] private LayerMask clickableLayerMask = default!;  // Inspector���� ����

    //private bool isDraggingDiamond = false;
    //private DiamondMask currentDiamondMask;

    private bool isTutorialMode = false;
    private string expectedButtonName = string.Empty;

    // �̹� ����� UI�� �ִ� ���, �� ��ũ��Ʈ�� �������� �ʾƵ� �ǰ� ��
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
            shouldSkipHandleClick = false; // �� ������ �ʱ�ȭ
        }
        if (Input.GetMouseButtonUp(0))
        {
            // UI Ŭ���� ������ ������ HandleClick ����
            if (!shouldSkipHandleClick)
            {
                HandleClick();
            }

            // ���� �������� ���� �ʱ�ȭ
            buttonClickedThisFrame = false;
            shouldSkipHandleClick = false;
        }
    }

    public void OnButtonClicked()
    {
        buttonClickedThisFrame = true;
        shouldSkipHandleClick = true; // ��� HandleClick�� ȣ��Ǵ� ���� ����
    }

    private void HandleMouseDown()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        List<RaycastResult> results = PerformScreenRaycast();
        foreach (var result in results)
        {
            //Debug.Log($"MouseButtonDown Raycast hit: {result.gameObject.name}");

            // ButtonDown ���� 1. ���̾Ƹ�� ���� Ŭ�� �� ���� ����
            DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
            if (diamondMask != null)
            {
                if (diamondMask.IsPointInsideDiamond(Input.mousePosition))
                {
                    //Debug.LogWarning("HandleUIClick : ���̾Ƹ�� ���� ");
                    DeployableManager.Instance!.IsMousePressed = true;
                    return;
                }
            }

            // ButtonDown ���� 2. ���۷����� �ڽ� �巡�� ���� ����
            DeployableBox deployableBox = result.gameObject.GetComponent<DeployableBox>();
            if (deployableBox != null)
            {
                deployableBox.OnPointerDown(pointerData);
                return;
            }
        }
    }



    // ** Ŭ���� ������ UI ���(GrpahicRayCaster�� �ִ� Canvas)�� �ִٸ� ���� ������(���⼭�� ó���� �ƴ�) **
    // ������ Ŭ�� ������ �����
 
    private void HandleClick()
    {
        List<RaycastResult> results = PerformScreenRaycast();

        foreach (RaycastResult result in results)
        {
            //Debug.Log($"Raycast Hit: {result.gameObject.name} (Layer: {result.gameObject.layer})");
        }

        bool isClickHandled = ProcessClickPriority(results);
        if (isClickHandled) return;
    }

    private bool ProcessClickPriority(List<RaycastResult> results)
    {
        // 1. UI ��� ó��: GraphicRaycaster ����� �ִ� ����� ���͸�
        var uiResults = results.Where(r => r.module is GraphicRaycaster).ToList();
        if (uiResults.Count > 0 && HandleUIClick(uiResults))
        {
            // UI ��Ұ� ó���Ǿ��ٸ� �� �̻� �������� ����
            return true;
        }

        // 2. ��ġ �� �巡�� Ȥ�� ���� ���� ���¶�� Ŭ�� ó�� �ߴ�
        if (DeployableManager.Instance!.IsSelectingDirection ||
            DeployableManager.Instance!.IsDraggingDeployable)
        {
            return true;
        }

        // 3. 3D ������Ʈ Ŭ�� ó��: 
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit clickableHit, Mathf.Infinity, clickableLayerMask))
        {
            HandleObjectClick(clickableHit);
        }
        else
        {
            HandleEmptySpaceClick();
        }

        return true;
    }

    private bool HandleUIClick(List<RaycastResult> uiResults)
    {
        foreach (var result in uiResults)
        {
            //Debug.Log($"ui Ž��: {result.gameObject.name}");

            // 1. ���̾Ƹ�� �ܺ� Ŭ�� �� ���� ����
            DiamondMask diamondMask = result.gameObject.GetComponent<DiamondMask>();
            if (diamondMask != null)
            {
                if (!diamondMask.IsPointInsideDiamond(Input.mousePosition))
                {
                    Debug.Log("HandleUIClick : ���̾Ƹ�� �ܺ� Ŭ��");

                    // ������ �ܺ� Ŭ�� ó��
                    DeployableManager.Instance!.CancelCurrentAction();
                    return true;
                }
            }

            // 2. OperatorUI ���� ��� Ŭ�� ó�� - Deployable.OnClick�� �����ϵ��� ����
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
        // ������ ���� Ŭ�� �� ���� ���� ���� - ActionUI�� DeployingUI ���� ����

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
                    // Operator�� �ƴ� ������ �� ��ư�� ��Ÿ���� �� 
                }
                else
                {
                    // clickedTile�� null�� ���� ���� �׼� ���
                    Debug.Log("Ŭ���� ��ġ ��� ���� - CancelCurrentAction ����");

                    DeployableManager.Instance!.CancelCurrentAction();
                }
            }
        }
    }
    
    private void HandleEmptySpaceClick()
    {
        Debug.Log("�� ���� Ŭ�� - CancelCurrentAction ����");
        DeployableManager.Instance!.CancelCurrentAction();
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

    /// <summary>
    /// Ŭ���� ������Ʈ�κ��� ���� ������Ʈ�� DeployableUnitEntity�� �ִ��� �˻���
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
