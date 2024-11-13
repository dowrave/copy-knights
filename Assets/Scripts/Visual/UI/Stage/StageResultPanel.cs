using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static StatisticsManager;

/// <summary>
/// �������� ������ ���������� ����� �Ŀ� ��Ÿ�� �г�
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
    [SerializeField] private GameObject resultStatisticPanelObject; // ��� �г� ��ü
    [SerializeField] private StatisticItem statisticItemPrefab;
    [SerializeField] private Transform statisticItemContainer;
    [SerializeField] private Button showPercentageTab; // ���� ��ġ <-> % ��ȯ ��ư
    [SerializeField] private Button damageDealtTab; // ���� ������� ��ư 
    [SerializeField] private Button damageTakenTab; // ���� ������� ��ư
    [SerializeField] private Button healingDoneTab; // ȸ���� ��ư

    [Header("Button Colors")]
    [SerializeField] private Color hoveredColor;
    [SerializeField] private Color selectedColor;
    private Color normalColor = Color.black;


    private bool showingPercentage = false;
    private StatisticsManager.StatType currentStatType = StatType.DamageDealt;
    private List<Button> statButtons; // ǥ��(%, Ÿ��) ��ȯ ��ư
    private List<StatisticItem> statItems = new List<StatisticItem>();
    private List<StatisticsManager.OperatorStats> allOperatorStats;
    private StageResultData resultData;
 
    private void Awake()
    {
        InitializeButtonList();

        // �г� ���� Ŭ�� �̺�Ʈ ����
        var panelClickHandler = gameObject.GetComponent<Button>();
        panelClickHandler.transition = Selectable.Transition.None; // �ð����� Ŭ�� ȿ�� ����
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
        // ��ư Ŭ�� �� �г� ���� ����
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            var currentSelected = EventSystem.current.currentSelectedGameObject;
        }

        // 3�� Ŭ��� �ƴ϶�� ���� ���������� ������ ���·� ���ư�
        if (!resultData.isCleared || resultData.StarCount < 3)
        {
            GameManagement.Instance.StageLoader.ReturnToMainMenuWithStageSelected();
        }
        else
        {
            GameManagement.Instance.StageLoader.ReturnToMainMenu();
        }
        Debug.Log("���� �޴��� ���ư��ϴ�");

    }

    /// <summary>
    /// �� ��ư�� ������ ���۰� �г� Ŭ�� ������ �����ϴ� �����ʸ� �߰��մϴ�.
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

        // �� ��ư�� ���� Ʈ������ ����
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
    /// �� �������� �����ִ� ������ �����մϴ�. 
    /// % ��ȯ�� �ְ�, DamageDealt, DamageTaken, HealingDone ��ȯ�� �ֽ��ϴ�.
    /// </summary>
    private void UpdateStats()
    {
        var sortedStats = StatisticsManager.Instance.GetSortedOperatorStats(currentStatType);
        Debug.Log("UpdateStats ����, sortedStats�� �޾ƿ�");
        Debug.Log($"CurrentStats : {currentStatType}");

        // StatItems ������
        for (int i = 0; i < statItems.Count; i++)
        {
            var (op, value) = sortedStats[i];

            // ���� ��ġ�� StatItem�� �ùٸ� ���۷����͸� ǥ���ϰ� �ִ��� Ȯ��
            if (statItems[i].Operator != op)
            {
                // �ùٸ� ��ġ�� StatItem ã��
                var correctItem = statItems.Find(item => item.Operator == op);
                if (correctItem != null)
                {
                    // Transform ���� ����
                    correctItem.transform.SetSiblingIndex(i);

                    // statItems ����Ʈ ������ ����
                    int oldIndex = statItems.IndexOf(correctItem);
                    statItems[oldIndex] = statItems[i];
                    statItems[i] = correctItem;
                }
            }

            // ��� ǥ�� ������Ʈ
            statItems[i].UpdateDisplay(currentStatType, showingPercentage);
        }
    }

    private void StopEventPropagation()
    {
        // ��ư Ŭ�� �̺�Ʈ�� �гη� ���ĵǴ� �� ������
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
    /// ��� â�� �������� ����, �̸�, Ŭ���� �ؽ�Ʈ�� �����մϴ�.
    /// </summary>
    private void UpdateHeaders()
    {
        StageData stageData = StageManager.Instance.StageData;
        stageIdText.text = $"{stageData.stageId}";
        stageNameText.text = $"{stageData.stageDetail}";
        clearOrFailedText.text = resultData.isCleared ? "���� ����" : "���� ����";
    }

    private void CreateStatItems()
    {
        foreach (var item in statItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        statItems.Clear();

        var sortedStats = StatisticsManager.Instance.GetSortedOperatorStats(currentStatType);

        // Grid Layout Group �� ũ�� ����
        AdjustGridCellSize(sortedStats.Count);

        // ���ο� ������ ����
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

            // �ۼ����� ��ư
            if (button == showPercentageTab)
            {
                colors.normalColor = showingPercentage ? selectedColor : normalColor;
            }

            // ��� Ÿ�� ��ư��
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
    /// ������ ũ�⿡ ���� Grid Layout Group�� �� ũ�⸦ �����մϴ�.
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