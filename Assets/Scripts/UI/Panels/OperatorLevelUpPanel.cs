using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DG.Tweening;

public class OperatorLevelUpPanel : MonoBehaviour
{
    [Header("Level Strip Components")]
    [SerializeField] private ScrollRect levelScrollRect = default!;
    [SerializeField] private RectTransform contentRect = default!;
    [SerializeField] private GameObject levelTextPrefab = default!;

    [Header("Info Display")]
    [SerializeField] private Button confirmButton = default!;
    [SerializeField] private Slider expGauge = default!;
    [SerializeField] private Button maxLevelButton = default!; 

    [Header("Info Settings")]
    [SerializeField] private float snapSpeed = 10f; // 스냅 애니메이션 속도

    [Header("Item Usage Display")]
    [SerializeField] private Transform itemUsageContainer = default!; // 아이템 표시 컨테이너
    [SerializeField] private ItemUIElement itemUIPrefab = default!; // 아이템 UI 프리팹
    [SerializeField] private Color usageItemBackgroundColor = new Color(0.8f, 0.4f, 0.2f, 1f); // 사용되는 아이템 갯수 색깔
    [SerializeField] private TextMeshProUGUI cantReachLevelText = default!; // 해당 레벨에 도달이 불가능할 때 아이템 대신 등장
    private List<ItemUIElement> activeItemElements = new List<ItemUIElement>(); // 현재 표시중인 아이템 요소들

    [SerializeField] private float velocityThreshold = 0.1f; // 스크롤이 멈췄다고 판단하는 속도 임계값

    private float snapThreshold; // 스냅 거리 임계값. 지우지 않도록 주의.
    private string updateColor = string.Empty;

    private ExpCalculationSystem.ExpItemUsagePlan currentUsagePlan;
    int maxReachableLevel;

    [System.Serializable]
    public class StatPreviewLine
    {
        public TextMeshProUGUI labelText = default!;
        public TextMeshProUGUI currentValue = default!;
        public TextMeshProUGUI arrowText = default!;
        public TextMeshProUGUI newValue = default!;
    }

    [Header("Stat Previews")]
    [SerializeField] private StatPreviewLine healthPreview = default!;
    [SerializeField] private StatPreviewLine attackPreview = default!;
    [SerializeField] private StatPreviewLine defensePreview = default!;
    [SerializeField] private StatPreviewLine magicResistancePreview = default!;

    private OwnedOperator op = default!;
    private int currentLevel;
    private int maxLevel;
    private int selectedLevel;
    private int totalLevels;

    // 스크롤 관련 변수
    private float contentHeight; // 전체 컨텐츠의 높이
    private float viewportHeight; // 뷰포트의 높이
    private bool isMousePressed = false;
    private bool isScrolling = false; // 관성 스크롤 중인지 여부
    private bool isSnapping = false;
    private bool isInitialized = false;
    private bool isUpdatingPanel = false;
    private bool isPanelUpdated = false; // 이 레벨에 대한 패널이 업데이트 됐으면 true

    private Vector2 maxButtonOriginalPosition;
    private readonly float buttonOffsetX = 50f;

    private ExpCalculationSystem.ExpItemUsagePlan maxLevelPlan;


    // 각 레벨에 대한 스크롤 위치를 저장하는 dict
    private Dictionary<int, float> levelToScrollPosition = new Dictionary<int, float>();

