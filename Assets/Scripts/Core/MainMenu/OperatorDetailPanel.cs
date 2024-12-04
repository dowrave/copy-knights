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
    [SerializeField] private float centerPositionOffset; // 타일 시각화 위치를 위한 중심 이동

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
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button promoteButton;

    [Header("Indicator")]
    [SerializeField] private TextMeshProUGUI maxLevelIndicator;
    [SerializeField] private TextMeshProUGUI canLevelUpIndicator;

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
    }

    private void OnPromoteClicked()
    {
        if (currentOperator.CanPromote)
        {
            GameObject promotionPanel = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorPromotion];
            MainMenuManager.Instance.FadeInAndHide(promotionPanel, gameObject);
        }
    }
    private void Start()
    {
        // AttackRangeHelper 초기화
        attackRangeHelper = UIHelper.Instance.CreateAttackRangeHelper(
            attackRangeContainer,
            centerPositionOffset
        );
        Debug.Log("OpeatorDetailPanel에서 attackRangeHelper의 초기화 완료");
    }

    private void OnEnable()
    {
        if (currentOperator != null)
        {
            UpdateAllUI();
        }
    }

    public void Initialize(OwnedOperator ownedOp)
    {
        if (currentOperator != ownedOp)
        {
            currentOperator = ownedOp;
            operatorData = ownedOp.BaseData;
        }
        UpdateAllUI();
    }

    private void UpdateAllUI()
    {
        UpdateBasicInfo();
        UpdateStats();
        UpdateGrowthInfo();
        UpdateButtonStates();
    }

    private void UpdateBasicInfo()
    {
        if (operatorData.Icon != null)
        {
            operatorIconImage.sprite = operatorData.Icon;
            operatorIconImage.enabled = true;
        }
        else
        {
            operatorIconImage.enabled = false; 
        }

        IconHelper.SetClassIcon(classIconImage, operatorData.operatorClass);

        operatorNameText.text = operatorData.entityName;
        phaseText.text = $"Elite {(int)currentOperator.currentPhase}";

        // 공격 범위 설정
        Debug.Log($"attackRangeHelper : {attackRangeHelper}");

        attackRangeHelper.ShowBasicRange(currentOperator.currentAttackableTiles);


        // 스킬 아이콘 - 현재 선택된 스킬을 OperatorData에서 가져옴
        // if (operatorData.skills.Count > 0)
        //{ for (int i=0; i < operatorData.skills.Count; )
        //    if (operatorData.)
        //    {
        //        skillIconImage.sprite = operatorData.skills[currentOperator.selectedSkillIndex].SkillIcon;
        //        skillIconImage.enabled = true;
        //    }
        //    else
        //    {
        //        skillIconImage.enabled = false;
        //    }
        //}
    }

    private void UpdateStats()
    {
        OperatorStats currentStats = currentOperator.GetOperatorStats();

        healthText.text = currentStats.Health.ToString();
        attackPowerText.text = currentStats.AttackPower.ToString();
        defenseText.text = currentStats.Defense.ToString();
        magicResistanceText.text = currentStats.MagicResistance.ToString();
        deploymentCostText.text = currentStats.DeploymentCost.ToString();
        redeployTimeText.text = currentStats.RedeployTime.ToString();
        blockCountText.text = currentStats.MaxBlockableEnemies.ToString();
        attackSpeedText.text = currentStats.AttackSpeed.ToString();
    }

    /// <summary>
    /// 현재 경험치, 현재 레벨 업데이트
    /// </summary>
    private void UpdateGrowthInfo()
    {
        float currentExp = currentOperator.currentExp;
        float maxExp = OperatorGrowthSystem.GetRequiredExp(currentOperator.currentLevel);
        int currentLevel = currentOperator.currentLevel;
        int maxLevel = OperatorGrowthSystem.GetMaxLevel(currentOperator.currentPhase);

        expText.text = $"EXP\n<size=44><color=#FFE61A>{currentExp.ToString()}</color>/{maxExp.ToString()}</size>";
        levelText.text = $"LV\n<size=100><b>{currentLevel.ToString()}</b></size=100>";
        maxLevelText.text = $"/{maxLevel.ToString()}";

        // exp 게이지 업데이트. float로 와야 함!
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

    private void UpdateButtonStates()
    {
        levelUpButton.interactable = currentOperator.currentLevel == OperatorGrowthSystem.GetMaxLevel(currentOperator.currentPhase);
        promoteButton.interactable = currentOperator.CanPromote;
    }

    private void OnDisable()
    {
    }
}
