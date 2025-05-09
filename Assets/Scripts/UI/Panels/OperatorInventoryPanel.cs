using System.Collections.Generic;
using System.Linq;
using Skills.Base;
using TMPro;
using Unity.VisualScripting;
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
    private OperatorSlot? _selectedSlot;
    public OperatorSlot? SelectedSlot
    {
        get { return _selectedSlot; }
        set
        {
            if (_selectedSlot != value)
            {
                _selectedSlot = value;
                UpdateSideView();
            }
        }
    }

    private List<OwnedOperator> availableOperators = new List<OwnedOperator>();

    private BaseSkill? selectedSkill;
    private int selectedSkillIndex;
    private Sprite noSkillSprite = default!;

    // 초기화 시에 커서 위치 관련
    private OwnedOperator? existingOperator; // squadEditPanel의 오퍼레이터가 있는 슬롯을 클릭한 상태로 인벤토리 패널에 들어왔을 때, 유지되는 정보
    private OwnedOperator? editingOperator; // 인벤토리 패널의 상세 정보를 통해 들어갔다가 나올 때


    /* 
    현재 편집 중인 슬롯 인덱스. 상황에 따라 다르게 쓰인다.
    bulk의 경우 슬롯이 클릭된 시점에 지정되는 게 맞음(다음 인덱스를 미리 갖는 게 아니라)
    */
    private int nowEditingIndex; 

    // UserSquadManager에서 편집 중인 상태 관리
    private bool isEditingSlot;
    private bool isEditingBulk;

    // 벌크에서만 사용
    private List<SquadOperatorInfo?> tempSquad = new List<SquadOperatorInfo?>();
    private List<OperatorSlot> clickedSlots = new List<OperatorSlot>();

    // 둘다 HandleSlotClicked 떄문에 구현
    private bool isSlotProcessing = false; // 재귀 방지 플래그
    private bool isInitializing = false; // Bulk에서 사용. 초기화 중에 이벤트 핸들러의 동작 방지를 위한 플래그

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
        isInitializing = true;

        isEditingSlot = GameManagement.Instance!.UserSquadManager.IsEditingSlot;
        isEditingBulk = GameManagement.Instance!.UserSquadManager.IsEditingBulk;

        bool isEditing = isEditingSlot || isEditingBulk;
        SetSquadEditMode(isEditing);

        if (isEditingSlot)
        {
            InitializeSingleSlotEditing();
        }
        else if (isEditingBulk)
        {
            InitializeBulkEditing();
        }
        else
        {
            availableOperators = GameManagement.Instance!.PlayerDataManager.GetOwnedOperators().ToList();
            InitializeOperatorSlots();
        }

        isInitializing = false;
    }

    // 오퍼레이터 1개 슬롯을 편집하는 상태로 패널 상태를 수정
    private void InitializeSingleSlotEditing()
    {
        InitializeSideView();
        ResetSelection();

        nowEditingIndex = GameManagement.Instance!.UserSquadManager.EditingSlotIndex;
        existingOperator = GameManagement.Instance!.PlayerDataManager.GetOperatorInSlot(nowEditingIndex); // 스쿼드 슬롯에 있던 오퍼레이터
        editingOperator = MainMenuManager.Instance!.CurrentEditingOperator; // 디테일에 들어갔다 나올 때 저장된 오퍼레이터

        availableOperators = GetAvailableOperatorsForSingleEditing();
        InitializeOperatorSlots();

        // 슬롯 자동 선택 상황
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

    // 스쿼드 전체를 편집하는 상태로 패널 상태를 수정
    private void InitializeBulkEditing()
    {
        InitializeSideView();
        ResetSelection();

        availableOperators = GetAvailableOperatorsForBulkEditing();
        AdjustSlotContainerSize(availableOperators);
        InitializeOperatorSlots();

        // 기존 스쿼드 표시 및 선택 상태로 지정
        nowEditingIndex = 0;
        int maxSquadSize = GameManagement.Instance!.UserSquadManager.MaxSquadSize;
        

        for (int i = 0; i < maxSquadSize; i++)
        {
            // 슬롯 스쿼드 갯수만큼 초기화
            clickedSlots.Add(null);
        }
        
        // tempSquad의 인덱스와 동일한 곳에 같은 내용의 slot을 채움
        for (int i = 0; i < maxSquadSize; i++)
        {
            if (tempSquad[i] != null)
            {
                OperatorSlot slot = operatorSlots.FirstOrDefault(s => s.OwnedOperator == tempSquad[i].op);
                if (slot != null)
                {
                    // 슬롯 선택된 상태로 지정
                    slot.SetSelected(true, i); // HandleSlotClicked 동작함(내부 이벤트 구독 때문에)
                    clickedSlots[i] = slot;

                    // 슬롯 우측 상단에 i + 1 표시 (나중에 구현)
                }
            }
        }

        UpdateConfirmButtonState();
    }

    private void InitializeSideView()
    {
        if (UIHelper.Instance != null)
        {
            // Awake에 둘 경우, 이 패널이 활성화된 채로 시작하면 오류 발생해서 OnEnable에서 동작하도록 수정
            attackRangeHelper = UIHelper.Instance.CreateAttackRangeHelper(
                attackRangeContainer,
                centerPositionOffset
            );
        }

        ClearSideView();
    }

    // availableOperators으로 operatorSlot들을 생성합니다. 
    private void InitializeOperatorSlots()
    {
        ClearSlots();

        foreach (OwnedOperator op in availableOperators)
        {
            OperatorSlot slot = Instantiate(slotButtonPrefab, operatorSlotContainer);

            // 이름 변경 - 튜토리얼에서 버튼 이름 추적할 때 필요함
            slot.gameObject.name = $"OperatorSlot({op.operatorName})";

            slot.Initialize(true, op);
            operatorSlots.Add(slot);
            slot.OnSlotClicked.AddListener(HandleSlotClicked);
        }

        AdjustSlotContainerSize(availableOperators);
    }

    private List<OwnedOperator> GetAvailableOperatorsForSingleEditing()
    {
        ClearSlots();

        // 현재 스쿼드 가져오기
        List<SquadOperatorInfo> currentSquad = GameManagement.Instance!.UserSquadManager.GetCurrentSquad();

        // 보유 중인 오퍼레이터 중, 현재 스쿼드에 없는 오퍼레이터만 가져옴
        List<OwnedOperator> availableOperators = GameManagement.Instance!.PlayerDataManager.GetOwnedOperators()
            .Where(ownedOp => !currentSquad.Any(squadInfo => squadInfo.op == ownedOp)) 
            .ToList();

        // 스쿼드 편집에서 오퍼레이터가 있는 슬롯을 클릭해서 들어온 경우, 해당 오퍼레이터를 맨 앞에 배치
        if (existingOperator != null)
        {
            availableOperators.Insert(0, existingOperator);
        }

        // 현재 수정한 오퍼레이터가 있다면 슬롯의 맨 앞으로 배치 (기존 existingOperator가 있다면 2번째로 밀림)
        if (editingOperator != null)
        {
            availableOperators.Remove(editingOperator);
            availableOperators.Insert(0, editingOperator);
        }

        return availableOperators;
    }

    private List<OwnedOperator> GetAvailableOperatorsForBulkEditing()
    {
        // 현재 스쿼드에 있는 오퍼레이터들을 순서대로 배열
        tempSquad = GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();

        //selectedOperators = new List<OwnedOperator?>(currentSquadWithNull);

        // OwnedOperator? 리스트 추출
        List<OwnedOperator?> currentOpWithNull = tempSquad
            .Select(squadInfo => squadInfo?.op)
            .ToList();

        // null이 아닌 오퍼레이터만 추출 (순서 유지)
        List<OwnedOperator> squadOperators = currentOpWithNull
            .Where(op => op != null)
            //.Cast<OwnedOperator>()
            .Select(op => op!)
            .ToList();

        List<OwnedOperator> allOperators = GameManagement.Instance!.PlayerDataManager.GetOwnedOperators().ToList();

        // 이미 스쿼드에 배치된 오퍼레이터들을 리스트에서 제거
        foreach (var op in squadOperators)
        {
            allOperators.Remove(op);
        }

        // 최종) 현재 스쿼드에 있는 오퍼레이터가 앞, 나머지 오퍼레이터가 뒤로 오는 배치
        return squadOperators.Concat(allOperators).ToList();
    }

    private void AdjustSlotContainerSize(List<OwnedOperator> availableOperators)
    {
        // 그리드 영역의 너비 조절
        RectTransform slotContainerRectTransform = operatorSlotContainer.gameObject.GetComponent<RectTransform>();
        if (availableOperators.Count > 12)
        {
            Vector2 currentSize = slotContainerRectTransform.sizeDelta;
            float additionalWidth = 250 * Mathf.Floor((availableOperators.Count - 12) / 2);
            slotContainerRectTransform.sizeDelta = new Vector2(currentSize.x + additionalWidth, currentSize.y);
        }
    }

    private void HandleSlotClicked(OperatorSlot clickedSlot)
    {
        // 슬롯 클릭 로직의 이벤트 발생 -> 이 메서드 재귀호출 방지 플래그
        if (isSlotProcessing) return;
        isSlotProcessing = true;
        try
        {
            if (isEditingSlot)
            {
                // 단일 슬롯 편집 

                // 이전 선택 해제
                if (SelectedSlot != null)
                {
                    SelectedSlot.SetSelected(false);
                    SelectedSlot = null;
                }

                // 기존 SideView에 할당된 요소 제거
                ClearSideView();

                // 새로운 선택 처리
                SelectedSlot = clickedSlot;
                //UpdateSideView();
                SelectedSlot.SetSelected(true);
                confirmButton.interactable = true;
                
            }
            else if (isEditingBulk)
            {
                // 스쿼드 전체 편집 모드
                ClearSideView();
                HandleSlotClickedForBulk(clickedSlot);
            }
            else
            {
                // 스쿼드 편집 중이 아니라면, 해당 오퍼레이터 세부 정보 패널로 이동
                MoveToDetailPanel(clickedSlot);
            }
        }
        finally
        {
            // 중간에 에러가 발생하더라도 슬롯 처리 초기화는 진행되도록 함
            isSlotProcessing = false;
        }
    }

    private void HandleSlotClickedForBulk(OperatorSlot clickedSlot)
    {
        // 초기화 중에는 동작을 막음
        if (isInitializing) return;

        // 슬롯 클릭 상태 해제 로직
        if (clickedSlots.Contains(clickedSlot))
        {
            // tempSquad에서의 할당 해제
            int index = tempSquad.FindIndex(opInfo => opInfo?.op == clickedSlot.OwnedOperator);
            if (index != -1)
            {
                tempSquad[index] = null;
                clickedSlots[index] = null; // clickedSlot에서도 할당 해제

                // 다음 인덱스 지정
                SetNowEditingIndexForBulk();
            }

            ClearSideView();
            SelectedSlot = null;
            clickedSlot.SetSelected(false);
        }
        // 클릭 상태 추가 로직
        else
        {
            SetNowEditingIndexForBulk(); // 현재 편집 중인 인덱스 설정

            clickedSlot.SetSelected(true, nowEditingIndex); // 재귀호출되지만 플래그 때문에 더 진행되진 않음

            tempSquad[nowEditingIndex] = new SquadOperatorInfo(clickedSlot.OwnedOperator, clickedSlot.OwnedOperator.DefaultSelectedSkillIndex);
            clickedSlots[nowEditingIndex] = clickedSlot;
            SelectedSlot = clickedSlot;
            UpdateSideView();
        }

        // 확인 버튼 UI 업데이트
        UpdateConfirmButtonState();
    }
    
    // tempSquad에서 null인 가장 낮은 인덱스를 현재 설정 중인 인덱스로 설정함
    private void SetNowEditingIndexForBulk()
    {
        int maxSquadSize = GameManagement.Instance!.UserSquadManager.MaxSquadSize;
        for (int i = 0; i < maxSquadSize; i++)
        {
            if (tempSquad[i] == null)
            {
                nowEditingIndex = i;
                break;
            }
        }
    }

    private void OnConfirmButtonClicked()
    {
        if (isEditingSlot)
        {
            if (SelectedSlot.OwnedOperator != null && selectedSkillIndex != -1)
            {
                //SelectedSlot.OwnedOperator.SetStageSelectedSkill(selectedSkill); // 위치 변경 시도중
                GameManagement.Instance!.UserSquadManager.ConfirmOperatorSelection(SelectedSlot.OwnedOperator, selectedSkillIndex);
            }
        }
        else if (isEditingBulk)
        {
            // 현재 선택된 오퍼레이터들 + 각각의 스킬을 이용해 스쿼드에 전달
            GameManagement.Instance!.UserSquadManager.UpdateFullSquad(tempSquad);
        }
        ReturnToSquadEditPanel();
    }

    private void OnSetEmptyButtonClicked()
    {
        if (isEditingSlot)
        {
            GameManagement.Instance!.UserSquadManager.TryReplaceOperator(nowEditingIndex, null);
            GameManagement.Instance!.UserSquadManager.CancelOperatorSelection(); // 현재 스쿼드의 배치 중인 인덱스를 없앰
            ReturnToSquadEditPanel();
        }
        else if (isEditingBulk)
        {
            // 벌크에서는 clickedSlot, tempSquads을 초기화시키기만 하고 되돌아가지 않음
            for (int i = 0; i < clickedSlots.Count; i++)
            {
                clickedSlots[i]?.SetSelected(false);
            }
        }
    }

    private void OnDetailButtonClicked()
    {
        if (SelectedSlot != null)
        {
            MoveToDetailPanel(SelectedSlot);
            MainMenuManager.Instance.SetCurrentEditingOperator(SelectedSlot.OwnedOperator);
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
        if (SelectedSlot != null)
        {
            SelectedSlot.SetSelected(false);
            SelectedSlot = null;
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
    private void UpdateSideView()
    {
        if (SelectedSlot != null) 
        {
            OwnedOperator? op = SelectedSlot.OwnedOperator;
            if (op != null)
            {
                OperatorStats opStats = op.CurrentStats;
                OperatorData opData = op.OperatorProgressData;

                operatorNameText.text = opData.entityName;
                healthText.text = Mathf.FloorToInt(opStats.Health).ToString();
                redeployTimeText.text = Mathf.FloorToInt(opStats.RedeployTime).ToString();
                attackPowerText.text = Mathf.FloorToInt(opStats.AttackPower).ToString();
                deploymentCostText.text = Mathf.FloorToInt(opStats.DeploymentCost).ToString();
                defenseText.text = Mathf.FloorToInt(opStats.Defense).ToString();
                magicResistanceText.text = Mathf.FloorToInt(opStats.MagicResistance).ToString();
                blockCountText.text = Mathf.FloorToInt(opStats.MaxBlockableEnemies).ToString();
                attackSpeedText.text = opStats.AttackSpeed.ToString("F2");

                // 공격 범위 시각화
                attackRangeHelper.ShowBasicRange(op.CurrentAttackableGridPos);

                // 스킬 버튼 초기화 및 설정
                UpdateSkillButtons(SelectedSlot);

                // 디테일 이동 버튼 활성화
                detailButton.interactable = true;
            }
        }
        else
        {
            ClearSideView();
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
        selectedSkillIndex = -1;
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
                selectedSkillIndex = op.UnlockedSkills.IndexOf(slot.SelectedSkill);
                selectedSkill = op.UnlockedSkills[selectedSkillIndex];
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
        if (SelectedSlot?.OwnedOperator == null) return;

        var skills = SelectedSlot.OwnedOperator.UnlockedSkills;

        if (skillIndex >= 0 && skillIndex < skills.Count)
        {
            selectedSkillIndex = skillIndex;
            selectedSkill = skills[selectedSkillIndex];
            SelectedSlot.UpdateSelectedSkill(selectedSkill);
            UpdateSkillSelectionIndicator();
            UpdateSkillDescription();

            if (isEditingBulk)
            {
                tempSquad[nowEditingIndex].skillIndex = selectedSkillIndex;
            }
        }
    }

    private void UpdateSkillSelectionIndicator()
    {
        if (SelectedSlot?.OwnedOperator == null) return;

        OwnedOperator op = SelectedSlot.OwnedOperator;

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

    private void UpdateConfirmButtonState()
    {
        if (isEditingBulk)
        {
            bool activeCondition = (tempSquad.Count > 0 && clickedSlots.Count > 0);
            confirmButton.interactable = activeCondition;
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

        //GameManagement.Instance!.UserSquadManager.SetIsBulkEditing(false);
    }
}
