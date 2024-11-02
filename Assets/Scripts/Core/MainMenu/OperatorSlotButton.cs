using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;


/// <summary>
/// ������ ���� �гΰ� ���۷����� ���� �гο��� �������� ���Ǵ� ���۷����� ���� ��ư�� ����
/// </summary>
public class OperatorSlotButton : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject activeComponent; // ��� ������ ������ �� ��Ÿ�� ���
    [SerializeField] private Image operatorImage;
    [SerializeField] private Image classIconImage;
    [SerializeField] private Image skillImage;
    [SerializeField] private Button button; // Ŭ�� �̺�Ʈ�� ����
    [SerializeField] private TextMeshProUGUI slotText; // ��� ���������� ��� �ְų�, �ƿ� ��� �Ұ����� �� ��� �ؽ�Ʈ

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.cyan;
    [SerializeField] private Color disabledColor = Color.gray;

    // ��� ������ ��ư�ΰ��� ǥ��
    private bool isThisActiveButton = false;

    // ���� ���Կ� �Ҵ�� ���۷����� ������
    private OperatorData assignedOperator;
    public OperatorData AssignedOperator => assignedOperator;

    // ���� ����
    private bool isSelected = false;
    public bool IsSelected => isSelected;

    public UnityEvent<OperatorSlotButton> OnSlotClicked = new UnityEvent<OperatorSlotButton>();

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(() => OnSlotClicked.Invoke(this));
    }

    public void Initialize(bool isActive)
    {
        isThisActiveButton = isActive;
        button.interactable = isThisActiveButton; 
        SetEmpty();
        UpdateVisuals();
    }

    private void SetDisabledButton()
    {
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);
    }

    public void AssignOperator(OperatorData operatorData)
    {
        assignedOperator = operatorData;

        if (operatorData != null)
        {
            // ���۷����� �̹��� ����
            if (operatorData.icon != null) 
            {
                operatorImage.sprite = operatorData.icon;
                operatorImage.gameObject.SetActive(true);
            }

            // Ŭ���� ������ ����
            IconHelper.SetClassIcon(classIconImage, operatorData.operatorClass);
            classIconImage.gameObject.SetActive(true);

            // �� ���� �ؽ�Ʈ �����?
            slotText.gameObject.SetActive(false);
        }

        else
        {
            SetEmpty();
        }

        UpdateVisuals();
    }

    public void SetEmpty()
    {
        assignedOperator = null;
        classIconImage.gameObject.SetActive(false);
        slotText.gameObject.SetActive(true);
    }

    /// <summary>
    /// ���� ��ȭ�� ���� ��ư�� ��� �ð����� ��Ҹ� ó����
    /// </summary>
    private void UpdateVisuals()
    {
        if (isThisActiveButton)
        {
            if (assignedOperator != null)
            {
                activeComponent.SetActive(true);
                slotText.gameObject.SetActive(false);
            }
            else
            {
                activeComponent.SetActive(false);
                slotText.gameObject.SetActive(true);
                slotText.text = "Empty\nSlot";
                slotText.fontSize = 44;
            }
        }
        else
        {
            activeComponent.SetActive(false);
            slotText.gameObject.SetActive(true);
            slotText.text = "X";
            slotText.fontSize = 90;
        }


        Color targetColor = button.interactable ? 
            (isSelected ? selectedColor : normalColor) : 
            disabledColor;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = targetColor; 
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
        UpdateVisuals();
    }

    public bool IsEmpty()
    {
        return assignedOperator == null;
    }

}
