using System;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEditor.Experimental.GraphView.GraphView;

public class ClickDetectionSystem : MonoBehaviour
{
    public static ClickDetectionSystem Instance { get; private set; }

    private Camera mainCamera;
    [SerializeField] private LayerMask clickableLayerMask;  // Inspector���� ����
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

    // ��ü���� Ŭ�� ������ �����
    private void HandleClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // ī�޶� -> ���콺 ��ġ�� ���ϴ� ���� ����
        RaycastHit hit; // ����ĳ��Ʈ ��� ���� ����

        // OperatorUI���� �浹 �˻�
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, operatorUILayerMask))
        {
            // OperatorUI�� Ŭ���� ���, ���⼭ ó���ϰ� return
            Debug.Log("OperatorUI clicked");
            return;
        }

        // Ȱ��ȭ�� OperatorActionUI�� �ְ�, �� ���� ������ Ŭ���� ���
        if (OperatorManager.Instance.CurrentActiveActionUI != null)
        {
            OperatorManager.Instance.HideAllActionUIs();
            return;
        }



        // ������ ���̾�鿡 ���� ����ĳ��Ʈ ����
        // - ray : �������� ������ ���� ����
        // - out hit : ����ĳ��Ʈ ��� ���� ����
        // - Mathf.Infinity : ����ĳ��Ʈ �ִ� �Ÿ�
        // - ClickableLayerMask.value : �˻��� ���̾� ����ũ
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickableLayerMask.value)) 
        {

            // Ŭ�� ���� ó��
            IClickable clickable = hit.collider.GetComponent<IClickable>();

            if (clickable != null)
            {
                clickable.OnClick();
            }
            else
            {
                // Tile�� Operator�� �ִ��� Ȯ�� <-- ��ġ�� Operator�� ũ�Ⱑ Ÿ���� �����ؼ� �� �̷��� ���� �ʾƵ� �Ǳ�� ��
                Tile clickedTile = hit.collider.GetComponent<Tile>();
                if (clickedTile != null && clickedTile.OccupyingOperator != null)
                {
                    clickedTile.OccupyingOperator.ShowActionUI();
                }
            }
        }

        else
        {
            //Debug.Log("����ĳ��Ʈ�� � ������Ʈ���� ���� �ʾҽ��ϴ�.");
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

    //    // GraphicRaycaster ������Ʈ�� �ִ� ��� Canvas ���� UI ��ҿ� ���� ����ĳ��Ʈ ����
    //    EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

    //    int operatorLayer = LayerMask.NameToLayer("Operator");
    //    int uiLayer = LayerMask.NameToLayer("UI");

    //    foreach (RaycastResult result in results)
    //    {
    //        string layerName = LayerMask.LayerToName(result.gameObject.layer);
    //        Debug.Log("Hit layer: " + layerName);

    //        // UI ���̾ �ִ��� Ȯ��
    //        if (result.gameObject.layer == uiLayer)
    //        {
    //            Debug.Log("UI clicked");
    //            return true;
    //        }
    //    }

    //    // UI ���̾ �ƴ� ��� false ��ȯ
    //    return false;
    //}

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

        Debug.Log("------------------------");
    }

    private void LogCameraCullingMask()
    {
        if (mainCamera != null)
        {
            int cullingMask = mainCamera.cullingMask;
            Debug.Log($"Camera Culling Mask: {cullingMask}");

            // Ȱ��ȭ�� ���̾� �̸� ���
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