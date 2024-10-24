using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeployableBox : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private GameObject boxIcon; // �ڽ� ������Ʈ BoxIcon
    private Image boxIconImage;
    private TextMeshProUGUI costText;
    [SerializeField] private GameObject deployablePrefab;
    private DeployableUnitEntity deployableComponent;
    private DeployableManager.DeployableInfo deployableInfo; 

    private Sprite icon;

    // ��ٿ� ����
    private Image inActiveImage;
    private TextMeshProUGUI cooldownText;
    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    // ���� ����
    private TextMeshProUGUI remainingCountText;

    private bool isDragging = false;

    // ��ġ �ڽ�Ʈ ����
    private int baseDeploymentCost;
    private int currentDeploymentCost;
    private int deployCount = 0;
    private const int MAX_COST_INCREASE = 2; // �ִ� �ڽ�Ʈ ���� Ƚ��
    private const float COST_INCREASE_RATE = 0.5f; // �ڽ�Ʈ ������

    public void Initialize(GameObject prefab)
    {
        deployablePrefab = prefab;
        deployableComponent = deployablePrefab.GetComponent<DeployableUnitEntity>(); // �ڽ� Ŭ���� Operator�� DeployableUnitEntity ���·� ����
        deployableInfo = DeployableManager.Instance.GetDeployableInfo(prefab);

        deployableComponent.InitializeFromPrefab();

        if (deployableComponent is Operator opComponent)
        {
            icon = opComponent.Data.Icon;
        }
        else if (deployableComponent is DeployableUnitEntity)
        {
            icon = deployableComponent.Data.Icon;
        }

        // �ʱ� �ڽ�Ʈ ����
        if (deployableComponent is Operator op)
        {
            baseDeploymentCost = op.Data.stats.DeploymentCost;
        }
        else
        {
            baseDeploymentCost = deployableComponent.Data.stats.DeploymentCost; 
        }
        currentDeploymentCost = baseDeploymentCost;
        deployCount = 0;

        // Operator�� �ʱ�ȭ�ߴ��� deployableComponent ���� �̸����� DeployableComponent�� ��� ��� ��� ����
        boxIcon = transform.Find("BoxIcon").gameObject;
        boxIconImage = boxIcon.GetComponent<Image>();
        costText = transform.Find("CostBackground/CostText").GetComponent<TextMeshProUGUI>();
        inActiveImage = transform.Find("InActiveOverlay").GetComponent<Image>();
        cooldownText = transform.Find("CooldownText").GetComponent<TextMeshProUGUI>();
        remainingCountText = transform.Find("RemainingCountText").GetComponent<TextMeshProUGUI>();

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
    /// �ϴ��� �ʱ�ȭ ���� ���� �ֱ�� �ϴ�
    /// </summary>
    private void InitializeVisuals()
    {
        // �������� �ִٸ� ���
        if (icon != null)
        {
            boxIconImage.sprite = icon;
            boxIconImage.color = Color.white; // ������ ������ �״�� ��Ÿ���� ����
        }
        // �ƴ϶�� deployable ���� ������ ������
        else if (deployablePrefab != null)
        {
            Renderer modelRenderer = deployablePrefab.GetComponentInChildren<Renderer>();
            if (modelRenderer != null && modelRenderer.sharedMaterial != null)
            {
                boxIconImage.color = modelRenderer.sharedMaterial.color;
            }
        }

        // �ڽ�Ʈ ���� �������� ��ġ�� �޶� �ۼ�
        if (deployableComponent is Operator opComponent)
        {
            costText.text = opComponent.currentStats.DeploymentCost.ToString();
        }
        else
        {
            costText.text = deployableComponent.currentStats.DeploymentCost.ToString();
        }

        // 1���� ��ġ ������ ��Ҵ� ���� �ϴ��� ���� ǥ�ø� ��Ȱ��ȭ��
        if (deployableInfo.maxDeployCount <= 1)
        {
            remainingCountText.gameObject.SetActive(false);
        }

        if (!isOnCooldown)
        {
            cooldownText.gameObject.SetActive(false);
        }

        if (StageManager.Instance.CurrentDeploymentCost >= currentDeploymentCost)
        {
            inActiveImage.gameObject.SetActive(false);
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
        inActiveImage.gameObject.SetActive(true);
        cooldownText.gameObject.SetActive(true);
        //InActiveImage.fillAmount = cooldownTimer / 70f; // ���ġ �ð� 70���� ���� (���߿� ���� �ʿ�)
        cooldownText.text = Mathf.Ceil(cooldownTimer).ToString();
    }

    private void EndCooldown()
    {
        isOnCooldown = false;
        inActiveImage.gameObject.SetActive(false);
        cooldownText.gameObject.SetActive(false);
    }

    private void UpdateAvailability()
    {
        if (CanInteract())
        {
            inActiveImage.gameObject.SetActive(false); // �ڽ� �帴�ϰ�
        }
        else
        {
            inActiveImage.gameObject.SetActive(true); // �帴�� �ڽ� ����
        }
    }

    // �� ��ũ��Ʈ�� �� Box�� ������ ���� 
    // ��, �ϴ� �г� �ڽ��� ������ �� �ڽ��� �Ҵ�� ���۷����� �����տ� ���� �������� �۵��ϱ� ������ 

    // ���콺 ���� ���� : �������� �ʴ´ٸ� ����� Ȯ���϶�
    // ���콺 ��ư�� �����ٰ� ���� ��ġ���� ���� �� �߻�
    public void OnPointerDown(PointerEventData eventData)
    {
        if (CanInteract())
        {
            DeployableManager.Instance.StartDeployableSelection(deployablePrefab);
        }
    }

    // ��ư�� ������ �ణ�� �������� ���� �� �߻�
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CanInteract())
        {
            isDragging = true;
            DeployableManager.Instance.StartDragging(deployablePrefab);
        }
    }

    // ��ư�� ���� ���¿��� �̵��� �� �����Ӹ��� �߻�
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && CanInteract())
        {
            DeployableManager.Instance.HandleDragging(deployablePrefab);
        }
    }

    // �巡�� �� ���콺 ��ư�� �� �� �߻� 
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
        return !isOnCooldown && StageManager.Instance.CurrentDeploymentCost >= currentDeploymentCost;
    }

    public void UpdateRemainingCount(int count)
    {
        if (remainingCountText != null)
        {
            if (count > 0)
            {
                remainingCountText.text = $"X{count}";
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 2ȸ���� ��ġ �ڽ�Ʈ�� ������Ʈ��
    /// </summary>
    private void UpdateDeploymentCost()
    {
        float multiplier = 1f + (Mathf.Min(deployCount, MAX_COST_INCREASE - 1) * COST_INCREASE_RATE);
        currentDeploymentCost = Mathf.RoundToInt(baseDeploymentCost * multiplier);
    }

    public void OnOperatorReturn()
    {
        deployCount++;
        UpdateDeploymentCost();

        // �ڽ�Ʈ UI ������Ʈ
        costText.text = currentDeploymentCost.ToString("F0");
    }

    public int GetCurrentDeploymentCost()
    {
        return currentDeploymentCost;
    }
}
