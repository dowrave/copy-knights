using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Skills.Base;


// 스쿼드 편집 패널과 오퍼레이터 선택 패널에서 공통으로 사용되는 오퍼레이터 슬롯 버튼을 구현
public class OperatorSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject activeComponent; // 사용 가능한 슬롯일 때 나타날 요소
    [SerializeField] private TextMeshProUGUI slotText; // 사용 가능하지만 비어 있거나, 아예 사용 불가능할 때 띄울 텍스트

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

    [SerializeField] private Button button;

    // 사용 가능한 버튼인가를 표시
    private bool isThisActiveButton = false;

    public OwnedOperator OwnedOperator { get; private set; }
    public OperatorData opData => OwnedOperator?.BaseData;

    public bool IsSelected { get; private set; } = false;
    public BaseSkill SelectedSkill { get; private set; } = null; // 초기화 때에만 null

    // OperatorSlotButton 타입의 파라미터를 받는 이벤트 정의
    public UnityEvent<OperatorSlot> OnSlotClicked = new UnityEvent<OperatorSlot>();

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();

        // 람다 함수를 온클릭 이벤트에 등록함. 람다 함수는 이벤트를 발생시킴.
        // 최종적으로 클릭 시 OnSlotClicked이라는 이벤트가 발생하는 원리
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
            InitializeEmptyOrDisabled(isActive);
        }
    }

    // 1. Empty와 Disabled의 구현 차이가 거의 없어서 함께 이용
    // 2. OperatorSelectionPanel에서 SquadEditPanel의 Slot을 비울 때에도 쓸 수 있음
    public void InitializeEmptyOrDisabled(bool isActive)
    { 
        OwnedOperator = null;
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);

        // 비활성 슬롯 표시
        if (!isThisActiveButton)
        {
            InitializeInactiveSlotVisuals();
            return;
        }

        // 빈 슬롯 표시
        if (OwnedOperator == null)
        {
            InitializeEmptySlotVisuals();
            return;
        }
    }


    // 현재 슬롯에 오퍼레이터를 할당하고 시각 요소 업데이트

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
        // 기본 UI 상태 설정
        activeComponent.SetActive(true);
        slotText.gameObject.SetActive(false);

        // 오퍼레이터 이미지 설정
        operatorImage.gameObject.SetActive(true);
        if (opData.icon != null)
        {
            operatorImage.sprite = opData.icon;
        }
        else
        {
            operatorImage.gameObject.SetActive(false);
        }

        // 경험치 게이지, 레벨, 정예화 표시
        int remainingExp = OperatorGrowthSystem.GetMaxExpForNextLevel(OwnedOperator.currentPhase, OwnedOperator.currentLevel);
        expSlider.value = (float)OwnedOperator.currentExp / remainingExp;
        levelText.text = $"LV\r\n<size=40><b>{OwnedOperator.currentLevel}</b>\r\n</size>";
        OperatorIconHelper.SetElitePhaseIcon(promotionImage, OwnedOperator.currentPhase);

        // 클래스 아이콘 설정
        classIconImage.gameObject.SetActive(true);
        OperatorIconHelper.SetClassIcon(classIconImage, opData.operatorClass);

        // 스킬 아이콘 설정
        InitializeSkillIcon();

        // 오퍼레이터 이름 설정
        operatorNameText.gameObject.SetActive(true);
        operatorNameText.text = opData.entityName;

        UpdateButtonColor();
        UpdateSelectionIndicator();
    }

    private void InitializeSkillIcon()
    {
        skillImage.gameObject.SetActive(true);

        // 스쿼드 편집 패널 : 스테이지에서 사용할 스킬 표시
        if (OwnedOperator.StageSelectedSkill != null &&
            MainMenuManager.Instance.CurrentPanel == MainMenuManager.MenuPanel.SquadEdit)
        {
            SelectedSkill = OwnedOperator.StageSelectedSkill;
            skillImage.sprite = SelectedSkill.skillIcon;
        }

        // 인벤토리 패널 : 기본 선택된 스킬 표시
        if (OwnedOperator.DefaultSelectedSkill != null &&
            MainMenuManager.Instance.CurrentPanel == MainMenuManager.MenuPanel.OperatorInventory)
        {
            SelectedSkill = OwnedOperator.DefaultSelectedSkill;
            skillImage.sprite = OwnedOperator.DefaultSelectedSkill.skillIcon;
        }
    }

    public void UpdateSelectedSkill(BaseSkill skill)
    {
        SelectedSkill = skill;
        skillImage.sprite = SelectedSkill.skillIcon;
    }

    private void UpdateSelectionIndicator()
    {
        if (selectedIndicator != null)
        {
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
