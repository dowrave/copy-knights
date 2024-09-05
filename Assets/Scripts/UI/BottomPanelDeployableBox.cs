using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BottomPanelDeployableBox : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private GameObject deployableIcon;
    private Image deployableIconImage;
    private TextMeshProUGUI costText;
    [SerializeField] private GameObject deployablePrefab;
    private IDeployable deployableInstance;

    // 쿨다운 관련
    private Image cooldownImage;
    private TextMeshProUGUI cooldownText;
    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    private bool isDragging = false; 

    public void Initialize(GameObject prefab)
    {
        deployablePrefab = prefab;
        deployableInstance = deployablePrefab.GetComponent<IDeployable>();

        deployableIcon = transform.Find("DeployableIcon").gameObject;
        deployableIconImage = deployableIcon.GetComponent<Image>();
        costText = transform.Find("CostBackground/CostText").GetComponent<TextMeshProUGUI>();

        // 쿨다운 UI 컴포넌트 찾기 # transform은 자기 자신과 1단계 자식 오브젝트를 검색함
        cooldownImage = transform.Find("CooldownOverlay").GetComponent<Image>();
        cooldownText = transform.Find("CooldownOverlay/CooldownText").GetComponent<TextMeshProUGUI>(); // 더 깊은 자식 오브젝트 찾기

        InitializeVisuals();

        // 코스트 변할 때의 델리게이트에 이벤트 추가
        StageManager.Instance.OnDeploymentCostChanged += UpdateAvailability;
    }

    private void OnDestroy()
    {
        StageManager.Instance.OnDeploymentCostChanged -= UpdateAvailability;
    }

    private void Update()
    {
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                EndCooldown();
            }

            else
            {
                UpdateCooldownVisuals();
            }
        }
    }

    /// <summary>
    /// 일단은 초기화 때만 쓰고 있기는 하다
    /// </summary>
    /// 
    private void InitializeVisuals()
    {
        if (deployableInstance.Icon != null)
        {
            deployableIconImage.sprite = deployableInstance.Icon;
            deployableIconImage.color = Color.white;
        }

        else if (deployablePrefab != null)
        {
            Renderer modelRenderer = deployablePrefab.GetComponentInChildren<Renderer>();
            if (modelRenderer != null && modelRenderer.sharedMaterial != null)
            {
                deployableIconImage.color = modelRenderer.sharedMaterial.color;
            }
        }

        costText.text = deployableInstance.DeploymentCost.ToString();

        cooldownImage.gameObject.SetActive(false);
        cooldownText.gameObject.SetActive(false);
    }
    public void StartCooldown(float cooldownTime)
    {
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
        gameObject.SetActive(true);
        UpdateCooldownVisuals();
    }

    private void UpdateCooldownVisuals()
    {
        cooldownImage.gameObject.SetActive(true);
        cooldownText.gameObject.SetActive(true);
        cooldownImage.fillAmount = cooldownTimer / 70f; // 재배치 시간 70으로 고정 (나중에 수정 필요)
        cooldownText.text = Mathf.Ceil(cooldownTimer).ToString();
    }

    private void EndCooldown()
    {
        isOnCooldown = false;
        cooldownImage.gameObject.SetActive(false);
        cooldownText.gameObject.SetActive(false);
    }

    private void UpdateAvailability()
    {
        bool isAvailable = StageManager.Instance.CurrentDeploymentCost >= deployableInstance.DeploymentCost;
        deployableIconImage.color = isAvailable ? Color.white : new Color(1, 1, 1, 0.3f);
    }

    // 마우스 동작 관련 : 동작하지 않는다면 상속을 확인하라
    // 마우스 버튼을 눌렀다가 같은 위치에서 뗐을 때 발생
    public void OnPointerDown(PointerEventData eventData) 
    {
        if (CanInteract())
        {
            DeployableManager.Instance.StartDeployableSelection(deployablePrefab);
        }
    }

    // 버튼을 누르고 약간의 움직임이 있을 때 발생
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CanInteract())
        {
            isDragging = true;
            DeployableManager.Instance.StartDragging(deployablePrefab);
        }
    }

    // 버튼을 누른 상태에서 이동할 때 프레임마다 발생
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && CanInteract())
        {
            DeployableManager.Instance.HandleDragging(deployablePrefab);
        }
    }

    // 드래그 중 마우스 버튼을 뗄 때 발생 
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            DeployableManager.Instance.EndDragging(deployablePrefab);
            isDragging = false; 
        }
    }

    private bool CanInteract()
    {
        return !isOnCooldown && StageManager.Instance.CurrentDeploymentCost >= deployableInstance.DeploymentCost;
    }
}