    private void Awake()
    {
        if (levelScrollRect != null)
        {
            // 스크롤이 변할 때마다 실행된다. onValueChanged는 내부의 normalizedPositon([0, 1]) 값의 변화를 감지함
            levelScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        if (maxLevelButton != null)
        {
            maxLevelButton.onClick.AddListener(OnMaxLevelButtonClicked);

            RectTransform? maxButtonRect = maxLevelButton.GetComponent<RectTransform>();
            if (maxButtonRect != null)
            {
                maxButtonOriginalPosition = maxButtonRect.anchoredPosition;
                updateColor = GameManagement.Instance!.ResourceManager.TextUpdateColor;
                cantReachLevelText.gameObject.SetActive(false);
                SetMaxLevelButtonVisible(true);
            }
        }
    }


    public void Initialize(OwnedOperator op)
    {
        this.op = op;
        currentLevel = op.CurrentLevel;
        maxLevel = OperatorGrowthSystem.GetMaxLevel(op.CurrentPhase);
        selectedLevel = currentLevel;
        viewportHeight = levelScrollRect.viewport.rect.height;
        totalLevels = maxLevel - currentLevel + 1;

        // UI 초기화
        InitializeLevelStrip();
        UpdateExpGauge();
        InitializeStatTexts();

        // 초기 위치 설정
        SetInitialScrollPositon(); 

        isInitialized = true; 
    }

    // 각 레벨 오브젝트를 스크롤 상에 배치함
    private void InitializeLevelStrip()
    {
        // 기존 레벨 텍스트 제거
        foreach (Transform child in contentRect)
        {
            Destroy(child.gameObject);
        }

        // 달성 가능 최대 레벨
        maxLevelPlan = OperatorGrowthManager.Instance!.CalculateRequiredItems(op, maxLevel);
        maxReachableLevel = maxLevelPlan.targetLevel; 

        VerticalLayoutGroup layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        float spacing = layoutGroup != null ? layoutGroup.spacing : 0f;
        float paddingHeight = viewportHeight * 0.5f - spacing;

        // 상단 여백 구현
        CreatePadding("TopPadding", paddingHeight);

        // 현재 ~ 최대 레벨의 텍스트 생성(최대 레벨부터 역순으로 배치)
        for (int level = maxLevel; level >= currentLevel ; level--)
        {
            GameObject levelObj = Instantiate(levelTextPrefab, contentRect);
            TextMeshProUGUI levelText = levelObj.GetComponent<TextMeshProUGUI>();
            if (levelText != null)
            {
                levelText.text = $"<size=32>Lv</size>\r\n{level}";

                if (level > maxReachableLevel)
                {
                    Color grayColor = new Color(0.5f, 0.5f, 0.5f);
                    levelText.color = grayColor;
                }
            }
        }

        // 하단 여백 구현
        CreatePadding("BottomPadding", paddingHeight);

        CalculateScrollPositions();
        SetSnapThreshold();
    }

    private void InitializeStatTexts()
    {
        if (op != null)
        {
            OperatorStats initialStats = op.CurrentStats;
            healthPreview.currentValue.text = Mathf.FloorToInt(initialStats.Health).ToString(); 
            attackPreview.currentValue.text = Mathf.FloorToInt(initialStats.AttackPower).ToString();
            defensePreview.currentValue.text = Mathf.FloorToInt(initialStats.Defense).ToString();
            magicResistancePreview.currentValue.text = Mathf.FloorToInt(initialStats.MagicResistance).ToString();

            healthPreview.newValue.text = Mathf.FloorToInt(initialStats.Health).ToString();
            attackPreview.newValue.text = Mathf.FloorToInt(initialStats.AttackPower).ToString();
            defensePreview.newValue.text = Mathf.FloorToInt(initialStats.Defense).ToString();
            magicResistancePreview.newValue.text = Mathf.FloorToInt(initialStats.MagicResistance).ToString();
        }
    }

    // 초기 스크롤의 위치 설정
    private void SetInitialScrollPositon()
    {
        // 스크롤 위치 즉시 설정
        levelScrollRect.verticalNormalizedPosition = GetScrollPositionForLevel(currentLevel);

        // 스크롤 속도 초기화
        levelScrollRect.velocity = Vector2.zero;
    }

    private void CreatePadding(string name, float height)
    {
        GameObject padding = new GameObject(name);
        padding.transform.SetParent(contentRect, false);
        RectTransform paddingRect = padding.AddComponent<RectTransform>();
        paddingRect.sizeDelta = new Vector2(0, height);
    }



    // 각 레벨 오브젝트와 스크롤 높이를 매핑하는 dict를 만듦
    // 스크롤이 끝나는 양쪽 끝에 currentLevel과 maxLevel이 와야 함
    private void CalculateScrollPositions()
    { 
        levelToScrollPosition.Clear();

        // 높이에 스페이싱 반영
        VerticalLayoutGroup layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        float spacing = layoutGroup != null ? layoutGroup.spacing : 0f;
        float paddingHeight = viewportHeight * 0.5f - spacing;

        // 전체 content 영역의 높이
        contentHeight = paddingHeight * 2 + // 상하 패딩
            (spacing * (totalLevels + 1));  // 각 요소 사이의 간격. 2개의 패딩까지 추가해서 spacing이 2개 더 생김

        // 스크롤 가능 영역 높이 (패딩 영역 제외)
        // 스크롤이 0일 때 currentLevel이 중앙에, 1일 때 maxLevel이 중앙에 온다
        float totalScrollHeight = contentHeight - viewportHeight; 

        for (int level = currentLevel; level <= maxLevel; level++)
        {
            int index = level - currentLevel;

            // 각 숫자 오브젝트가 중앙에 오는 높이
            float centerOffset = index * spacing; 

            // 정규화된 위치 계산 (0~1 사이로 보장)
            float normalizedPosition = Mathf.Clamp01(centerOffset / totalScrollHeight); // 정규화

            // 정규화된 스크롤값은 currentLevel일 때 0, maxLevel일 때 1이 된다.
            levelToScrollPosition[level] = normalizedPosition;
        }
    }

    // 스냅핑 간격 설정
    private void SetSnapThreshold()
    {
        // 현재 정예화의 최대 레벨이 아닐 때에만 계산
        if (currentLevel != OperatorGrowthSystem.GetMaxLevel(op.CurrentPhase))
        {
            snapThreshold = levelToScrollPosition[currentLevel + 1] - levelToScrollPosition[currentLevel];
        }
    }

    // 특정 레벨의 스크롤 위치를 가져옴
    private float GetScrollPositionForLevel(int level)
    {
        return levelToScrollPosition.TryGetValue(level, out float position) ? position : 0f;
    }

    // 현재 스크롤 위치에서 가장 가까운 레벨을 찾음
    private int FindNearestLevel(float currentScrollPosition)
    {
        return levelToScrollPosition
            .OrderBy(kvp => Mathf.Abs(kvp.Value - currentScrollPosition))
            .First()
            .Key;
    }

    private void SetScrollToLevel(int targetLevel, bool animate = false)
    {
        float targetPosition = GetScrollPositionForLevel(targetLevel);

        if (animate)
        {
            isSnapping = true;
            DOTween.Kill(levelScrollRect);

            DOTween.To(
                () => levelScrollRect.verticalNormalizedPosition,
                x => levelScrollRect.verticalNormalizedPosition = x,
                targetPosition,
                0.25f // 애니메이션 속도
            )
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
             {
                 // 스크롤 완료 후 해당 레벨 정보 업데이트
                 StartCoroutine(UpdatePanelWithDelay(targetLevel));
                 isSnapping = false;
             });
        }
        else
        {
            // 즉시 이동
            levelScrollRect.verticalNormalizedPosition = targetPosition;
        }
    }

