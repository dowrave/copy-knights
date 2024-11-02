using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;


/// <summary>
/// 스쿼드 편집 패널과 오퍼레이터 선택 패널에서 공통으로 사용되는 오퍼레이터 슬롯 버튼을 구현
/// </summary>
public class OperatorSlotButton : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject activeComponent; // 사용 가능한 슬롯일 때 나타날 요소
    [SerializeField] private Image operatorImage;
    [SerializeField] private Image classIconImage;
    [SerializeField] private Image skillImage;
    [SerializeField] private Button button; // 클릭 이벤트를 받음
    [SerializeField] private TextMeshProUGUI slotText; // 사용 가능하지만 비어 있거나, 아예 사용 불가능할 때 띄울 텍스트

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.cyan;
    [SerializeField] private Color disabledColor = Color.gray;

    // 사용 가능한 버튼인가를 표시
    private bool isThisActiveButton = false;

    // 현재 슬롯에 할당된 오퍼레이터 데이터
    private OperatorData assignedOperator;
    public OperatorData AssignedOperator => assignedOperator;

    // 선택 상태
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
            // 오퍼레이터 이미지 설정
            if (operatorData.icon != null) 
            {
                operatorImage.sprite = operatorData.icon;
                operatorImage.gameObject.SetActive(true);
            }

            // 클래스 아이콘 설정
            IconHelper.SetClassIcon(classIconImage, operatorData.operatorClass);
            classIconImage.gameObject.SetActive(true);

            // 빈 슬롯 텍스트 숨기기?
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
    /// 상태 변화에 따른 버튼의 모든 시각적인 요소를 처리함
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
