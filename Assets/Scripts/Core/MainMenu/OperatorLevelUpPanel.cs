
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
    [SerializeField] private float snapSpeed = 10f; // ���� �ִϸ��̼� �ӵ�
    [SerializeField] private float velocityThreshold = 0.01f; // ��ũ���� ����ٰ� �Ǵ��ϴ� �ӵ� �Ӱ谪

    private float snapThreshold; // ���� ��ġ���� �Ÿ� �Ӱ谪

    private OwnedOperator op;
    private int currentLevel;
    private int maxLevel;
    private int selectedLevel;
    private int totalLevels; // ǥ�õ� ��ü ���� ��

    // ��ũ�� ���� ����
    private float levelItemHeight = 200f; // �� ���� �׸��� ����, levelTextPrefab�� Height���� ����. �ϵ��ڵ� ��
    private float contentHeight; // ��ü �������� ����
    private float viewportHeight; // ����Ʈ�� ����
    private float paddingHeight; // ����Ʈ�� ���� ����
    private bool isDragging = false; // �巡�� ������ ����
    private bool isScrolling = false; // ���� ��ũ�� ������ ����
    //private float targetScrollPosition = 0f; 
    private bool isInitialized = false;

    // �� ������ ���� ��ũ�� ��ġ�� �����ϴ� dict
    private Dictionary<int, float> levelToScrollPosition = new Dictionary<int, float>();

    private void Awake()
    {
        if (levelScrollRect != null)
        {
            // onValueChanged�� ������ normalizedPositon([0, 1]) ���� ��ȭ�� ������
            // �� ��ũ���� ���� ������ ����ȴٰ� ���� �ǰڴ�
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

        // UI �ʱ�ȭ
        InitializeLevelStrip();
        UpdateUI();

        // �ʱ� ��ġ ���� : ���� ������ �� �߾�
        SetScrollToLevel(currentLevel);

        isInitialized = true; 
    }

    private void InitializeLevelStrip()
    {
        // ���� ���� �ؽ�Ʈ ����
        foreach (Transform child in contentRect)
        {
            Destroy(child.gameObject);
        }

        VerticalLayoutGroup layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        float spacing = layoutGroup != null ? layoutGroup.spacing : 0f;
        float actualPaddingHeight = viewportHeight * 0.5f - spacing - levelItemHeight / 2; // �е�, ������ ���� �� �ݿ�

        // ��� ���� ����
        GameObject topPadding = new GameObject("TopPadding");
        topPadding.transform.SetParent(contentRect, false);
        RectTransform topPaddingRect = topPadding.AddComponent<RectTransform>();
        topPaddingRect.sizeDelta = new Vector2(0, actualPaddingHeight);

        // ���� ~ �ִ� ������ �ؽ�Ʈ ����(�ִ� �������� �������� ��ġ)
        for (int level = maxLevel; level >= currentLevel ; level--)
        {
            GameObject levelObj = Instantiate(levelTextPrefab, contentRect);
            TextMeshProUGUI levelText = levelObj.GetComponent<TextMeshProUGUI>();
            if (levelText != null)
            {
                levelText.text = $"<size=32>Lv</size>\r\n{level}";
            }
        }

        // �ϴ� ���� ����
        GameObject bottomPadding = new GameObject("BottomPadding");
        bottomPadding.transform.SetParent(contentRect, false);
        RectTransform bottomPaddingRect = bottomPadding.AddComponent<RectTransform>();
        bottomPaddingRect.sizeDelta = new Vector2(0, actualPaddingHeight);

        // RectTransform�� ũ�� ��ȭ�� ������ ���Ŀ� ���ǹǷ�, ������ ������
        Canvas.ForceUpdateCanvases(); 

        contentHeight = contentRect.rect.height;

        CalculateScrollPositions();
        SetSnapThreshold();
    }

    /// <summary>
    /// �� ���� ������Ʈ�� ��ũ�� ���̸� �����ϴ� dict�� ����
    /// </summary>
    private void CalculateScrollPositions()
    {
        levelToScrollPosition.Clear();

        for (int i = currentLevel; i <= maxLevel; i++)
        {
            int level = i;

            // �ش� ������ ���߾ӿ� �� ���� ��ũ�� ��ġ�� ���
            float levelIndex = maxLevel - i;
            float itemPosition = levelIndex * levelItemHeight + paddingHeight;
            float normalizedPosition = Mathf.Clamp01(1f - (itemPosition / (contentHeight - viewportHeight)));

            levelToScrollPosition[i] = normalizedPosition;
            Debug.Log($"level {i}�� ����ȭ ������ : {normalizedPosition}");
        }
    }

    // ������ ���� ����
    private void SetSnapThreshold()
    {
        // �������� �� �� ���� ������ �� �гο� ���� �� �ֱ� ������ currentLevel + 1�� ������ ������
        snapThreshold = levelToScrollPosition[currentLevel + 1] - levelToScrollPosition[currentLevel];
    }

    // Ư�� ������ ��ũ�� ��ġ�� ������
    private float GetScrollPositionForLevel(int level)
    {
        return levelToScrollPosition.TryGetValue(level, out float position) ? position : 0f;
    }

    // ���� ��ũ�� ��ġ���� ���� ����� ������ ã��
    private int FindNearestLevel(float currentScrollPosition)
    {
        return levelToScrollPosition
            .OrderBy(kvp => Mathf.Abs(kvp.Value - currentScrollPosition))
            .First()
            .Key;
    }

    private void SetScrollToLevel(int targetLevel)
    {
        // ��ũ�� ��ġ ����
        levelScrollRect.verticalNormalizedPosition = GetScrollPositionForLevel(targetLevel);
    }

    private void Update()
    {
        if (!isInitialized) return;

        // ��ũ�� ���� Ȯ��
        Vector2 velocity = levelScrollRect.velocity;
        isScrolling = velocity.magnitude > velocityThreshold; // �Ӱ�ġ�� ������ ��ũ�� ���̶�� �Ǵ�

        // �巡�� ���� �ƴϰ�, ��ũ���� ���� ������ ��
        if (!isDragging && !isScrolling)
        {
            float currentScrollPos = levelScrollRect.verticalNormalizedPosition;
            int nearestLevelFromCurrentScroll = FindNearestLevel(currentScrollPos);
            float nearestLevelPos = GetScrollPositionForLevel(nearestLevelFromCurrentScroll);

            Debug.Log($"currentPos : {currentScrollPos}");
            Debug.Log($"���� ����� ���� : {nearestLevelFromCurrentScroll}");
            Debug.Log($"currentPos : {nearestLevelPos}");

            // ���� ����� ������ �ε巴�� ����
            levelScrollRect.verticalNormalizedPosition = Mathf.Lerp(
                currentScrollPos,
                nearestLevelPos,
                Time.deltaTime * snapSpeed
            );
        }
    }

    /// <summary>
    /// scrollRect���� ���� ���� ������ ȣ��Ǵ� �޼���
    /// </summary>
    private void OnScrollValueChanged(Vector2 value)
    {
        if (!isInitialized) return;

        // 1. ���� ��ũ���� normalizedPosition�� ���� ������ ���� ��ġ�� ��ȯ��
        float normalizedPosition = levelScrollRect.verticalNormalizedPosition;
        float scrollContentPosition = (1f - normalizedPosition) * (contentHeight - viewportHeight);

        // 2. ��� ������ ����� ���� ��ġ ���
        float levelPosition = scrollContentPosition - (viewportHeight * 0.5f);

        // 3. ���� ��ġ�� ���� ���� ���ڷ� ��ȯ��
        int newSelectedLevel = maxLevel - Mathf.RoundToInt(levelPosition / levelItemHeight);
        newSelectedLevel = Mathf.Clamp(newSelectedLevel, currentLevel, maxLevel);

        if (selectedLevel != newSelectedLevel)
        {
            selectedLevel = newSelectedLevel;

            // ��ũ���� ���� ������ ������ Ÿ�� ������ ������Ʈ
            if (!isScrolling)
            {
                SetScrollToLevel(selectedLevel);
            }
        }
    }

    private void UpdateUI()
    {
        // ���� �ؽ�Ʈ ������Ʈ

        // Ȯ�� ��ư Ȱ��ȭ ���� ������Ʈ
        if (confirmButton != null)
        {
            confirmButton.interactable = selectedLevel > currentLevel; 
        }
    }

    // IBeginDragHandler ����
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    // IEndDragHandler ����
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        // �巡�װ� �������� ������ ���� ��ũ���� ��ӵ�
        // ���� ������ Update���� ��ũ�� �ӵ��� ���� ���ϰ� ���� �� �����
    }

    private void OnConfirmButtonClicked()
    {
        // ������ ���� ���� ����
        Debug.Log($"{selectedLevel}�� ������ �Ϸ�");
    }


}
