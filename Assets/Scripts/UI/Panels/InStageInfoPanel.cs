using Skills.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// 스테이지에서 사용되는, 오퍼레이터의 정보를 표시하는 패널.
public class InStageInfoPanel : MonoBehaviour
{

    [Header("Base Info Fields")]
    [SerializeField] private Image classIconImage;
    [SerializeField] private GameObject operatorLevelTextBox;
    [SerializeField] private Image promotionIconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Stat Fields")]
    [SerializeField] private GameObject statsContainer;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI magicResistanceText;
    [SerializeField] private TextMeshProUGUI blockCountText;

    [Header("Tabs")]
    [SerializeField] private Button SkillTab; // 아마 이것만 쓸 것 같긴 한데

    [Header("Ability Panel")]
    [SerializeField] private GameObject abilityContainer; 
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillDetailText;

    [Header("Cancel Panel")]
    [SerializeField] private Button cancelPanel;


    // 배치 요소 정보
    private DeployableManager.DeployableInfo currentDeployableInfo;
    private DeployableUnitState currentDeployableUnitState;

    // 오퍼레이터에서만 사용
    private Operator currentOperator;
    private BaseSkill operatorSkill;

    private void Awake()
    {
        cancelPanel.gameObject.SetActive(false);
    }

    public void UpdateInfo(DeployableManager.DeployableInfo deployableInfo)
    {
        currentDeployableInfo = deployableInfo;
        currentDeployableUnitState = DeployableManager.Instance.UnitStates[currentDeployableInfo];

        if (currentDeployableUnitState == null)
        {
            Debug.LogError("현재 배치 요소의 정보가 없음");
            return;
        }

        // 배치된 요소가 아닐 때에만 CancelPanel이 나타남 (배치된 요소는 다이아몬드가 나타나므로 필요 x)
        if (!currentDeployableUnitState.IsDeployed)
        {
            cancelPanel.gameObject.SetActive(true);
            cancelPanel.onClick.AddListener(OnCancelPanelClicked);
        }

        if (currentDeployableUnitState.IsOperator)
        {
            UpdateOperatorInfo();
        }
        else
        {
            UpdateDeployableInfo();
        }
    }

    private void UpdateOperatorInfo()
    {
        ShowOperatorPanels();

        // 이름
        nameText.text = currentDeployableInfo.operatorData.entityName;

        // 레벨
        levelText.text = $"{currentDeployableInfo.ownedOperator.currentLevel}";

        // 스킬
        UpdateSkillInfo();

        // 스탯
        UpdateStatInfo();
    }

    private void UpdateDeployableInfo()
    {
        HideOperatorPanels();

        nameText.text = currentDeployableInfo.deployableUnitData.entityName;
    }

    private void UpdateStatInfo()
    {
        // 기존 오퍼레이터가 있었다면 구독 해제
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
            currentOperator.OnStatsChanged -= UpdateOperatorInfo;
        }

        if (currentDeployableUnitState.IsDeployed)
        {
            // 배치된 경우 실시간 정보를 가져옴
            currentOperator = currentDeployableInfo.deployedOperator;

            UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
            attackText.text = $"공격력: {Mathf.Ceil(currentOperator.currentStats.AttackPower)}";
            defenseText.text = $"방어력: {Mathf.Ceil(currentOperator.currentStats.Defense)}";
            magicResistanceText.text = $"마법저항력: {Mathf.Ceil(currentOperator.currentStats.MagicResistance)}";
            blockCountText.text = $"저지수: {Mathf.Ceil(currentOperator.currentStats.MaxBlockableEnemies)}";

            // 이벤트 구독
            currentOperator.OnHealthChanged += UpdateHealthText;
            currentOperator.OnStatsChanged += UpdateOperatorInfo;
        }
        else
        {
            OperatorStats ownedOperatorStats = currentDeployableInfo.ownedOperator.CurrentStats;

            float initialHealth = ownedOperatorStats.Health;
            UpdateHealthText(initialHealth, initialHealth);
            attackText.text = $"공격력: {Mathf.Ceil(ownedOperatorStats.AttackPower)}";
            defenseText.text = $"방어력: {Mathf.Ceil(ownedOperatorStats.Defense)}";
            magicResistanceText.text = $"마법저항력: {Mathf.Ceil(ownedOperatorStats.MagicResistance)}";
            blockCountText.text = $"저지수: {Mathf.Ceil(ownedOperatorStats.MaxBlockableEnemies)}";
        }
    }

    private void UpdateHealthText(float currentHealth, float maxHealth, float currentShield = 0)
    {
        healthText.text = $"체력 <color=#ff6666>{Mathf.Ceil(currentHealth)} / {Mathf.Ceil(maxHealth)}</color>";
    }

    private void ShowOperatorPanels()
    {
        classIconImage.gameObject.SetActive(true);
        operatorLevelTextBox.gameObject.SetActive(true);
        statsContainer.SetActive(true);
        abilityContainer.SetActive(true);
    }

    private void HideOperatorPanels()
    {
        classIconImage.gameObject.SetActive(false);
        operatorLevelTextBox.gameObject.SetActive(false);
        statsContainer.SetActive(false);
        abilityContainer.SetActive(false);
    }

    private void UpdateSkillInfo()
    {
        operatorSkill = currentDeployableInfo.ownedOperator.StageSelectedSkill;

        if (operatorSkill != null)
        {
            ShowOperatorPanels();
            OperatorIconHelper.SetClassIcon(classIconImage, currentDeployableInfo.operatorData.operatorClass);
            OperatorIconHelper.SetElitePhaseIcon(promotionIconImage, currentDeployableInfo.ownedOperator.currentPhase);

            skillIconImage.sprite = operatorSkill.skillIcon;
            skillNameText.text = operatorSkill.skillName;
            skillDetailText.text = operatorSkill.description;
        }
    }

    private void OnCancelPanelClicked()
    {
        if (DeployableManager.Instance.IsDeployableSelecting)
        {
            DeployableManager.Instance.CancelDeployableSelection();
        }
    }

    private void OnDisable()
    {
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
            currentOperator.OnStatsChanged -= UpdateOperatorInfo;
            currentOperator = null;
        }

        cancelPanel.onClick.RemoveAllListeners();
        cancelPanel.gameObject.SetActive(false);
    }
}
