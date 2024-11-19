using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// �������� ������ ���������� ����� �Ŀ� ��Ÿ�� �г�
/// </summary>
public class StageResultPanel : MonoBehaviour
{
    [Header("Star Rating")]
    [SerializeField] private Image[] starImages; // star1, star2, star3 ������Ʈ��
    [SerializeField] private Sprite inactiveStarSprite;
    [SerializeField] private Sprite activeStarSprite;

    [Header("Star Aniamtion")]
    [SerializeField] private float starActivationInterval = 0.5f; // ���� �ִϸ��̼� ���۱��� ���͹�
    [SerializeField] private float starAnimationDuration = 0.5f; // �� �ִϸ��̼� ������ ���� �ӵ�
    [SerializeField] private Color inActiveStarColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color activeStarColor = Color.cyan;


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
    private StatisticsManager.StatType currentStatType = StatisticsManager.StatType.DamageDealt;
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
        // Ŭ���� UI ��Ұ� ��� �г��̳� �� �ڽ����� Ȯ��
        if (IsClickableElement(resultStatisticPanelObject))
        {
            Debug.Log("��� �г� Ŭ��");
            return;
        }

        bool isPerfectClear = resultData.passedEnemies == 0;
        StageManager.Instance.ReturnToMainMenu(isPerfectClear);
    }


    /// <summary>
    /// ��ü �г� �ܿ� ������ �����ؾ� �� ��Ҹ� Ŭ���ߴ°�?
    /// </summary>
    private bool IsClickableElement(GameObject targetObj)
    {
        if (!EventSystem.current.IsPointerOverGameObject()) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            // Ŭ���� ��ü�� ��� �г��̳� �� �ڽ����� Ȯ��
            Transform current = result.gameObject.transform;
            while (current != null)
            {
                if (current.gameObject == targetObj)
                {
                    return true;
                }
                current = current.parent;

            }    
        }

        return false;
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
            currentStatType = StatisticsManager.StatType.DamageDealt;
            UpdateStats();
            UpdateButtonVisuals();
            StopEventPropagation();
        });

        damageTakenTab.onClick.AddListener(() =>
        {
            currentStatType = StatisticsManager.StatType.DamageTaken;
            UpdateStats();
            UpdateButtonVisuals();
            StopEventPropagation();
        });

        healingDoneTab.onClick.AddListener(() =>
        {
            currentStatType = StatisticsManager.StatType.HealingDone;
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
    /// ��迡�� �����ִ� ������ �����մϴ�. 
    /// </summary>
    private void UpdateStats()
    {
        var sortedStats = StatisticsManager.Instance.GetSortedOperatorStats(currentStatType);

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
        UpdateButtonVisuals(); // ��ư�� ���� ���·� ���̵��� �ʱ�ȭ
    }

    private void UpdateStarRating()
    {
        // ��Ȱ��ȭ ���·� �ʱ�ȭ
        for (int i = 0; i < starImages.Length; i++)
        {
            SetStarInactive(starImages[i]);
        }

        if (resultData.StarCount > 0)
        {
            StartCoroutine(AnimateStars());
        }

    }

    private void SetStarInactive(Image starImage)
    {
        starImage.sprite = inactiveStarSprite;
        starImage.color = inActiveStarColor;
        starImage.transform.localScale = Vector3.one; 
    }

    // ���׸��� ���� ��Ȳ�̶� Ÿ���� �Ʒ�ó�� ���� / ��ҿ��� IEnumerator�� ���
    /// <summary>
    /// Star���� �ִϸ��̼�
    /// </summary>
    private System.Collections.IEnumerator AnimateStars()
    {
        for (int i = 0; i < resultData.StarCount; i++)
        {

            if (starImages[i] == null)
            {
                Debug.LogError($"Star image at index {i} is null!");
                continue;
            }
            int currentIndex = i;

            Image currentStar = starImages[currentIndex];

            // ���� ��� �۾����ٰ� 
            currentStar.transform.DOScale(0.5f, starAnimationDuration * 0.2f);

            // ��������Ʈ�� ������ ������ �� Ŀ���� �ִϸ��̼�
            Sequence starSequence = DOTween.Sequence().SetUpdate(true).SetAutoKill();

            starSequence.AppendCallback(() =>
            {
                currentStar.sprite = activeStarSprite;
            });

            starSequence.Append(currentStar.transform
                .DOScale(1.2f, starAnimationDuration * 0.4f)
                .SetEase(Ease.OutBack));

            starSequence.Join(currentStar.DOColor(activeStarColor, starAnimationDuration * 0.4f));

            // ũ�� ���� ����
            starSequence.Append(currentStar.transform
                .DOScale(1f, starAnimationDuration * 0.4f)
                .SetEase(Ease.OutBounce));

            // ������ �Ϸ� ���
            yield return starSequence.WaitForCompletion();

            // ���� ������ ������
            
            yield return new WaitForSecondsRealtime(starActivationInterval); // ���� : WaitForSeconds�� Time.timeScale�� ������ �޴´�.

        }

        Debug.Log("All star animations completed");
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
            item.Initialize(op, StatisticsManager.StatType.DamageDealt, false);
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
                StatisticsManager.StatType buttonType = GetButtonStatType(button);
                colors.normalColor = (currentStatType == buttonType) ? selectedColor : normalColor;
            }

            button.colors = colors;
        }
    }

    private StatisticsManager.StatType GetButtonStatType(Button button)
    {
        if (button == damageDealtTab) return StatisticsManager.StatType.DamageDealt;
        if (button == damageTakenTab) return StatisticsManager.StatType.DamageTaken;
        if (button == healingDoneTab) return StatisticsManager.StatType.HealingDone;
        return StatisticsManager.StatType.DamageDealt; // default
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