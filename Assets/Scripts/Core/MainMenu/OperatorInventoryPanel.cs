using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보유한 오퍼레이터들을 보여주는 패널. 스쿼드를 편집하는 패널은 SquadEditPanel로 혼동에 주의하시오 
/// </summary>
public class OperatorInventoryPanel : MonoBehaviour
{
    [Header("UI References")]
    //[SerializeField] private ScrollRect operatorSlotContainerscrollRect;
    [SerializeField] private Transform operatorSlotContainer;
    [SerializeField] private TextMeshProUGUI operatorNameText;
    [SerializeField] private OperatorSlot slotButtonPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button setEmptyButton; // 현재 슬롯을 비우는 버튼
    [SerializeField] private Button detailButton; // OperatorDetailPanel로 가는 버튼

    [Header("Attack Range Visualization")]
    [SerializeField] private RectTransform attackRangeContainer;
    [SerializeField] private float centerPositionOffset; // 타일 시각화 위치를 위한 중심 이동
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

    // 화면 오른쪽에 나타나는 사용 가능한 오퍼레이터 리스트 -- 혼동 주의!!
    private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    private OperatorSlot selectedSlot;


    // UserSquadManager에서 현재 편집 중인 인덱스
    private int nowEditingIndex;  

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        setEmptyButton.onClick.AddListener(OnSetEmptyButtonClicked);
        detailButton.onClick.AddListener(OnDetailButtonClicked);

        confirmButton.interactable = false;
        detailButton.interactable = false;
        //gameObject.SetActive(false); // 이거 있으면 실행 전에 이 오브젝트가 비활성화된 경우 ShowPanel 등에 의한 활성화가 아예 안됨
    }

    private void Start()
    {
        // AttackRangeHelper 초기화
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
        Debug.Log($"인벤토리 패널 OnEnabled 활성화됨, 현재 인덱스 : {nowEditingIndex}");
    }


    /// <summary>
    /// 보유한 오퍼레이터 리스트를 만들고 오퍼레이터 슬롯들을 초기화합니다.
    /// </summary>
    private void PopulateOperators()
    {
        // 슬롯 정리
        ClearSlots();

        // 현재 스쿼드 가져오기
        List<OwnedOperator> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquad();


        // 보유 중인 오퍼레이터 중, 현재 스쿼드에 없는 오퍼레이터만 가져옴
        List<OwnedOperator> availableOperators = GameManagement.Instance.PlayerDataManager.GetOwnedOperators()
            .Where(op => !currentSquad.Contains(op))
            .ToList();

        // 그리드 영역의 너비 조절
        RectTransform slotContainerRectTransform = operatorSlotContainer.gameObject.GetComponent<RectTransform>(); 
        if (availableOperators.Count > 12)
        {
            Vector2 currentSize = slotContainerRectTransform.sizeDelta;
            float additionalWidth = 250 * Mathf.Floor( (availableOperators.Count - 12) / 2);
            
            slotContainerRectTransform.sizeDelta = new Vector2(currentSize.x + additionalWidth, currentSize.y);
        }

        // 오퍼레이터 별로 슬롯 생성
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
        // 이미 선택된 슬롯 재클릭시 무시 (이거 없으면 무한 이벤트로 인한 스택 오버플로우 뜸)
        if (selectedSlot == clickedSlot) return; 

        // 이전 선택 해제
        if (selectedSlot != null) { selectedSlot.SetSelected(false); }

        // 새로운 선택 처리
        selectedSlot = clickedSlot;
        UpdateSideView(clickedSlot);
        selectedSlot.SetSelected(true);
        confirmButton.interactable = true;
        detailButton.interactable = true;
    }
    
    /// <summary>
    /// 확인 버튼 클릭 시 동작
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        if (selectedSlot != null && selectedSlot.OwnedOperator != null)
        {
            GameManagement.Instance.UserSquadManager.ConfirmOperatorSelection(selectedSlot.OwnedOperator);
            // 돌아가기
            ReturnToSquadEditPanel();
        }
    }

    private void OnSetEmptyButtonClicked()
    {
        GameManagement.Instance.UserSquadManager.TryReplaceOperator(nowEditingIndex, null);
        GameManagement.Instance.UserSquadManager.CancelOperatorSelection(); // 현재 스쿼드의 배치 중인 인덱스를 없앰
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
    /// OperatorSelectionPanel의 SideView에 나타나는 오퍼레이터와 관련된 정보를 업데이트한다.
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

        // 공격 범위 시각화
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
