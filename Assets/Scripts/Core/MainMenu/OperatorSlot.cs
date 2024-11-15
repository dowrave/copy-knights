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
    [SerializeField] private Image classIconImage;
    [SerializeField] private Image skillImage;
    [SerializeField] private TextMeshProUGUI operatorNameText;
    [SerializeField] private Image selectedIndicator;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    //[SerializeField] private Color selectedColor = Color.cyan;
    [SerializeField] private Color disabledColor = Color.gray;

    private Button button;

    // ��� ������ ��ư�ΰ��� ǥ��
    private bool isThisActiveButton = false;

    // ���� ���Կ� �Ҵ�� "���۷����� ������"
    public OperatorData AssignedOperator { get; private set; }

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

    /// <summary>
    /// ���۷����� �Ҵ��� AssignOperator���� ������ ����
    /// </summary>
    public void Initialize(bool isActive, OperatorData operatorData = null)
    {
        isThisActiveButton = isActive;
        button.interactable = isThisActiveButton;

        if (operatorData != null)
        {
            AssignOperator(operatorData);
        }
        else
        {
            SetEmptyOrDisabled(isActive);
        }
    }

    /// <summary>
    /// 1. Empty�� Disabled�� ���� ���̰� ���� ��� �Բ� �̿�
    /// 2. OperatorSelectionPanel���� SquadEditPanel�� Slot�� ��� ������ �� �� ����
    /// </summary>
    public void SetEmptyOrDisabled(bool isActive)
    {
        AssignedOperator = null;
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);
        UpdateVisuals();
    }

    /// <summary>
    /// ���� ���Կ� ���۷����͸� �Ҵ��ϰ� �ð� ��� ������Ʈ
    /// </summary>
    public void AssignOperator(OperatorData operatorData)
    {
        AssignedOperator = operatorData;
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

        if (AssignedOperator == null)
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
        return AssignedOperator == null;
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
        if (AssignedOperator.icon != null)
        {
            operatorImage.sprite = AssignedOperator.icon;
        }
        else
        {
            operatorImage.gameObject.SetActive(false);
        }

        // Ŭ���� ������ ����
        classIconImage.gameObject.SetActive(true);
        IconHelper.SetClassIcon(classIconImage, AssignedOperator.operatorClass);

        // ��ų ������ ����
        UpdateSkillIcon();

        // ���۷����� �̸� ����
        operatorNameText.gameObject.SetActive(true);
        operatorNameText.text = AssignedOperator.entityName;
    }

    private void UpdateSkillIcon()
    {
        if (AssignedOperator.skills != null && AssignedOperator.skills.Count > 0)
        {
            skillImage.gameObject.SetActive(true);
            Sprite skillIcon = AssignedOperator.skills[0].SkillIcon;
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
