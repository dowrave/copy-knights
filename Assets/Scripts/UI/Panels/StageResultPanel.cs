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
    [SerializeField] private Image[] starImages; // star1, star2, star3 오브젝트들
    [SerializeField] private Sprite inactiveStarSprite;
    [SerializeField] private Sprite activeStarSprite;

    [Header("Star Aniamtion")]
    [SerializeField] private float starActivationInterval = 0.5f; // 다음 애니메이션 시작까지 인터벌
    [SerializeField] private float starAnimationDuration = 0.5f; // 별 애니메이션 동작의 기준 속도
    [SerializeField] private Color inActiveStarColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color activeStarColor = Color.cyan;


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
    private StatisticsManager.StatType currentStatType = StatisticsManager.StatType.DamageDealt;
    private List<Button> statButtons; // 표시(%, 타입) 전환 버튼
    private List<StatisticItem> statItems = new List<StatisticItem>();
    private List<StatisticsManager.OperatorStats> allOperatorStats;
    private StageResultData resultData;
    private int stars;

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
        // 클릭된 UI 요소가 통계 패널이나 그 자식인지 확인
        if (IsClickableElement(resultStatisticPanelObject))
        {
            Debug.Log("통계 패널 클릭");
            return;
        }

        bool isPerfectClear = stars == 3;
        StageManager.Instance.ReturnToMainMenu(isPerfectClear);
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
        var sortedStats = StatisticsManager.Instance.GetSortedOperatorStats(currentStatType);

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


    public void Initialize(int stars)
    {
        this.stars = stars; 
        allOperatorStats = StatisticsManager.Instance.GetAllOperatorStats();

        UpdateStarRating();
        UpdateHeaders();
        CreateStatItems();
        UpdateButtonVisuals(); // 버튼이 눌린 상태로 보이도록 초기화
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
    /// <summary>
    /// Star들의 애니메이션
    /// </summary>
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
        StageData stageData = StageManager.Instance.StageData;
        stageIdText.text = $"{stageData.stageId}";
        stageNameText.text = $"{stageData.stageDetail}";
        clearOrFailedText.text = stars > 0 ? "작전 종료" : "작전 실패";
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
            item.Initialize(op, StatisticsManager.StatType.DamageDealt, false);
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