using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static StatisticsManager;

/// <summary>
/// 스테이지 씬에서 스테이지가 종료된 후에 나타날 패널
/// </summary>
public class StageResultPanel : MonoBehaviour
{
    [Header("Star Rating")]
    [SerializeField] private Image[] starImages;

    [Header("Result Text")]
    [SerializeField] private TextMeshProUGUI stageIdText;
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private TextMeshProUGUI clearOrFailedText;

    [Header("Statistics")]
    [SerializeField] private GameObject resultStatisticPanelObject; // 통계 패널 전체
    [SerializeField] private StatisticItem statisticItemPrefab;
    [SerializeField] private Transform statisticItemContainer;
    [SerializeField] private Button showPercentageTab; // 절대 수치 <-> % 변환 버튼
    [SerializeField] private Button damageDealtTab; // 가한 대미지량 버튼 
    [SerializeField] private Button damageTakenTab; // 받은 대미지량 버튼
    [SerializeField] private Button healingDoneTab; // 회복량 버튼

    [Header("Button Colors")]
    [SerializeField] private Color hoveredColor;
    [SerializeField] private Color selectedColor;
    private Color normalColor = Color.black;


    private bool showingPercentage = false;
    private StatisticsManager.StatType currentStatType = StatType.DamageDealt;
    private List<Button> statButtons; // 표시(%, 타입) 전환 버튼
    private List<StatisticItem> statItems = new List<StatisticItem>();
    private List<StatisticsManager.OperatorStats> allOperatorStats;
    private StageResultData resultData;
 
    private void Awake()
    {
        InitializeButtonList();

        // 패널 영역 클릭 이벤트 설정
        var panelClickHandler = gameObject.GetComponent<Button>();
        panelClickHandler.transition = Selectable.Transition.None; // 시각적인 클릭 효과 제거
        panelClickHandler.onClick.AddListener(OnPanelClicked);

        SetupButtons();
    }

    private void InitializeButtonList()
    {
        statButtons = new List<Button>
        {
            showPercentageTab,
            damageDealtTab,
            damageTakenTab,
            healingDoneTab
        };
    }

    private void OnPanelClicked()
    {
        // 버튼 클릭 시 패널 동작 무시
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            var currentSelected = EventSystem.current.currentSelectedGameObject;
        }

