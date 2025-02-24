using Skills.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// ������������ ���Ǵ�, ���۷������� ������ ǥ���ϴ� �г�.
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
    [SerializeField] private Button SkillTab; // �Ƹ� �̰͸� �� �� ���� �ѵ�

    [Header("Ability Panel")]
    [SerializeField] private GameObject abilityContainer; 
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillDetailText;

    [Header("Cancel Panel")]
    [SerializeField] private Button cancelPanel;


    // ��ġ ��� ����
    private DeployableManager.DeployableInfo currentDeployableInfo;
    private DeployableUnitState currentDeployableUnitState;

    // ���۷����Ϳ����� ���
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
            Debug.LogError("���� ��ġ ����� ������ ����");
            return;
        }

        // ��ġ�� ��Ұ� �ƴ� ������ CancelPanel�� ��Ÿ�� (��ġ�� ��Ҵ� ���̾Ƹ�尡 ��Ÿ���Ƿ� �ʿ� x)
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

        // �̸�
        nameText.text = currentDeployableInfo.operatorData.entityName;

        // ����
        levelText.text = $"{currentDeployableInfo.ownedOperator.currentLevel}";

        // ��ų
        UpdateSkillInfo();

        // ����
        UpdateStatInfo();
    }

    private void UpdateDeployableInfo()
    {
        HideOperatorPanels();

        nameText.text = currentDeployableInfo.deployableUnitData.entityName;
    }

    private void UpdateStatInfo()
    {
        // ���� ���۷����Ͱ� �־��ٸ� ���� ����
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
            currentOperator.OnStatsChanged -= UpdateOperatorInfo;
        }

        if (currentDeployableUnitState.IsDeployed)
        {
            // ��ġ�� ��� �ǽð� ������ ������
            currentOperator = currentDeployableInfo.deployedOperator;

            UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
            attackText.text = $"���ݷ�: {Mathf.Ceil(currentOperator.currentStats.AttackPower)}";
            defenseText.text = $"����: {Mathf.Ceil(currentOperator.currentStats.Defense)}";
            magicResistanceText.text = $"�������׷�: {Mathf.Ceil(currentOperator.currentStats.MagicResistance)}";
            blockCountText.text = $"������: {Mathf.Ceil(currentOperator.currentStats.MaxBlockableEnemies)}";

            // �̺�Ʈ ����
            currentOperator.OnHealthChanged += UpdateHealthText;
            currentOperator.OnStatsChanged += UpdateOperatorInfo;
        }
        else
        {
            OperatorStats ownedOperatorStats = currentDeployableInfo.ownedOperator.CurrentStats;

            float initialHealth = ownedOperatorStats.Health;
            UpdateHealthText(initialHealth, initialHealth);
            attackText.text = $"���ݷ�: {Mathf.Ceil(ownedOperatorStats.AttackPower)}";
            defenseText.text = $"����: {Mathf.Ceil(ownedOperatorStats.Defense)}";
            magicResistanceText.text = $"�������׷�: {Mathf.Ceil(ownedOperatorStats.MagicResistance)}";
            blockCountText.text = $"������: {Mathf.Ceil(ownedOperatorStats.MaxBlockableEnemies)}";
        }
    }

    private void UpdateHealthText(float currentHealth, float maxHealth, float currentShield = 0)
    {
        healthText.text = $"ü�� <color=#ff6666>{Mathf.Ceil(currentHealth)} / {Mathf.Ceil(maxHealth)}</color>";
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
