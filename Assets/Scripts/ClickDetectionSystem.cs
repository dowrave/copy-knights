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
        // UI 요소 클릭 체크
        if (EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("UI 요소가 클릭되었습니다.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 모든 레이어에 대해 레이캐스트 수행
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, Physics.AllLayers))
        {
            DebugClick(hit);

            // 클릭 로직 처리
            IClickable clickable = hit.collider.GetComponent<IClickable>();
            if (clickable != null)
            {
                clickable.OnClick();
            }
            else
            {
                // Tile에 Operator가 있는지 확인
                Tile clickedTile = hit.collider.GetComponent<Tile>();
                if (clickedTile != null && clickedTile.OccupyingOperator != null)
                {
                    clickedTile.OccupyingOperator.ShowActionUI();
                }
            }
        }
        else
        {
            Debug.Log("레이캐스트가 어떤 오브젝트에도 닿지 않았습니다.");
        }
    }

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