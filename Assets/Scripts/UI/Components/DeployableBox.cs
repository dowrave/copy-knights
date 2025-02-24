using DG.Tweening;
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
    private DeployableUnitState deployableUnitState;

    // �ִϸ��̼� ����
    private Vector3 originalPosition;
    public float animationDuration = 0.2f;
    public float animationHeight = 20f;
    private Tween currentTween;
    private bool isOriginalPositionSet;

    public bool IsSelected { get; private set; } = false;

    // ���� ����
    private bool isDragging = false;
    private int currentDeploymentCost;

    public void Initialize(DeployableManager.DeployableInfo info)
    {
        deployableInfo = info;
        deployablePrefab = info.prefab;
        deployableComponent = deployablePrefab.GetComponent<DeployableUnitEntity>(); // ������ Ȱ��
        deployableUnitState = DeployableManager.Instance.UnitStates[deployableInfo];

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

        InitializeVisuals();

        StageManager.Instance.OnDeploymentCostChanged += UpdateAvailability;

        StageManager.Instance.OnPreparationCompleted += InitializeVisuals;
        DeployableManager.Instance.OnCurrentOperatorDeploymentCountChanged += UpdateAvailability;
    }

    public void UpdateDisplay(DeployableUnitState unitState)
    {
        costText.text = unitState.CurrentDeploymentCost.ToString();

        // ���۷����Ͱ� �ƴ϶�� ��ġ ���� Ƚ�� ǥ��
        if (!unitState.IsOperator)
        {
            countText.gameObject.SetActive(true);
            countText.text = $"x{unitState.RemainingDeployCount}";
        }
        else
        {
            countText.gameObject.SetActive(false);
        }

        UpdateAvailability();

        // ��Ÿ�� ���� ǥ��
        if (unitState.IsOnCooldown)
        {
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = Mathf.Ceil(unitState.CooldownTimer).ToString();
        }
        else
        {
            cooldownText.gameObject.SetActive(false);
        }
    }


    private void Update()
    {
        if (deployableUnitState.IsOnCooldown)
        {
            deployableUnitState.UpdateCooldown();
            UpdateDisplay(deployableUnitState);
        }
    }

    // �ϴ��� �ʱ�ȭ ���� ���� �ֱ�� �ϴ�
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

        UpdateDisplay(deployableUnitState);
    }

    private void UpdateAvailability()
    {
        if (CanInteract())
        {
            inActiveImage.gameObject.SetActive(false); // �帴�� �̹��� ����
        }
        else
        {
            inActiveImage.gameObject.SetActive(true); // �帴�� �̹��� Ȱ��ȭ
        }
    }

    public void Select()
    {
        IsSelected = true;
        AnimateSelection();
    }

    public void Deselect()
    {
        IsSelected = false;
        ResetAnimation();
    }

    // ���콺 ���� ���� : �������� �ʴ´ٸ� ����� Ȯ���϶�
    // ���콺 ��ư�� �����ٰ� ���� ��ġ���� ���� �� �߻�
    public void OnPointerDown(PointerEventData eventData)
    {
        if (CanInteract())
        {
            DeployableManager.Instance.StartDeployableSelection(deployableInfo);
            Select();
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
    
    // �ڽ��� ���õ��� ���� �ִϸ��̼� ����
    private void AnimateSelection()
    {
        // �ִϸ��̼��� ���� ���� ��ġ ����
        originalPosition = GetComponent<RectTransform>().anchoredPosition;
        isOriginalPositionSet = true;

        currentTween?.Kill();

        Sequence sequence = DOTween.Sequence();

        RectTransform rectTransform = GetComponent<RectTransform>();

        sequence.Append(rectTransform.DOAnchorPosY(originalPosition.y + animationHeight, animationDuration / 2)
            .SetEase(Ease.OutQuad));
        //sequence.Append(rectTransform.DOAnchorPosY(originalPosition.y, animationDuration / 2)
        //    .SetEase(Ease.InQuad));

        currentTween = sequence;
    }

    public void ResetAnimation()
    {
        currentTween?.Kill();
        if (isOriginalPositionSet)
        {
            GetComponent<RectTransform>().anchoredPosition = originalPosition;
            isOriginalPositionSet = false;
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
        if (deployableComponent is Operator op)
        {
            return !deployableUnitState.IsOnCooldown && 
                StageManager.Instance.CurrentDeploymentCost >= currentDeploymentCost &&
                DeployableManager.Instance.CurrentOperatorDeploymentCount < DeployableManager.Instance.MaxOperatorDeploymentCount;
        }
        else
        {
            return !deployableUnitState.IsOnCooldown &&
                    StageManager.Instance.CurrentDeploymentCost >= currentDeploymentCost;
        }


    }

    private void OnDestroy()
    {
        StageManager.Instance.OnPreparationCompleted -= InitializeVisuals;
        StageManager.Instance.OnDeploymentCostChanged -= UpdateAvailability;
    }

}

#nullable enable