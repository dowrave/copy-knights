using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// 스쿼드 편집 패널과 오퍼레이터 선택 패널에서 공통으로 사용되는 오퍼레이터 슬롯 버튼을 구현
/// </summary>
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

    private Button button;

    // 사용 가능한 버튼인가를 표시
    private bool isThisActiveButton = false;

    public OwnedOperator OwnedOperator { get; private set; }
    public OperatorData opData => OwnedOperator?.BaseData;


    // 선택 상태
    private bool isSelected = false;
    public bool IsSelected => isSelected;

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
            SetEmptyOrDisabled(isActive);
        }
    }

    /// <summary>
    /// Initialize를 수정, 기존 타입을 받는 구조도 수정된 Initialize로 보내게끔 이렇게 구성함
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
    /// 1. Empty와 Disabled의 구현 차이가 거의 없어서 함께 이용
    /// 2. OperatorSelectionPanel에서 SquadEditPanel의 Slot을 비울 때에도 쓸 수 있음
    /// </summary>
    public void SetEmptyOrDisabled(bool isActive)
    { 
        OwnedOperator = null;
        activeComponent.SetActive(false);
        slotText.gameObject.SetActive(true);
        UpdateVisuals();
    }

    /// <summary>
    /// 현재 슬롯에 오퍼레이터를 할당하고 시각 요소 업데이트
    /// </summary>
    public void AssignOperator(OwnedOperator newOwnedOperator)
    {
        OwnedOperator = newOwnedOperator;
        UpdateVisuals();
    }

    /// <summary>
    /// 상태 변화에 따른 버튼의 모든 시각적인 요소를 처리함
    /// </summary>
    private void UpdateVisuals()
    {
        if (!isThisActiveButton)
        {
            // 비활성 슬롯 표시
            SetInactiveSlotVisuals();
            return;
        }

        if (OwnedOperator == null)
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
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
        OnSlotClicked.Invoke(this);
    }

    public bool IsEmpty()
    {
        return opData == null;
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
        UpdateSkillIcon();

        // 오퍼레이터 이름 설정
        operatorNameText.gameObject.SetActive(true);
        operatorNameText.text = opData.entityName;
    }

    private void UpdateSkillIcon()
    {
        if (OwnedOperator.unlockedSkills != null && OwnedOperator.unlockedSkills.Count > 0)
        {
            skillImage.gameObject.SetActive(true);
            Sprite skillIcon = OwnedOperator.unlockedSkills[0].SkillIcon;
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
