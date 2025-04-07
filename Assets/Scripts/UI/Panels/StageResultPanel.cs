using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스테이지 씬에서 스테이지가 종료된 후에 나타날 패널
/// </summary>
public class StageResultPanel : MonoBehaviour
{
    [Header("Star Rating")]
    [SerializeField] private Image[] starImages = default!; // star1, star2, star3 오브젝트들
    [SerializeField] private Sprite inactiveStarSprite = default!;
    [SerializeField] private Sprite activeStarSprite = default!;

    [Header("Star Aniamtion")]
    [SerializeField] private float starActivationInterval = 0.5f; // 다음 애니메이션 시작까지 인터벌
    [SerializeField] private float starAnimationDuration = 0.5f; // 별 애니메이션 동작의 기준 속도
    [SerializeField] private Color inActiveStarColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color activeStarColor = Color.cyan;

    [Header("Result Text")]
    [SerializeField] private TextMeshProUGUI stageIdText = default!;
    [SerializeField] private TextMeshProUGUI stageNameText = default!;
    [SerializeField] private TextMeshProUGUI clearOrFailedText = default!;

    [Header("Statistics")]
    [SerializeField] private TextMeshProUGUI statsTitleText = default!; 
    //[SerializeField] private GameObject resultStatisticPanelObject = default!; // 통계 패널 전체
    [SerializeField] private StatisticItem statisticItemPrefab = default!;
    [SerializeField] private Transform statisticItemContainer = default!;
    [SerializeField] private Button showPercentageTab = default!; // 절대 수치 <-> % 변환 버튼
    [SerializeField] private Button damageDealtTab = default!; // 가한 대미지량 버튼 
    [SerializeField] private Button damageTakenTab = default!; // 받은 대미지량 버튼
    [SerializeField] private Button healingDoneTab = default!; // 회복량 버튼

    [Header("About Reward Item")]
    [SerializeField] private ScrollRect rewardItemsScrollRect = default!;
    [SerializeField] private RectTransform rewardItemsViewportRect = default!;
    [SerializeField] private RectTransform rewardItemContentsContainerRect = default!;
    [SerializeField] private ItemUIElement itemUIPrefab = default!;

    private int visibleItemCount = 4; // 뷰포트에 최대로 나타나는 아이템의 갯수
    private float rightBoundaryPosition = 0; // 오른쪽 경계 스크롤 위치 (0 ~ 1)

    [Header("Button Colors")]
    [SerializeField] private Color hoveredColor = default!;
    [SerializeField] private Color selectedColor = default!;
    private Color normalColor = Color.black;

    [Header("Return To Lobby Button")]
    [SerializeField] private Button returnToLobbyButton = default!; // 오브젝트 순서 : 버튼이 위, 보상 아이템 컨테이너가 아래


