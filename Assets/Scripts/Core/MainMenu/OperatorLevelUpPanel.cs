
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
    [SerializeField] private float snapSpeed = 10f; // ���� �ִϸ��̼� �ӵ�

    [SerializeField] private float velocityThreshold = 0.5f; // ��ũ���� ����ٰ� �Ǵ��ϴ� �ӵ� �Ӱ谪

    private float snapThreshold; // ���� �Ÿ� �Ӱ谪. IDE���� �Ⱦ��ٰ� �ϴµ� ���� �ִ�. ����.
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

    // ��ũ�� ���� ����
    private float contentHeight; // ��ü �������� ����
    private float viewportHeight; // ����Ʈ�� ����
    private bool isMousePressed = false;
    private bool isScrolling = false; // ���� ��ũ�� ������ ����
    private bool isInitialized = false;
    private bool isUpdatingPanel = false;
    private bool isPanelUpdated = false; // �� ������ ���� �г��� ������Ʈ ������ true

    // �� ������ ���� ��ũ�� ��ġ�� �����ϴ� dict
    private Dictionary<int, float> levelToScrollPosition = new Dictionary<int, float>();

    private void Awake()
    {
        if (levelScrollRect != null)
        {
            // ��ũ���� ���� ������ ����ȴ�. onValueChanged�� ������ normalizedPositon([0, 1]) ���� ��ȭ�� ������
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

        // UI �ʱ�ȭ
        InitializeLevelStrip();
        UpdateExpGauge();
        InitializeStatTexts();
        UpdateConfirmButton();

        // �ʱ� ��ġ ����
        SetInitialScrollPositon(); 

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
    /// �ʱ� ��ũ���� ��ġ ����
    /// </summary>
    private void SetInitialScrollPositon()
    {
        // ��ũ�� ��ġ ��� ����
        levelScrollRect.verticalNormalizedPosition = GetScrollPositionForLevel(currentLevel);

        // ��ũ�� �ӵ� �ʱ�ȭ
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
        }
    }

    // ������ ���� ����
    private void SetSnapThreshold()
    {
        // ���� ����ȭ�� �ִ� ������ �ƴ� ������ ���
        if (currentLevel != OperatorGrowthSystem.GetMaxLevel(op.currentPhase))
        {
            snapThreshold = levelToScrollPosition[currentLevel + 1] - levelToScrollPosition[currentLevel];
        }
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
        isMousePressed = Input.GetMouseButton(0);

        // ���� ��ũ�� ���� ���� selectedLevel�� ������Ʈ
        if (isMousePressed || isScrolling)
        {
            int newLevel = FindNearestLevel(levelScrollRect.verticalNormalizedPosition);
            if (selectedLevel != newLevel)
            {
                selectedLevel = newLevel;
                isPanelUpdated = false; // ������ �ٲ�� �г��� ���� ������Ʈ�ؾ� ��
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

        magicResistancePreview.newValue.text = targetLevelStats.MagicResistance.ToString(); // ���� ���׷��� ���������� �ٲ��� ����

        // �г� ������Ʈ �Ϸ�
        isUpdatingPanel = false;
        isPanelUpdated = true;  // ������ �ٲ�� �ٽ� false�� ��
        UpdateConfirmButton();
    }

    private void OnScrollValueChanged(Vector2 value)
    {
        if (!isInitialized) return;
    }

    private void UpdateExpGauge()
    {
        // �������� ����ؼ� � ������ ���� ���� ����ġ ���� ���� �ʿ���
        
        // Ư�� ������ �����ϱ� ���� �ʿ��� �ּ����� ������ ��, �� ���� ������ ���� ����ġ, �� ������ �ִ� ����ġ�� ���ϸ� ��
        // �� ���� ��ü�� ���⼭ ������ �� �ƴϰ� ����� �ð�ȭ�� �����
        // ������ ����ġ �������� �������� �ʾ����Ƿ� �װ� ������ ������ �����ϸ� ��

        // �̰� �ӽù���
        float currentExp = op.currentExp;
        float maxExp = OperatorGrowthSystem.GetRequiredExp(op.currentLevel);

        expGauge.value = currentExp / maxExp;
    }

    private void UpdateConfirmButton()
    {
        bool canConfirm = !isScrolling && isPanelUpdated && selectedLevel > currentLevel;

        // Ȯ�� ��ư Ȱ��ȭ ���� ������Ʈ
        if (confirmButton != null)
        {
            confirmButton.interactable = canConfirm;
        }
    }

    private void OnConfirmButtonClicked()
    {
        if (!confirmButton.interactable || selectedLevel <= currentLevel) return;

        // ������ ����
        OperatorGrowthManager.Instance.TryLevelUpOperator(op, selectedLevel);

        // �������� ���� �гΰ��� ��ũ�� ������Ʈ ����
        InitializeStatTexts();
        UpdateLevelStrip(selectedLevel);
        UpdateConfirmButton();

        // �г�
        MainMenuManager.Instance.ShowNotification($"������ �Ϸ�");
    }

    /// <summary>
    /// ������ �Ϸ� ��, ��ũ�� �κ� ������Ʈ
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
        // �г� Ȱ��ȭ���� ��ũ�� ��ġ�� ���� ������ �ʱ�ȭ
        if (isInitialized)
        {
            SetInitialScrollPositon();
        }
    }

    private void OnDisable()
    {
        // �г��� ��Ȱ��ȭ�� �� ��� ���¸� �ʱ�ȭ
        isInitialized = false;
        currentLevel = 0;
        maxLevel = 0;
        selectedLevel = 0;
        totalLevels = 0;
        levelToScrollPosition.Clear();

        // ��ũ�Ѻ��� �������� ����
        if (contentRect != null)
        {
            foreach (Transform child in contentRect)
            {
                Destroy(child.gameObject);
            }
        }

        // ��ũ�� ��ġ�� ����
        if (levelScrollRect != null)
        {
            levelScrollRect.velocity = Vector2.zero;
            levelScrollRect.verticalNormalizedPosition = 0f;
        }
    }

}
