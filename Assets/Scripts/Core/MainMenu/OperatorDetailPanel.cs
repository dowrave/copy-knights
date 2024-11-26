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
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private Image[] skillIconImages;

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
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button promoteButton;

    private OperatorData operatorData;
    private OwnedOperator ownedOperator;

    private void Awake()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        levelUpButton.onClick.AddListener(OnLevelUpClicked);
        promoteButton.onClick.AddListener(OnPromoteClicked);
    }

    private void OnLevelUpClicked()
    {
        if (ownedOperator.CanLevelUp)
        {
            GameObject levelUpPanel = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorLevelUp];
            MainMenuManager.Instance.FadeInAndHide(levelUpPanel, gameObject);
        }
    }

    private void OnPromoteClicked()
    {
        if (ownedOperator.CanPromote)
        {
            GameObject promotionPanel = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorPromotion];
            MainMenuManager.Instance.FadeInAndHide(promotionPanel, gameObject);
        }
    }

    public void Initialize(OperatorData operatorData)
    {
        this.operatorData = operatorData;
        this.ownedOperator = GameManagement.Instance.PlayerDataManager.GetOwnedOperator(operatorData.entityName);

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
        levelText.text = $"Level.{ownedOperator.currentLevel}";
        phaseText.text = $"Elite {(int)ownedOperator.currentPhase}";

        // 스킬 아이콘 - 현재 선택된 스킬을 OperatorData에서 가져옴
        // if (operatorData.skills.Count > 0)
        //{ for (int i=0; i < operatorData.skills.Count; )
        //    if (operatorData.)
        //    {
        //        skillIconImage.sprite = operatorData.skills[ownedOperator.selectedSkillIndex].SkillIcon;
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
        OperatorStats currentStats = ownedOperator.GetCurrentStats();

        healthText.text = currentStats.Health.ToString();
        attackPowerText.text = currentStats.AttackPower.ToString();
        defenseText.text = currentStats.Defense.ToString();
        magicResistanceText.text = currentStats.MagicResistance.ToString();
        deploymentCostText.text = currentStats.DeploymentCost.ToString();
        redeployTimeText.text = currentStats.RedeployTime.ToString();
        blockCountText.text = currentStats.MaxBlockableEnemies.ToString();
        attackSpeedText.text = currentStats.AttackSpeed.ToString();
    }

    private void UpdateGrowthInfo()
    {
        string currentExp = ownedOperator.currentExp.ToString();
        string maxExp = OperatorGrowthSystem.GetRequiredExp(ownedOperator.currentLevel).ToString();
        expText.text = $"{currentExp} / {maxExp}";
    }

    private void UpdateButtonStates()
    {
        levelUpButton.interactable = ownedOperator.CanLevelUp;
        promoteButton.interactable = ownedOperator.CanPromote; 
    }
}
