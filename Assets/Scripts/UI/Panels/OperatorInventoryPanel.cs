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
    [SerializeField] private Transform operatorSlotContainer;
    [SerializeField] private TextMeshProUGUI operatorNameText;
    [SerializeField] private OperatorSlot slotButtonPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button setEmptyButton; // ���� ������ ���� ��ư
    [SerializeField] private Button detailButton; // OperatorDetailPanel�� ���� ��ư

    [Header("Attack Range Visualization")]
    [SerializeField] private RectTransform attackRangeContainer;
    [SerializeField] private float centerPositionOffset; // Ÿ�� �ð�ȭ ��ġ�� ���� �߽� �̵�
    private UIHelper.AttackRangeHelper attackRangeHelper;

    [Header("Operator Stat Boxes")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI redeployTimeText;
    [SerializeField] private TextMeshProUGUI attackPowerText;
    [SerializeField] private TextMeshProUGUI deploymentCostText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI blockCountText;
    [SerializeField] private TextMeshProUGUI magicResistanceText;
    [SerializeField] private TextMeshProUGUI attackSpeedText;

    [Header("Skills")]
    [SerializeField] private Button skill1Button;
    [SerializeField] private Image skill1SelectedIndicator;
    [SerializeField] private Button skill2Button;
    [SerializeField] private Image skill2SelectedIndicator;
    [SerializeField] private TextMeshProUGUI skillDetailText;

    // ȭ�� �����ʿ� ��Ÿ���� ��� ������ ���۷����� ����Ʈ -- ȥ�� ����!!
    private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    private OperatorSlot selectedSlot;

    private BaseSkill selectedSkill;
    private Sprite noSkillSprite;

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

        skill1Button.interactable = true;
        skill2Button.interactable = false;

        noSkillSprite = skill1Button.GetComponent<Image>().sprite;
    }

    private void Start()
    {
        // AttackRangeHelper �ʱ�ȭ
        attackRangeHelper = UIHelper.Instance.CreateAttackRangeHelper(
            attackRangeContainer,
            centerPositionOffset
        );
    }

    private void OnEnable()
    {
        PopulateOperators();
        ResetSelection();

        nowEditingIndex = GameManagement.Instance.UserSquadManager.EditingSlotIndex;
    }


    // ������ ���۷����� ����Ʈ�� ����� ���۷����� ���Ե��� �ʱ�ȭ�մϴ�.
    private void PopulateOperators()
    {
        // ���� ����
        ClearSlots();

        // ���� ������ ��������
        List<OwnedOperator> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquad();

        // ���� ���� ���۷����� ��, ���� �����忡 ���� ���۷����͸� ������
        List<OwnedOperator> availableOperators = GameManagement.Instance.PlayerDataManager.GetOwnedOperators()
            .Where(op => !currentSquad.Contains(op))
            .ToList();

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
        if (selectedSlot != null && selectedSlot.OwnedOperator != null)
        {
            selectedSlot.OwnedOperator.StageSelectedSkill = selectedSkill;
            GameManagement.Instance.UserSquadManager.ConfirmOperatorSelection(selectedSlot.OwnedOperator);
            // ���ư���
            ReturnToSquadEditPanel();
        }
    }

    private void OnSetEmptyButtonClicked()
    {
        GameManagement.Instance.UserSquadManager.TryReplaceOperator(nowEditingIndex, null);
        GameManagement.Instance.UserSquadManager.CancelOperatorSelection(); // ���� �������� ��ġ ���� �ε����� ����
        ReturnToSquadEditPanel();
    }

    private void OnDetailButtonClicked()
    {
        if (selectedSlot != null && selectedSlot.OwnedOperator != null)
        {
            GameObject detailPanel = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorDetail];
            detailPanel.GetComponent<OperatorDetailPanel>().Initialize(selectedSlot.OwnedOperator);
            MainMenuManager.Instance.ActivateAndFadeOut(detailPanel, gameObject);
        }
        MainMenuManager.Instance.ActivateAndFadeOut(MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorDetail], gameObject);
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
        OwnedOperator op = slot.OwnedOperator;
        OperatorStats opStats = op.CurrentStats;
        OperatorData opData = op.BaseData;

        operatorNameText.text = opData.entityName;
        healthText.text = opStats.Health.ToString();
        redeployTimeText.text = opStats.RedeployTime.ToString();
        attackPowerText.text = opStats.AttackPower.ToString();
        deploymentCostText.text = opStats.DeploymentCost.ToString();
        defenseText.text = opStats.Defense.ToString();
        magicResistanceText.text = opStats.MagicResistance.ToString();
        blockCountText.text = opStats.MaxBlockableEnemies.ToString();
        attackSpeedText.text = opStats.AttackSpeed.ToString();

        // ���� ���� �ð�ȭ
        attackRangeHelper.ShowBasicRange(op.CurrentAttackableTiles);

        // ��ų ��ư �ʱ�ȭ �� ����
        UpdateSkillButtons(op);
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

        selectedSkill = null;
    }

    private void UpdateSkillButtons(OwnedOperator op)
    {
        var unlockedSkills = op.UnlockedSkills;
        skill1Button.GetComponent<Image>().sprite = unlockedSkills[0].skillIcon;

        // ����Ʈ ��ų ����
        if (selectedSkill == null)
        {
            selectedSkill = op.DefaultSelectedSkill;
            UpdateSkillSelection();
            UpdateSkillDescription();
        }

        // 1����ȭ��� ��ų�� 2���� ��
        if (unlockedSkills.Count > 1)
        {
            skill2Button.GetComponent<Image>().sprite = unlockedSkills[1].skillIcon;
        }
        else
        {
            skill2Button.interactable = false; 
        }
    }

    private void OnSkillButtonClicked(int skillIndex)
    {
        if (selectedSlot?.OwnedOperator == null) return;

        var skills = selectedSlot.OwnedOperator.UnlockedSkills;
        if (skillIndex < skills.Count)
        {
            selectedSkill = skills[skillIndex];
            UpdateSkillSelection();
            UpdateSkillDescription();
        }
    }

    private void UpdateSkillSelection()
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
        MainMenuManager.Instance.ActivateAndFadeOut(MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.SquadEdit], gameObject);
    }
    


    private void OnDisable()
    {
        ClearSlots();
        ClearSideView();
        ResetSelection();
    }
}
