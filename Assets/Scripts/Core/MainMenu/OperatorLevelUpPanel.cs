
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class OperatorLevelUpPanel : MonoBehaviour
{
    [Header("Level Strip Components")]
    [SerializeField] private ScrollRect levelScrollRect;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private GameObject levelTextPrefab;
    [SerializeField] private RectTransform gaugeExpCircle;

    [Header("Info Display")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Info Settings")]
    [SerializeField] private float snapSpeed = 10f; // 스냅 애니메이션 속도
    [SerializeField] private float velocityThreshold = 0.5f; // 스크롤이 멈췄다고 판단하는 속도 임계값

    private float snapThreshold; // 스냅 위치와의 거리 임계값

    private OwnedOperator op;
    private int currentLevel;
    private int maxLevel;
    private int selectedLevel;
    private int totalLevels;

    // 스크롤 관련 변수
    //private float levelItemHeight = 0f; // 각 레벨 항목의 높이, levelTextPrefab의 Height값과 동일. 하드코딩 ㄱ
    private float contentHeight; // 전체 컨텐츠의 높이
    private float viewportHeight; // 뷰포트의 높이
    private float paddingHeight; // 뷰포트의 절반 높이
    private bool isDragging = false; // 드래그 중인지 여부
    private bool isScrolling = false; // 관성 스크롤 중인지 여부
    //private float targetScrollPosition = 0f; 
    private bool isInitialized = false;
    private bool isUpdatingPanel = false;
    private bool isPanelUpdated = false; // 이 레벨에 대한 패널이 업데이트 됐으면 true

    // 각 레벨에 대한 스크롤 위치를 저장하는 dict
    private Dictionary<int, float> levelToScrollPosition = new Dictionary<int, float>();

    private void Awake()
    {
        if (levelScrollRect != null)
        {
            // onValueChanged는 내부의 normalizedPositon([0, 1]) 값의 변화를 감지함
            // 즉 스크롤이 변할 때마다 실행된다고 보면 되겠다
            levelScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
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
        UpdateUI();

        // 초기 위치 설정 : 현재 레벨이 원 중앙
        SetScrollToLevel(currentLevel);

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

            Debug.Log($"Level {level} normalized position: {normalizedPosition}");
        }
    }

    // 스냅핑 간격 설정
    private void SetSnapThreshold()
    {
        // 레벨업을 할 수 있을 때에만 이 패널에 들어올 수 있기 때문에 currentLevel + 1이 무조건 존재함
        snapThreshold = levelToScrollPosition[currentLevel + 1] - levelToScrollPosition[currentLevel];
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
        bool isMousePressed = Input.GetMouseButton(0);

        // 자유 스크롤 중일 때는 selectedLevel만 업데이트
        if (isMousePressed || isScrolling)
        {
            int newLevel = FindNearestLevel(levelScrollRect.verticalNormalizedPosition);
            if (selectedLevel != newLevel)
            {
                selectedLevel = newLevel;
                isPanelUpdated = false; // 레벨이 바뀌면 패널을 새로 업데이트해야 함
                UpdateUI();
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

        Debug.Log($"레벨 {level}에 대한 패널 업데이트");

        // 패널 업데이트 완료
        isUpdatingPanel = false;
        isPanelUpdated = true;  // 레벨이 바뀌면 다시 false가 됨
    }

    private void OnScrollValueChanged(Vector2 value)
    {
        if (!isInitialized) return;
        
        // 스크롤 이벤트 감지할 게 있으면 추가
    }

    private void UpdateUI()
    {
        // 정보 텍스트 업데이트

        // 확인 버튼 활성화 상태 업데이트
        if (confirmButton != null)
        {
            confirmButton.interactable = selectedLevel > currentLevel; 
        }
    }

    private void OnConfirmButtonClicked()
    {
        // 레벨업 로직 구현 예정
        Debug.Log($"{selectedLevel}로 레벨업 완료");
    }


}
