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

    // ��ٿ� ����
    private Image InActiveImage;
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

        InActiveImage = transform.Find("InActiveOverlay").GetComponent<Image>();
        cooldownText = transform.Find("CooldownText").GetComponent<TextMeshProUGUI>(); // �� ���� �ڽ� ������Ʈ ã��

        // �ڽ�Ʈ ���� ���� ��������Ʈ�� �̺�Ʈ �߰�
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
    /// 
    private void InitializeVisuals()
    {
        // deployable ��ü�� �Ҵ�� �������� �ִٸ� DeployableIcon ������Ʈ�� �Ҵ�
        if (deployableInstance.Icon != null)
        {
            deployableIconImage.sprite = deployableInstance.Icon;
            deployableIconImage.color = Color.white; // ������ ������ �״�� ��Ÿ���� ����
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

        InActiveImage.gameObject.SetActive(false);
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
        return !isOnCooldown && StageManager.Instance.CurrentDeploymentCost >= deployableInstance.DeploymentCost;
    }

}
