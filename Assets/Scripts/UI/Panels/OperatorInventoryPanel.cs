using System.Collections.Generic;
using System.Linq;
using Skills.Base;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


// ������ ���۷����͵��� �����ִ� �г�. �����带 �����ϴ� �г��� SquadEditPanel�� ȥ���� �����Ͻÿ� 
public class OperatorInventoryPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image leftArea = default!;
    [SerializeField] private Transform operatorSlotContainer = default!;
    [SerializeField] private TextMeshProUGUI operatorNameText = default!;
    [SerializeField] private OperatorSlot slotButtonPrefab = default!;
    [SerializeField] private Button confirmButton = default!;
    [SerializeField] private Button setEmptyButton = default!; // ���� ������ ���� ��ư
    [SerializeField] private Button detailButton = default!; // OperatorDetailPanel�� ���� ��ư

    [Header("Attack Range Visualization")]
    [SerializeField] private RectTransform attackRangeContainer = default!;
    [SerializeField] private float centerPositionOffset = default!; // Ÿ�� �ð�ȭ ��ġ�� ���� �߽� �̵�
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

    // ȭ�� �����ʿ� ��Ÿ���� ��� ������ ���۷����� ����Ʈ -- ȥ�� ����!!
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

    // �ʱ�ȭ �ÿ� Ŀ�� ��ġ ����
    private OwnedOperator? existingOperator; // squadEditPanel�� ���۷����Ͱ� �ִ� ������ Ŭ���� ���·� �κ��丮 �гο� ������ ��, �����Ǵ� ����
    private OwnedOperator? editingOperator; // �κ��丮 �г��� �� ������ ���� ���ٰ� ���� ��


    /* 
    ���� ���� ���� ���� �ε���. ��Ȳ�� ���� �ٸ��� ���δ�.
    bulk�� ��� ������ Ŭ���� ������ �����Ǵ� �� ����(���� �ε����� �̸� ���� �� �ƴ϶�)
    */
    private int nowEditingIndex; 

    // UserSquadManager���� ���� ���� ���� ����
    private bool isEditingSlot;
    private bool isEditingBulk;

    // ��ũ������ ���
    private List<SquadOperatorInfo?> tempSquad = new List<SquadOperatorInfo?>();
    private List<OperatorSlot> clickedSlots = new List<OperatorSlot>();

    // �Ѵ� HandleSlotClicked ������ ����
    private bool isSlotProcessing = false; // ��� ���� �÷���
    private bool isInitializing = false; // Bulk���� ���. �ʱ�ȭ �߿� �̺�Ʈ �ڵ鷯�� ���� ������ ���� �÷���

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

    // ���۷����� 1�� ������ �����ϴ� ���·� �г� ���¸� ����
    private void InitializeSingleSlotEditing()
    {
        InitializeSideView();
        ResetSelection();

        nowEditingIndex = GameManagement.Instance!.UserSquadManager.EditingSlotIndex;
        existingOperator = GameManagement.Instance!.PlayerDataManager.GetOperatorInSlot(nowEditingIndex); // ������ ���Կ� �ִ� ���۷�����
        editingOperator = MainMenuManager.Instance!.CurrentEditingOperator; // �����Ͽ� ���� ���� �� ����� ���۷�����

        availableOperators = GetAvailableOperatorsForSingleEditing();
        InitializeOperatorSlots();

        // ���� �ڵ� ���� ��Ȳ
        // 1. DetailPanel�� ���� ���� ��� �ش� ���۷����� ����
        if (editingOperator != null)
        {
            HandleSlotClicked(operatorSlots[0]);
            MainMenuManager.Instance.SetCurrentEditingOperator(null);
        }
        // 2. SquadEditPanel���� ���۷����Ͱ� �ִ� ������ Ŭ���ؼ� ���� ���, �ش� ���۷����� ����
        else if (existingOperator != null)
        {
            HandleSlotClicked(operatorSlots[0]);
        }
    }

    // ������ ��ü�� �����ϴ� ���·� �г� ���¸� ����
    private void InitializeBulkEditing()
    {
        InitializeSideView();
        ResetSelection();

        availableOperators = GetAvailableOperatorsForBulkEditing();
        AdjustSlotContainerSize(availableOperators);
        InitializeOperatorSlots();

        // ���� ������ ǥ�� �� ���� ���·� ����
        nowEditingIndex = 0;
        int maxSquadSize = GameManagement.Instance!.UserSquadManager.MaxSquadSize;
        

        for (int i = 0; i < maxSquadSize; i++)
        {
            // ���� ������ ������ŭ �ʱ�ȭ
            clickedSlots.Add(null);
        }
        
        // tempSquad�� �ε����� ������ ���� ���� ������ slot�� ä��
        for (int i = 0; i < maxSquadSize; i++)
        {
            if (tempSquad[i] != null)
            {
                OperatorSlot slot = operatorSlots.FirstOrDefault(s => s.OwnedOperator == tempSquad[i].op);
                if (slot != null)
                {
                    // ���� ���õ� ���·� ����
                    slot.SetSelected(true, i); // HandleSlotClicked ������(���� �̺�Ʈ ���� ������)
                    clickedSlots[i] = slot;

                    // ���� ���� ��ܿ� i + 1 ǥ�� (���߿� ����)
                }
            }
        }

        UpdateConfirmButtonState();
    }

    private void InitializeSideView()
    {
        if (UIHelper.Instance != null)
        {
            // Awake�� �� ���, �� �г��� Ȱ��ȭ�� ä�� �����ϸ� ���� �߻��ؼ� OnEnable���� �����ϵ��� ����
            attackRangeHelper = UIHelper.Instance.CreateAttackRangeHelper(
                attackRangeContainer,
                centerPositionOffset
            );
        }

        ClearSideView();
    }

    // availableOperators���� operatorSlot���� �����մϴ�. 
    private void InitializeOperatorSlots()
    {
        ClearSlots();

        foreach (OwnedOperator op in availableOperators)
        {
            OperatorSlot slot = Instantiate(slotButtonPrefab, operatorSlotContainer);

            // �̸� ���� - Ʃ�丮�󿡼� ��ư �̸� ������ �� �ʿ���
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

        // ���� ������ ��������
        List<SquadOperatorInfo> currentSquad = GameManagement.Instance!.UserSquadManager.GetCurrentSquad();

        // ���� ���� ���۷����� ��, ���� �����忡 ���� ���۷����͸� ������
        List<OwnedOperator> availableOperators = GameManagement.Instance!.PlayerDataManager.GetOwnedOperators()
            .Where(ownedOp => !currentSquad.Any(squadInfo => squadInfo.op == ownedOp)) 
            .ToList();

        // ������ �������� ���۷����Ͱ� �ִ� ������ Ŭ���ؼ� ���� ���, �ش� ���۷����͸� �� �տ� ��ġ
        if (existingOperator != null)
        {
            availableOperators.Insert(0, existingOperator);
        }

        // ���� ������ ���۷����Ͱ� �ִٸ� ������ �� ������ ��ġ (���� existingOperator�� �ִٸ� 2��°�� �и�)
        if (editingOperator != null)
        {
            availableOperators.Remove(editingOperator);
            availableOperators.Insert(0, editingOperator);
        }

        return availableOperators;
    }

    private List<OwnedOperator> GetAvailableOperatorsForBulkEditing()
    {
        // ���� �����忡 �ִ� ���۷����͵��� ������� �迭
        tempSquad = GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();

        //selectedOperators = new List<OwnedOperator?>(currentSquadWithNull);

        // OwnedOperator? ����Ʈ ����
        List<OwnedOperator?> currentOpWithNull = tempSquad
            .Select(squadInfo => squadInfo?.op)
            .ToList();

        // null�� �ƴ� ���۷����͸� ���� (���� ����)
        List<OwnedOperator> squadOperators = currentOpWithNull
            .Where(op => op != null)
            //.Cast<OwnedOperator>()
            .Select(op => op!)
            .ToList();

        List<OwnedOperator> allOperators = GameManagement.Instance!.PlayerDataManager.GetOwnedOperators().ToList();

        // �̹� �����忡 ��ġ�� ���۷����͵��� ����Ʈ���� ����
        foreach (var op in squadOperators)
        {
            allOperators.Remove(op);
        }

        // ����) ���� �����忡 �ִ� ���۷����Ͱ� ��, ������ ���۷����Ͱ� �ڷ� ���� ��ġ
        return squadOperators.Concat(allOperators).ToList();
    }

    private void AdjustSlotContainerSize(List<OwnedOperator> availableOperators)
    {
        // �׸��� ������ �ʺ� ����
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
        // ���� Ŭ�� ������ �̺�Ʈ �߻� -> �� �޼��� ���ȣ�� ���� �÷���
        if (isSlotProcessing) return;
        isSlotProcessing = true;
        try
        {
            if (isEditingSlot)
            {
                // ���� ���� ���� 

                // ���� ���� ����
                if (SelectedSlot != null)
                {
                    SelectedSlot.SetSelected(false);
                    SelectedSlot = null;
                }

                // ���� SideView�� �Ҵ�� ��� ����
                ClearSideView();

                // ���ο� ���� ó��
                SelectedSlot = clickedSlot;
                //UpdateSideView();
                SelectedSlot.SetSelected(true);
                confirmButton.interactable = true;
                
            }
            else if (isEditingBulk)
            {
                // ������ ��ü ���� ���
                ClearSideView();
                HandleSlotClickedForBulk(clickedSlot);
            }
            else
            {
                // ������ ���� ���� �ƴ϶��, �ش� ���۷����� ���� ���� �гη� �̵�
                MoveToDetailPanel(clickedSlot);
            }
        }
        finally
        {
            // �߰��� ������ �߻��ϴ��� ���� ó�� �ʱ�ȭ�� ����ǵ��� ��
            isSlotProcessing = false;
        }
    }

    private void HandleSlotClickedForBulk(OperatorSlot clickedSlot)
    {
        // �ʱ�ȭ �߿��� ������ ����
        if (isInitializing) return;

        // ���� Ŭ�� ���� ���� ����
        if (clickedSlots.Contains(clickedSlot))
        {
            // tempSquad������ �Ҵ� ����
            int index = tempSquad.FindIndex(opInfo => opInfo?.op == clickedSlot.OwnedOperator);
            if (index != -1)
            {
                tempSquad[index] = null;
                clickedSlots[index] = null; // clickedSlot������ �Ҵ� ����

                // ���� �ε��� ����
                SetNowEditingIndexForBulk();
            }

            ClearSideView();
            SelectedSlot = null;
            clickedSlot.SetSelected(false);
        }
        // Ŭ�� ���� �߰� ����
        else
        {
            SetNowEditingIndexForBulk(); // ���� ���� ���� �ε��� ����

            clickedSlot.SetSelected(true, nowEditingIndex); // ���ȣ������� �÷��� ������ �� ������� ����

            tempSquad[nowEditingIndex] = new SquadOperatorInfo(clickedSlot.OwnedOperator, clickedSlot.OwnedOperator.DefaultSelectedSkillIndex);
            clickedSlots[nowEditingIndex] = clickedSlot;
            SelectedSlot = clickedSlot;
            UpdateSideView();
        }

        // Ȯ�� ��ư UI ������Ʈ
        UpdateConfirmButtonState();
    }
    
    // tempSquad���� null�� ���� ���� �ε����� ���� ���� ���� �ε����� ������
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
                //SelectedSlot.OwnedOperator.SetStageSelectedSkill(selectedSkill); // ��ġ ���� �õ���
                GameManagement.Instance!.UserSquadManager.ConfirmOperatorSelection(SelectedSlot.OwnedOperator, selectedSkillIndex);
            }
        }
        else if (isEditingBulk)
        {
            // ���� ���õ� ���۷����͵� + ������ ��ų�� �̿��� �����忡 ����
            GameManagement.Instance!.UserSquadManager.UpdateFullSquad(tempSquad);
        }
        ReturnToSquadEditPanel();
    }

    private void OnSetEmptyButtonClicked()
    {
        if (isEditingSlot)
        {
            GameManagement.Instance!.UserSquadManager.TryReplaceOperator(nowEditingIndex, null);
            GameManagement.Instance!.UserSquadManager.CancelOperatorSelection(); // ���� �������� ��ġ ���� �ε����� ����
            ReturnToSquadEditPanel();
        }
        else if (isEditingBulk)
        {
            // ��ũ������ clickedSlot, tempSquads�� �ʱ�ȭ��Ű�⸸ �ϰ� �ǵ��ư��� ����
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


    // ���� �г��� SideView�� ��Ÿ���� ���۷����Ϳ� ���õ� ������ ������Ʈ�Ѵ�.
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

                // ���� ���� �ð�ȭ
                attackRangeHelper.ShowBasicRange(op.CurrentAttackableGridPos);

                // ��ų ��ư �ʱ�ȭ �� ����
                UpdateSkillButtons(SelectedSlot);

                // ������ �̵� ��ư Ȱ��ȭ
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

            // ����Ʈ ��ų ����  
            if (selectedSkill == null)
            {
                selectedSkillIndex = op.UnlockedSkills.IndexOf(slot.SelectedSkill);
                selectedSkill = op.UnlockedSkills[selectedSkillIndex];
                UpdateSkillSelectionIndicator();
                UpdateSkillDescription();
            }

            // 1����ȭ��� ��ų�� 2���� ��  
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

        // ù ��° ��ų�� �⺻ ���� ��ų�� ������ skillIconBox1�� ��ư�� ������� ��
        if (unlockedSkills.Count > 0 && selectedSkill == unlockedSkills[0])
        { 
            targetButtonTransform = skillIconBoxes[0].transform;
        }
        // �� ��° ��ų�� �⺻ ���� ��ų�� ������ skillIconBox2�� ��ư�� ������� ��
        else if (unlockedSkills.Count > 1 && selectedSkill == unlockedSkills[1])
        {
            targetButtonTransform = skillIconBoxes[1].transform;
        }

        // �ε������� ��ġ ��ġ
        if (targetButtonTransform != null)
        {
            // �ε������͸� ���õ� ��ų ��ư�� ù ��° �ڽ����� ���ġ
            skillSelectionIndicator.transform.SetParent(targetButtonTransform, false);
            skillSelectionIndicator.transform.SetSiblingIndex(0);

            // ��ġ �̵�
            skillSelectionIndicator.transform.localPosition = Vector3.zero; // ��ġ�� �ٲ���

            // Ȱ��ȭ
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
        // ������ ���� �� or �ܼ��� ���۷����� ���� ����
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
