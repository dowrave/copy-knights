using System.Collections.Generic;
using System.Linq;
using Skills.Base;
using TMPro;
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
    private OperatorSlot? selectedSlot;

    private BaseSkill? selectedSkill;
    private Sprite noSkillSprite = default!;

    // �ʱ�ȭ �ÿ� Ŀ�� ��ġ ����
    private OwnedOperator? existingOperator; // squadEditPanel�� ���۷����Ͱ� �ִ� ������ Ŭ���� ���·� �κ��丮 �гο� ������ ��, �����Ǵ� ����
    private OwnedOperator? editingOperator; // �κ��丮 �г��� �� ������ ���� ���ٰ� ���� ��


    // UserSquadManager���� ���� ���� ���� ����
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
            // Awake�� �� ���, �� �г��� Ȱ��ȭ�� ä�� �����ϸ� ���� �߻��ؼ� ����� �̵�
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

            // �ڵ� ����

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
        else
        {
            PopulateOperators();
        }
    }


    // ������ ���۷����� ����Ʈ�� ����� ���۷����� ���Ե��� �ʱ�ȭ�մϴ�.
    private void PopulateOperators()
    {
        List<OwnedOperator> availableOperators;

        // ���� ����
        ClearSlots();

        if (isEditing)
        {
            // ���� ������ ��������
            List<OwnedOperator> currentSquad = GameManagement.Instance!.UserSquadManager.GetCurrentSquad();

            // ���� ���� ���۷����� ��, ���� �����忡 ���� ���۷����͸� ������
            availableOperators = GameManagement.Instance!.PlayerDataManager.GetOwnedOperators()
                .Where(op => !currentSquad.Contains(op))
                .ToList();

            // ������ �������� ���۷����Ͱ� �ִ� ������ Ŭ���ؼ� ���� ���, �ش� ���۷����Ͱ� �� ��
            if (existingOperator != null)
            {
                // ���� �� �ε����� ����
                availableOperators.Insert(0, existingOperator);
            }

            // ���� ������ ���۷����Ͱ� �ִٸ� ������ �� ������ (existingOperator�� �ִٸ� 2��° ������ �ڿ������� �з���)
            if (editingOperator != null)
            {
                availableOperators.Remove(editingOperator);
                availableOperators.Insert(0, editingOperator);
            }


            // �׸��� ������ �ʺ� ����
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


        // ���۷����� ���� ���� ����
        foreach (OwnedOperator op in availableOperators)
        {
            OperatorSlot slot = Instantiate(slotButtonPrefab, operatorSlotContainer);

            // �̸� ���� - Ʃ�丮�󿡼� ��ư �̸� ������ �� �ʿ���
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
            // ������ ���� ��

            // �̹� ���õ� ���� ��Ŭ���� ���� (�̰� ������ ���� �̺�Ʈ�� ���� ���� �����÷ο� ��)
            if (selectedSlot == clickedSlot) return;

            // ���� ���� ����
            if (selectedSlot != null) { selectedSlot.SetSelected(false); }

            // ���� SideView�� �Ҵ�� ��� ����
            ClearSideView();

            // ���ο� ���� ó��
            selectedSlot = clickedSlot;
            UpdateSideView(clickedSlot);
            selectedSlot.SetSelected(true);
            confirmButton.interactable = true;
            detailButton.interactable = true;
        }
        else
        {
            // ������ ���� ���� �ƴ϶��, �ش� ���۷����� ���� ���� �гη� ��
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
        GameManagement.Instance!.UserSquadManager.CancelOperatorSelection(); // ���� �������� ��ġ ���� �ε����� ����
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


    // ���� �г��� SideView�� ��Ÿ���� ���۷����Ϳ� ���õ� ������ ������Ʈ�Ѵ�.
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

            // ���� ���� �ð�ȭ
            attackRangeHelper.ShowBasicRange(op.CurrentAttackableGridPos);

            // ��ų ��ư �ʱ�ȭ �� ����
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

            // ����Ʈ ��ų ����
            if (selectedSkill == null)
            {
                selectedSkill = slot.SelectedSkill;
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
    }


}
