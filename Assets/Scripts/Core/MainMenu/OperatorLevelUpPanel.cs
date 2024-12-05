
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class OperatorLevelUpPanel : MonoBehaviour
{
    [Header("Level Strip Components")]
    [SerializeField] private ScrollRect levelScrollRect;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private GameObject levelTextPrefab;

    [Header("Info Display")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Slider expGauge;

    [Header("Info Settings")]
    [SerializeField] private float snapSpeed = 10f; // 스냅 애니메이션 속도

    [SerializeField] private float velocityThreshold = 0.5f; // 스크롤이 멈췄다고 판단하는 속도 임계값

    private float snapThreshold; // 스냅 거리 임계값. IDE에서 안쓴다고 하는데 쓰고 있다. 주의.
    private string updateColor;

    [System.Serializable]
    public class StatPreviewLine
    {
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI currentValue;
        public TextMeshProUGUI arrowText;
        public TextMeshProUGUI newValue;
    }

    [Header("Stat Previews")]
    [SerializeField] private StatPreviewLine healthPreview;
    [SerializeField] private StatPreviewLine attackPreview;
    [SerializeField] private StatPreviewLine defensePreview;
    [SerializeField] private StatPreviewLine magicResistancePreview;

    private OwnedOperator op;
    private int currentLevel;
    private int maxLevel;
    private int selectedLevel;
    private int totalLevels;

    // 스크롤 관련 변수
    private float contentHeight; // 전체 컨텐츠의 높이
    private float viewportHeight; // 뷰포트의 높이
    private bool isMousePressed = false;
    private bool isScrolling = false; // 관성 스크롤 중인지 여부
    private bool isInitialized = false;
    private bool isUpdatingPanel = false;
    private bool isPanelUpdated = false; // 이 레벨에 대한 패널이 업데이트 됐으면 true

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

        updateColor = GameManagement.Instance.ResourceManager.textUpdateColor;
    }

    public void Initialize(OwnedOperator op)
    {
        this.op = op;
        currentLevel = op.currentLevel;
        maxLevel = OperatorGrowthSystem.GetMaxLevel(op.currentPhase);
        selectedLevel = currentLevel;
        viewportHeight = levelScrollRect.viewport.rect.height;
        totalLevels = maxLevel - currentLevel + 1;

        // UI 초기화
        InitializeLevelStrip();
        UpdateExpGauge();
        InitializeStatTexts();
        UpdateConfirmButton();

        // 초기 위치 설정
        SetInitialScrollPositon(); 

        isInitialized = true; 
    }

    /// <summary>
    /// 각 레벨 오브젝트를 스크롤 상에 배치함
    /// </summary>
    private void InitializeLevelStrip()
    {
        // 기존 레벨 텍스트 제거
        foreach (Transform child in contentRect)
        {
            Destroy(child.gameObject);
        }

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
            OperatorStats initialStats = op.currentStats;
            healthPreview.currentValue.text = initialStats.Health.ToString();
            attackPreview.currentValue.text = initialStats.AttackPower.ToString();
            defensePreview.currentValue.text = initialStats.Defense.ToString();
            magicResistancePreview.currentValue.text = initialStats.MagicResistance.ToString();

            healthPreview.newValue.text = initialStats.Health.ToString();
            attackPreview.newValue.text = initialStats.AttackPower.ToString();
            defensePreview.newValue.text = initialStats.Defense.ToString();
            magicResistancePreview.newValue.text = initialStats.MagicResistance.ToString();
        }
    }

    /// <summary>
    /// 초기 스크롤의 위치 설정
    /// </summary>
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


    /// <summary>
    /// 각 레벨 오브젝트와 스크롤 높이를 매핑하는 dict를 만듦
    /// 스크롤이 끝나는 양쪽 끝에 currentLevel과 maxLevel이 와야 함
    /// </summary>
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

        // 스크롤 가능 영역 높이 : 패딩 영역 제외하기
        float totalScrollHeight = contentHeight - viewportHeight; // 스크롤이 0일 때 currentLevel이 중앙에, 1일 때 maxLevel이 중앙에 온다

        for (int level = currentLevel; level <= maxLevel; level++)
        {
            int index = level - currentLevel; 
            float centerOffset = index * spacing; // 각 숫자 오브젝트가 중앙에 오는 높이

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
        if (currentLevel != OperatorGrowthSystem.GetMaxLevel(op.currentPhase))
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

    private void SetScrollToLevel(int targetLevel)
    {
        // 스크롤 위치 설정
        levelScrollRect.verticalNormalizedPosition = GetScrollPositionForLevel(targetLevel);
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 스크롤 상태 확인
        Vector2 velocity = levelScrollRect.velocity;

        isScrolling = velocity.magnitude > velocityThreshold; // 임계치를 넘으면 스크롤 중이라고 판단

        // 마우스 버튼 다운 확인
        isMousePressed = Input.GetMouseButton(0);

        // 자유 스크롤 중일 때는 selectedLevel만 업데이트
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
            int nearestLevelFromCurrentScroll = FindNearestLevel(currentScrollPos);
            float nearestLevelPos = GetScrollPositionForLevel(nearestLevelFromCurrentScroll);

            // 가장 가까운 레벨로 부드럽게 스냅
            levelScrollRect.verticalNormalizedPosition = Mathf.Lerp(
                currentScrollPos,
                nearestLevelPos,
                Time.deltaTime * snapSpeed
            );

            if (Mathf.Abs(currentScrollPos - nearestLevelPos) < 0.001f && !isPanelUpdated) // 레벨 당 간격이 0.02 언더라서 조건은 적합해보임
            {
                StartCoroutine(UpdatePanelWithDelay(nearestLevelFromCurrentScroll));
            }
        }
    }

    private IEnumerator UpdatePanelWithDelay(int level)
    {
        // 패널 업데이트 시작
        isUpdatingPanel = true;
        selectedLevel = level;

        yield return new WaitForSeconds(0.1f);

        OperatorStats targetLevelStats = OperatorGrowthSystem.CalculateStatsForLevel(op, level);

        if (level == currentLevel)
        {
            healthPreview.newValue.text = targetLevelStats.Health.ToString();
            attackPreview.newValue.text = targetLevelStats.AttackPower.ToString();
            defensePreview.newValue.text = targetLevelStats.Defense.ToString();
        }
        else
        {
            healthPreview.newValue.text = $"<color={updateColor}>{targetLevelStats.Health.ToString()}</color>";
            attackPreview.newValue.text = $"<color={updateColor}>{targetLevelStats.AttackPower.ToString()}</color>";
            defensePreview.newValue.text = $"<color={updateColor}>{targetLevelStats.Defense.ToString()}</color>";
        }

        magicResistancePreview.newValue.text = targetLevelStats.MagicResistance.ToString(); // 마법 저항력은 레벨업으로 바뀌지 않음

        // 패널 업데이트 완료
        isUpdatingPanel = false;
        isPanelUpdated = true;  // 레벨이 바뀌면 다시 false가 됨
        UpdateConfirmButton();
    }

    private void OnScrollValueChanged(Vector2 value)
    {
        if (!isInitialized) return;
    }

    private void UpdateExpGauge()
    {
        // 아이템을 사용해서 어떤 레벨이 됐을 때의 경험치 연산 식이 필요함
        
        // 특정 레벨에 도달하기 위해 필요한 최소한의 아이템 수, 그 때의 레벨과 현재 경험치, 그 레벨의 최대 경험치를 구하면 됨
        // 그 로직 자체는 여기서 구현할 건 아니고 여기는 시각화만 담당함
        // 아직은 경험치 아이템을 구현하지 않았으므로 그걸 구현한 다음에 진행하면 됨

        // 이건 임시방편
        float currentExp = op.currentExp;
        float maxExp = OperatorGrowthSystem.GetRequiredExp(op.currentLevel);

        expGauge.value = currentExp / maxExp;
    }

    private void UpdateConfirmButton()
    {
        bool canConfirm = !isScrolling && isPanelUpdated && selectedLevel > currentLevel;

        // 확인 버튼 활성화 상태 업데이트
        if (confirmButton != null)
        {
            confirmButton.interactable = canConfirm;
        }
    }

    private void OnConfirmButtonClicked()
    {
        if (!confirmButton.interactable || selectedLevel <= currentLevel) return;

        // 레벨업 진행
        OperatorGrowthManager.Instance.TryLevelUpOperator(op, selectedLevel);

        // 레벨업에 따른 패널값과 스크롤 오브젝트 수정
        InitializeStatTexts();
        UpdateLevelStrip(selectedLevel);
        UpdateConfirmButton();

        // 패널
        MainMenuManager.Instance.ShowNotification($"레벨업 완료");
    }

    /// <summary>
    /// 레벨업 완료 후, 스크롤 부분 업데이트
    /// </summary>
    private void UpdateLevelStrip(int newLevel)
    {
        currentLevel = newLevel; 
        totalLevels = maxLevel - currentLevel + 1;
        InitializeLevelStrip();
        SetScrollToLevel(currentLevel);
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
