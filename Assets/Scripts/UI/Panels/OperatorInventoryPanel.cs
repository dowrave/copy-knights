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
    [SerializeField] private Image leftArea = default!;
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
    [SerializeField] private List<SkillIconBox> skillIconBoxes = new List<SkillIconBox>();
    [SerializeField] private Image skillSelectionIndicator = default!;
    [SerializeField] private TextMeshProUGUI skillDetailText = default!;

    // 화면 오른쪽에 나타나는 사용 가능한 오퍼레이터 리스트 -- 혼동 주의!!
    private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    private OperatorSlot? selectedSlot;

    private BaseSkill? selectedSkill;
    private Sprite noSkillSprite = default!;

    // 초기화 시에 커서 위치 관련
    private OwnedOperator? existingOperator; // squadEditPanel의 오퍼레이터가 있는 슬롯을 클릭한 상태로 인벤토리 패널에 들어왔을 때, 유지되는 정보
    private OwnedOperator? editingOperator; // 인벤토리 패널의 상세 정보를 통해 들어갔다가 나올 때


    // UserSquadManager에서 편집 중인 상태 관리
    private int nowEditingIndex;
    private bool isEditing;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        setEmptyButton.onClick.AddListener(OnSetEmptyButtonClicked);
        detailButton.onClick.AddListener(OnDetailButtonClicked);

        for (int i = 0; i < skillIconBoxes.Count; i++)
        {
            int index = i;
            skillIconBoxes[i].OnButtonClicked += () => OnSkillButtonClicked(index);
        }

        confirmButton.interactable = false;
        detailButton.interactable = false;
    }

    private void OnEnable()
    {
        isEditing = GameManagement.Instance!.UserSquadManager.IsEditingSquad;

        SetSquadEditMode(isEditing);

        if (isEditing)
        {
            // Awake에 둘 경우, 이 패널이 활성화된 채로 시작하면 오류 발생해서 여기로 이동
            if (UIHelper.Instance != null)
            {
                attackRangeHelper = UIHelper.Instance.CreateAttackRangeHelper(
                    attackRangeContainer,
                    centerPositionOffset
                );
            }

            ClearSideView();

            ResetSelection();
            nowEditingIndex = GameManagement.Instance!.UserSquadManager.EditingSlotIndex;
            existingOperator = GameManagement.Instance!.PlayerDataManager.GetOperatorInSlot(nowEditingIndex);
            editingOperator = MainMenuManager.Instance!.CurrentEditingOperator;

            PopulateOperators();

            // 자동 선택

            // 1. DetailPanel에 들어갔다 나온 경우 해당 오퍼레이터 선택
            if (editingOperator != null)
            {
                HandleSlotClicked(operatorSlots[0]);
                MainMenuManager.Instance.SetCurrentEditingOperator(null);
            }
            // 2. SquadEditPanel에서 오퍼레이터가 있는 슬롯을 클릭해서 들어온 경우, 해당 오퍼레이터 선택
            else if (existingOperator != null)
            {
                HandleSlotClicked(operatorSlots[0]);
            }
        }
        else
        {
            PopulateOperators();
        }
    }


    // 보유한 오퍼레이터 리스트를 만들고 오퍼레이터 슬롯들을 초기화합니다.
    private void PopulateOperators()
    {
        List<OwnedOperator> availableOperators;

        // 슬롯 정리
        ClearSlots();

        if (isEditing)
        {
            // 현재 스쿼드 가져오기
            List<OwnedOperator> currentSquad = GameManagement.Instance!.UserSquadManager.GetCurrentSquad();

            // 보유 중인 오퍼레이터 중, 현재 스쿼드에 없는 오퍼레이터만 가져옴
            availableOperators = GameManagement.Instance!.PlayerDataManager.GetOwnedOperators()
                .Where(op => !currentSquad.Contains(op))
                .ToList();

            // 스쿼드 편집에서 오퍼레이터가 있는 슬롯을 클릭해서 들어온 경우, 해당 오퍼레이터가 맨 앞
            if (existingOperator != null)
            {
                // 가장 앞 인덱스에 넣음
                availableOperators.Insert(0, existingOperator);
            }

            // 현재 수정한 오퍼레이터가 있다면 슬롯의 맨 앞으로 (existingOperator가 있다면 2번째 순서로 자연스럽게 밀려남)
            if (editingOperator != null)
            {
                availableOperators.Remove(editingOperator);
                availableOperators.Insert(0, editingOperator);
            }


            // 그리드 영역의 너비 조절
            RectTransform slotContainerRectTransform = operatorSlotContainer.gameObject.GetComponent<RectTransform>(); 
            if (availableOperators.Count > 12)
            {
                Vector2 currentSize = slotContainerRectTransform.sizeDelta;
                float additionalWidth = 250 * Mathf.Floor( (availableOperators.Count - 12) / 2);
            
                slotContainerRectTransform.sizeDelta = new Vector2(currentSize.x + additionalWidth, currentSize.y);
            }
        }
        else
        {
            availableOperators = GameManagement.Instance!.PlayerDataManager.GetOwnedOperators().ToList();
        }


        // 오퍼레이터 별로 슬롯 생성
        foreach (OwnedOperator op in availableOperators)
        {
            OperatorSlot slot = Instantiate(slotButtonPrefab, operatorSlotContainer);

            // 이름 변경 - 튜토리얼에서 버튼 이름 추적할 때 필요함
            slot.gameObject.name = $"OperatorSlot({op.operatorName})";

            slot.Initialize(true, op);
            operatorSlots.Add(slot);
            slot.OnSlotClicked.AddListener(HandleSlotClicked);
        }
    }

    private void HandleSlotClicked(OperatorSlot clickedSlot)
    {
        if (isEditing)
        {
            // 스쿼드 편집 중

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
        else
        {
            // 스쿼드 편집 중이 아니라면, 해당 오퍼레이터 세부 정보 패널로 들어감
            MoveToDetailPanel(clickedSlot);
        }
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
        if (selectedSlot != null)
        {
            MoveToDetailPanel(selectedSlot);
            MainMenuManager.Instance.SetCurrentEditingOperator(selectedSlot.OwnedOperator);
        }
    }

    private void MoveToDetailPanel(OperatorSlot slot)
    {
        if (slot.OwnedOperator != null)
        {
            GameObject detailPanel = MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.OperatorDetail];
            detailPanel.GetComponent<OperatorDetailPanel>().Initialize(slot.OwnedOperator);
            MainMenuManager.Instance!.FadeInAndHide(detailPanel, gameObject);
        }
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

        for (int i = 0; i < skillIconBoxes.Count; i++)
        {
            int index = i;
            skillIconBoxes[i].ResetSkillIcon();
            skillIconBoxes[i].SetButtonInteractable(false);
        }

        skillSelectionIndicator.gameObject.SetActive(false);

        skillDetailText.text = "";
        selectedSkill = null;
    }

    private void UpdateSkillButtons(OperatorSlot slot)
    {
        if (slot.OwnedOperator != null)
        {
            OwnedOperator op = slot.OwnedOperator;

            skillIconBoxes[0].Initialize(op.UnlockedSkills[0], true, true);
            skillIconBoxes[0].SetButtonInteractable(true);

            // 디폴트 스킬 설정
            if (selectedSkill == null)
            {
                selectedSkill = slot.SelectedSkill;
                UpdateSkillSelectionIndicator();
                UpdateSkillDescription();
            }

            // 1정예화라면 스킬이 2개일 것
            if (op.UnlockedSkills.Count > 1)
            {
                skillIconBoxes[1].Initialize(op.UnlockedSkills[1], true, true);
                skillIconBoxes[1].SetButtonInteractable(true);
            }
            else
            {
                skillIconBoxes[1].ResetSkillIcon();
                skillIconBoxes[1].SetButtonInteractable(false);
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

        OwnedOperator op = selectedSlot.OwnedOperator;

        Transform targetButtonTransform = null;
        List<BaseSkill> unlockedSkills = op.UnlockedSkills;

        // 첫 번째 스킬이 기본 선택 스킬과 같으면 skillIconBox1의 버튼을 대상으로 함
        if (unlockedSkills.Count > 0 && selectedSkill == unlockedSkills[0])
        { 
            targetButtonTransform = skillIconBoxes[0].transform;
        }
        // 두 번째 스킬이 기본 선택 스킬과 같으면 skillIconBox2의 버튼을 대상으로 함
        else if (unlockedSkills.Count > 1 && selectedSkill == unlockedSkills[1])
        {
            targetButtonTransform = skillIconBoxes[1].transform;
        }

        // 인디케이터 위치 배치
        if (targetButtonTransform != null)
        {
            // 인디케이터를 선택된 스킬 버튼의 첫 번째 자식으로 재배치
            skillSelectionIndicator.transform.SetParent(targetButtonTransform, false);
            skillSelectionIndicator.transform.SetSiblingIndex(0);

            // 위치 이동
            skillSelectionIndicator.transform.localPosition = Vector3.zero; // 위치도 바꿔줌

            // 활성화
            skillSelectionIndicator.gameObject.SetActive(true);
        }
        else
        {
            skillSelectionIndicator.gameObject.SetActive(false);
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

    private void SetSquadEditMode(bool isEditing)
    { 
        // 스쿼드 편집 중 or 단순히 오퍼레이터 상태 보기
        leftArea.gameObject.SetActive(isEditing);
        confirmButton.gameObject.SetActive(isEditing);
        setEmptyButton.gameObject.SetActive(isEditing);
    }

    private void OnDisable()
    {
        ClearSlots();
        ClearSideView();
        ResetSelection();
    }


}
