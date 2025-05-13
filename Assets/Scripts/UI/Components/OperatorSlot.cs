using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Skills.Base;


// 스쿼드 편집 패널과 오퍼레이터 선택 패널에서 공통으로 사용되는 오퍼레이터 슬롯 버튼을 구현
public class OperatorSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject activeComponent = default!; // 사용 가능한 슬롯일 때 나타날 요소
    [SerializeField] private TextMeshProUGUI slotText = default!; // 사용 가능하지만 비어 있거나, 아예 사용 불가능할 때 띄울 텍스트

    [Header("Active Component References")]
    [SerializeField] private Image operatorImage = default!;
    [SerializeField] private Slider expSlider = default!;
    [SerializeField] private TextMeshProUGUI levelText = default!;
    [SerializeField] private Image classIconImage = default!;
    [SerializeField] private Image skillImage = default!;
    [SerializeField] private Image promotionImage = default!;
    [SerializeField] private TextMeshProUGUI operatorNameText = default!;
    [SerializeField] private Image selectedIndicator = default!;
    [SerializeField] private TextMeshProUGUI indexText = default!;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color disabledColor = Color.gray;

    [SerializeField] private Button button = default!;

    // 사용 가능한 버튼인가를 표시
    private bool isThisActiveButton = false;

    public OwnedOperator? OwnedOperator { get; private set; }
    public OperatorData? opData => OwnedOperator?.OperatorProgressData;

    public bool IsSelected { get; private set; } = false;
    public BaseSkill? SelectedSkill { get; private set; } // 슬롯이니까 null일 수 있음

    // OperatorSlotButton 타입의 파라미터를 받는 이벤트 정의
    public UnityEvent<OperatorSlot> OnSlotClicked = new UnityEvent<OperatorSlot>();

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();

        // 리스너에는 함수가 들어가야 하므로 람다 함수로 넣음
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

        ClearIndexText();
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

    private void OnEnable()
    {
        if (OwnedOperator != null)
        {
            UpdateActiveSlotVisuals();
        }
    }

    // 현재 슬롯에 오퍼레이터를 할당하고 시각 요소 업데이트

    public void AssignOperator(OwnedOperator newOwnedOperator)
    {
        OwnedOperator = newOwnedOperator;
        InitializeActiveSlotVisuals();
    }
    
    // idx는 벌크 편집일 때에만 전달된다.
    public void SetSelected(bool selected, int? idx = null)
    {
        IsSelected = selected;
        UpdateSelectionIndicator(selected);

        if (selected && idx != null) // .HasValue를 써도 무방함
        {
            UpdateIndexText(idx.Value);
        }
        else if (!selected)
        {
            ClearIndexText();
        }


        OnSlotClicked?.Invoke(this);
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
            // 기본 UI 상태 설정
            activeComponent.SetActive(true);
            slotText.gameObject.SetActive(false);

            // 오퍼레이터 이미지 설정
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

            // 클래스 아이콘 설정
            classIconImage.gameObject.SetActive(true);
            if (opData != null)
            {
                OperatorIconHelper.SetClassIcon(classIconImage, opData.operatorClass);
            }

            // 경험치 게이지, 레벨, 정예화 표시
            UpdateActiveSlotVisuals();

            // 스킬 아이콘 설정
            InitializeSkill();

            // 오퍼레이터 이름 설정
            operatorNameText.gameObject.SetActive(true);
            operatorNameText.text = opData?.entityName ?? string.Empty;

            UpdateButtonColor();
            UpdateSelectionIndicator(IsSelected);
        }
    }

    // 패널을 오가면서 변할 수 있는 요소들에 대한 UI 업데이트
    private void UpdateActiveSlotVisuals()
    {
        // 경험치 게이지, 레벨, 정예화 표시
        int remainingExp = OperatorGrowthSystem.GetMaxExpForNextLevel(OwnedOperator.currentPhase, OwnedOperator.currentLevel);
        expSlider.value = (float)OwnedOperator.currentExp / remainingExp;
        levelText.text = $"LV\r\n<size=40><b>{OwnedOperator.currentLevel}</b>\r\n</size>";
        OperatorIconHelper.SetElitePhaseIcon(promotionImage, OwnedOperator.currentPhase);

        // 스킬 설정은 따로 건드릴 필요 없을 듯
        // 인벤토리에서 스킬1 선택 -> 디테일에서 기본 스킬2로 지정 -> 인벤토리에서는 스킬1 유지되어야 함
        // 디테일에서 지정하는 건 "기본 스킬"이지 "현재 설정한 스킬"이 아니기 때문
    }

    private void InitializeSkill()
    {
        if (OwnedOperator == null) return;

        skillImage.gameObject.SetActive(true);

        int skillIndex = GameManagement.Instance!.UserSquadManager.GetCurrentSkillIndex(OwnedOperator);
        
        // 스쿼드 편집 패널 : 스테이지에서 사용할 스킬 표시
        if (OwnedOperator.UnlockedSkills.Count > skillIndex &&
            MainMenuManager.Instance!.CurrentPanel == MainMenuManager.MenuPanel.SquadEdit)
        {
            SelectedSkill = OwnedOperator.UnlockedSkills[skillIndex];
            skillImage.sprite = SelectedSkill.skillIcon;
        }

        // 인벤토리 패널 : 기본 선택된 스킬 표시
        if (OwnedOperator.DefaultSelectedSkill != null &&
            MainMenuManager.Instance!.CurrentPanel == MainMenuManager.MenuPanel.OperatorInventory)
        {
            // 현재 편집 중인 스쿼드의 인덱스
            int nowEditingIndex = GameManagement.Instance!.UserSquadManager.EditingSlotIndex;

            OwnedOperator? existingOperator = (nowEditingIndex != -1)
                                              ? GameManagement.Instance!.PlayerDataManager.GetOperatorInSlot(nowEditingIndex)
                                              : null;
            OwnedOperator? currentEditingOpereator = MainMenuManager.Instance!.CurrentEditingOperator;


            // 스쿼드 편집에서 인벤토리로 들어갔을 때는 현재 스쿼드에서 사용 중인 스킬로 초기화한다
            // 스쿼드에서의 최초 스킬 선택 조건
            bool skillInSquadCondition = nowEditingIndex != -1 && 
                existingOperator == OwnedOperator && // SquadEditPanel에서 선택해서 들어온 오퍼레이터가
                currentEditingOpereator != OwnedOperator; // 디테일 패널에서 수정된 오퍼레이터가 아닐 때

            SelectedSkill = skillInSquadCondition
                            ? OwnedOperator.UnlockedSkills[skillIndex] // 오퍼레이터를 선택해 들어왔다면 스쿼드에서의 스킬을 설정
                            : OwnedOperator.DefaultSelectedSkill; // 아니라면 기본 지정 스킬로 설정

            skillImage.sprite = SelectedSkill.skillIcon;
        }
    }

    public void UpdateSelectedSkill(BaseSkill skill)
    {
        SelectedSkill = skill;

        // 지우지 말것) 인벤토리 좌측 패널의 스킬 아이콘 클릭 시 우측 스킬 슬롯의 아이콘 업데이트
        skillImage.sprite = SelectedSkill.skillIcon; 
    }

    public void UpdateSelectionIndicator(bool isActive)
    {
        if (selectedIndicator != null)
        {
            selectedIndicator.gameObject.SetActive(isActive);
        }
    }

    private void UpdateIndexText(int idx)
    {
        indexText.text = $"{idx + 1}";
    }

    private void ClearIndexText()
    {
        indexText.text = "";
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
