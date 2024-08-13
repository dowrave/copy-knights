using UnityEngine;
using UnityEngine.EventSystems;

public class ClickDetectionSystem : MonoBehaviour
{
    public static ClickDetectionSystem Instance { get; private set; }

    private Camera mainCamera;

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
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUIObject())
        {
            HandleClick();
            //DebugClick();
        }
    }

    private void HandleClick()
    {
        // UI ��� Ŭ�� üũ
        if (EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("UI ��Ұ� Ŭ���Ǿ����ϴ�.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // ��� ���̾ ���� ����ĳ��Ʈ ����
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, Physics.AllLayers))
        {
            DebugClick(hit);

            // Ŭ�� ���� ó��
            IClickable clickable = hit.collider.GetComponent<IClickable>();
            if (clickable != null)
            {
                clickable.OnClick();
            }
            else
            {
                // Tile�� Operator�� �ִ��� Ȯ��
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

    private bool IsPointerOverUIObject()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}

public interface IClickable
{
    void OnClick();
}