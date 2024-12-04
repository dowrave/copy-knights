using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ������ ���۷����͵��� �����ִ� �г�. �����带 �����ϴ� �г��� SquadEditPanel�� ȥ���� �����Ͻÿ� 
/// </summary>
public class OperatorInventoryPanel : MonoBehaviour
{
    [Header("UI References")]
    //[SerializeField] private ScrollRect operatorSlotContainerscrollRect;
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

    // ȭ�� �����ʿ� ��Ÿ���� ��� ������ ���۷����� ����Ʈ -- ȥ�� ����!!
    private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    private OperatorSlot selectedSlot;


    // UserSquadManager���� ���� ���� ���� �ε���
    private int nowEditingIndex;  

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        setEmptyButton.onClick.AddListener(OnSetEmptyButtonClicked);
        detailButton.onClick.AddListener(OnDetailButtonClicked);

        confirmButton.interactable = false;
        detailButton.interactable = false;
        //gameObject.SetActive(false); // �̰� ������ ���� ���� �� ������Ʈ�� ��Ȱ��ȭ�� ��� ShowPanel � ���� Ȱ��ȭ�� �ƿ� �ȵ�
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
        Debug.Log($"�κ��丮 �г� OnEnabled Ȱ��ȭ��, ���� �ε��� : {nowEditingIndex}");
    }


    /// <summary>
    /// ������ ���۷����� ����Ʈ�� ����� ���۷����� ���Ե��� �ʱ�ȭ�մϴ�.
    /// </summary>
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

        // ���ο� ���� ó��
        selectedSlot = clickedSlot;
        UpdateSideView(clickedSlot);
        selectedSlot.SetSelected(true);
        confirmButton.interactable = true;
        detailButton.interactable = true;
    }
    
    /// <summary>
    /// Ȯ�� ��ư Ŭ�� �� ����
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        if (selectedSlot != null && selectedSlot.OwnedOperator != null)
        {
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

    /// <summary>
    /// OperatorSelectionPanel�� SideView�� ��Ÿ���� ���۷����Ϳ� ���õ� ������ ������Ʈ�Ѵ�.
    /// </summary>
    private void UpdateSideView(OperatorSlot slot)
    {

        OwnedOperator op = slot.OwnedOperator;
        OperatorStats opStats = op.currentStats;
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
        attackRangeHelper.ShowBasicRange(op.currentAttackableTiles);
        //UpdateVisualAttackRange(opData);
    }

    private void ClearSideView()
    {
        operatorNameText.text = "";
        healthText.text = "";
        redeployTimeText.text = "";
        attackPowerText.text = "";
        deploymentCostText.text = "";
        defenseText.text = "";
        magicResistanceText.text = "";
        blockCountText.text = "";
        attackSpeedText.text = "";
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
