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
        if (EventSystem.current.IsPointerOverGameObject())
        {
            #if UNITY_EDITOR
            Logger.Log("EventSystem�� UI Ŭ���� ó���߽��ϴ�. ������Ʈ Ŭ�� ó���� �����մϴ�.");
            #endif
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            // UI�� Ŭ������ �ʾ��� ���� ���� (��: ProcessClickMapObject())
            ProcessClickMapObject();
        }
    }

    public void OnButtonClicked()
    {
        buttonClickedThisFrame = true;
        shouldSkipHandleClick = true; // ��� HandleClick�� ȣ��Ǵ� ���� ����
    }

    private void ProcessClickMapObject()
    {
        // 1. ��ġ �� �巡�� Ȥ�� ���� ���� ���¶�� Ŭ�� ó�� �ߴ�
        // ��ġ ��Ȳ�� DeployableManager���� ó���Ѵ�. ���⼭�� �ٸ� ������Ʈ�� Ŭ�� ������ ���� ���� ���ܵ�.
        if (DeploymentInputHandler.Instance!.CurrentState == DeploymentInputHandler.InputState.SelectingDirection)
        {
            Logger.Log("HandleClick : ��ġ �� �巡�� Ȥ�� ���� ���� ���� - Ŭ�� ó�� �ߴ�");
            return;
        }

        // 2. 3D ������Ʈ Ŭ�� ó��
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

    private void HandleObjectClick(RaycastHit[] hits)
    {
        // ������ : ��� ������Ʈ ���
        foreach (var hit in hits)
        {
            Logger.Log($"RaycastAll hit: {hit.collider.name}");
        }

        // 1. DeployableUnitEntity�� ���� ã��
        foreach (var hit in hits.OrderBy(h => h.distance)) // ī�޶󿡼� ����� ����
        {
            DeployableUnitEntity? clickable = hit.collider.GetComponentInParent<DeployableUnitEntity>();
            if (clickable != null && !DeploymentInputHandler.Instance!.IsClickingPrevented)
            {
                clickable.OnClick();
                Logger.Log("DeployableUnitEntity �ݶ��̴� ���� �� Ŭ����");
                return;
            }
        }

        // 2. Ÿ�Ͽ� ��ġ�� ������ �ִ��� Ȯ��
        foreach (var hit in hits.OrderBy(h => h.distance))
        {
            Tile? clickedTile = hit.collider.GetComponent<Tile>();
            if (clickedTile != null && clickedTile.OccupyingDeployable != null)
            {
                clickedTile.OccupyingDeployable.OnClick();
                Logger.Log("Ÿ���� Ŭ���Ǿ �� ���� Deployable�� Ŭ����");
                return;
            }
        }

        // 3. Ÿ���� �ִٸ� �� Ÿ���� Ŭ���� ������ ����
        if (hits.Any(h => h.collider.GetComponent<Tile>() != null))
        {
            DeployableManager.Instance!.CancelCurrentAction();
            Logger.Log("�� Ÿ���� Ŭ����");
            return;
        }

        // 4. ���� ���ǿ� �� �ش����� ������ �� ���� Ŭ������ ó��
        HandleEmptySpaceClick();
    }
    
    private void HandleEmptySpaceClick()
    {
        Logger.Log("�� ���� Ŭ�� - CancelCurrentAction ����");
        DeployableManager.Instance!.CancelCurrentAction();
    }

    /// <summary>
    /// ���콺 ������ Ŭ�� �� ��� ��� UI ����ĳ��Ʈ ����� ��ȯ��
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

    // Ŭ���� ������Ʈ�κ��� ���� ������Ʈ�� DeployableUnitEntity�� �ִ��� �˻���
    // private DeployableUnitEntity? GetAssociatedDeployableUnitEntity(GameObject clickedObject)
    // {
    //     Logger.Log($"GetAssociatedDeployableUnitEntity ����, clickedObject : {clickedObject.name}");
    //     Transform? current = clickedObject.transform;
    //     while (current != null)
    //     {
    //         DeployableUnitEntity deployable = current.GetComponent<DeployableUnitEntity>();
    //         if (deployable != null)
    //         {
    //             return deployable;
    //         }
    //         current = current.parent;
    //     }

    //     return null;
    // }
}