using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Skills.Base;


// ������ ���� �гΰ� ���۷����� ���� �гο��� �������� ���Ǵ� ���۷����� ���� ��ư�� ����
public class OperatorSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject activeComponent = default!; // ��� ������ ������ �� ��Ÿ�� ���
    [SerializeField] private TextMeshProUGUI slotText = default!; // ��� ���������� ��� �ְų�, �ƿ� ��� �Ұ����� �� ��� �ؽ�Ʈ

    [Header("Active Component References")]
    [SerializeField] private Image operatorImage = default!;
    [SerializeField] private Slider expSlider = default!;
    [SerializeField] private TextMeshProUGUI levelText = default!;
    [SerializeField] private Image classIconImage = default!;
    [SerializeField] private Image skillImage = default!;
    [SerializeField] private Image promotionImage = default!;
    [SerializeField] private TextMeshProUGUI operatorNameText = default!;
    [SerializeField] private Image selectedIndicator = default!;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color disabledColor = Color.gray;

    [SerializeField] private Button button = default!;

    // ��� ������ ��ư�ΰ��� ǥ��
    private bool isThisActiveButton = false;

    public OwnedOperator? OwnedOperator { get; private set; }
    public OperatorData? opData => OwnedOperator?.OperatorProgressData;

    public bool IsSelected { get; private set; } = false;
    public BaseSkill? SelectedSkill { get; private set; } // �����̴ϱ� null�� �� ����

    // OperatorSlotButton Ÿ���� �Ķ���͸� �޴� �̺�Ʈ ����
    public UnityEvent<OperatorSlot> OnSlotClicked = new UnityEvent<OperatorSlot>();

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();

        // �����ʿ��� �Լ��� ���� �ϹǷ� ���� �Լ��� ����
        button.onClick.AddListener(() => OnSlotClicked?.Invoke(this)); 
    }


    public void Initialize(bool isActive, OwnedOperator? ownedOp = null)
    {
        isThisActiveButton = isActive;
        button.interactable = isThisActiveButton;

        if (ownedOp != null)
        {
            AssignOperator(ownedOp);
        }
        else
        {
            InitializeEmptyOrDisabled(isActive);
        }
    }

    // 1. Empty�� Disabled�� ���� ���̰� ���� ��� �Բ� �̿�
    // 2. OperatorSelectionPanel���� SquadEditPanel�� Slot�� ��� ������ �� �� ����
    public void InitializeEmptyOrDisabled(bool isActive)
    { 
        OwnedOperator = null;
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);

        // ��Ȱ�� ���� ǥ��
        if (!isThisActiveButton)
        {
            InitializeInactiveSlotVisuals();
            return;
        }

        // �� ���� ǥ��
        if (OwnedOperator == null)
        {
            InitializeEmptySlotVisuals();
            return;
        }
    }


    // ���� ���Կ� ���۷����͸� �Ҵ��ϰ� �ð� ��� ������Ʈ

    public void AssignOperator(OwnedOperator newOwnedOperator)
    {
        OwnedOperator = newOwnedOperator;
        InitializeActiveSlotVisuals();
    }
    
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        UpdateSelectionIndicator();
        OnSlotClicked.Invoke(this);
    }

    public bool IsEmpty()
    {
        return opData == null;
    }

    private void InitializeInactiveSlotVisuals()
    {
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);
        slotText.text = "X";
        slotText.fontSize = 90;
        UpdateButtonColor();
    }

    private void InitializeEmptySlotVisuals()
    {
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);
        slotText.text = "Empty\nSlot";
        slotText.fontSize = 44;
        UpdateButtonColor();
    }

    private void InitializeActiveSlotVisuals()
    {
        if (OwnedOperator != null)
        {
            // �⺻ UI ���� ����
            activeComponent.SetActive(true);
            slotText.gameObject.SetActive(false);

            // ���۷����� �̹��� ����
            operatorImage.gameObject.SetActive(true);

            if (opData == null) return;

            if (opData.icon != null)
            {
                operatorImage.sprite = opData.icon;
            }
            else
            {
                operatorImage.gameObject.SetActive(false);
            }
            // ����ġ ������, ����, ����ȭ ǥ��
            int remainingExp = OperatorGrowthSystem.GetMaxExpForNextLevel(OwnedOperator.currentPhase, OwnedOperator.currentLevel);
            expSlider.value = (float)OwnedOperator.currentExp / remainingExp;
            levelText.text = $"LV\r\n<size=40><b>{OwnedOperator.currentLevel}</b>\r\n</size>";
            OperatorIconHelper.SetElitePhaseIcon(promotionImage, OwnedOperator.currentPhase);

            // Ŭ���� ������ ����
            classIconImage.gameObject.SetActive(true);
            if (opData != null)
            {
                OperatorIconHelper.SetClassIcon(classIconImage, opData.operatorClass);
            }

            // ��ų ������ ����
            InitializeSkill();

            // ���۷����� �̸� ����
            operatorNameText.gameObject.SetActive(true);
            operatorNameText.text = opData?.entityName ?? string.Empty;

            UpdateButtonColor();
            UpdateSelectionIndicator();
        }
    }

    private void InitializeSkill()
    {
        if (OwnedOperator == null) return;

        skillImage.gameObject.SetActive(true);
        
        // ������ ���� �г� : ������������ ����� ��ų ǥ��
        if (OwnedOperator.StageSelectedSkill != null &&
            MainMenuManager.Instance!.CurrentPanel == MainMenuManager.MenuPanel.SquadEdit)
        {
            SelectedSkill = OwnedOperator.StageSelectedSkill;
            skillImage.sprite = SelectedSkill.skillIcon;
        }

        // �κ��丮 �г� : �⺻ ���õ� ��ų ǥ��
        if (OwnedOperator.DefaultSelectedSkill != null &&
            MainMenuManager.Instance!.CurrentPanel == MainMenuManager.MenuPanel.OperatorInventory)
        {
            // ������ �������� �κ��丮�� ���� ���� ���� �����忡�� ��� ���� ��ų�� �ʱ�ȭ�Ѵ�
            int nowEditingIndex = GameManagement.Instance!.UserSquadManager.EditingSlotIndex;
            OwnedOperator? existingOperator = (nowEditingIndex != -1)
                                              ? GameManagement.Instance!.PlayerDataManager.GetOperatorInSlot(nowEditingIndex)
                                              : null;

            SelectedSkill = (nowEditingIndex != -1 && existingOperator == OwnedOperator)
                            ? OwnedOperator.StageSelectedSkill
                            : OwnedOperator.DefaultSelectedSkill;

            skillImage.sprite = SelectedSkill.skillIcon;
        }
    }

    public void UpdateSelectedSkill(BaseSkill skill)
    {
        SelectedSkill = skill;

        // ������ ����) �κ��丮 ���� �г��� ��ų ������ Ŭ�� �� ���� ��ų ������ ������ ������Ʈ
        skillImage.sprite = SelectedSkill.skillIcon; 
    }

    private void UpdateSelectionIndicator()
    {
        if (selectedIndicator != null)
        {
            Debug.Log($"������ �ε������� Ȱ��ȭ ���� : {IsSelected}");
            selectedIndicator.gameObject.SetActive(IsSelected);
        }
    }

    private void UpdateButtonColor()
    {
        Image buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = button.interactable ? normalColor : disabledColor;
        }
    }
}
