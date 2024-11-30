
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
    [SerializeField] private float snapSpeed = 10f; // ���� �ִϸ��̼� �ӵ�
    [SerializeField] private float velocityThreshold = 0.5f; // ��ũ���� ����ٰ� �Ǵ��ϴ� �ӵ� �Ӱ谪

    private float snapThreshold; // ���� ��ġ���� �Ÿ� �Ӱ谪

    private OwnedOperator op;
    private int currentLevel;
    private int maxLevel;
    private int selectedLevel;
    private int totalLevels;

    // ��ũ�� ���� ����
    //private float levelItemHeight = 0f; // �� ���� �׸��� ����, levelTextPrefab�� Height���� ����. �ϵ��ڵ� ��
    private float contentHeight; // ��ü �������� ����
    private float viewportHeight; // ����Ʈ�� ����
    private float paddingHeight; // ����Ʈ�� ���� ����
    private bool isDragging = false; // �巡�� ������ ����
    private bool isScrolling = false; // ���� ��ũ�� ������ ����
    //private float targetScrollPosition = 0f; 
    private bool isInitialized = false;
    private bool isUpdatingPanel = false;
    private bool isPanelUpdated = false; // �� ������ ���� �г��� ������Ʈ ������ true

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
        viewportHeight = levelScrollRect.viewport.rect.height;
        totalLevels = maxLevel - currentLevel + 1;

        // UI �ʱ�ȭ
        InitializeLevelStrip();
        UpdateUI();

        // �ʱ� ��ġ ���� : ���� ������ �� �߾�
        SetScrollToLevel(currentLevel);

        isInitialized = true; 
    }

    /// <summary>
    /// �� ���� ������Ʈ�� ��ũ�� �� ��ġ��
    /// </summary>
    private void InitializeLevelStrip()
    {
        // ���� ���� �ؽ�Ʈ ����
        foreach (Transform child in contentRect)
        {
            Destroy(child.gameObject);
        }

        VerticalLayoutGroup layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        float spacing = layoutGroup != null ? layoutGroup.spacing : 0f;
        float paddingHeight = viewportHeight * 0.5f - spacing;

        // ��� ���� ����
        CreatePadding("TopPadding", paddingHeight);

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
    /// �� ���� ������Ʈ�� ��ũ�� ���̸� �����ϴ� dict�� ����
    /// ��ũ���� ������ ���� ���� currentLevel�� maxLevel�� �;� ��
    /// </summary>
    private void CalculateScrollPositions()
    { 
        levelToScrollPosition.Clear();

        // ���̿� �����̽� �ݿ�
        VerticalLayoutGroup layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        float spacing = layoutGroup != null ? layoutGroup.spacing : 0f;
        float paddingHeight = viewportHeight * 0.5f - spacing;

        // ��ü content ������ ����
        contentHeight = paddingHeight * 2 + // ���� �е�
            (spacing * (totalLevels + 1));  // �� ��� ������ ����. 2���� �е����� �߰��ؼ� spacing�� 2�� �� ����

        // ��ũ�� ���� ���� ���� : �е� ���� �����ϱ�
        float totalScrollHeight = contentHeight - viewportHeight; // ��ũ���� 0�� �� currentLevel�� �߾ӿ�, 1�� �� maxLevel�� �߾ӿ� �´�

        for (int level = currentLevel; level <= maxLevel; level++)
        {
            int index = level - currentLevel; 
            float centerOffset = index * spacing; // �� ���� ������Ʈ�� �߾ӿ� ���� ����

            // ����ȭ�� ��ġ ��� (0~1 ���̷� ����)
            float normalizedPosition = Mathf.Clamp01(centerOffset / totalScrollHeight); // ����ȭ

            // ����ȭ�� ��ũ�Ѱ��� currentLevel�� �� 0, maxLevel�� �� 1�� �ȴ�.

            levelToScrollPosition[level] = normalizedPosition;

            Debug.Log($"Level {level} normalized position: {normalizedPosition}");
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

        // ���콺 ��ư �ٿ� Ȯ��
        bool isMousePressed = Input.GetMouseButton(0);

        // ���� ��ũ�� ���� ���� selectedLevel�� ������Ʈ
        if (isMousePressed || isScrolling)
        {
            int newLevel = FindNearestLevel(levelScrollRect.verticalNormalizedPosition);
            if (selectedLevel != newLevel)
            {
                selectedLevel = newLevel;
                isPanelUpdated = false; // ������ �ٲ�� �г��� ���� ������Ʈ�ؾ� ��
                UpdateUI();
            }
            return; 
        }

        // ��ũ���� ���߰� ���콺�� �������� ������ ó��
        if (!isUpdatingPanel)
        {
            float currentScrollPos = levelScrollRect.verticalNormalizedPosition;
            int nearestLevelFromCurrentScroll = FindNearestLevel(currentScrollPos);
            float nearestLevelPos = GetScrollPositionForLevel(nearestLevelFromCurrentScroll);

            // ���� ����� ������ �ε巴�� ����
            levelScrollRect.verticalNormalizedPosition = Mathf.Lerp(
                currentScrollPos,
                nearestLevelPos,
                Time.deltaTime * snapSpeed
            );

            if (Mathf.Abs(currentScrollPos - nearestLevelPos) < 0.001f && !isPanelUpdated) // ���� �� ������ 0.02 ����� ������ �����غ���
            {
                StartCoroutine(UpdatePanelWithDelay(nearestLevelFromCurrentScroll));
            }
        }
    }

    private IEnumerator UpdatePanelWithDelay(int level)
    {
        // �г� ������Ʈ ����
        isUpdatingPanel = true;
        selectedLevel = level;

        yield return new WaitForSeconds(0.1f);

        Debug.Log($"���� {level}�� ���� �г� ������Ʈ");

        // �г� ������Ʈ �Ϸ�
        isUpdatingPanel = false;
        isPanelUpdated = true;  // ������ �ٲ�� �ٽ� false�� ��
    }

    private void OnScrollValueChanged(Vector2 value)
    {
        if (!isInitialized) return;
        
        // ��ũ�� �̺�Ʈ ������ �� ������ �߰�
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

    private void OnConfirmButtonClicked()
    {
        // ������ ���� ���� ����
        Debug.Log($"{selectedLevel}�� ������ �Ϸ�");
    }


}
