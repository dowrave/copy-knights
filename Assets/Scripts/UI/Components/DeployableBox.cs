using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeployableBox : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image operatorIllustImage = default!;
    [SerializeField] private GameObject operatorClassIconBox = default!; // Ŭ���� �������� �θ� ������Ʈ
    [SerializeField] private Image operatorClassIconImage = default!; // ������ ��ü �Ҵ�
    [SerializeField] private Image inActiveImage = default!;
    [SerializeField] private TextMeshProUGUI costText = default!;
    [SerializeField] private TextMeshProUGUI countText = default!;
    [SerializeField] private Image promotionIconImage = default!;

    [Header("Cooldown Container")]
    [SerializeField] private GameObject cooldownContainer = default!;
    [SerializeField] private Image cooldownGauge = default!;
    [SerializeField] private TextMeshProUGUI cooldownText = default!;

    private Color cannotDeployColor = new Color(0, 0, 0, 0.95f);
    private Color onCooldownColor = new Color(0.3f, 0, 0, 0.95f);

    private Sprite? boxIcon;
    private string deployableTag;
    private GameObject deployablePrefab;
    private GameObject deployableObject = default!;
    private DeployableUnitEntity deployableComponent = default!;
    private DeployableManager.DeployableInfo deployableInfo = default!;
    private DeployableUnitState deployableUnitState = default!;

    // �ִϸ��̼� ����
    private Vector3 originalPosition;
    public float animationDuration = 0.2f;
    public float animationHeight = 20f;
    private Tween currentTween = default!;
    private bool isOriginalPositionSet;

    // box�� ���� ����
    public bool IsSelected { get; private set; } = false;

    // ���� ����
    private bool isDragging = false;
    private int currentDeploymentCost;

    public void Initialize(DeployableManager.DeployableInfo info)
    {
        deployableInfo = info;
        deployableTag = info.poolTag;
        deployablePrefab = info.prefab;
        deployableComponent = deployablePrefab.GetComponent<DeployableUnitEntity>(); // ������ Ȱ��
        deployableUnitState = DeployableManager.Instance!.UnitStates[deployableInfo];

        if (deployableComponent is Operator)
        {
            OwnedOperator op = deployableInfo.ownedOperator!;
            OperatorIconHelper.SetClassIcon(operatorClassIconImage, op.OperatorProgressData.operatorClass); // Ŭ���� ������ ����
            boxIcon = op.OperatorProgressData?.Icon;
        }
        else
        {
            operatorClassIconBox.gameObject.SetActive(false);
            boxIcon = deployableInfo.deployableUnitData?.Icon;
        }

        InitializeVisuals();

        StageManager.Instance!.OnDeploymentCostChanged += UpdateAvailability;
        StageManager.Instance!.OnPreparationCompleted += InitializeVisuals;
        DeployableManager.Instance!.OnCurrentOperatorDeploymentCountChanged += UpdateAvailability;
    }

    public void UpdateVisuals()
    {
        // ǥ���� ��ġ �ڽ�Ʈ 
        UpdateDeploymentCost();

        // ��ġ ���� ��
        UpdateCountText();

        // ǥ�õ� �ڽ��� ��� ���������� ���� �г�
        UpdateAvailability();

        // ���ġ ��Ÿ�� ǥ��
        UpdateCooldownContainer();

        // Box ��ü�� Ȱ��ȭ ���� ����
        SetBoxActivation();

    }

    private void UpdateDeploymentCost()
    {
        currentDeploymentCost = deployableUnitState.CurrentDeploymentCost;
        costText.text = currentDeploymentCost.ToString();
    }

    // ��ġ ���� �� UI ������Ʈ
    private void UpdateCountText()
    {
        if (!deployableUnitState.IsOperator)
        {
            countText.gameObject.SetActive(true);
            countText.text = $"x{deployableUnitState.RemainingDeployCount}";
        }
        else
        {
            countText.gameObject.SetActive(false);
        }
    }

    // Box�� ���� inactiveOverlay�� UpdateAvailability���� ������
    private void UpdateCooldownContainer()
    {
        if (deployableUnitState.IsOnCooldown)
        {
            cooldownContainer.SetActive(true);

            // ���۷����Ͱ� �ƴ� ��쿡�� ��ġ �Ŀ� ���� ���� ������ ������(unitState ����)
            float currentCooldown = deployableUnitState.CooldownTimer; // max -> 0���� ���� ����
            float maxCooldown = deployableInfo.redeployTime; 

            cooldownText.text = currentCooldown.ToString("F1"); // �Ҽ� 1�ڸ����� ǥ��
            cooldownGauge.fillAmount = (maxCooldown - currentCooldown) / maxCooldown;
        }
        else
        {
            cooldownContainer.SetActive(false);
        }
    }

    private void SetBoxActivation()
    {
        // ���۷����� : ��ġ�� �Ǿ� �ִ� ������ �� box ��Ȱ��ȭ
        if (deployableUnitState.IsOperator && deployableUnitState.IsDeployed)
        {
            gameObject.SetActive(false);
        }
        // ��ġ ���� ��� : ���� Ƚ���� 0 ���ϸ� ��Ȱ��ȭ
        else if (!deployableUnitState.IsOperator && deployableUnitState.RemainingDeployCount <= 0)
        {
            gameObject.SetActive(false);
        }
        // �������� Ȱ��ȭ
        else
        {
            gameObject.SetActive(true);
        }
    }


    private void Update()
    {
        UpdateVisuals();
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
        else if (deployableComponent != null)
        {
            if (deployableComponent is Operator && deployableInfo.operatorData != null)
            {
                operatorIllustImage.color = deployableInfo.operatorData.PrimaryColor;
            }
        }

        // ���� �ϴ� ����ȭ ������ Ȱ��ȭ ����
        if (deployableComponent is Operator && deployableInfo.ownedOperator.currentPhase == OperatorGrowthSystem.ElitePhase.Elite1)
        {
            promotionIconImage.gameObject.SetActive(true);
        }
        else
        {
            promotionIconImage.gameObject.SetActive(false);
        }

        UpdateVisuals();
    }

    // inactiveOverlay ���� ����
    private void UpdateAvailability()
    {
        bool canInteract = CanInteract();

        inActiveImage.gameObject.SetActive(!canInteract);

        if (!canInteract)
        {
            if (deployableUnitState.IsOnCooldown)
            {
                inActiveImage.color = onCooldownColor;
            }
            else
            {
                inActiveImage.color = cannotDeployColor;
            }
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
            DeployableManager.Instance!.StartDeployableSelection(deployableInfo);
            Select();
        }
    }

    // ��ư�� ������ �ణ�� �������� ���� �� �߻�
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CanInteract())
        {
            isDragging = true;
            DeployableManager.Instance!.StartDragging(deployableInfo);
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
            DeployableManager.Instance!.HandleDragging(deployableInfo);
        }
    }

    // �巡�� �� ���콺 ��ư�� �� �� �߻� 
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            DeployableManager.Instance!.EndDragging();
            isDragging = false;
        }
    }

    private bool CanInteract()
    {
        if (deployableComponent is Operator op)
        {
            return !deployableUnitState.IsOnCooldown && 
                StageManager.Instance!.CurrentDeploymentCost >= currentDeploymentCost &&
                DeployableManager.Instance!.CurrentOperatorDeploymentCount < DeployableManager.Instance!.MaxOperatorDeploymentCount;
        }
        else
        {
            return !deployableUnitState.IsOnCooldown &&
                    StageManager.Instance!.CurrentDeploymentCost >= currentDeploymentCost;
        }
    }

    private void OnDestroy()
    {
        StageManager.Instance!.OnPreparationCompleted -= InitializeVisuals;
        StageManager.Instance!.OnDeploymentCostChanged -= UpdateAvailability;
    }

}

#nullable enable