using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeployableBox : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private GameObject boxIcon; // 자식 오브젝트 BoxIcon
    private Image boxIconImage;
    private TextMeshProUGUI costText;
    [SerializeField] private GameObject deployablePrefab;
    private DeployableUnitEntity deployableComponent;

    private Sprite icon;

    // 쿨다운 관련
    private Image InActiveImage;
    private TextMeshProUGUI cooldownText;
    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    private bool isDragging = false;

    [SerializeField] private TextMeshProUGUI remainingCountText;


    public void Initialize(GameObject prefab)
    {
        deployablePrefab = prefab;
        deployableComponent = deployablePrefab.GetComponent<DeployableUnitEntity>(); // 자식 클래스 Operator도 DeployableUnitEntity 형태로 저장
        deployableComponent.InitializeFromPrefab();

        if (deployableComponent is Operator opComponent)
        {
            icon = opComponent.Data.Icon;
        }
        else if (deployableComponent is DeployableUnitEntity)
        {
            icon = deployableComponent.Data.Icon;
        }

        // Operator로 초기화했더라도 deployableComponent 변수 이름으로 DeployableComponent의 모든 기능 사용 가능
        boxIcon = transform.Find("BoxIcon").gameObject;
        boxIconImage = boxIcon.GetComponent<Image>();
        costText = transform.Find("CostBackground/CostText").GetComponent<TextMeshProUGUI>();
        InActiveImage = transform.Find("InActiveOverlay").GetComponent<Image>();
        cooldownText = transform.Find("CooldownText").GetComponent<TextMeshProUGUI>();

        StageManager.Instance.OnDeploymentCostChanged += UpdateAvailability;
        InitializeVisuals();

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
    private void InitializeVisuals()
    {
        // 아이콘이 있다면 사용
        if (icon != null)
        {
            boxIconImage.sprite = icon;
            boxIconImage.color = Color.white; // 원래의 아이콘 그대로 나타내기 위함
        }
        // 아니라면 deployable 모델의 설정을 가져옴
        else if (deployablePrefab != null)
        {
            Renderer modelRenderer = deployablePrefab.GetComponentInChildren<Renderer>();
            if (modelRenderer != null && modelRenderer.sharedMaterial != null)
            {
                boxIconImage.color = modelRenderer.sharedMaterial.color;
            }
        }
        

        if (deployableComponent is Operator opComponent)
        {
            costText.text = opComponent.currentStats.DeploymentCost.ToString();
        }
        else
        {
            costText.text = deployableComponent.currentStats.DeploymentCost.ToString();
        }

        if (CanInteract())
        {
            InActiveImage.gameObject.SetActive(false);
            cooldownText.gameObject.SetActive(false);
        }
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
        InActiveImage.gameObject.SetActive(true);
        cooldownText.gameObject.SetActive(true);
        //InActiveImage.fillAmount = cooldownTimer / 70f; // 재배치 시간 70으로 고정 (나중에 수정 필요)
        cooldownText.text = Mathf.Ceil(cooldownTimer).ToString();
    }

    private void EndCooldown()
    {
        isOnCooldown = false;
        InActiveImage.gameObject.SetActive(false);
        cooldownText.gameObject.SetActive(false);
    }

    private void UpdateAvailability()
    {
        if (CanInteract())
        {
            InActiveImage.gameObject.SetActive(false); // 박스 흐릿하게
        }
        else
        {
            InActiveImage.gameObject.SetActive(true); // 흐릿한 박스 제거
        }
    }

    // 이 스크립트는 각 Box를 구현한 거임 
    // 즉, 하단 패널 박스를 누르면 그 박스에 할당된 오퍼레이터 프리팹에 관한 로직들이 작동하기 시작함 

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
        return !isOnCooldown && StageManager.Instance.CurrentDeploymentCost >= deployableComponent.currentStats.DeploymentCost;
    }

    public void UpdateRemainingCount(int count)
    {
        if (remainingCountText != null) 
        {
            remainingCountText.text = count.ToString();
            remainingCountText.gameObject.SetActive(count > 0);
        }
    }

}
