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

    private List<OwnedOperator> ownedOperators = new List<OwnedOperator>();

    private BaseSkill? selectedSkill;
    private int selectedSkillIndex;
    private Sprite noSkillSprite = default!;

    // �ʱ�ȭ �ÿ� Ŀ�� ��ġ ����
    private OwnedOperator? existingOperator; // squadEditPanel�� ���۷����Ͱ� �ִ� ������ Ŭ���� ���·� �κ��丮 �гο� ������ ��, �����Ǵ� ����
    private OwnedOperator? editingOperator; // �κ��丮 �г��� �� ������ ���� ���ٰ� ���� ��


    // ���� ���� ���� ���� �ε���. ��Ȳ�� ���� �ٸ��� ���δ�.
    private int nowEditingIndex; 

    // UserSquadManager���� ���� ���� ���� ����
    private bool isEditingSlot;
    private bool isEditingBulk;

    // ��ũ������ ���
    private bool isBulkInitializing = false; // ��ũ �ʱ�ȭ���� HandleSlotClicked�� ������ �����ϱ� ���� �÷���
    private List<SquadOperatorInfo?> tempSquad = new List<SquadOperatorInfo?>();
    private List<OperatorSlot> clickedSlots = new List<OperatorSlot>();

    // HandleSlotClicked�� �ߺ� ���� ����
    private bool isSlotProcessing = false; // ��� ���� �÷���

    public bool IsOnSelectionSession => MainMenuManager.Instance!.OperatorSelectionSession;

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

        // ��� ���� ����
        InitializeAllSlots();
    }

    private void OnEnable()
    {
        // �κ��丮�θ� �����ϰų�, ���� ������ �� ������� ������ ����
        if (!IsOnSelectionSession)
        {
            // ��� ������ ��ų�� �⺻ ��ų�� �ʱ�ȭ
            SessionInitializeSkillSlot();

            isEditingSlot = GameManagement.Instance!.UserSquadManager.IsEditingSlot;
            isEditingBulk = GameManagement.Instance!.UserSquadManager.IsEditingBulk;

            bool isEditing = isEditingSlot || isEditingBulk;
            SetSquadEditMode(isEditing);

            Debug.Log($"isEditingSlotIndex : {GameManagement.Instance!.UserSquadManager.EditingSlotIndex}");
            Debug.Log($"isEditingSlot : {isEditingSlot}");
            Debug.Log($"isEditingBulk : {isEditingBulk}");

            if (isEditingSlot)
            {
                InitializeSingleSlotEditing();
            }
            else if (isEditingBulk)
            {
                InitializeBulkEditing();
            }

            MainMenuManager.Instance.SetOperatorSelectionSession(isEditing); // IsOnSession Ȱ��ȭ
        }

        // ������ ���� �ִ� ���¿��� �ٸ� �гο� �ٳ�� ���
        if (SelectedSlot != null)
        {
            UpdateSideView();
        }
    }



    // �κ��丮�� ���ų�, ������ �����ϱ� ���� ��� ������ ��ų�� �⺻ ���� ��ų�� �ʱ�ȭ��
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
    
    // ������ ��� ���۷����͵��� �������� �ʱ�ȭ�մϴ�.
    private void InitializeAllSlots()
    {
        ownedOperators = GameManagement.Instance!.PlayerDataManager.GetOwnedOperators().ToList();
        InitializeOperatorSlots();
    }

    // ���۷����� 1�� ������ �����ϴ� ���·� �г� ���¸� ����
    private void InitializeSingleSlotEditing()
    {
        InitializeSideView();
        ResetSelection();

        // 1������ ���� �־�� �� �� - existingOperator ������
        nowEditingIndex = GameManagement.Instance!.UserSquadManager.EditingSlotIndex;
        existingOperator = GameManagement.Instance!.PlayerDataManager.GetOperatorInSlot(nowEditingIndex); // ������ ���Կ� �ִ� ���۷�����
        editingOperator = MainMenuManager.Instance!.CurrentEditingOperator; // �����Ͽ� ���� ���� �� ����� ���۷�����

        // 1. ������ ���� ���۷����� ����
        List<OwnedOperator> operatorsToDisplay = GetAvailableOperatorsForSingleEditing();

        // 2. ��� ���� ��Ȱ��ȭ
        foreach (OperatorSlot slot in operatorSlots)
        {
            slot.UpdateSelectionIndicator(false);
            slot.gameObject.SetActive(false);
        }

        // 3. �ڵ� ������ ���۷����� ã��
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

        // �켱 �����ؾ� �� ���۷����͸� 0�� �ε����� ����
        if (operatorToAutoSelect != null)
        {
            if (operatorsToDisplay.Contains(operatorToAutoSelect))
            {
                operatorsToDisplay.Remove(operatorToAutoSelect);
                operatorsToDisplay.Insert(0, operatorToAutoSelect);
            }
            else
            {
                throw new System.InvalidOperationException("���۷����� ���͸��� ����� �� �� ��?");
            }
        }

        // 4. operatorsToDisplay�� ���� ���� Ȱ��ȭ �� ���� ����
        int visibleSlotOrder = 0;
        foreach (OwnedOperator opToShow in operatorsToDisplay)
        {
            OperatorSlot slotToActivate = operatorSlots.FirstOrDefault(s => s.OwnedOperator == opToShow);
            if (slotToActivate != null)
            {
                slotToActivate.gameObject.SetActive(true);

                // ������ ��ġ ���� ����
                slotToActivate.transform.SetSiblingIndex(visibleSlotOrder++);
            }
        }

        // 5. ���� �ڵ� ���� ��Ȳ
        // 0�� �ε����� �����ص� ������, ��Ȯ�� + �߰��� + ��Ŀ�ø��� ���� "ã�ƾ� �ϴ� ����"�� ��Ȯ�ϰ� �����ϴ� ������� ����
        if (operatorToAutoSelect != null)
        {
            OperatorSlot slotToClick = operatorSlots.FirstOrDefault(s => s.gameObject.activeSelf && s.OwnedOperator == operatorToAutoSelect);
            if (slotToClick != null)
            {
                HandleSlotClicked(slotToClick);
            }
        }
    }

    // ������ ��ü�� �����ϴ� ���·� �г� ���¸� ����
    private void InitializeBulkEditing()
    {
        isBulkInitializing = true;

        InitializeSideView();
        ResetSelection();

        // 1. ������ ���۷����� ����
        List<OwnedOperator> operatorsToDisplay = GetAvailableOperatorsForBulkEditing();

        // 2. ��� ���� ��Ȱ��ȭ
        foreach (OperatorSlot slot in operatorSlots)
        {
            slot.UpdateSelectionIndicator(false);
            slot.gameObject.SetActive(false);
        }

        // 3. operatorsToDisplay�� ���� ���� Ȱ��ȭ �� ���� ����
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

        // 4. clickedSlots ����Ʈ �ʱ�ȭ
        int maxSquadSize = GameManagement.Instance!.UserSquadManager.MaxSquadSize;
        clickedSlots.Clear();
        for (int i=0; i < maxSquadSize; i++)
        {
            clickedSlots.Add(null);
        }
        
        // 5. tempSquad�� clickedSlot ä���
        for (int i = 0; i < tempSquad.Count; i++)
        {
            if (tempSquad[i] != null)
            {
                SquadOperatorInfo opInfo = tempSquad[i];
                OperatorSlot slot = operatorSlots.FirstOrDefault(s => s.OwnedOperator == opInfo.op);
                if (slot != null && slot.gameObject.activeSelf) // Ȱ��ȭ�� ���Կ� ����
                {
                    // ������ ���õ� ���·� ����
                    slot.SetSelected(true, idx: i);
                    clickedSlots[i] = slot;

                    if (opInfo.skillIndex >= 0 && opInfo.skillIndex < slot.OwnedOperator.UnlockedSkills.Count)
                    {
                        slot.UpdateSelectedSkill(slot.OwnedOperator.UnlockedSkills[opInfo.skillIndex]);
                    }

                    // ���������� Ŭ���� ���Կ� ���� ���̵�� Ȱ��ȭ
                    SelectedSlot = slot;
                }
            }
        }

        // 6. ���� ������ ���� �ε��� ����
        SetNowEditingIndexForBulk();

        // 7. Ȯ�� ��ư ���� ������Ʈ
        UpdateConfirmButtonState();

        isBulkInitializing = false;
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

        foreach (OwnedOperator op in ownedOperators)
        {
            OperatorSlot slot = Instantiate(slotButtonPrefab, operatorSlotContainer);

            // �̸� ���� - Ʃ�丮�󿡼� ��ư �̸� ������ �� �ʿ���
            slot.Initialize(true, op);
            operatorSlots.Add(slot);
            slot.OnSlotClicked.AddListener(HandleSlotClicked);
        }

        AdjustSlotContainerSize(ownedOperators);
    }

    // ǥ���� ���� ���۷����� ���͸�
    private List<OwnedOperator> GetAvailableOperatorsForSingleEditing()
    {
        // ���� ������ ��������
        List<SquadOperatorInfo> currentSquad = GameManagement.Instance!.UserSquadManager.GetCurrentSquad();

        // ���� ���� ���۷����� ��, ���� �����忡 ���� ���۷����͸� ������
        List<OwnedOperator> availableOperators = ownedOperators.Where(ownedOp => !currentSquad.Any(squadInfo => squadInfo.op == ownedOp)) 
            .ToList();

        // ������ �������� ���۷����Ͱ� �ִ� ������ Ŭ���ؼ� ���� ���, �ش� ���۷����͸� �� �տ� ��ġ
        if (existingOperator != null && !availableOperators.Contains(existingOperator))
        {
            //availableOperators.Insert(0, existingOperator);
            availableOperators.Add(existingOperator);
        }

        return availableOperators;
    }

    // 
    private List<OwnedOperator> GetAvailableOperatorsForBulkEditing()
    {
        // 1. tempSquad�� ���� ������ ���·� ä��
        tempSquad = GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();

        // 2. ������ ���� ������ ���۷����� ��������
        List<OwnedOperator> squadOperatorsInOrder = tempSquad
            .Where(squadInfo => squadInfo != null)
            .Select(squadInfo => squadInfo.op)
            .ToList();

        // 3. �����忡 �̹� �ִ� ���۷����ʹ� ����
        List<OwnedOperator> allOwnedOperatorsCopy = new List<OwnedOperator>(ownedOperators);
        foreach (var opInSquad in squadOperatorsInOrder)
        {
            allOwnedOperatorsCopy.Remove(opInSquad);
        }

        // 4. ���� ����Ʈ : ������ ���(���� ����) + ������ ���� ���۷�����
        List<OwnedOperator> displayOrder = new List<OwnedOperator>(squadOperatorsInOrder);
        displayOrder.AddRange(allOwnedOperatorsCopy);

        //for (int i = 0; i < displayOrder.Count; i++)
        //{
        //    Debug.Log($"displayOrder[{i}] = {displayOrder[i].operatorName}");
        //}


        return displayOrder;
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
                //ClearSideView();

                // ���ο� ���� ó��
                SelectedSlot = clickedSlot;
                //UpdateSideView(); // SelectedSlot�� ������Ʈ�Ǹ� �ڵ� ����
                SelectedSlot.SetSelected(true);
                confirmButton.interactable = true;
                
            }
            else if (isEditingBulk)
            {
                // ������ ��ü ���� ���
                //ClearSideView();
                HandleSlotClickedForBulk(clickedSlot);
            }
            else
            {
                // ������ ���� ���� �ƴ϶��, �ش� ���۷����� ���� ���� �гη� �ٷ� �̵�
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
        // ��ũ �ʱ�ȭ �߿��� ������ ����
        if (isBulkInitializing) return;

        int clickedSlotCurrentIndex = -1; // ���� ������ ���� �߰�, �ƴϸ� ���� ����
        for (int i = 0; i < clickedSlots.Count; ++i)
        {
            if (clickedSlots[i] == clickedSlot)
            {
                clickedSlotCurrentIndex = i;
                break;
            }
        }

        // ���� Ŭ�� ���� ���� ����
        if (clickedSlotCurrentIndex != -1)
        {
            Debug.Log("��ũ ���� : ���� ���� ���� ����");

            // tempSquad������ �Ҵ� ����
            tempSquad[clickedSlotCurrentIndex] = null;
            clickedSlots[clickedSlotCurrentIndex] = null;

            // ���� UI ������Ʈ
            clickedSlot.SetSelected(false); 

            // ���� ���õ� ���� ����
            SelectedSlot = null;

            // �ε��� ����
            SetNowEditingIndexForBulk();
        }
        // Ŭ�� ���� �߰� ����
        else
        {
            Debug.Log("��ũ ���� : ���� �Ҵ� ���� ����");

            SetNowEditingIndexForBulk(); // nowEditingIndex ����, �� ���۷����͸� ����Ʈ�� ��� ���� ���ΰ��� �����Ѵ�.

            // �ٸ� ���۷����ͷ� ä���� �ִ� ������ �ִٸ� ���� ���� - �ʿ��ұ�?

            if (nowEditingIndex < tempSquad.Count)
            {
                tempSquad[nowEditingIndex] = new SquadOperatorInfo(clickedSlot.OwnedOperator, clickedSlot.OwnedOperator.DefaultSelectedSkillIndex);
                clickedSlots[nowEditingIndex] = clickedSlot;

                clickedSlot.SetSelected(true, nowEditingIndex); // ���ȣ������� �÷��� ������ �� ������� ����
                SelectedSlot = clickedSlot;

                // ���� ������ �ε��� ���� : �ʿ��ұ�?
                //SetNowEditingIndexForBulk();
            }
            else
            {
                // �����尡 �� á�µ� �� ������ �����Ϸ��� ��� - �Ƹ� �߻����� ���� �ǵ� Ȥ�ó�

                throw new System.InvalidOperationException("�����尡 �� á��");
            }


            //SelectedSlot = clickedSlot;
            //UpdateSideView();
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
    // SelectedSlot�� ������Ʈ�Ǹ� �ڵ� ����
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

                // ��ų ��ư �ʱ�ȭ �� ���õ� ��ų ����
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

        detailButton.interactable = false;
    }

    private void UpdateSkillButtons(OperatorSlot slot)
    {
        if (slot.OwnedOperator != null)
        {
            OwnedOperator op = slot.OwnedOperator;

            // �⺻ ��ų �ʱ�ȭ
            skillIconBoxes[0].Initialize(op.UnlockedSkills[0],
                                            showDurationBox: true,
                                            showSkillName: true);
            skillIconBoxes[0].SetButtonInteractable(true);

            // 1����ȭ��� ��ų�� 2���� ��  
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

            // ��ų ���� ����

            // ���Կ� �Ҵ�� ��ų�� �������� �� �⺻
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
        List<BaseSkill> unlockedSkills = op.UnlockedSkills;

        // �ε��������� Transform ����
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
        MainMenuManager.Instance!.SetOperatorSelectionSession(false);
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
        // ������ ���� ������ �г��� �����Ƿ� ��ȿ��
        if (!IsOnSelectionSession)
        {
            // ���� �߿�
            ResetSelectionUI();
            ResetPanelState();
        }
    }

    // ���ǿ��� "���� ���� ����"�� ���� �������� �ʱ�ȭ��
    private void ResetPanelState()
    {
        // ��ų ���� �ʱ�ȭ
        selectedSkillIndex = -1;
        selectedSkill = null;

        // "���� ��" ���۷����͵� �ʱ�ȭ
        existingOperator = null;
        editingOperator = null;

        nowEditingIndex = -1;
        SelectedSlot = null;

        // ��ũ ���� ���µ� �ʱ�ȭ
        tempSquad.Clear();
        clickedSlots.Clear();

        // ���� �ʱ�ȭ
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
            Debug.Log(slot.opData?.entityName);
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