    private void Update()
    {
        if (!isInitialized) return;
        if (isSnapping) return; 

        // 스크롤 상태 확인
        Vector2 velocity = levelScrollRect.velocity;

        //임계치를 넘으면 스크롤 중이라고 판단
        isScrolling = velocity.magnitude > velocityThreshold;

        // 마우스 버튼 다운 중인지 확인(GetMouseButtonDown은 눌린 순간만을 감지)
        isMousePressed = Input.GetMouseButton(0);

        if (isScrolling)
        {
            SetMaxLevelButtonVisible(false);

            // isMousePressed의 조건 상, 확인 버튼을 누르려고 할 때 동작할 가능성이 있어서 따로 분리
            UpdateConfirmButton();
        }

        // 스크롤 중일 때의 처리
        if (isMousePressed || isScrolling)
        {
            int newLevel = FindNearestLevel(levelScrollRect.verticalNormalizedPosition);
            if (selectedLevel != newLevel)
            {
                selectedLevel = newLevel;
                isPanelUpdated = false; // 레벨이 바뀌면 패널을 새로 업데이트해야 함
            }
            return; 
        }


        // 스크롤이 멈추고 마우스가 떨어지면 스냅핑 처리
        if (!isUpdatingPanel)
        {
            float currentScrollPos = levelScrollRect.verticalNormalizedPosition;

            SetSelectedLevelByItemUsagePlan(currentScrollPos);

            float selectedLevelPos = GetScrollPositionForLevel(selectedLevel);

            // 가장 가까운 레벨로 부드럽게 스냅
            levelScrollRect.verticalNormalizedPosition = Mathf.Lerp(
                currentScrollPos,
                selectedLevelPos,
                Time.deltaTime * snapSpeed
            );

            if (Mathf.Abs(currentScrollPos - selectedLevelPos) < 0.05f && !isPanelUpdated) // 레벨 당 간격이 0.02 언더라서 조건은 적합해보임
            {
                StartCoroutine(UpdatePanelWithDelay(selectedLevel));
            }
        }
    }