        // 3성 클리어가 아니라면 현재 스테이지를 선택한 상태로 돌아감
        if (!resultData.isCleared || resultData.StarCount < 3)
        {
            GameManagement.Instance.StageLoader.ReturnToMainMenuWithStageSelected();
        }
        else
        {
            GameManagement.Instance.StageLoader.ReturnToMainMenu();
        }
        Debug.Log("메인 메뉴로 돌아갑니다");

    }

    /// <summary>
    /// 각 버튼에 각각의 동작과 패널 클릭 동작을 방지하는 리스너를 추가합니다.
    /// </summary>
    private void SetupButtons()
    {
        showPercentageTab.onClick.AddListener(() =>
        {
            showingPercentage = !showingPercentage;
            UpdateStats();
            UpdateButtonVisuals();
            StopEventPropagation();
        });

        damageDealtTab.onClick.AddListener(() =>
        {
            currentStatType = StatType.DamageDealt;
            UpdateStats();
            UpdateButtonVisuals();
            StopEventPropagation();
        });

        damageTakenTab.onClick.AddListener(() =>
        {
            currentStatType = StatType.DamageTaken;
            UpdateStats();
            UpdateButtonVisuals();
            StopEventPropagation();
        });

        healingDoneTab.onClick.AddListener(() =>
        {
            currentStatType = StatType.HealingDone;
            UpdateStats();
            UpdateButtonVisuals();
            StopEventPropagation();
        });

        // 각 버튼에 색상 트랜지션 설정
        foreach (var button in statButtons)
        {
            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = hoveredColor;
            colors.selectedColor = selectedColor;
            colors.pressedColor = selectedColor * 0.9f;
            button.colors = colors;
        }
    }

    /// <summary>
    /// 각 아이템이 보여주는 정보를 변경합니다. 
    /// % 전환이 있고, DamageDealt, DamageTaken, HealingDone 전환이 있습니다.
    /// </summary>
    private void UpdateStats()
    {
        var sortedStats = StatisticsManager.Instance.GetSortedOperatorStats(currentStatType);
        Debug.Log("UpdateStats 동작, sortedStats을 받아옴");
        Debug.Log($"CurrentStats : {currentStatType}");

        // StatItems 재정렬
        for (int i = 0; i < statItems.Count; i++)
        {
            var (op, value) = sortedStats[i];

            // 현재 위치의 StatItem이 올바른 오퍼레이터를 표시하고 있는지 확인
            if (statItems[i].Operator != op)
            {
                // 올바른 위치의 StatItem 찾기
                var correctItem = statItems.Find(item => item.Operator == op);
                if (correctItem != null)
                {
                    // Transform 순서 변경
                    correctItem.transform.SetSiblingIndex(i);

                    // statItems 리스트 순서도 변경
                    int oldIndex = statItems.IndexOf(correctItem);
                    statItems[oldIndex] = statItems[i];
                    statItems[i] = correctItem;
                }
            }

            // 통계 표시 업데이트
            statItems[i].UpdateDisplay(currentStatType, showingPercentage);
        }
    }

    private void StopEventPropagation()
    {
        // 버튼 클릭 이벤트가 패널로 전파되는 걸 방지함
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }


    public void Initialize(StageResultData data)
    {
        resultData = data;
        allOperatorStats = StatisticsManager.Instance.GetAllOperatorStats();

        UpdateStarRating();
        UpdateHeaders();
        CreateStatItems();
    }

    private void UpdateStarRating()
    {
        for (int i = 0; i < resultData.StarCount; i ++)
        {
            starImages[i].color = Color.cyan;
        }
    }

    /// <summary>
    /// 결과 창의 스테이지 숫자, 이름, 클리어 텍스트를 변경합니다.
    /// </summary>
    private void UpdateHeaders()
    {
        StageData stageData = StageManager.Instance.StageData;
        stageIdText.text = $"{stageData.stageId}";
        stageNameText.text = $"{stageData.stageDetail}";
        clearOrFailedText.text = resultData.isCleared ? "작전 종료" : "작전 실패";
    }

    private void CreateStatItems()
    {
        foreach (var item in statItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        statItems.Clear();

        var sortedStats = StatisticsManager.Instance.GetSortedOperatorStats(currentStatType);

        // Grid Layout Group 셀 크기 조정
        AdjustGridCellSize(sortedStats.Count);

        // 새로운 아이템 생성
        foreach (var (op, value) in sortedStats)
        {
            StatisticItem item = Instantiate(statisticItemPrefab, statisticItemContainer);
            item.Initialize(op, StatType.DamageDealt, false);
            statItems.Add(item);
        }
    }

    private void UpdateButtonVisuals()
    {
        foreach (var button in statButtons)
        {
            var colors = button.colors;

            // 퍼센테지 버튼
            if (button == showPercentageTab)
            {
                colors.normalColor = showingPercentage ? selectedColor : normalColor;
            }

            // 통계 타입 버튼들
            else
            {
                StatType buttonType = GetButtonStatType(button);
                colors.normalColor = (currentStatType == buttonType) ? selectedColor : normalColor;
            }

            button.colors = colors;
        }
    }

    private StatType GetButtonStatType(Button button)
    {
        if (button == damageDealtTab) return StatType.DamageDealt;
        if (button == damageTakenTab) return StatType.DamageTaken;
        if (button == healingDoneTab) return StatType.HealingDone;
        return StatType.DamageDealt; // default
    }

    /// <summary>
    /// 스쿼드 크기에 따라 Grid Layout Group의 셀 크기를 조정합니다.
    /// </summary>
    private void AdjustGridCellSize(int squadCount)
    {
        var gridLayout = statisticItemContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            Vector2 cellSize = gridLayout.cellSize;
            cellSize.x = squadCount > 8 ? 250f : 400f;
            gridLayout.cellSize = cellSize;
        }
        else
        {
            Debug.LogWarning("Grid Layout Group component not found on statisticItemContainer");
        }
    }

    private void OnDestroy()
    {
        foreach (Button button in statButtons)
        {
            button.onClick.RemoveAllListeners();
        }
    }
}