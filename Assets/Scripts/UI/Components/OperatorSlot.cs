using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// ������ ���� �гΰ� ���۷����� ���� �гο��� �������� ���Ǵ� ���۷����� ���� ��ư�� ����
/// </summary>
public class OperatorSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject activeComponent; // ��� ������ ������ �� ��Ÿ�� ���
    [SerializeField] private TextMeshProUGUI slotText; // ��� ���������� ��� �ְų�, �ƿ� ��� �Ұ����� �� ��� �ؽ�Ʈ

    [Header("Active Component References")]
    [SerializeField] private Image operatorImage;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image classIconImage;
    [SerializeField] private Image skillImage;
    [SerializeField] private Image promotionImage;
    [SerializeField] private TextMeshProUGUI operatorNameText;
    [SerializeField] private Image selectedIndicator;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color disabledColor = Color.gray;

    private Button button;

    // ��� ������ ��ư�ΰ��� ǥ��
    private bool isThisActiveButton = false;

    public OwnedOperator OwnedOperator { get; private set; }
    public OperatorData AssignedOperatorData => OwnedOperator?.BaseData;


    // ���� ����
    private bool isSelected = false;
    public bool IsSelected => isSelected;

    // OperatorSlotButton Ÿ���� �Ķ���͸� �޴� �̺�Ʈ ����
    public UnityEvent<OperatorSlot> OnSlotClicked = new UnityEvent<OperatorSlot>();

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();

        // ���� �Լ��� ��Ŭ�� �̺�Ʈ�� �����. ���� �Լ��� �̺�Ʈ�� �߻���Ŵ.
        // ���������� Ŭ�� �� OnSlotClicked�̶�� �̺�Ʈ�� �߻��ϴ� ����
        button.onClick.AddListener(() => OnSlotClicked.Invoke(this)); 
    }


    public void Initialize(bool isActive, OwnedOperator ownedOp = null)
    {
        isThisActiveButton = isActive;
        button.interactable = isThisActiveButton;

        if (ownedOp != null)
        {
            AssignOperator(ownedOp);
        }
        else
        {
            SetEmptyOrDisabled(isActive);
        }
    }

    /// <summary>
    /// Initialize�� ����, ���� Ÿ���� �޴� ������ ������ Initialize�� �����Բ� �̷��� ������
    /// </summary>
    public void Initialize(bool isActive, OperatorData operatorData)
    {
        if (operatorData != null)
        {
            var ownedOp = GameManagement.Instance.PlayerDataManager.GetOwnedOperator(operatorData.entityName);
            Initialize(isActive, ownedOp);
        }
        else
        {
            Initialize(isActive, (OwnedOperator)null);
        }
    }

    /// <summary>
    /// 1. Empty�� Disabled�� ���� ���̰� ���� ��� �Բ� �̿�
    /// 2. OperatorSelectionPanel���� SquadEditPanel�� Slot�� ��� ������ �� �� ����
    /// </summary>
    public void SetEmptyOrDisabled(bool isActive)
    { 
        OwnedOperator = null;
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);
        UpdateVisuals();
    }

    /// <summary>
    /// ���� ���Կ� ���۷����͸� �Ҵ��ϰ� �ð� ��� ������Ʈ
    /// </summary>
    public void AssignOperator(OwnedOperator newOwnedOperator)
    {
        OwnedOperator = newOwnedOperator;
        UpdateVisuals();
    }

    /// <summary>
    /// ���� ��ȭ�� ���� ��ư�� ��� �ð����� ��Ҹ� ó����
    /// </summary>
    private void UpdateVisuals()
    {
        if (!isThisActiveButton)
        {
            // ��Ȱ�� ���� ǥ��
            SetInactiveSlotVisuals();
            return;
        }

        if (OwnedOperator == null)
        {
            // �� ���� ǥ��
            SetEmptySlotVisuals();
            return;
        }

        // ���۷����Ͱ� �Ҵ�� ���� ǥ��
        SetOperatorSlotVisuals();

        // ���� ���� ǥ��
        UpdateSelectionIndicator();

        // ��ư ���� ������Ʈ
        UpdateButtonColor();
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
        OnSlotClicked.Invoke(this);
    }

    public bool IsEmpty()
    {
        return AssignedOperatorData == null;
    }

    private void SetInactiveSlotVisuals()
    {
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);
        slotText.text = "X";
        slotText.fontSize = 90;
        UpdateButtonColor();
    }

    private void SetEmptySlotVisuals()
    {
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);
        slotText.text = "Empty\nSlot";
        slotText.fontSize = 44;
        UpdateButtonColor();
    }

    private void SetOperatorSlotVisuals()
    {
        // �⺻ UI ���� ����
        activeComponent.SetActive(true);
        slotText.gameObject.SetActive(false);

        // ���۷����� �̹��� ����
        operatorImage.gameObject.SetActive(true);
        if (AssignedOperatorData.icon != null)
        {
            operatorImage.sprite = AssignedOperatorData.icon;
        }
        else
        {
            operatorImage.gameObject.SetActive(false);
        }

        // ����ġ ������, ����, ����ȭ ǥ��
        int remainingExp = OperatorGrowthSystem.GetRemainingExpForNextLevel(
            OwnedOperator.currentPhase, 
            OwnedOperator.currentLevel, 
            OwnedOperator.currentExp);
        expSlider.value = (float)OwnedOperator.currentExp / remainingExp;
        levelText.text = $"LV\r\n<size=40><b>{OwnedOperator.currentLevel}</b>\r\n</size>";
        OperatorIconHelper.SetElitePhaseIcon(promotionImage, OwnedOperator.currentPhase);

        // Ŭ���� ������ ����
        classIconImage.gameObject.SetActive(true);
        OperatorIconHelper.SetClassIcon(classIconImage, AssignedOperatorData.operatorClass);

        // ��ų ������ ����
        UpdateSkillIcon();

        // ���۷����� �̸� ����
        operatorNameText.gameObject.SetActive(true);
        operatorNameText.text = AssignedOperatorData.entityName;
    }

    private void UpdateSkillIcon()
    {
        if (AssignedOperatorData.skills != null && AssignedOperatorData.skills.Count > 0)
        {
            skillImage.gameObject.SetActive(true);
            Sprite skillIcon = AssignedOperatorData.skills[0].SkillIcon;
            if (skillIcon != null)
            {
                skillImage.sprite = skillIcon;
            }
            else
            {
                skillImage.gameObject.SetActive(false);
            }
        }
        else
        {
            skillImage.gameObject.SetActive(false);
        }
    }

    private void UpdateSelectionIndicator()
    {
        if (selectedIndicator != null)
        {
            selectedIndicator.gameObject.SetActive(isSelected);
        }
    }

    private void UpdateButtonColor()
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = button.interactable ? normalColor : disabledColor;
        }
    }
}