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
    [SerializeField] private TextMeshProUGUI slotText; // 사용 가능하지만 비어 있거나, 아예 사용 불가능할 때 띄울 텍스트

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

    // 사용 가능한 버튼인가를 표시
    private bool isThisActiveButton = false;

    // 현재 슬롯에 할당된 "오퍼레이터 데이터"
    public OperatorData AssignedOperator { get; private set; }

    // 선택 상태
    private bool isSelected = false;
    public bool IsSelected => isSelected;

    // OperatorSlotButton 타입의 파라미터를 받는 이벤트 정의
    public UnityEvent<OperatorSlotButton> OnSlotClicked = new UnityEvent<OperatorSlotButton>();

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();

        // Button 클릭 시 OnSlotClicked 이벤트 발생, 현재 OperatorSlotButton(this)을 파라미터로 전달함
        button.onClick.AddListener(() => OnSlotClicked.Invoke(this));
    }

    public void Initialize(bool isActive)
    {
        SetEmptyOrDisabled(isActive);
        UpdateVisuals();
    }

    /// <summary>
    /// 1. Empty와 Disabled의 구현 차이가 거의 없어서 초기화 시 이용
    /// 2. OperatorSelectionPanel에서 SquadEditPanel의 Slot을 비울 때에도 쓸 수 있음
    /// </summary>
    public void SetEmptyOrDisabled(bool isActive)
    {
        isThisActiveButton = isActive;
        button.interactable = isThisActiveButton;

        AssignedOperator = null;
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);
    }

    public void AssignOperator(OperatorData operatorData)
    {
        AssignedOperator = operatorData;
        UpdateVisuals();
    }

    /// <summary>
    /// 상태 변화에 따른 버튼의 모든 시각적인 요소를 처리함
    /// </summary>
    private void UpdateVisuals()
    {

        if (isThisActiveButton)
        {
            if (!isThisActiveButton)
            {
                // 비활성 슬롯 표시
                SetInactiveSlotVisuals();
                return;
            }

            if (AssignedOperator == null)
            {
                // 빈 슬롯 표시
                SetEmptySlotVisuals();
                return;
            }

            // 오퍼레이터가 할당된 슬롯 표시
            SetOperatorSlotVisuals();

            // 선택 상태 표시
            UpdateSelectionIndicator();

            // 버튼 색상 업데이트
            UpdateButtonColor();
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
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
    }

    private void SetEmptySlotVisuals()
    {
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);
        slotText.text = "Empty\nSlot";
        slotText.fontSize = 44;
    }

    private void SetOperatorSlotVisuals()
    {
        // 기본 UI 상태 설정
        activeComponent.SetActive(true);
        slotText.gameObject.SetActive(false);

        // 오퍼레이터 이미지 설정
        operatorImage.gameObject.SetActive(true);
        if (AssignedOperator.icon != null)
        {
            operatorImage.sprite = AssignedOperator.icon;
        }
        else
        {
            operatorImage.gameObject.SetActive(false);
        }

        // 클래스 아이콘 설정
        classIconImage.gameObject.SetActive(true);
        IconHelper.SetClassIcon(classIconImage, AssignedOperator.operatorClass);

        // 스킬 아이콘 설정
        UpdateSkillIcon();

        // 오퍼레이터 이름 설정
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
