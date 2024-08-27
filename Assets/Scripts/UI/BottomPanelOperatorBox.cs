using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BottomPanelOperatorBox : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform operatorIcon; // 오브젝트
    private Image operatorIconImage; // Image 컴포넌트
    private Color originalIconColor;
    private TextMeshProUGUI costText;
    [SerializeField] private OperatorData operatorData;

    // 쿨다운 관련
    private Image cooldownImage;
    private TextMeshProUGUI cooldownText;
    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    private bool isDragging = false; 

    public void Initialize(OperatorData data)
    {
        operatorData = data;
        operatorIcon = transform.Find("OperatorIcon");
        operatorIconImage = operatorIcon.GetComponentInChildren<Image>(); // OperatorIcon의 Image 컴포넌트에 접근
        costText = transform.Find("CostBackground/CostText").GetComponent<TextMeshProUGUI>(); // CostText의 TextMeshPro에 접근

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
    private void InitializeVisuals()
    {
        // operatorData에 해당하는 아이콘이 있다면 그걸 넣고 
        if (operatorData.icon != null)
        {
            operatorIconImage.sprite = operatorData.icon;
            operatorIconImage.color = new Color(1f, 1f, 1f, 1f); // 일러스트에 변화를 주지 않고 그대로 넣음
        }

        // 없다면 operatorData에 해당하는 색깔만 가져와서 할당한다.
        else if (operatorData.prefab != null)
        {
            Renderer modelRenderer = operatorData.prefab.GetComponentInChildren<Renderer>();
            if (modelRenderer != null && modelRenderer.sharedMaterial != null)
            {   
                originalIconColor = modelRenderer.sharedMaterial.color;
                operatorIconImage.color = originalIconColor;
            }
        }

        costText.text = operatorData.deploymentCost.ToString();
        //UpdateAvailability();

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
        cooldownImage.fillAmount = cooldownTimer / operatorData.reDeployTime;
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
        bool isAvailable = StageManager.Instance.CurrentDeploymentCost >= operatorData.deploymentCost;
        Color iconColor = isAvailable ? originalIconColor : new Color(originalIconColor.r, originalIconColor.g, originalIconColor.b, 0.3f);
        operatorIconImage.color = iconColor;
        //costText.color = isAvailable ? Color.white : Color.gray;
    }

    // 마우스 동작 관련 : 동작하지 않는다면 상속을 확인하라
    // 마우스 버튼을 눌렀다가 같은 위치에서 뗐을 때 발생
    public void OnPointerDown(PointerEventData eventData) 
    {
        if (CanInteract())
        {
            OperatorManager.Instance.StartOperatorSelection(operatorData);
        }
    }

    // 버튼을 누르고 약간의 움직임이 있을 때 발생
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CanInteract())
        {
            isDragging = true; 
            OperatorManager.Instance.StartDragging(operatorData);
        }
    }

    // 버튼을 누른 상태에서 이동할 때 프레임마다 발생
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && CanInteract())
        {
            OperatorManager.Instance.HandleDragging(operatorData);
        }
    }

    // 드래그 중 마우스 버튼을 뗄 때 발생 
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            OperatorManager.Instance.EndDragging(operatorData);
            isDragging = false; 
        }
    }

    private bool CanInteract()
    {
        return !isOnCooldown && StageManager.Instance.CurrentDeploymentCost >= operatorData.deploymentCost;
    }
}
