using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeployableBox : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image operatorIllustImage;
    [SerializeField] private GameObject operatorClassIconBox; // Ŭ���� �������� �θ� ������Ʈ
    [SerializeField] private Image operatorClassIconImage; // ������ ��ü �Ҵ�
    [SerializeField] private Image inActiveImage;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI countText;

    private Sprite boxIcon;
    private GameObject deployablePrefab;
    private DeployableUnitEntity deployableComponent;
    private DeployableManager.DeployableInfo deployableInfo;
    private DeployableGameState deployableGameState;

    // ���� ����
    private bool isDragging = false;
     private int currentDeploymentCost;

    public void Initialize(DeployableManager.DeployableInfo info)
    {
        deployableInfo = info;
        deployablePrefab = info.prefab;
        deployableComponent = deployablePrefab.GetComponent<DeployableUnitEntity>(); // ������ Ȱ��
        deployableGameState = DeployableManager.Instance.GameStates[deployableInfo];
        //deployableComponent.InitializeFromPrefab(); // �������� �⺻ ���� �ʱ�ȭ

        if (deployableComponent is Operator)
        {
            OwnedOperator op = deployableInfo.ownedOperator;
            currentDeploymentCost = op.BaseData.stats.DeploymentCost; // �ʱ� ��ġ �ڽ�Ʈ ����
            OperatorIconHelper.SetClassIcon(operatorClassIconImage, op.BaseData.operatorClass); // Ŭ���� ������ ����
            boxIcon = op.BaseData.Icon;
        }
        else
        {
            currentDeploymentCost = deployableInfo.deployableUnitData.stats.DeploymentCost;
            operatorClassIconBox.gameObject.SetActive(false);
            boxIcon = deployableInfo.deployableUnitData.Icon;
        }

        StageManager.Instance.OnDeploymentCostChanged += UpdateAvailability;
        InitializeVisuals();
    }

    public void UpdateDisplay(DeployableGameState gameState)
    {
        costText.text = gameState.CurrentDeploymentCost.ToString();

        // ���۷����Ͱ� �ƴ϶�� ��ġ ���� Ƚ�� ǥ��
        if (!gameState.IsOperator)
        {
            countText.gameObject.SetActive(true);
            countText.text = $"x{gameState.RemainingDeployCount}";
        }
        else
        {
            countText.gameObject.SetActive(false);
        }

        // ��Ÿ�� ���� ǥ��
        if (gameState.IsOnCooldown)
        {
            inActiveImage.gameObject.SetActive(true);
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = Mathf.Ceil(gameState.CooldownTimer).ToString();
        }
        else
        {
            inActiveImage.gameObject.SetActive(false);
            cooldownText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        StageManager.Instance.OnDeploymentCostChanged -= UpdateAvailability;
    }

    private void Update()
    {
        if (deployableGameState.IsOnCooldown)
        {
            deployableGameState.UpdateCooldown();
            UpdateDisplay(deployableGameState);
        }
    }

    /// <summary>
    /// �ϴ��� �ʱ�ȭ ���� ���� �ֱ�� �ϴ�
    /// </summary>
    private void InitializeVisuals()
    {
        // �Ϸ���Ʈ�� �ִٸ� �ڽ� ���������� ���
        if (boxIcon != null)
        {
            operatorIllustImage.sprite = boxIcon;
            operatorIllustImage.color = Color.white; // ������ ������ �״�� ��Ÿ���� ����
        }
        // �ƴ϶�� deployable ���� ������ ������
        else if (deployablePrefab != null)
        {
            Renderer modelRenderer = deployablePrefab.GetComponentInChildren<Renderer>();
            if (modelRenderer != null && modelRenderer.sharedMaterial != null)
            {
                operatorIllustImage.color = modelRenderer.sharedMaterial.color;
            }
        }

        UpdateDisplay(deployableGameState);
        UpdateAvailability();
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

    // ���콺 ���� ���� : �������� �ʴ´ٸ� ����� Ȯ���϶�
    // ���콺 ��ư�� �����ٰ� ���� ��ġ���� ���� �� �߻�
    public void OnPointerDown(PointerEventData eventData)
    {
        if (CanInteract())
        {
            DeployableManager.Instance.StartDeployableSelection(deployableInfo);
        }
    }

    // ��ư�� ������ �ణ�� �������� ���� �� �߻�
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CanInteract())
        {
            isDragging = true;
            DeployableManager.Instance.StartDragging(deployableInfo);
        }
    }

    // ��ư�� ���� ���¿��� �̵��� �� �����Ӹ��� �߻�
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && CanInteract())
        {
            DeployableManager.Instance.HandleDragging(deployableInfo);
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
        return !deployableGameState.IsOnCooldown && 
            StageManager.Instance.CurrentDeploymentCost >= currentDeploymentCost;
    }
}

#nullable enable