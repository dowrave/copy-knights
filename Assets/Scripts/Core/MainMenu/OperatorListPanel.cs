using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class OperatorListPanel : MonoBehaviour
{
    [Header("UI References")]
    //[SerializeField] private ScrollRect operatorSlotContainerscrollRect;
    [SerializeField] private Transform operatorSlotContainer;
    [SerializeField] private TextMeshProUGUI operatorNameText;
    [SerializeField] private OperatorSlot slotButtonPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Attack Range Visualization")]
    [SerializeField] private RectTransform attackRangeContainer;
    [SerializeField] private Image filledTilePrefab;
    [SerializeField] private Image outlineTilePrefab;
    [SerializeField] private float centerPositionOffset; // 타일 시각화 위치를 위한 중심 이동

    [Header("Operator Stat Boxes")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI redeployTimeText;
    [SerializeField] private TextMeshProUGUI attackPowerText;
    [SerializeField] private TextMeshProUGUI deploymentCostText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI blockCountText;
    [SerializeField] private TextMeshProUGUI magicResistanceText;
    [SerializeField] private TextMeshProUGUI attackSpeedText;

    private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    private OperatorSlot selectedSlot;
    private List<Image> rangeTiles = new List<Image>();
    private float tileSize; 

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        confirmButton.interactable = false;
        //gameObject.SetActive(false); // 이거 있으면 실행 전에 이 오브젝트가 비활성화된 경우 ShowPanel 등에 의한 활성화가 아예 안됨
    }

    private void OnEnable()
    {
        PopulateOperators();
        ResetSelection();
    }


    /// <summary>
    /// 보유한 오퍼레이터 리스트를 만들고 오퍼레이터 슬롯들을 초기화합니다.
    /// </summary>
    private void PopulateOperators()
    {
        // 슬롯 정리
        ClearSlots();

        // 현재 스쿼드 가져오기
        List<OperatorData> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquad();

        // 보유 중인 오퍼레이터 중, 현재 스쿼드에 없는 오퍼레이터만 가져옴
        List<OperatorData> availableOperators = PlayerDataManager.Instance.GetOwnedOperators()
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
        foreach (OperatorData operatorData in availableOperators)
        {
            OperatorSlot slot = Instantiate(slotButtonPrefab, operatorSlotContainer);
            slot.Initialize(true, operatorData);
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
    }
    
    /// <summary>
    /// 확인 버튼 클릭 시 동작
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        if (selectedSlot != null && selectedSlot.AssignedOperator != null)
        {
            GameManagement.Instance.UserSquadManager.ConfirmOperatorSelection(selectedSlot.AssignedOperator);
            // 돌아가기
            MainMenuManager.Instance.ShowPanel(MainMenuManager.MenuPanel.SquadEdit);
        }
    }

    /// <summary>
    /// 취소 버튼 클릭 시 동작
    /// </summary>
    private void OnCancelButtonClicked()
    {
        MainMenuManager.Instance.ShowPanel(MainMenuManager.MenuPanel.SquadEdit);
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
    /// OperatorSelectionPanel의 SideView에 나타나는 오퍼레이터와 관련된 정보를 업데이트한다.
    /// </summary>
    /// <param name="slot"></param>
    private void UpdateSideView(OperatorSlot slot)
    {

        OperatorData opData = slot.AssignedOperator;
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

        // 공격 범위 시각화
        UpdateVisualAttackRange(opData);
    }

    /// <summary>
    /// 공격 범위를 시각화합니다.
    /// </summary>
    private void UpdateVisualAttackRange(OperatorData opData)
    {
        // 기존 타일 제거
        ClearVisualAttackRange();

        // 기준점 타일 생성
        CreateRangeTile(Vector2Int.zero, true);

        foreach (Vector2Int pos in opData.attackableTiles)
        {
            // 180도 회전(원본이 <- 방향을 보기 때문에 -> 방향을 보도록 수정)
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
        // 프리팹 선택
        Image tilePrefab = isCenterTile ? filledTilePrefab : outlineTilePrefab;

        // 타일 생성
        Image tile = Instantiate(tilePrefab, attackRangeContainer);

        // 위치 개념 설정 -  사실상 gridPos 값에 (tileSize  + 1.5) 곱한 거랑 동일함
        tileSize = tile.rectTransform.rect.width;
        float interval = tileSize / 4f; 
        float gridX = gridPos.x * tileSize + gridPos.x * interval;
        float gridY = gridPos.y * tileSize + gridPos.y * interval;

        // 위치 설정 : 오프셋 반영
        tile.rectTransform.anchoredPosition = new Vector2(
            gridX - centerPositionOffset,
            gridY
        );

        rangeTiles.Add(tile);
    }

    private void OnDisable()
    {
        ClearVisualAttackRange();
        ResetSelection();
    }
}
