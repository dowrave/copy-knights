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
    [SerializeField] private GameObject buttonEmptySpace = default!; 
    [SerializeField] private Button setEmptyButton = default!; // 현재 슬롯을 비우는 버튼
    [SerializeField] private Button detailButton = default!; // OperatorDetailPanel로 가는 버튼
    [SerializeField] private Button growthResetButton = default!;

    [Header("Attack Range Visualization")]
    [SerializeField] private RectTransform attackRangeContainer = default!;
    [SerializeField] private float centerPositionOffset = default!; // 타일 시각화 위치를 위한 중심 이동
    [SerializeField] private float tileSize = default!; // 타일의 크기
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

    private List<OwnedOperator> ownedOperators = new List<OwnedOperator>();

    private OperatorSkill? selectedSkill;
    private int selectedSkillIndex;
    private Sprite noSkillSprite = default!;

    // 초기화 시에 커서 위치 관련
    private OwnedOperator? existingOperator; // squadEditPanel의 오퍼레이터가 있는 슬롯을 클릭한 상태로 인벤토리 패널에 들어왔을 때, 유지되는 정보
    private OwnedOperator? editingOperator; // 인벤토리 패널의 상세 정보를 통해 들어갔다가 나올 때


    // 현재 편집 중인 슬롯 인덱스. 상황에 따라 다르게 쓰인다.
    private int nowEditingIndex; 

    // UserSquadManager에서 편집 중인 상태 관리
    private bool isEditingSlot;
    private bool isEditingBulk;

    // 벌크에서만 사용
    private bool isBulkInitializing = false; // 벌크 초기화에서 HandleSlotClicked의 등작을 방지하기 위한 플래그
    private List<SquadOperatorInfo?> tempSquad = new List<SquadOperatorInfo?>();
    private List<OperatorSlot> clickedSlots = new List<OperatorSlot>();

    // HandleSlotClicked의 중복 실행 방지
    private bool isSlotProcessing = false; // 재귀 방지 플래그

    public bool? IsOnSelectionSession => MainMenuManager.Instance?.OperatorSelectionSession;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        setEmptyButton.onClick.AddListener(OnSetEmptyButtonClicked);
        detailButton.onClick.AddListener(OnDetailButtonClicked);
        growthResetButton.onClick.AddListener(OnGrowthResetButtonClicked);

        for (int i = 0; i < skillIconBoxes.Count; i++)
        {
            int index = i;
            skillIconBoxes[i].OnButtonClicked += () => OnSkillButtonClicked(index);
        }
    }

    private void OnEnable()
    {
        // 얕은 패널(SquadEditPanel)에서 들어왔을 때에만 실행
        // 더 깊은 패널에서 돌아왔을 때는 이전 정보를 보존하기 위해 실행하지 않음
        if (IsOnSelectionSession == false)
        {
            confirmButton.interactable = false;
            detailButton.interactable = false;

            // 모든 슬롯 생성
            InitializeAllSlots(); 

            // 모든 슬롯의 스킬을 기본 스킬로 초기화
            SessionInitializeSkillSlot();

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

            MainMenuManager.Instance.SetOperatorSelectionSession(isEditing); // IsOnSession 활성화
        }

        // 세션이 켜져 있는 상태에서 다른 패널에 다녀온 경우
        if (SelectedSlot != null)
        {
            UpdateSideView();
        }
    }

    // 인벤토리만 보거나, 편집을 시작하기 전에 모든 슬롯의 스킬을 기본 설정 스킬로 초기화함
    private void SessionInitializeSkillSlot()
    {
        foreach (OperatorSlot slot in operatorSlots)
        {
            if (slot.OwnedOperator != null && slot.OwnedOperator.UnlockedSkills.Count > 0)
            {
                slot.UpdateSelectedSkill(slot.OwnedOperator.DefaultSelectedSkill);
            }
        }
    }
    
    // 보유한 모든 오퍼레이터들을 슬롯으로 초기화합니다.
    private void InitializeAllSlots()
    {
        ownedOperators = GameManagement.Instance!.PlayerDataManager.OwnedOperators;
        InitializeOperatorSlots();
    }

    // 오퍼레이터 1개 슬롯을 편집하는 상태로 패널 상태를 수정
    private void InitializeSingleSlotEditing()
    {
        InitializeSideView();
        ResetSelection();

        // 1번보다 위에 있어야 할 듯 - existingOperator 때문에
        nowEditingIndex = GameManagement.Instance!.UserSquadManager.EditingSlotIndex;
        existingOperator = GameManagement.Instance!.PlayerDataManager.GetOperatorInSlot(nowEditingIndex); // 스쿼드 슬롯에 있던 오퍼레이터
        editingOperator = MainMenuManager.Instance!.CurrentEditingOperator; // 디테일에 들어갔다 나올 때 저장된 오퍼레이터

        // 1. 보여줄 보유 오퍼레이터 정리
        List<OwnedOperator> operatorsToDisplay = GetAvailableOperatorsForSingleEditing();

        // 2. 모든 슬롯 비활성화
        foreach (OperatorSlot slot in operatorSlots)
        {
            slot.UpdateSelectionIndicator(false);
            slot.gameObject.SetActive(false);
        }

        // 3. 자동 선택할 오퍼레이터 찾기
        OwnedOperator operatorToAutoSelect = null;

        if (editingOperator != null)
        {
            operatorToAutoSelect = editingOperator;
            MainMenuManager.Instance.SetCurrentEditingOperator(null);
        }
        else if (existingOperator != null)
        {
            operatorToAutoSelect = existingOperator;
        }

        // 선택해야 할 오퍼레이터를 0번 인덱스로 보냄
        if (operatorToAutoSelect != null)
        {
            if (operatorsToDisplay.Contains(operatorToAutoSelect))
            {
                operatorsToDisplay.Remove(operatorToAutoSelect);
                operatorsToDisplay.Insert(0, operatorToAutoSelect);
            }
            else
            {
                throw new System.InvalidOperationException("오퍼레이터 필터링이 제대로 안 된 듯?");
            }
        }

        // 4. operatorsToDisplay에 따라 슬롯 활성화 및 순서 지정
        int visibleSlotOrder = 0;
        foreach (OwnedOperator opToShow in operatorsToDisplay)
        {
            // 자동으로 선택되는 오퍼레이터의 스킬은 현재 스쿼드에서 사용 중인 스킬로 선택됨
            if (operatorToAutoSelect != null && opToShow == operatorToAutoSelect)
            {
                selectedSkillIndex = GameManagement.Instance!.UserSquadManager.GetCurrentSkillIndex(operatorToAutoSelect);
            }
            // 나머지는 오퍼레이터의 기본 선택 스킬을 사용
            else
            {
                selectedSkillIndex = opToShow.DefaultSelectedSkillIndex;
            }

            OperatorSlot slotToActivate = operatorSlots.FirstOrDefault(s => s.OwnedOperator == opToShow);

            if (slotToActivate != null)
            {
                slotToActivate.gameObject.SetActive(true);

                // 슬롯의 배치 순서 지정
                slotToActivate.transform.SetSiblingIndex(visibleSlotOrder++);

                // 슬롯의 스킬 선택 상태 초기화  
                slotToActivate.UpdateSelectedSkill(opToShow.UnlockedSkills[selectedSkillIndex]);
            }
        }

        // 5. 슬롯 자동 선택 상황
        // 0번 인덱스를 선택해도 되지만, 명확성 + 견고함 + 디커플링을 위해 "찾아야 하는 슬롯"을 명확하게 정의하는 방식으로 수정
        if (operatorToAutoSelect != null)
        {
            OperatorSlot slotToClick = operatorSlots.FirstOrDefault(s => s.gameObject.activeSelf && s.OwnedOperator == operatorToAutoSelect);
            if (slotToClick != null)
            {
                HandleSlotClicked(slotToClick);
            }
        }
    }

    // 스쿼드 전체를 편집하는 상태로 패널 상태를 수정
    private void InitializeBulkEditing()
    {
        isBulkInitializing = true;

        InitializeSideView();
        ResetSelection();

        // 1. 보여줄 오퍼레이터 정리
        List<OwnedOperator> operatorsToDisplay = GetAvailableOperatorsForBulkEditing();

        // 2. 모든 슬롯 비활성화
        foreach (OperatorSlot slot in operatorSlots)
        {
            slot.UpdateSelectionIndicator(false);
            slot.gameObject.SetActive(false);
        }

        // 3. operatorsToDisplay에 따른 슬롯 활성화 및 순서 지정
        int visibleSlotOrder = 0;
        foreach (OwnedOperator opToShow in operatorsToDisplay)
        {
            OperatorSlot slotToActivate = operatorSlots.FirstOrDefault(s => s.OwnedOperator == opToShow);
            if (slotToActivate != null)
            {
                slotToActivate.gameObject.SetActive(true);
                slotToActivate.transform.SetSiblingIndex(visibleSlotOrder++);
            }
        }

        // 4. clickedSlots 리스트 초기화
        int maxSquadSize = GameManagement.Instance!.UserSquadManager.MaxSquadSize;
        clickedSlots.Clear();
        for (int i=0; i < maxSquadSize; i++)
        {
            clickedSlots.Add(null);
        }
        
        // 5. tempSquad로 clickedSlot 채우기
        for (int i = 0; i < tempSquad.Count; i++)
        {
            if (tempSquad[i] != null)
            {
                SquadOperatorInfo opInfo = tempSquad[i];
                OperatorSlot slot = operatorSlots.FirstOrDefault(s => s.OwnedOperator == opInfo.op);
                if (slot != null && slot.gameObject.activeSelf) // 활성화된 슬롯에 대해
                {
                    // 슬롯을 선택된 상태로 만듦
                    slot.SetSelected(true, idx: i);
                    clickedSlots[i] = slot;

                    if (opInfo.skillIndex >= 0 && opInfo.skillIndex < slot.OwnedOperator.UnlockedSkills.Count)
                    {
                        slot.UpdateSelectedSkill(slot.OwnedOperator.UnlockedSkills[opInfo.skillIndex]);
                    }

                    // 마지막으로 클릭된 슬롯에 대해 사이드뷰 활성화
                    SelectedSlot = slot;
                }
            }
        }

        // 6. 현재 편집할 슬롯 인덱스 설정
        InitializeNowEditingIndexForBulk();

        // 7. 확인 버튼 상태 업데이트
        UpdateConfirmButtonState();

        isBulkInitializing = false;
    }

    private void InitializeSideView()
    {
        if (UIHelper.Instance != null)
        {
            // Awake에 둘 경우, 이 패널이 활성화된 채로 시작하면 오류 발생해서 OnEnable에서 동작하도록 수정
            attackRangeHelper = UIHelper.Instance.CreateAttackRangeHelper(
                attackRangeContainer,
                centerPositionOffset,
                tileSize // 타일 크기
            );
        }

        ClearSideView();
    }

    // availableOperators으로 operatorSlot들을 생성합니다. 
    private void InitializeOperatorSlots()
    {
        ClearSlots();

        foreach (OwnedOperator op in ownedOperators)
        {
            OperatorSlot slot = Instantiate(slotButtonPrefab, operatorSlotContainer);

            slot.Initialize(true, op);
            operatorSlots.Add(slot);
            slot.OnSlotClicked.AddListener(HandleSlotClicked);
        }

        AdjustSlotContainerSize(ownedOperators);
    }

    // 표시할 보유 오퍼레이터 필터링
    private List<OwnedOperator> GetAvailableOperatorsForSingleEditing()
    {
        // 현재 스쿼드 가져오기
        List<SquadOperatorInfo> currentSquad = GameManagement.Instance!.UserSquadManager.GetCurrentSquad();

        // 보유 중인 오퍼레이터 중, 현재 스쿼드에 없는 오퍼레이터만 가져옴
        List<OwnedOperator> availableOperators = ownedOperators.Where(ownedOp => !currentSquad.Any(squadInfo => squadInfo.op == ownedOp)) 
            .ToList();

        // 스쿼드 편집에서 오퍼레이터가 있는 슬롯을 클릭해서 들어온 경우, 해당 오퍼레이터를 맨 앞에 배치
        if (existingOperator != null)
        {
            availableOperators.Add(existingOperator);
        }

        return availableOperators;
    }

    // 
    private List<OwnedOperator> GetAvailableOperatorsForBulkEditing()
    {
        // 1. tempSquad를 현재 스쿼드 상태로 채움
        tempSquad = GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();

        // 2. 순서에 따른 스쿼드 오퍼레이터 가져오기
        List<OwnedOperator> squadOperatorsInOrder = tempSquad
            .Where(squadInfo => squadInfo != null)
            .Select(squadInfo => squadInfo.op)
            .ToList();

        // 3. 스쿼드에 이미 있는 오퍼레이터는 제외
        List<OwnedOperator> allOwnedOperatorsCopy = new List<OwnedOperator>(ownedOperators);
        foreach (var opInSquad in squadOperatorsInOrder)
        {
            allOwnedOperatorsCopy.Remove(opInSquad);
        }

        // 4. 최종 리스트 : 스쿼드 멤버(순서 유지) + 나머지 보유 오퍼레이터
        List<OwnedOperator> displayOrder = new List<OwnedOperator>(squadOperatorsInOrder);
        displayOrder.AddRange(allOwnedOperatorsCopy);

        return displayOrder;
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

                // 새로운 선택 처리
                SelectedSlot = clickedSlot;
                SelectedSlot.SetSelected(true);
                confirmButton.interactable = true;
                
            }
            else if (isEditingBulk)
            {
                // 스쿼드 전체 편집 모드
                HandleSlotClickedForBulk(clickedSlot);
            }
            else
            {
                // 스쿼드 편집 중이 아니라면, 해당 오퍼레이터 세부 정보 패널로 바로 이동
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
        // 벌크 초기화 중에는 동작을 막음
        if (isBulkInitializing) return;

        int clickedSlotCurrentIndex = -1; // 값이 있으면 선택 추가, 아니면 선택 해제
        for (int i = 0; i < clickedSlots.Count; ++i)
        {
            if (clickedSlots[i] == clickedSlot)
            {
                clickedSlotCurrentIndex = i;
                break;
            }
        }

        // 슬롯 클릭 상태 해제 로직
        if (clickedSlotCurrentIndex != -1)
        {
            // tempSquad에서의 할당 해제
            tempSquad[clickedSlotCurrentIndex] = null;
            clickedSlots[clickedSlotCurrentIndex] = null;

            // 슬롯 UI 업데이트
            clickedSlot.SetSelected(false); 

            // 현재 선택된 슬롯 해제
            SelectedSlot = null;

            // 인덱스 갱신
            SetNowEditingIndexForBulk();
        }
        // 클릭 상태 추가 로직
        else
        {
            // 새 오퍼레이터를 리스트의 어디에 넣을 것인가를 결정한다.
            SetNowEditingIndexForBulk(); 

            if (nowEditingIndex < tempSquad.Count)
            {
                tempSquad[nowEditingIndex] = new SquadOperatorInfo(clickedSlot.OwnedOperator, clickedSlot.OwnedOperator.DefaultSelectedSkillIndex);
                clickedSlots[nowEditingIndex] = clickedSlot;

                // 재귀호출되지만 플래그 때문에 더 진행되진 않음
                clickedSlot.SetSelected(true, nowEditingIndex);
                
                SelectedSlot = clickedSlot;
            }
            else
            {
                // 스쿼드가 꽉 찼는데 새 슬롯을 선택하려는 경우
                throw new System.InvalidOperationException("스쿼드가 꽉 찼음");
            }
        }

        // 확인 버튼 UI 업데이트
        UpdateConfirmButtonState();
    }

    // 스쿼드의 null이 아닌 가장 마지막 인덱스를 nowEditingIndex로 지정함
    private void InitializeNowEditingIndexForBulk()
    {
        int maxSquadSize = GameManagement.Instance!.UserSquadManager.MaxSquadSize;
        for (int i = 0; i < maxSquadSize; i++)
        {
            if (tempSquad[i] != null)
            {
                nowEditingIndex = i;
            }
        }
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
                return;
            }
        }        
    }

    private void OnConfirmButtonClicked()
    {
        if (isEditingSlot)
        {
            if (SelectedSlot.OwnedOperator != null && selectedSkillIndex != -1)
            {
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

    private void OnGrowthResetButtonClicked()
    {
        // 정말 초기화할 것인지에 대한 경고 패널을 보여줌
        ConfirmationPopup popup = PopupManager.Instance!.ShowConfirmationPopup("모든 오퍼레이터의 육성 상태를 0정예화 1레벨로 초기화합니다.\n초기화를 진행하시겠습니까?", 
            isCancelButton: true, 
            blurAreaActivation: true,
            onConfirm:  () => {
                // 모든 오퍼레이터 성장 정보 초기화 
                OperatorGrowthManager.Instance.ResetAllOperatorsGrowth();

                // 슬롯 UI 갱신
                foreach (OperatorSlot slot in operatorSlots)
                {
                    slot.UpdateActiveSlotVisuals(); // 스킬 외의 성장 정보들 업데이트
                    slot.InitializeSkill(); // 스킬 정보 업데이트
                }

                // 우측 상단 알림 표시
                NotificationToastManager.Instance!.ShowNotification($"모든 오퍼레이터 레벨 초기화 및 육성 재화 회수 완료");

                UpdateSideView();
            }
        );
    }

    private void MoveToDetailPanel(OperatorSlot slot)
    {
        if (slot.OwnedOperator != null)
        {
            GameObject detailPanel = MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.OperatorDetail];
            detailPanel.GetComponent<OperatorDetailPanel>().Initialize(slot.OwnedOperator);
            MainMenuManager.Instance!.ChangePanel(detailPanel, gameObject);
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
    // SelectedSlot이 업데이트되면 자동 실행
    private void UpdateSideView()
    {
        if (SelectedSlot != null) 
        {
            OwnedOperator? op = SelectedSlot.OwnedOperator;
            if (op != null)
            {
                OperatorStats opStats = op.CurrentStats;
                OperatorData opData = op.OperatorData;

                operatorNameText.text = GameManagement.Instance!.LocalizationManager.GetText(opData.EntityNameLocalizationKey);
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

                // 스킬 버튼 초기화 및 선택된 스킬 설정
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

        detailButton.interactable = false;
    }

    private void UpdateSkillButtons(OperatorSlot slot)
    {
        if (slot.OwnedOperator != null)
        {
            OwnedOperator op = slot.OwnedOperator;

            // 기본 스킬 초기화
            skillIconBoxes[0].Initialize(op.UnlockedSkills[0],
                                            showDurationBox: true,
                                            showSkillName: true);
            skillIconBoxes[0].SetButtonInteractable(true);

            // 1정예화라면 스킬이 2개일 것  
            if (op.UnlockedSkills.Count > 1)
            {
                skillIconBoxes[1].Initialize(op.UnlockedSkills[1], 
                                                showDurationBox: true, 
                                                showSkillName: true);
                skillIconBoxes[1].SetButtonInteractable(true);
            }
            else
            {
                skillIconBoxes[1].ResetSkillIcon();
                skillIconBoxes[1].SetButtonInteractable(false);
            }

            // 스킬 선택 로직

            // 슬롯에 할당된 스킬을 가져오는 게 기본
            selectedSkillIndex = op.UnlockedSkills.IndexOf(slot.SelectedSkill);
            selectedSkill = op.UnlockedSkills[selectedSkillIndex];
            UpdateSkillSelectionIndicator();
            UpdateSkillDescription();
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
        List<OperatorSkill> unlockedSkills = op.UnlockedSkills;

        // 인디케이터의 Transform 지정
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
            bool activeCondition = tempSquad.Count > 0 && clickedSlots.Count > 0;
            confirmButton.interactable = activeCondition;
        }
    }

    private void ReturnToSquadEditPanel()
    {
        MainMenuManager.Instance!.SetOperatorSelectionSession(false);
        MainMenuManager.Instance!.ChangePanel(MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.SquadEdit], gameObject);
    }

    private void SetSquadEditMode(bool isEditing)
    { 
        // 스쿼드 편집 중 or 단순히 오퍼레이터 상태 보기
        leftArea.gameObject.SetActive(isEditing);
        confirmButton.gameObject.SetActive(isEditing);
        buttonEmptySpace.SetActive(isEditing);
        setEmptyButton.gameObject.SetActive(isEditing);
    }

    private void OnDisable()
    { 
        // 세션이 먼저 꺼지고 패널이 꺼지므로 유효함
        if (IsOnSelectionSession == false) 
        {
            // 순서 중요
            ResetSelectionUI();
            ResetPanelState();
        }
    }

    // 세션에서 "현재 편집 중인"에 관한 정보들을 초기화함
    private void ResetPanelState()
    {
        // 스킬 상태 초기화
        selectedSkillIndex = -1;
        selectedSkill = null;

        // "편집 중" 오퍼레이터들 초기화
        existingOperator = null;
        editingOperator = null;

        nowEditingIndex = -1;
        SelectedSlot = null;

        // 벌크 관련 상태들 초기화
        tempSquad.Clear();
        clickedSlots.Clear();

        // 상태 초기화
        GameManagement.Instance!.UserSquadManager.ResetIsEditingSlotIndex();
        GameManagement.Instance!.UserSquadManager.SetIsEditingBulk(false);
    }

    private void ResetSelectionUI()
    {
        foreach (OperatorSlot slot in operatorSlots.Where(slot => slot != null))
        {
            slot.UpdateSelectionIndicator(false);
        }

        foreach (OperatorSlot slot in clickedSlots.Where(slot => slot != null))
        {
            Logger.Log(slot.opData?.EntityID);
            slot.ClearIndexText();
        }

    }

    private void OnDestroy()
    {
        confirmButton.onClick.RemoveAllListeners();
        setEmptyButton.onClick.RemoveAllListeners();
        detailButton.onClick.RemoveAllListeners();

        foreach (OperatorSlot slot in operatorSlots)
        {
            slot.OnSlotClicked.RemoveAllListeners();
            Destroy(slot.gameObject);
        }
    }
}
