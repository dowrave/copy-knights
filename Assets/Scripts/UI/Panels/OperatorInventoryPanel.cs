using System.Collections.Generic;
using System.Linq;
using Skills.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// 보유한 오퍼레이터들을 보여주는 패널. 스쿼드를 편집하는 패널은 SquadEditPanel로 혼동에 주의하시오 
public class OperatorInventoryPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform operatorSlotContainer = default!;
    [SerializeField] private TextMeshProUGUI operatorNameText = default!;
    [SerializeField] private OperatorSlot slotButtonPrefab = default!;
    [SerializeField] private Button confirmButton = default!;
    [SerializeField] private Button setEmptyButton = default!; // 현재 슬롯을 비우는 버튼
    [SerializeField] private Button detailButton = default!; // OperatorDetailPanel로 가는 버튼

    [Header("Attack Range Visualization")]
    [SerializeField] private RectTransform attackRangeContainer = default!;
    [SerializeField] private float centerPositionOffset = default!; // 타일 시각화 위치를 위한 중심 이동
    private UIHelper.AttackRangeHelper attackRangeHelper = default!;

    [Header("Operator Stat Boxes")]
    [SerializeField] private TextMeshProUGUI healthText = default!;
    [SerializeField] private TextMeshProUGUI redeployTimeText = default!;
    [SerializeField] private TextMeshProUGUI attackPowerText = default!;
    [SerializeField] private TextMeshProUGUI deploymentCostText = default!;
    [SerializeField] private TextMeshProUGUI defenseText = default!;
    [SerializeField] private TextMeshProUGUI blockCountText = default!;
    [SerializeField] private TextMeshProUGUI magicResistanceText = default!;
    [SerializeField] private TextMeshProUGUI attackSpeedText = default!;

    [Header("Skills")]
    [SerializeField] private Button skill1Button = default!;
    [SerializeField] private Image skill1SelectedIndicator = default!;
    [SerializeField] private Button skill2Button = default!;
    [SerializeField] private Image skill2SelectedIndicator = default!;
    [SerializeField] private TextMeshProUGUI skillDetailText = default!;

    // 화면 오른쪽에 나타나는 사용 가능한 오퍼레이터 리스트 -- 혼동 주의!!
    private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    private OperatorSlot? selectedSlot;

    private BaseSkill? selectedSkill;
    private Sprite noSkillSprite = default!;

    // 오퍼레이터가 들어가 있는 상태에서 해당 슬롯을 수정할 경우에만 사용
    private OwnedOperator? existingOperator;

    // UserSquadManager에서 현재 편집 중인 인덱스
    private int nowEditingIndex;  

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        setEmptyButton.onClick.AddListener(OnSetEmptyButtonClicked);
        detailButton.onClick.AddListener(OnDetailButtonClicked);

        skill1Button.onClick.AddListener(() => OnSkillButtonClicked(0));
        skill2Button.onClick.AddListener(() => OnSkillButtonClicked(1));

        confirmButton.interactable = false;
        detailButton.interactable = false;

        skill1Button.interactable = false;
        skill2Button.interactable = false;

        noSkillSprite = skill1Button.GetComponent<Image>().sprite;
    }

    private void Start()
    {
        // AttackRangeHelper 초기화
        attackRangeHelper = UIHelper.Instance!.CreateAttackRangeHelper(
            attackRangeContainer,
            centerPositionOffset
        );
    }

    private void OnEnable()
    {
        ResetSelection();
        nowEditingIndex = GameManagement.Instance!.UserSquadManager.EditingSlotIndex;

        existingOperator = GameManagement.Instance!.PlayerDataManager.GetOperatorInSlot(nowEditingIndex);
        PopulateOperators();

        // 이미 배치된 오퍼레이터가 있다면 해당 오퍼레이터 슬롯을 클릭한 상태로 시작
        if (existingOperator != null)
        {
            HandleSlotClicked(operatorSlots[0]);
        }
    }


    // 보유한 오퍼레이터 리스트를 만들고 오퍼레이터 슬롯들을 초기화합니다.
    private void PopulateOperators()
    {
        // 슬롯 정리
        ClearSlots();

        // 현재 스쿼드 가져오기
        List<OwnedOperator> currentSquad = GameManagement.Instance!.UserSquadManager.GetCurrentSquad();

        // 보유 중인 오퍼레이터 중, 현재 스쿼드에 없는 오퍼레이터만 가져옴
        List<OwnedOperator> availableOperators = GameManagement.Instance!.PlayerDataManager.GetOwnedOperators()
            .Where(op => !currentSquad.Contains(op))
            .ToList();

        // 이미 오퍼레이터가 있는 슬롯을 클릭해서 들어온 경우, 해당 오퍼레이터도 나타남
        if (existingOperator != null)
        {
            // 가장 앞 인덱스에 넣음
            availableOperators.Insert(0, existingOperator);
        }

        // 그리드 영역의 너비 조절
        RectTransform slotContainerRectTransform = operatorSlotContainer.gameObject.GetComponent<RectTransform>(); 
        if (availableOperators.Count > 12)
        {
            Vector2 currentSize = slotContainerRectTransform.sizeDelta;
            float additionalWidth = 250 * Mathf.Floor( (availableOperators.Count - 12) / 2);
            
            slotContainerRectTransform.sizeDelta = new Vector2(currentSize.x + additionalWidth, currentSize.y);
        }

        // 오퍼레이터 별로 슬롯 생성
        foreach (OwnedOperator op in availableOperators)
        {
            OperatorSlot slot = Instantiate(slotButtonPrefab, operatorSlotContainer);
            slot.Initialize(true, op);
            operatorSlots.Add(slot);
            slot.OnSlotClicked.AddListener(HandleSlotClicked);
        }
    }

    private void HandleSlotClicked(OperatorSlot clickedSlot)
    {
        // 이미 선택된 슬롯 재클릭시 무시 (이거 없으면 무한 이벤트로 인한 스택 오버플로우 뜸)
        if (selectedSlot == clickedSlot) return; 

        // 이전 선택 해제
        if (selectedSlot != null) { selectedSlot.SetSelected(false); }

        // 기존 SideView에 할당된 요소 제거
        ClearSideView();

        // 새로운 선택 처리
        selectedSlot = clickedSlot;
        UpdateSideView(clickedSlot);
        selectedSlot.SetSelected(true);
        confirmButton.interactable = true;
        detailButton.interactable = true;
    }
    
    private void OnConfirmButtonClicked()
    {
        if (selectedSlot != null && 
            selectedSlot.OwnedOperator != null && 
            selectedSkill != null)
        {
            selectedSlot.OwnedOperator.SetStageSelectedSkill(selectedSkill);
            GameManagement.Instance!.UserSquadManager.ConfirmOperatorSelection(selectedSlot.OwnedOperator);
            ReturnToSquadEditPanel();
        }
    }

    private void OnSetEmptyButtonClicked()
    {
        GameManagement.Instance!.UserSquadManager.TryReplaceOperator(nowEditingIndex, null);
        GameManagement.Instance!.UserSquadManager.CancelOperatorSelection(); // 현재 스쿼드의 배치 중인 인덱스를 없앰
        ReturnToSquadEditPanel();
    }

    private void OnDetailButtonClicked()
    {
        if (selectedSlot != null && selectedSlot.OwnedOperator != null)
        {
            GameObject detailPanel = MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.OperatorDetail];
            detailPanel.GetComponent<OperatorDetailPanel>().Initialize(selectedSlot.OwnedOperator);
            MainMenuManager.Instance!.ActivateAndFadeOut(detailPanel, gameObject);
        }
        MainMenuManager.Instance!.ActivateAndFadeOut(MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.OperatorDetail], gameObject);
    }


    private void ResetSelection()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
            detailButton.interactable = false;
            confirmButton.interactable = false;
        }
    }

    private void ClearSlots()
    {
        foreach (OperatorSlot slot in operatorSlots)
        {
            Destroy(slot.gameObject);
        }
        operatorSlots.Clear();
    }


    // 왼쪽 패널의 SideView에 나타나는 오퍼레이터와 관련된 정보를 업데이트한다.
    private void UpdateSideView(OperatorSlot slot)
    {
        OwnedOperator? op = slot.OwnedOperator;
        if (op != null)
        {
            OperatorStats opStats = op.CurrentStats;
            OperatorData opData = op.OperatorProgressData;

            operatorNameText.text = opData.entityName;
            healthText.text = Mathf.Floor(opStats.Health).ToString();
            redeployTimeText.text = Mathf.Floor(opStats.RedeployTime).ToString();
            attackPowerText.text = Mathf.Floor(opStats.AttackPower).ToString();
            deploymentCostText.text = Mathf.Floor(opStats.DeploymentCost).ToString();
            defenseText.text = Mathf.Floor(opStats.Defense).ToString();
            magicResistanceText.text = Mathf.Floor(opStats.MagicResistance).ToString();
            blockCountText.text = Mathf.Floor(opStats.MaxBlockableEnemies).ToString();
            attackSpeedText.text = Mathf.Floor(opStats.AttackSpeed).ToString();

            // 공격 범위 시각화
            attackRangeHelper.ShowBasicRange(op.CurrentAttackableGridPos);

            // 스킬 버튼 초기화 및 설정
            UpdateSkillButtons(slot);
        }
    }

    private void ClearSideView()
    {
        if (attackRangeHelper != null)
        {
            attackRangeHelper.ClearTiles();
        }

        operatorNameText.text = "";
        healthText.text = "";
        redeployTimeText.text = "";
        attackPowerText.text = "";
        deploymentCostText.text = "";
        defenseText.text = "";
        magicResistanceText.text = "";
        blockCountText.text = "";
        attackSpeedText.text = "";

        skill1Button.GetComponent<Image>().sprite = noSkillSprite;
        skill2Button.GetComponent<Image>().sprite = noSkillSprite;
        skill1SelectedIndicator.gameObject.SetActive(false);
        skill2SelectedIndicator.gameObject.SetActive(false);
        skillDetailText.text = "";

        skill1Button.interactable = false;
        skill2Button.interactable = false;

        selectedSkill = null;
    }

    private void UpdateSkillButtons(OperatorSlot slot)
    {
        if (slot.OwnedOperator != null)
        {
            OwnedOperator op = slot.OwnedOperator;

            skill1Button.interactable = true;
            var unlockedSkills = op.UnlockedSkills;
            skill1Button.GetComponent<Image>().sprite = unlockedSkills[0].skillIcon;

            // 디폴트 스킬 설정
            if (selectedSkill == null)
            {
                selectedSkill = slot.SelectedSkill;
                UpdateSkillSelectionIndicator();
                UpdateSkillDescription();
            }

            // 1정예화라면 스킬이 2개일 것
            if (unlockedSkills.Count > 1)
            {
                skill2Button.GetComponent<Image>().sprite = unlockedSkills[1].skillIcon;
                skill2Button.interactable = true;
            }
            else
            {
                skill2Button.interactable = false; 
            }
        }
    }

    private void OnSkillButtonClicked(int skillIndex)
    {
        if (selectedSlot?.OwnedOperator == null) return;

        var skills = selectedSlot.OwnedOperator.UnlockedSkills;
        if (skillIndex < skills.Count)
        {
            selectedSkill = skills[skillIndex];
            selectedSlot.UpdateSelectedSkill(selectedSkill);
            UpdateSkillSelectionIndicator();
            UpdateSkillDescription();
        }
    }

    private void UpdateSkillSelectionIndicator()
    {
        if (selectedSlot?.OwnedOperator == null) return;

        skill1SelectedIndicator.gameObject.SetActive(selectedSkill == selectedSlot.OwnedOperator.UnlockedSkills[0]);

        if (selectedSlot.OwnedOperator.UnlockedSkills.Count > 1)
        {
            skill2SelectedIndicator.gameObject.SetActive(selectedSkill == selectedSlot.OwnedOperator.UnlockedSkills[1]);
        }
    }

    private void UpdateSkillDescription()
    {
        if (selectedSkill != null)
        {
            skillDetailText.text = selectedSkill.description;
        }
        else
        {
            skillDetailText.text = "";
        }
    }

    private void ReturnToSquadEditPanel()
    {
        MainMenuManager.Instance!.ActivateAndFadeOut(MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.SquadEdit], gameObject);
    }


    private void OnDisable()
    {
        ClearSlots();
        ClearSideView();
        ResetSelection();
    }
}
