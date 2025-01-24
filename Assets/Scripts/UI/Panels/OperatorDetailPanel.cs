using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OperatorDetailPanel : MonoBehaviour
{
    [Header("Basic Info")]
    [SerializeField] private Image operatorIconImage;
    [SerializeField] private Image classIconImage;
    [SerializeField] private TextMeshProUGUI operatorNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI maxLevelText;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private Image[] skillIconImages;

    [Header("Attack Range Visualization")]
    [SerializeField] private RectTransform attackRangeContainer;
    [SerializeField] private float centerPositionOffset; // Ÿ�� �ð�ȭ ��ġ�� ���� �߽� �̵�
    [SerializeField] private float tileSize = 25f;

    private UIHelper.AttackRangeHelper attackRangeHelper;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackPowerText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI magicResistanceText;
    [SerializeField] private TextMeshProUGUI deploymentCostText;
    [SerializeField] private TextMeshProUGUI redeployTimeText;
    [SerializeField] private TextMeshProUGUI blockCountText;
    [SerializeField] private TextMeshProUGUI attackSpeedText;

    [Header("Growth")]
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private Slider expGauge;
    [SerializeField] private Image promotionImage;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button promoteButton;

    [Header("Indicator")]
    [SerializeField] private TextMeshProUGUI maxLevelIndicator;
    [SerializeField] private TextMeshProUGUI maxPromotionIndicator;
    [SerializeField] private TextMeshProUGUI canLevelUpIndicator;

    [Header("Skills")]
    [SerializeField] private Button skill1Button;
    [SerializeField] private Image skill1SelectedIndicator;
    [SerializeField] private Button skill2Button;
    [SerializeField] private Image skill2SelectedIndicator;
    [SerializeField] private TextMeshProUGUI skillDetailText;

    private OperatorData operatorData;
    private OwnedOperator currentOperator;

    private void Awake()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        levelUpButton.interactable = true;
        promoteButton.interactable = true;

        levelUpButton.onClick.AddListener(OnLevelUpClicked);
        promoteButton.onClick.AddListener(OnPromoteClicked);

        skill1Button.onClick.AddListener(() => OnSkillButtonClicked(0));
        skill2Button.onClick.AddListener(() => OnSkillButtonClicked(1));
    }


    public void Initialize(OwnedOperator ownedOp)
    {
        if (currentOperator != ownedOp)
        {
            ClearAttackRange();

            currentOperator = ownedOp;
            operatorData = ownedOp.BaseData;

            // AttackRangeHelper �ʱ�ȭ
            attackRangeHelper = UIHelper.Instance.CreateAttackRangeHelper(
                attackRangeContainer,
                centerPositionOffset,
                tileSize
            );

            UpdateAllUI();
        }
    }

    private void OnEnable()
    {
        // ���� ���۷����Ͱ� �Ҵ�� ��쿡�� UI ������Ʈ ����
        if (currentOperator != null)
        {
            UpdateAllUI();
        }
    }

    private void UpdateAllUI()
    {
        UpdateBasicInfo();
        UpdateStats();
        UpdateGrowthInfo();
        UpdateSkillsUI();
        //UpdateButtonStates();
    }


    private void UpdateBasicInfo()
    {
        // ����� �Ǵ� ���۷����� �̹��� ����
        if (operatorData.Icon != null)
        {
            operatorIconImage.sprite = operatorData.Icon;
            operatorIconImage.enabled = true;
        }
        else
        {
            operatorIconImage.enabled = false; 
        }

        operatorNameText.text = operatorData.entityName;

        // Ŭ���� ������ ����
        OperatorIconHelper.SetClassIcon(classIconImage, operatorData.operatorClass);

        // ���� ���� ����
        attackRangeHelper.ShowBasicRange(currentOperator.CurrentAttackableTiles);
    }

    private void UpdateStats()
    {
        OperatorStats currentStats = currentOperator.CurrentStats;

        healthText.text = currentStats.Health.ToString();
        attackPowerText.text = currentStats.AttackPower.ToString();
        defenseText.text = currentStats.Defense.ToString();
        magicResistanceText.text = currentStats.MagicResistance.ToString();
        deploymentCostText.text = currentStats.DeploymentCost.ToString();
        redeployTimeText.text = currentStats.RedeployTime.ToString();
        blockCountText.text = currentStats.MaxBlockableEnemies.ToString();
        attackSpeedText.text = currentStats.AttackSpeed.ToString();
    }

    // ���� ����ġ, ���� ����, ����ȭ ���� ������Ʈ
    private void UpdateGrowthInfo()
    {
        UpdateExpInfo();
        UpdatePromotionInfo();
    }


    // ����ġ, ���� ���� ���� ������Ʈ
    private void UpdateExpInfo()
    {
        int currentLevel = currentOperator.currentLevel;
        int maxLevel = OperatorGrowthSystem.GetMaxLevel(currentOperator.currentPhase);
        int currentExp = currentOperator.currentExp;
        float maxExp = OperatorGrowthSystem.GetMaxExpForNextLevel(
            currentOperator.currentPhase,
            currentLevel
        );

        expText.text = $"EXP\n<size=44><color=#FFE61A>{currentExp.ToString()}</color>/{maxExp.ToString()}</size>";
        levelText.text = $"LV\n<size=100><b>{currentLevel.ToString()}</b></size=100>";
        maxLevelText.text = $"/{maxLevel.ToString()}";
        expGauge.value = currentExp / maxExp;

        if (currentLevel < maxLevel)
        {
            maxLevelIndicator.gameObject.SetActive(false);
            canLevelUpIndicator.gameObject.SetActive(true);
        }
        else
        {
            maxLevelIndicator.gameObject.SetActive(true);
            canLevelUpIndicator.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ����ȭ ���� ���� ������Ʈ
    /// </summary>
    private void UpdatePromotionInfo()
    {
        phaseText.text = $"{(int)currentOperator.currentPhase}";
        OperatorIconHelper.SetElitePhaseIcon(promotionImage, currentOperator.currentPhase);

        if (currentOperator.currentPhase == OperatorGrowthSystem.ElitePhase.Elite0)
        {
            maxPromotionIndicator.gameObject.SetActive(false);
        }
        else
        {
            maxPromotionIndicator.gameObject.SetActive(true);
        }

    }

    private void ClearAttackRange()
    {
        if (attackRangeHelper != null)
        {
            attackRangeHelper.ClearTiles();
        }
    }


    private void OnLevelUpClicked()
    {
        if (currentOperator.CanLevelUp)
        {
            GameObject levelUpPanelObject = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorLevelUp];
            OperatorLevelUpPanel levelUpPanel = levelUpPanelObject.GetComponent<OperatorLevelUpPanel>();
            MainMenuManager.Instance.FadeInAndHide(levelUpPanelObject, gameObject);
            levelUpPanel.Initialize(currentOperator);
        }
        else if (currentOperator.currentLevel == OperatorGrowthSystem.GetMaxLevel(currentOperator.currentPhase))
        {
            MainMenuManager.Instance.ShowNotification("���� ����ȭ������ �ִ� �����Դϴ�.");
        }
    }

    private void OnPromoteClicked()
    {
        // 0����ȭ�� ������ ���� ����
        if (currentOperator.currentPhase == OperatorGrowthSystem.ElitePhase.Elite0)
        {
            GameObject promotionPanelObject = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorPromotion];
            MainMenuManager.Instance.FadeInAndHide(promotionPanelObject, gameObject);
            OperatorPromotionPanel promotionPanel = promotionPanelObject.GetComponent<OperatorPromotionPanel>();
            promotionPanel.Initialize(currentOperator);
        }
    }

    private void UpdateSkillsUI()
    {
        var unlockedSkills = currentOperator.UnlockedSkills;

        skill1Button.GetComponent<Image>().sprite = unlockedSkills[0].skillIcon;
        if (unlockedSkills.Count > 1)
        {
            skill2Button.GetComponent<Image>().sprite = unlockedSkills[1].skillIcon;
        }
        else
        {
            skill2Button.interactable = false;
        }

        UpdateSkillSelection();
        UpdateSkillDescription();
    }

    private void OnSkillButtonClicked(int skillIndex)
    {
        if (currentOperator == null) return;

        var skills = currentOperator.UnlockedSkills;
        if (skillIndex < skills.Count)
        {
            // Set as default skill for this operator
            currentOperator.SetDefaultSelectedSkills(skills[skillIndex]);
            UpdateSkillSelection();
            UpdateSkillDescription();
        }
    }

    private void UpdateSkillSelection()
    {
        if (currentOperator == null) return;

        // Update selection indicators
        skill1SelectedIndicator.gameObject.SetActive(
            currentOperator.DefaultSelectedSkill == currentOperator.UnlockedSkills[0]);

        if (currentOperator.UnlockedSkills.Count > 1)
        {
            skill2SelectedIndicator.gameObject.SetActive(
                currentOperator.DefaultSelectedSkill == currentOperator.UnlockedSkills[1]);
        }
    }

    private void UpdateSkillDescription()
    {
        if (currentOperator?.DefaultSelectedSkill != null)
        {
            skillDetailText.text = currentOperator.DefaultSelectedSkill.description;
        }
        else
        {
            skillDetailText.text = "";
        }
    }
}
