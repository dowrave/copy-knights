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
    [SerializeField] private Button skill1Button = default!;
    [SerializeField] private Image skill1SelectedIndicator = default!;
    [SerializeField] private Button skill2Button = default!;
    [SerializeField] private Image skill2SelectedIndicator = default!;
    [SerializeField] private TextMeshProUGUI skillDetailText = default!;

    // ȭ�� �����ʿ� ��Ÿ���� ��� ������ ���۷����� ����Ʈ -- ȥ�� ����!!
    private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    private OperatorSlot? selectedSlot;

    private BaseSkill? selectedSkill;
    private Sprite noSkillSprite = default!;

    // ���۷����Ͱ� �� �ִ� ���¿��� �ش� ������ ������ ��쿡�� ���
    private OwnedOperator? existingOperator;

    // UserSquadManager���� ���� ���� ���� �ε���
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
        // AttackRangeHelper �ʱ�ȭ
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

        // �̹� ��ġ�� ���۷����Ͱ� �ִٸ� �ش� ���۷����� ������ Ŭ���� ���·� ����
        if (existingOperator != null)
        {
            HandleSlotClicked(operatorSlots[0]);
        }
    }


    // ������ ���۷����� ����Ʈ�� ����� ���۷����� ���Ե��� �ʱ�ȭ�մϴ�.
    private void PopulateOperators()
    {
        // ���� ����
        ClearSlots();

        // ���� ������ ��������
        List<OwnedOperator> currentSquad = GameManagement.Instance!.UserSquadManager.GetCurrentSquad();

        // ���� ���� ���۷����� ��, ���� �����忡 ���� ���۷����͸� ������
        List<OwnedOperator> availableOperators = GameManagement.Instance!.PlayerDataManager.GetOwnedOperators()
            .Where(op => !currentSquad.Contains(op))
            .ToList();

        // �̹� ���۷����Ͱ� �ִ� ������ Ŭ���ؼ� ���� ���, �ش� ���۷����͵� ��Ÿ��
        if (existingOperator != null)
        {
            // ���� �� �ε����� ����
            availableOperators.Insert(0, existingOperator);
        }

        // �׸��� ������ �ʺ� ����
        RectTransform slotContainerRectTransform = operatorSlotContainer.gameObject.GetComponent<RectTransform>(); 
        if (availableOperators.Count > 12)
        {
            Vector2 currentSize = slotContainerRectTransform.sizeDelta;
            float additionalWidth = 250 * Mathf.Floor( (availableOperators.Count - 12) / 2);
            
            slotContainerRectTransform.sizeDelta = new Vector2(currentSize.x + additionalWidth, currentSize.y);
        }

        // ���۷����� ���� ���� ����
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

            // ����Ʈ ��ų ����
            if (selectedSkill == null)
            {
                selectedSkill = slot.SelectedSkill;
                UpdateSkillSelectionIndicator();
                UpdateSkillDescription();
            }

            // 1����ȭ��� ��ų�� 2���� ��
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
