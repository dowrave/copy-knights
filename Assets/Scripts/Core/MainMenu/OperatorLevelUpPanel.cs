
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class OperatorLevelUpPanel : MonoBehaviour, IBeginDragHandler, IEndDragHandler
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
    [SerializeField] private float velocityThreshold = 0.01f; // 스크롤이 멈췄다고 판단하는 속도 임계값

    private float snapThreshold; // 스냅 위치와의 거리 임계값

    private OwnedOperator op;
    private int currentLevel;
    private int maxLevel;
    private int selectedLevel;
    private int totalLevels; // 표시될 전체 레벨 수

    // 스크롤 관련 변수
    private float levelItemHeight = 200f; // 각 레벨 항목의 높이, levelTextPrefab의 Height값과 동일. 하드코딩 ㄱ
    private float contentHeight; // 전체 컨텐츠의 높이
    private float viewportHeight; // 뷰포트의 높이
    private float paddingHeight; // 뷰포트의 절반 높이
    private bool isDragging = false; // 드래그 중인지 여부
    private bool isScrolling = false; // 관성 스크롤 중인지 여부
    //private float targetScrollPosition = 0f; 
    private bool isInitialized = false;

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
        totalLevels = maxLevel - currentLevel + 1;
        viewportHeight = levelScrollRect.viewport.rect.height;

        // UI 초기화
        InitializeLevelStrip();
        UpdateUI();

        // 초기 위치 설정 : 현재 레벨이 원 중앙
        SetScrollToLevel(currentLevel);

        isInitialized = true; 
    }

    private void InitializeLevelStrip()
    {
        // 기존 레벨 텍스트 제거
        foreach (Transform child in contentRect)
        {
            Destroy(child.gameObject);
        }

        VerticalLayoutGroup layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        float spacing = layoutGroup != null ? layoutGroup.spacing : 0f;
        float actualPaddingHeight = viewportHeight * 0.5f - spacing - levelItemHeight / 2; // 패딩, 아이템 높이 등 반영

        // 상단 여백 구현
        GameObject topPadding = new GameObject("TopPadding");
        topPadding.transform.SetParent(contentRect, false);
        RectTransform topPaddingRect = topPadding.AddComponent<RectTransform>();
        topPaddingRect.sizeDelta = new Vector2(0, actualPaddingHeight);

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
        GameObject bottomPadding = new GameObject("BottomPadding");
        bottomPadding.transform.SetParent(contentRect, false);
        RectTransform bottomPaddingRect = bottomPadding.AddComponent<RectTransform>();
        bottomPaddingRect.sizeDelta = new Vector2(0, actualPaddingHeight);

        // RectTransform의 크기 변화는 프레임 이후에 계산되므로, 강제로 갱신함
        Canvas.ForceUpdateCanvases(); 

        contentHeight = contentRect.rect.height;

        CalculateScrollPositions();
        SetSnapThreshold();
    }

    /// <summary>
    /// 각 레벨 오브젝트와 스크롤 높이를 매핑하는 dict를 만듦
    /// </summary>
    private void CalculateScrollPositions()
    {
        levelToScrollPosition.Clear();

        for (int i = currentLevel; i <= maxLevel; i++)
        {
            int level = i;

            // 해당 레벨이 정중앙에 올 때의 스크롤 위치를 계산
            float levelIndex = maxLevel - i;
            float itemPosition = levelIndex * levelItemHeight + paddingHeight;
            float normalizedPosition = Mathf.Clamp01(1f - (itemPosition / (contentHeight - viewportHeight)));

            levelToScrollPosition[i] = normalizedPosition;
            Debug.Log($"level {i}의 정규화 포지션 : {normalizedPosition}");
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

        // 드래그 중이 아니고, 스크롤이 거의 멈췄을 때
        if (!isDragging && !isScrolling)
        {
            float currentScrollPos = levelScrollRect.verticalNormalizedPosition;
            int nearestLevelFromCurrentScroll = FindNearestLevel(currentScrollPos);
            float nearestLevelPos = GetScrollPositionForLevel(nearestLevelFromCurrentScroll);

            Debug.Log($"currentPos : {currentScrollPos}");
            Debug.Log($"가장 가까운 레벨 : {nearestLevelFromCurrentScroll}");
            Debug.Log($"currentPos : {nearestLevelPos}");

            // 가장 가까운 레벨로 부드럽게 스냅
            levelScrollRect.verticalNormalizedPosition = Mathf.Lerp(
                currentScrollPos,
                nearestLevelPos,
                Time.deltaTime * snapSpeed
            );
        }
    }

    /// <summary>
    /// scrollRect에서 값이 변할 때마다 호출되는 메서드
    /// </summary>
    private void OnScrollValueChanged(Vector2 value)
    {
        if (!isInitialized) return;

        // 1. 현재 스크롤의 normalizedPosition을 실제 컨텐츠 상의 위치로 변환함
        float normalizedPosition = levelScrollRect.verticalNormalizedPosition;
        float scrollContentPosition = (1f - normalizedPosition) * (contentHeight - viewportHeight);

        // 2. 상단 여백을 고려한 실제 위치 계산
        float levelPosition = scrollContentPosition - (viewportHeight * 0.5f);

        // 3. 레벨 위치를 실제 레벨 숫자로 변환함
        int newSelectedLevel = maxLevel - Mathf.RoundToInt(levelPosition / levelItemHeight);
        newSelectedLevel = Mathf.Clamp(newSelectedLevel, currentLevel, maxLevel);

        if (selectedLevel != newSelectedLevel)
        {
            selectedLevel = newSelectedLevel;

            // 스크롤이 거의 멈췄을 때에만 타겟 포지션 업데이트
            if (!isScrolling)
            {
                SetScrollToLevel(selectedLevel);
            }
        }
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

    // IBeginDragHandler 구현
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    // IEndDragHandler 구현
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        // 드래그가 끝나더라도 관성에 의한 스크롤이 계속됨
        // 실제 스냅은 Update에서 스크롤 속도가 일정 이하가 됐을 때 수행됨
    }

    private void OnConfirmButtonClicked()
    {
        // 레벨업 로직 구현 예정
        Debug.Log($"{selectedLevel}로 레벨업 완료");
    }


}
