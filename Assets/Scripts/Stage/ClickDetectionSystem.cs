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
    /// ��ü���� Ŭ�� ������ �����
    /// 1. Ŭ���� ������ UI ���(GrpahicRayCaster�� �ִ� Canvas)�� �ִٸ� ���� ����
    /// 2. ���ٸ� 3D ������Ʈ�� IClickable�� ��� �ش� Ŭ���� ���� ����
    /// </summary>
    private void HandleClick()
    {
        // 1. UI ��Ҹ� Ŭ���ߴ��� ����
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        List<RaycastResult> allResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, allResults);

        // UI ��Ҹ� ���͸�(GraphicRaycaster�� ���� �ִ� �͵鸸)
        List<RaycastResult> uiResults = allResults.Where(r => r.module is GraphicRaycaster).ToList();

        // 2. UI ��Ұ� Ŭ���Ǿ��ٸ� �ش� UI ��� ����
        // �ϴ� ��ư�� ���ؼ��� ����
        if (uiResults.Count > 0)
        {            
            HandleUIClick(uiResults);
            return;
        }

        // 3. UI ��Ұ� Ŭ������ ���� ���¿��� �ٸ� Clickable ��� ó��
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
                    Debug.LogWarning("HandleUIClick : ���̾Ƹ�� ���� ");

                    // ������ ���� Ŭ�� ó��
                    HandleDiamondInteriorClick(result);
                    return;
                }
                else
                {
                    Debug.LogWarning("HandleUIClick : ���̾Ƹ�� �ܺ�");

                    // ������ �ܺ� Ŭ�� ó��
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

        // Ŭ���� ��Ұ� ������ ���� �������� ������ ���߰� ��
        //OperatorManager.Instance.CancelCurrentAction();
    }

    private void HandleDiamondInteriorClick(RaycastResult result)
    {
        // ������ ���� Ŭ�� �� ���� ���� ���� - ActionUI�� DeployingUI ���� ����

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

        IClickable clickable = hit.collider.GetComponent<IClickable>();
        if (clickable != null)
        {
            Debug.Log("Hit ������Ʈ�� IClickable �������̽��� �����߽��ϴ�.");
        }
        else
        {
            Debug.Log("Hit ������Ʈ�� IClickable �������̽��� �������� �ʾҽ��ϴ�.");
        }
    }

}

public interface IClickable
{
    void OnClick();
}