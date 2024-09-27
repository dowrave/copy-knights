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

    private Sprite icon;

    // ��ٿ� ����
    private Image InActiveImage;
    private TextMeshProUGUI cooldownText;
    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    private bool isDragging = false;

    [SerializeField] private TextMeshProUGUI remainingCountText;


    public void Initialize(GameObject prefab)
    {
        deployablePrefab = prefab;
        deployableComponent = deployablePrefab.GetComponent<DeployableUnitEntity>(); // �ڽ� Ŭ���� Operator�� DeployableUnitEntity ���·� ����
        deployableComponent.InitializeFromPrefab();

        if (deployableComponent is Operator opComponent)
        {
            icon = opComponent.Data.Icon;
        }
        else if (deployableComponent is DeployableUnitEntity)
        {
            icon = deployableComponent.Data.Icon;
        }

        // Operator�� �ʱ�ȭ�ߴ��� deployableComponent ���� �̸����� DeployableComponent�� ��� ��� ��� ����
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
        //InActiveImage.fillAmount = cooldownTimer / 70f; // ���ġ �ð� 70���� ���� (���߿� ���� �ʿ�)
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
            InActiveImage.gameObject.SetActive(false); // �ڽ� �帴�ϰ�
        }
        else
        {
            InActiveImage.gameObject.SetActive(true); // �帴�� �ڽ� ����
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
