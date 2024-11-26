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
    [SerializeField] private Image filledTilePrefab;
    [SerializeField] private Image outlineTilePrefab;
    [SerializeField] private float centerPositionOffset; // Ÿ�� �ð�ȭ ��ġ�� ���� �߽� �̵�

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

    // ���� â�� ��Ÿ�� ���ݹ��� ǥ��
    private List<Image> rangeTiles = new List<Image>();
    private float tileSize;

    // UserSquadManager���� ���� ���� ���� �ε���
    private int nowEditingIndex; 

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        setEmptyButton.onClick.AddListener(OnSetEmptyButtonClicked);
        detailButton.onClick.AddListener(OnDetailButtonClicked);

        confirmButton.interactable = false;
        //gameObject.SetActive(false); // �̰� ������ ���� ���� �� ������Ʈ�� ��Ȱ��ȭ�� ��� ShowPanel � ���� Ȱ��ȭ�� �ƿ� �ȵ�
    }

    private void OnEnable()
    {
        PopulateOperators();
        ResetSelection();
        nowEditingIndex = GameManagement.Instance.UserSquadManager.EditingSlotIndex;
        Debug.Log($"���� ���� ���� ������ �ε��� : {nowEditingIndex}");
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

        Debug.Log(currentSquad.Count);

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
        // (��������)���Ը��� OwnedOperator�� �ٲ�� ��
        // selectedSlot�� ownedOperator�� ���޵ž� ��
        MainMenuManager.Instance.ActivateAndFadeOut(MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorDetail], gameObject);
    }


    private void ResetSelection()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
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
    /// <param name="slot"></param>
    private void UpdateSideView(OperatorSlot slot)
    {

        OperatorData opData = slot.AssignedOperatorData;
        OperatorStats opStats = opData.stats;

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
        UpdateVisualAttackRange(opData);
    }

    /// <summary>
    /// ���� ������ �ð�ȭ�մϴ�.
    /// </summary>
    private void UpdateVisualAttackRange(OperatorData opData)
    {
        // ���� Ÿ�� ����
        ClearVisualAttackRange();

        // ������ Ÿ�� ����
        CreateRangeTile(Vector2Int.zero, true);

        foreach (Vector2Int pos in opData.attackableTiles)
        {
            // 180�� ȸ��(������ <- ������ ���� ������ -> ������ ������ ����)
            Vector2Int convertedPos = new Vector2Int(-pos.x, -pos.y);
            if (convertedPos != Vector2Int.zero)
            {
                CreateRangeTile(convertedPos, false);
            }
        }

    }

    private void ClearVisualAttackRange()
    {
        foreach (var tile in rangeTiles)
        {
            Destroy(tile.gameObject);
        }
        rangeTiles.Clear();
    }

    private void CreateRangeTile(Vector2Int gridPos, bool isCenterTile)
    {
        // ������ ����
        Image tilePrefab = isCenterTile ? filledTilePrefab : outlineTilePrefab;

        // Ÿ�� ����
        Image tile = Instantiate(tilePrefab, attackRangeContainer);

        // ��ġ ���� ���� -  ��ǻ� gridPos ���� (tileSize  + 1.5) ���� �Ŷ� ������
        tileSize = tile.rectTransform.rect.width;
        float interval = tileSize / 4f; 
        float gridX = gridPos.x * tileSize + gridPos.x * interval;
        float gridY = gridPos.y * tileSize + gridPos.y * interval;

        // ��ġ ���� : ������ �ݿ�
        tile.rectTransform.anchoredPosition = new Vector2(
            gridX - centerPositionOffset,
            gridY
        );

        rangeTiles.Add(tile);
    }

    private void ReturnToSquadEditPanel()
    {
        MainMenuManager.Instance.ActivateAndFadeOut(MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.SquadEdit], gameObject);
    }


    private void OnDisable()
    {
        ClearVisualAttackRange();
        ResetSelection();
    }
}