    private bool showingPercentage = false;
    private StatisticsManager.StatType currentStatType = StatisticsManager.StatType.DamageDealt;
    private List<Button> statButtons = new List<Button>(); // 표시(%, 타입) 전환 버튼
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
        Debug.Log("로비로 돌아가기 버튼이 클릭됨");
        bool isPerfectClear = stars == 3;
        StageManager.Instance!.ReturnToMainMenu(isPerfectClear);
    }


    /// <summary>
    /// 전체 패널 외에 별도로 동작해야 할 요소를 클릭했는가?
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
            // 클릭된 객체가 통계 패널이나 그 자식인지 확인
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
    /// 각 버튼에 각각의 동작과 패널 클릭 동작을 방지하는 리스너를 추가합니다.
    /// </summary>
    private void SetupButtons()
    {
        showPercentageTab.onClick.AddListener(() =>
        {
            showingPercentage = !showingPercentage;
            UpdateStatContainer();
            StopEventPropagation();
        });

        damageDealtTab.onClick.AddListener(() =>
        {
            currentStatType = StatisticsManager.StatType.DamageDealt;
            UpdateStatContainer();
            StopEventPropagation();
        });

        damageTakenTab.onClick.AddListener(() =>
        {
            currentStatType = StatisticsManager.StatType.DamageTaken;
            UpdateStatContainer();
            StopEventPropagation();
        });

        healingDoneTab.onClick.AddListener(() =>
        {
            currentStatType = StatisticsManager.StatType.HealingDone;
            UpdateStatContainer();
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
    /// 통계에서 보여주는 정보를 변경합니다. 
    /// </summary>
    private void UpdateStats()
    {
        var sortedStats = StatisticsManager.Instance!.GetSortedOperatorStats(currentStatType);

        // 아이템 이름과 statisicItem 매핑
        Dictionary<string, StatisticItem> itemDict = new Dictionary<string, StatisticItem>();
        foreach (var item in statItems)
        {
            itemDict[item.OpData.entityName] = item;
        }

        // 순서를 정함
        List<StatisticItem> newOrder = new List<StatisticItem>();
        foreach (var (opData, _) in sortedStats)
        {
            newOrder.Add(itemDict[opData.entityName]);
        }

        // UI 업데이트
        for (int i=0; i < newOrder.Count; i++)
        {
            newOrder[i].transform.SetSiblingIndex(i);
            newOrder[i].UpdateDisplay(currentStatType, showingPercentage);
            
        }

        statItems = newOrder;
    }

    private void StopEventPropagation()
    {
        // 버튼 클릭 이벤트가 패널로 전파되는 걸 방지함
        EventSystem.current.SetSelectedGameObject(null!);
    }

    public void Initialize(int stars)
    {
        this.stars = stars; 
        allOperatorStats = StatisticsManager.Instance!.GetAllOperatorStats();

        UpdateStarRating();
        UpdateHeaders();
        CreateStatItems();
        UpdateStatContainer();
        ShowRewardItemsUI(); // stars > 0 일 때에만 동작
    }

    private void UpdateStarRating()
    {
        // 비활성화 상태로 초기화
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

    // 제네릭을 쓰는 상황이라 타입을 아래처럼 지정 / 평소에는 IEnumerator로 충분
    // Star들의 애니메이션
    private IEnumerator AnimateStars()
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

            // 별이 잠시 작아졌다가 
            currentStar.transform.DOScale(0.5f, starAnimationDuration * 0.2f);

            // 스프라이트와 색상을 변경한 후 커지는 애니메이션
            Sequence starSequence = DOTween.Sequence().SetUpdate(true).SetAutoKill();

            starSequence.AppendCallback(() =>
            {
                currentStar.sprite = activeStarSprite;
            });

            starSequence.Append(currentStar.transform
                .DOScale(1.2f, starAnimationDuration * 0.4f)
                .SetEase(Ease.OutBack));

            starSequence.Join(currentStar.DOColor(activeStarColor, starAnimationDuration * 0.4f));

            // 크기 원상 복구
            starSequence.Append(currentStar.transform
                .DOScale(1f, starAnimationDuration * 0.4f)
                .SetEase(Ease.OutBounce));

            // 시퀀스 완료 대기
            yield return starSequence.WaitForCompletion();

            // 다음 별까지 딜레이
            yield return new WaitForSecondsRealtime(starActivationInterval); // 주의 : WaitForSeconds는 Time.timeScale의 영향을 받는다.

        }
    }
    /// <summary>
    /// 결과 창의 스테이지 숫자, 이름, 클리어 텍스트를 변경합니다.
    /// </summary>
    private void UpdateHeaders()
    {
        StageData? stageData = StageManager.Instance!.StageData;
        if (stageData != null)
        {
            stageIdText.text = $"{stageData.stageId}";
            stageNameText.text = $"{stageData.stageName}";
            clearOrFailedText.text = stars > 0 ? "작전 종료" : "작전 실패";
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

        // Grid Layout Group 셀 크기 조정
        AdjustGridCellSize(sortedStats.Count);

        // 새로운 아이템 생성
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

            // 퍼센티지 버튼
            if (button == showPercentageTab)
            {
                colors.normalColor = showingPercentage ? selectedColor : normalColor;
            }

            // 통계 타입 버튼들
            else
            {
                StatisticsManager.StatType buttonType = GetButtonStatType(button);
                colors.normalColor = (currentStatType == buttonType) ? selectedColor : normalColor;
            }

            button.colors = colors;
        }
    }

    // 스테이지 클리어로 받는 아이템들을 보여줌
    private void ShowRewardItemsUI()
    {
        // 스테이지를 클리어하지 못했으면 동작 안 함
        if (stars == 0) return;

        RemoveRewardItemsUI();

        ShowItemElements(StageManager.Instance!.FirstClearRewards, true);
        ShowItemElements(StageManager.Instance!.BasicClearRewards);
    }

    private void ShowItemElements(IReadOnlyList<ItemWithCount> rewards, bool showFirst = false)
    {
        if (rewards.Count > 0)
        {
            foreach (var itemPair in rewards)
            {
                ItemUIElement itemElement = Instantiate(itemUIPrefab, rewardItemContentsContainerRect.transform);
                itemElement.Initialize(itemPair.itemData, itemPair.count, true, showFirst);
                activeItemElements.Add(itemElement);
            }
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


    // 스쿼드 크기에 따라 Grid Layout Group의 셀 크기를 조정합니다.
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

    private void UpdateStatContainer()
    {
        UpdateStats();
        UpdateButtonVisuals();
        UpdateStatContainerTitle();
    }

    private void UpdateStatContainerTitle()
    {
        string statTypeTitle = currentStatType switch
        {
            StatisticsManager.StatType.DamageDealt => "적에게 가한 피해",
            StatisticsManager.StatType.DamageTaken => "적으로부터 받은 피해",
            StatisticsManager.StatType.HealingDone => "치유",
            _ => "Unknown Stat"
        };

        string valueTypeTitle = showingPercentage ? "%" : "수치";

        statsTitleText.text = $"{statTypeTitle}({valueTypeTitle})";
    }

    private void OnScrollValueChanged(Vector2 normalizedPosition)
    {
        float horizontalPos = normalizedPosition.x;

        if (horizontalPos < rightBoundaryPosition + 0.01f)
        {
            rewardItemsScrollRect.horizontalNormalizedPosition = rightBoundaryPosition;
        }
    }

    // OnDisable에 구현하면 최초에 이 패널이 비활성화되므로 동작하지 않는 것들이 생김
    private void OnDestroy()
    {
        foreach (Button button in statButtons)
        {
            button.onClick.RemoveAllListeners();
        }

        returnToLobbyButton.onClick.RemoveAllListeners();
        RemoveRewardItemsUI();

        rewardItemsScrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
    }
}