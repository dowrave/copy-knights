using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEditor.Experimental.GraphView.GraphView;

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
        //if (IsPointerOverUIObject())
        //{
        //    Debug.Log("UI ��� Ŭ����");
        //    return;
        //}

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // ������ ���̾�鿡 ���� ����ĳ��Ʈ ����
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
            Debug.Log("����ĳ��Ʈ�� � ������Ʈ���� ���� �ʾҽ��ϴ�.");
        }
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