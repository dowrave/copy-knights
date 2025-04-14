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
    [SerializeField] private float snapSpeed = 10f; // ���� �ִϸ��̼� �ӵ�

    [Header("Item Usage Display")]
    [SerializeField] private Transform itemUsageContainer = default!; // ������ ǥ�� �����̳�
    [SerializeField] private ItemUIElement itemUIPrefab = default!; // ������ UI ������
    [SerializeField] private Color usageItemBackgroundColor = new Color(0.8f, 0.4f, 0.2f, 1f); // ���Ǵ� ������ ���� ����
    [SerializeField] private TextMeshProUGUI cantReachLevelText = default!; // �ش� ������ ������ �Ұ����� �� ������ ��� ����
    private List<ItemUIElement> activeItemElements = new List<ItemUIElement>(); // ���� ǥ������ ������ ��ҵ�

    [SerializeField] private float velocityThreshold = 0.1f; // ��ũ���� ����ٰ� �Ǵ��ϴ� �ӵ� �Ӱ谪

    private float snapThreshold; // ���� �Ÿ� �Ӱ谪. ������ �ʵ��� ����.
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

    // ��ũ�� ���� ����
    private float contentHeight; // ��ü �������� ����
    private float viewportHeight; // ����Ʈ�� ����
    private bool isMousePressed = false;
    private bool isScrolling = false; // ���� ��ũ�� ������ ����
    private bool isSnapping = false;
    private bool isInitialized = false;
    private bool isUpdatingPanel = false;
    private bool isPanelUpdated = false; // �� ������ ���� �г��� ������Ʈ ������ true

    private Vector2 maxButtonOriginalPosition;
    private readonly float buttonOffsetX = 50f;

    private ExpCalculationSystem.ExpItemUsagePlan maxLevelPlan;


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
        currentLevel = op.currentLevel;
        maxLevel = OperatorGrowthSystem.GetMaxLevel(op.currentPhase);
        selectedLevel = currentLevel;
        viewportHeight = levelScrollRect.viewport.rect.height;
        totalLevels = maxLevel - currentLevel + 1;

        // UI �ʱ�ȭ
        InitializeLevelStrip();
        UpdateExpGauge();
        InitializeStatTexts();

        // �ʱ� ��ġ ����
        SetInitialScrollPositon(); 

        isInitialized = true; 
    }

    // �� ���� ������Ʈ�� ��ũ�� �� ��ġ��
    private void InitializeLevelStrip()
    {
        // ���� ���� �ؽ�Ʈ ����
        foreach (Transform child in contentRect)
        {
            Destroy(child.gameObject);
        }

        // �޼� ���� �ִ� ����
        maxLevelPlan = OperatorGrowthManager.Instance!.CalculateRequiredItems(op, maxLevel);
        maxReachableLevel = maxLevelPlan.targetLevel; 

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

                if (level > maxReachableLevel)
                {
                    Color grayColor = new Color(0.5f, 0.5f, 0.5f);
                    levelText.color = grayColor;
                }
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

    // �ʱ� ��ũ���� ��ġ ����
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



    // �� ���� ������Ʈ�� ��ũ�� ���̸� �����ϴ� dict�� ����
    // ��ũ���� ������ ���� ���� currentLevel�� maxLevel�� �;� ��
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

        // ��ũ�� ���� ���� ���� (�е� ���� ����)
        // ��ũ���� 0�� �� currentLevel�� �߾ӿ�, 1�� �� maxLevel�� �߾ӿ� �´�
        float totalScrollHeight = contentHeight - viewportHeight; 

        for (int level = currentLevel; level <= maxLevel; level++)
        {
            int index = level - currentLevel;

            // �� ���� ������Ʈ�� �߾ӿ� ���� ����
            float centerOffset = index * spacing; 

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
                0.25f // �ִϸ��̼� �ӵ�
            )
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
             {
                 // ��ũ�� �Ϸ� �� �ش� ���� ���� ������Ʈ
                 StartCoroutine(UpdatePanelWithDelay(targetLevel));
                 isSnapping = false;
             });
        }
        else
        {
            // ��� �̵�
            levelScrollRect.verticalNormalizedPosition = targetPosition;
        }
    }

    private void Update()
    {
        if (!isInitialized) return;
        if (isSnapping) return; 

        // ��ũ�� ���� Ȯ��
        Vector2 velocity = levelScrollRect.velocity;

        //�Ӱ�ġ�� ������ ��ũ�� ���̶�� �Ǵ�
        isScrolling = velocity.magnitude > velocityThreshold;

        // ���콺 ��ư �ٿ� ������ Ȯ��(GetMouseButtonDown�� ���� �������� ����)
        isMousePressed = Input.GetMouseButton(0);

        if (isScrolling)
        {
            SetMaxLevelButtonVisible(false);

            // isMousePressed�� ���� ��, Ȯ�� ��ư�� �������� �� �� ������ ���ɼ��� �־ ���� �и�
            UpdateConfirmButton();
        }

        // ��ũ�� ���� ���� ó��
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

            SetSelectedLevelByItemUsagePlan(currentScrollPos);

            float selectedLevelPos = GetScrollPositionForLevel(selectedLevel);

            // ���� ����� ������ �ε巴�� ����
            levelScrollRect.verticalNormalizedPosition = Mathf.Lerp(
                currentScrollPos,
                selectedLevelPos,
                Time.deltaTime * snapSpeed
            );

            if (Mathf.Abs(currentScrollPos - selectedLevelPos) < 0.05f && !isPanelUpdated) // ���� �� ������ 0.02 ����� ������ �����غ���
            {
                StartCoroutine(UpdatePanelWithDelay(selectedLevel));
            }
        }
    }


    /// <summary>
    /// ��ũ�� ��ġ���� ���� ����� ������ �����ϰ� 
    /// �ش� �������� �ʿ��� ������ ��� ��ȹ�� ����Ͽ�,
    /// ���� ��� targetLevel�� ����Ǹ� �ش� ������ ��ũ�� �̵��մϴ�.
    /// </summary>
    private void SetSelectedLevelByItemUsagePlan(float currentScrollPos)
    {
        selectedLevel = FindNearestLevel(currentScrollPos);

        // �ش� ������ �����ϱ� ���� ������ ��� ��ȹ�� ������
        currentUsagePlan = OperatorGrowthManager.Instance!.CalculateRequiredItems(op, selectedLevel);

        // �����۵��� ������� �� �����ϴ� ������ ������ ������ �������� ���ٸ� �ش� ������ �̵�
        if (currentUsagePlan.targetLevel > selectedLevel)
        {
            SetScrollToLevel(currentUsagePlan.targetLevel, true);
            selectedLevel = currentUsagePlan.targetLevel;
            isPanelUpdated = false; // ���¸� �־����� �г� ������Ʈ �������� ��Ȱ��ȭ
        }
    }

    private IEnumerator UpdatePanelWithDelay(int targetLevel)
    {
        // �г� ������Ʈ ����
        isUpdatingPanel = true;
        selectedLevel = targetLevel;

        yield return null; // �� ������ ���

        // ���� �г� ������Ʈ
        OperatorStats targetLevelStats = OperatorGrowthSystem.CalculateStats(op, targetLevel, op.currentPhase);

        // ������ ��Ȳ�� �� �ؽ�Ʈ�� ���� �߰�
        if (targetLevel == currentLevel)
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


        UpdateLevelUpPreviewDisplay();

        // �г� ������Ʈ �Ϸ�
        isUpdatingPanel = false;
        isPanelUpdated = true;  // ������ �ٲ�� �ٽ� false�� ��
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
            float currentExp = op.currentExp;
            float maxExp = OperatorGrowthSystem.GetMaxExpForNextLevel(op.currentPhase, op.currentLevel);
            expGauge.value = currentExp / maxExp;
        }
        else
        {
            expGauge.value = 0f;
        }
    }

    private void UpdateConfirmButton()
    {
        bool canConfirm = !isScrolling && // ��ũ�� ���� �ƴ� ��
            !isSnapping && // ������ ���� �ƴ� ��
            isPanelUpdated && // �г��� �� ������Ʈ ���� ��
            selectedLevel > currentLevel && // ���� ����
            selectedLevel <= maxReachableLevel;

        // Ȯ�� ��ư Ȱ��ȭ ���� ������Ʈ
        if (confirmButton != null)
        {
            confirmButton.interactable = canConfirm;
        }
    }

    private void OnConfirmButtonClicked()
    {
        if (!confirmButton.interactable || selectedLevel <= currentLevel) return;

        // ������ ���� ��û
        bool success = OperatorGrowthManager.Instance!.TryLevelUpOperator(
            op,
            selectedLevel,
            currentUsagePlan
        );

        if (success)
        {
            // UI ������Ʈ
            InitializeStatTexts();
            ClearItemDisplay();
            UpdateExpGauge();
            MainMenuManager.Instance!.ShowNotification($"������ �Ϸ� : ���� ���� {selectedLevel}");
            UpdateLevelStrip(selectedLevel);
            UpdateConfirmButton();
        }
        else
        {
            MainMenuManager.Instance!.ShowNotification("�������� �����߽��ϴ�");
        }
    }


    // ������ �Ϸ� ��, ��ũ�� �κ� ������Ʈ
    private void UpdateLevelStrip(int newLevel)
    {
        currentLevel = newLevel; 
        totalLevels = maxLevel - currentLevel + 1;
        InitializeLevelStrip();
        SetScrollToLevel(currentLevel);
    }


    // �������� �ʿ��� ������ �̸�����, ����ġ ������ ǥ�� ��
    private void UpdateLevelUpPreviewDisplay()
    {
        ClearItemDisplay();

        // ������ �Ұ����ϴٸ� ������ ǥ�� �гο� �Ұ��� ǥ��
        if (selectedLevel > maxReachableLevel)
        {
            cantReachLevelText.gameObject.SetActive(true);
        }
        else
        {
            cantReachLevelText.gameObject.SetActive(false);

            // UI�� ���� ������ ǥ��
            foreach (var itemPair in currentUsagePlan.itemsToUse)
            {
                ItemUIElement itemElement = Instantiate(itemUIPrefab, itemUsageContainer);
                itemElement.Initialize(itemPair.Key, itemPair.Value, false);

                // ��� ���� �������� ���� ����
                if (itemElement.itemCountBackground != null)
                {
                    itemElement.itemCountBackground.color = usageItemBackgroundColor;
                }

                activeItemElements.Add(itemElement);
            }

            // �ش� ���������� ����ġ ������ ǥ��
            if (currentUsagePlan.totalItemExp > 0)
            {
                float maxExpForLevel = OperatorGrowthSystem.GetMaxExpForNextLevel(
                    op.currentPhase,
                    currentUsagePlan.targetLevel
                );
                expGauge.value = currentUsagePlan.remainingExp / maxExpForLevel;
            }
        }
         
    }

    private void ClearItemDisplay()
    {
        // ���� ������ UI ����
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

        Debug.Log($"��� ������ ��� �� �ִ� ���� ���� : {maxLevel}");

        // ��ũ�� ����
        SetScrollToLevel(maxLevel, true);
        UpdateLevelUpPreviewDisplay();

        if (maxLevel > op.currentLevel)
        {
            currentUsagePlan = usagePlan;
        }
    }


    // ��ũ�� ���¿� ���� ��ư ǥ�� ����
    private void SetMaxLevelButtonVisible(bool visible)
    {
        maxLevelButton.transform.DOKill(); // ���� ���� �ִϸ��̼� �ߴ�
        CanvasGroup buttonCanvasGroup = maxLevelButton.GetComponent<CanvasGroup>();

        // ��ǥ ��ġ�� ���� ����
        Vector2 targetAnchoredPosition = visible ?
            maxButtonOriginalPosition :
            maxButtonOriginalPosition + new Vector2(buttonOffsetX, 0f);

        float targetAlpha = visible ? 1f : 0.3f;

        // �ִϸ��̼� ������ ����
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