    /// <summary>
    /// 스크롤 위치에서 가장 가까운 레벨을 선택하고 
    /// 해당 레벨까지 필요한 아이템 사용 계획을 계산하여,
    /// 만약 결과 targetLevel이 변경되면 해당 레벨로 스크롤 이동합니다.
    /// </summary>
    private void SetSelectedLevelByItemUsagePlan(float currentScrollPos)
    {
        selectedLevel = FindNearestLevel(currentScrollPos);

        // 해당 레벨에 도달하기 위한 아이템 사용 계획을 가져옴
        currentUsagePlan = OperatorGrowthManager.Instance!.CalculateRequiredItems(op, selectedLevel);

        // 아이템들을 사용했을 때 도달하는 레벨이 유저가 지정한 레벨보다 높다면 해당 레벨로 이동
        if (currentUsagePlan.targetLevel > selectedLevel)
        {
            SetScrollToLevel(currentUsagePlan.targetLevel, true);
            selectedLevel = currentUsagePlan.targetLevel;
            isPanelUpdated = false; // 상태를 넣었으니 패널 업데이트 전까지는 비활성화
        }
    }

    private IEnumerator UpdatePanelWithDelay(int targetLevel)
    {
        // 패널 업데이트 시작
        isUpdatingPanel = true;
        selectedLevel = targetLevel;

        yield return null; // 한 프레임 대기

        // 스탯 패널 업데이트
        OperatorStats targetLevelStats = OperatorGrowthSystem.CalculateStats(op, targetLevel, op.CurrentPhase);

        // 레벨업 상황일 때 텍스트에 색깔만 추가
        if (targetLevel == currentLevel)
        {
            healthPreview.newValue.text = Mathf.FloorToInt(targetLevelStats.Health).ToString();
            attackPreview.newValue.text = Mathf.FloorToInt(targetLevelStats.AttackPower).ToString();
            defensePreview.newValue.text = Mathf.FloorToInt(targetLevelStats.Defense).ToString();
        }
        else
        {
            healthPreview.newValue.text = $"<color={updateColor}>{Mathf.FloorToInt(targetLevelStats.Health).ToString()}</color>";
            attackPreview.newValue.text = $"<color={updateColor}>{Mathf.FloorToInt(targetLevelStats.AttackPower).ToString()}</color>";
            defensePreview.newValue.text = $"<color={updateColor}>{Mathf.FloorToInt(targetLevelStats.Defense).ToString()}</color>";
        }
        magicResistancePreview.newValue.text = targetLevelStats.MagicResistance.ToString(); // 마법 저항력은 레벨업으로 바뀌지 않음


        UpdateLevelUpPreviewDisplay();

        // 패널 업데이트 완료
        isUpdatingPanel = false;
        isPanelUpdated = true;  // 레벨이 바뀌면 다시 false가 됨
        UpdateConfirmButton();
        SetMaxLevelButtonVisible(true);
    }

    private void OnScrollValueChanged(Vector2 value)
    {
        if (!isInitialized) return;
    }

    private void UpdateExpGauge()
    {
        if (selectedLevel <= maxReachableLevel)
        {
            float currentExp = op.CurrentExp;
            float maxExp = OperatorGrowthSystem.GetMaxExpForNextLevel(op.CurrentPhase, op.CurrentLevel);
            expGauge.value = currentExp / maxExp;
        }
        else
        {
            expGauge.value = 0f;
        }
    }

    private void UpdateConfirmButton()
    {
        bool canConfirm = !isScrolling && // 스크롤 중이 아닐 때
            !isSnapping && // 스냅핑 중이 아닐 때
            isPanelUpdated && // 패널이 다 업데이트 됐을 때
            selectedLevel > currentLevel && // 레벨 제한
            selectedLevel <= maxReachableLevel;

        // 확인 버튼 활성화 상태 업데이트
        if (confirmButton != null)
        {
            confirmButton.interactable = canConfirm;
        }
    }

    private void OnConfirmButtonClicked()
    {
        if (!confirmButton.interactable || selectedLevel <= currentLevel) return;

        // 레벨업 진행 요청
        bool success = OperatorGrowthManager.Instance!.TryLevelUpOperator(
            op,
            selectedLevel,
            currentUsagePlan
        );

        if (success)
        {
            // UI 업데이트
            InitializeStatTexts();
            ClearItemDisplay();
            UpdateExpGauge();
            NotificationToastManager.Instance!.ShowNotification($"레벨업 완료 : 현재 레벨 {selectedLevel}");
            UpdateLevelStrip(selectedLevel);
            UpdateConfirmButton();
        }
        else
        {
            NotificationToastManager.Instance!.ShowNotification("레벨업에 실패했습니다");
        }
    }


