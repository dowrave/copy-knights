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
    [SerializeField] private Image[] starImages = default!; // star1, star2, star3 ������Ʈ��
    [SerializeField] private Sprite inactiveStarSprite = default!;
    [SerializeField] private Sprite activeStarSprite = default!;

    [Header("Star Aniamtion")]
    [SerializeField] private float starActivationInterval = 0.5f; // ���� �ִϸ��̼� ���۱��� ���͹�
    [SerializeField] private float starAnimationDuration = 0.5f; // �� �ִϸ��̼� ������ ���� �ӵ�
    [SerializeField] private Color inActiveStarColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color activeStarColor = Color.cyan;

    [Header("Result Text")]
    [SerializeField] private TextMeshProUGUI stageIdText = default!;
    [SerializeField] private TextMeshProUGUI stageNameText = default!;
    [SerializeField] private TextMeshProUGUI clearOrFailedText = default!;

    [Header("Statistics")]
    //[SerializeField] private GameObject resultStatisticPanelObject = default!; // ��� �г� ��ü
    [SerializeField] private StatisticItem statisticItemPrefab = default!;
    [SerializeField] private Transform statisticItemContainer = default!;
    [SerializeField] private Button showPercentageTab = default!; // ���� ��ġ <-> % ��ȯ ��ư
    [SerializeField] private Button damageDealtTab = default!; // ���� ������� ��ư 
    [SerializeField] private Button damageTakenTab = default!; // ���� ������� ��ư
    [SerializeField] private Button healingDoneTab = default!; // ȸ���� ��ư

    [Header("About Reward Item")]
    [SerializeField] private Transform rewardItemContainer = default!;
    [SerializeField] private ItemUIElement itemUIPrefab = default!;

    [Header("Button Colors")]
    [SerializeField] private Color hoveredColor = default!;
    [SerializeField] private Color selectedColor = default!;
    private Color normalColor = Color.black;

    [Header("Return To Lobby Button")]
    [SerializeField] private Button returnToLobbyButton = default!;

    private bool showingPercentage = false;
    private StatisticsManager.StatType currentStatType = StatisticsManager.StatType.DamageDealt;
    private List<Button> statButtons = new List<Button>(); // ǥ��(%, Ÿ��) ��ȯ ��ư
    private List<StatisticItem> statItems = new List<StatisticItem>();
    private List<StatisticsManager.OperatorStats> allOperatorStats = new List<StatisticsManager.OperatorStats>();
    private int stars;

    private List<ItemUIElement> activeItemElements = new List<ItemUIElement>();

    private void Awake()
    {
        InitializeButtonList();
        returnToLobbyButton.onClick.AddListener(OnReturnButtonClicked);
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

    private void OnReturnButtonClicked()
    {
        bool isPerfectClear = stars == 3;
        StageManager.Instance!.ReturnToMainMenu(isPerfectClear);
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
        var sortedStats = StatisticsManager.Instance!.GetSortedOperatorStats(currentStatType);

        // StatItems ������
        for (int i = 0; i < statItems.Count; i++)
        {
            var (opData, value) = sortedStats[i];

            // ���� ��ġ�� StatItem�� �ùٸ� ���۷����͸� ǥ���ϰ� �ִ��� Ȯ��
            if (statItems[i].OpData != opData)
            {
                // �ùٸ� ��ġ�� StatItem ã��
                var correctItem = statItems.Find(item => item.OpData == opData);
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
        EventSystem.current.SetSelectedGameObject(null!);
    }


    public void Initialize(int stars)
    {
        this.stars = stars; 
        allOperatorStats = StatisticsManager.Instance!.GetAllOperatorStats();

        UpdateStarRating();
        UpdateHeaders();
        CreateStatItems();
        UpdateButtonVisuals(); // ��ư�� ���� ���·� ���̵��� �ʱ�ȭ
        ShowRewardItemsUI(); // stars > 0 �� ������ ����
    }

    private void UpdateStarRating()
    {
        // ��Ȱ��ȭ ���·� �ʱ�ȭ
        for (int i = 0; i < starImages.Length; i++)
        {
            SetStarInactive(starImages[i]);
        }

        if (stars > 0)
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
    // Star���� �ִϸ��̼�
    private System.Collections.IEnumerator AnimateStars()
    {
        for (int i = 0; i < stars; i++)
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
    }
    /// <summary>
    /// ��� â�� �������� ����, �̸�, Ŭ���� �ؽ�Ʈ�� �����մϴ�.
    /// </summary>
    private void UpdateHeaders()
    {
        StageData? stageData = StageManager.Instance!.StageData;
        if (stageData != null)
        {
            stageIdText.text = $"{stageData.stageId}";
            stageNameText.text = $"{stageData.stageDetail}";
            clearOrFailedText.text = stars > 0 ? "���� ����" : "���� ����";
        }
    }

    private void CreateStatItems()
    {
        foreach (var item in statItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        statItems.Clear();

        var sortedStats = StatisticsManager.Instance!.GetSortedOperatorStats(currentStatType);

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

            // �ۼ�Ƽ�� ��ư
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

    // �������� Ŭ����� �޴� �����۵��� ������
    private void ShowRewardItemsUI()
    {
        // ���������� Ŭ�������� �������� ���� �� ��
        if (stars == 0) return;

        RemoveRewardItemsUI();

        // UI�� ���� ������ ǥ��
        foreach (var itemPair in StageManager.Instance!.StageData!.rewardItems)
        {
            ItemUIElement itemElement = Instantiate(itemUIPrefab, rewardItemContainer);
            itemElement.Initialize(itemPair.itemData, itemPair.count, true);

            // ��� ���� �������� ���� ����
            //if (itemElement.itemCountBackground != null)
            //{
            //    itemElement.itemCountBackground.color = usageItemBackgroundColor;
            //}

            activeItemElements.Add(itemElement);
        }
    }

    private void RemoveRewardItemsUI()
    {
        foreach (ItemUIElement itemUIElement in activeItemElements)
        {
            Destroy(itemUIElement.gameObject);
        }
        activeItemElements.Clear();
    }

    private StatisticsManager.StatType GetButtonStatType(Button button)
    {
        if (button == damageDealtTab) return StatisticsManager.StatType.DamageDealt;
        if (button == damageTakenTab) return StatisticsManager.StatType.DamageTaken;
        if (button == healingDoneTab) return StatisticsManager.StatType.HealingDone;
        return StatisticsManager.StatType.DamageDealt; // default
    }


    // ������ ũ�⿡ ���� Grid Layout Group�� �� ũ�⸦ �����մϴ�.
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
    private void OnDisable()
    {
        foreach (Button button in statButtons)
        {
            button.onClick.RemoveAllListeners();
        }

        returnToLobbyButton.onClick.RemoveAllListeners();
        RemoveRewardItemsUI();
    }

    private void OnDestroy()
    {

    }
}