    // 레벨업 완료 후, 스크롤 부분 업데이트
    private void UpdateLevelStrip(int newLevel)
    {
        currentLevel = newLevel; 
        totalLevels = maxLevel - currentLevel + 1;
        InitializeLevelStrip();
        SetScrollToLevel(currentLevel);
    }


    // 레벨업에 필요한 아이템 미리보기, 경험치 게이지 표시 등
    private void UpdateLevelUpPreviewDisplay()
    {
        ClearItemDisplay();

        // 도달이 불가능하다면 아이템 표시 패널에 불가능 표시
        if (selectedLevel > maxReachableLevel)
        {
            cantReachLevelText.gameObject.SetActive(true);
        }
        else
        {
            cantReachLevelText.gameObject.SetActive(false);

            // UI에 사용될 아이템 표시
            foreach (var itemPair in currentUsagePlan.itemsToUse)
            {
                ItemUIElement itemElement = Instantiate(itemUIPrefab, itemUsageContainer);
                itemElement.Initialize(itemPair.Key, itemPair.Value, false);

                // 사용 예정 아이템의 배경색 변경
                if (itemElement.itemCountBackground != null)
                {
                    itemElement.itemCountBackground.color = usageItemBackgroundColor;
                }

                activeItemElements.Add(itemElement);
            }

            // 해당 레벨에서의 경험치 게이지 표시
            if (currentUsagePlan.totalItemExp > 0)
            {
                float maxExpForLevel = OperatorGrowthSystem.GetMaxExpForNextLevel(
                    op.CurrentPhase,
                    currentUsagePlan.targetLevel
                );
                expGauge.value = currentUsagePlan.remainingExp / maxExpForLevel;
            }
        }
         
    }

    private void ClearItemDisplay()
    {
        // 기존 아이템 UI 제거
        foreach (var element in activeItemElements)
        {
            Destroy(element.gameObject);
        }
        activeItemElements.Clear();
    }

    private void OnMaxLevelButtonClicked()
    {
        List<(ItemData, int)> availableItems = GameManagement.Instance!.PlayerDataManager.GetAllItems()
            .Where(x => x.itemData.type == ItemData.ItemType.Exp)
            .ToList();

        var (maxLevel, usagePlan) = ExpCalculationSystem.CalculateMaxLevel(op, availableItems);


        // 스크롤 설정
        SetScrollToLevel(maxLevel, true);
        UpdateLevelUpPreviewDisplay();

        if (maxLevel > op.CurrentLevel)
        {
            currentUsagePlan = usagePlan;
        }
    }


    // 스크롤 상태에 따른 버튼 표시 설정
    private void SetMaxLevelButtonVisible(bool visible)
    {
        maxLevelButton.transform.DOKill(); // 진행 중인 애니메이션 중단
        CanvasGroup buttonCanvasGroup = maxLevelButton.GetComponent<CanvasGroup>();

        // 목표 위치와 투명도 설정
        Vector2 targetAnchoredPosition = visible ?
            maxButtonOriginalPosition :
            maxButtonOriginalPosition + new Vector2(buttonOffsetX, 0f);

        float targetAlpha = visible ? 1f : 0.3f;

        // 애니메이션 시퀀스 생성
        Sequence sequence = DOTween.Sequence();
        sequence.Join(maxLevelButton.GetComponent<RectTransform>()
            .DOAnchorPos(targetAnchoredPosition, 0.3f)
            .SetEase(Ease.OutQuad));
        sequence.Join(buttonCanvasGroup
            .DOFade(targetAlpha, 0.3f)
            .SetEase(Ease.OutQuad));
    }

    private void OnEnable()
    {
        // 패널 활성화마다 스크롤 위치를 현재 레벨로 초기화
        if (isInitialized)
        {
            SetInitialScrollPositon();
        }
    }
    private void OnDisable()
    {
        // 패널이 비활성화될 때 모든 상태를 초기화
        isInitialized = false;
        currentLevel = 0;
        maxLevel = 0;
        selectedLevel = 0;
        totalLevels = 0;
        levelToScrollPosition.Clear();

        // 스크롤뷰의 컨텐츠도 정리
        if (contentRect != null)
        {
            foreach (Transform child in contentRect)
            {
                Destroy(child.gameObject);
            }
        }

        // 스크롤 위치도 리셋
        if (levelScrollRect != null)
        {
            levelScrollRect.velocity = Vector2.zero;
            levelScrollRect.verticalNormalizedPosition = 0f;
        }
    }

}
