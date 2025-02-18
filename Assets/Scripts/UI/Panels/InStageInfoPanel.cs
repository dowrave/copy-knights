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

    // ��ġ ��� ����
    private DeployableUnitEntity currentDeployable;
    private DeployableManager.DeployableInfo currentDeployableInfo;

    // ���۷����Ϳ����� ���
    private Operator currentOperator;
    private BaseSkill operatorSkill;

    private void Awake() 
    {
        statsContainer = transform.Find("OperatorInfoContent/StatsContainer").gameObject;
        statsContainer.SetActive(false);
    }

    // ��ġ���� ���� ���� Ŭ�� �� �г� ���� ����
    public void UpdateUnDeployedInfo(DeployableManager.DeployableInfo deployableInfo)
    {
        currentDeployableInfo = deployableInfo;

        // Operator ���� ������Ʈ
        if (currentDeployableInfo.ownedOperator != null)
        {
            currentOperator = currentDeployableInfo.prefab.GetComponent<Operator>();
            currentDeployable = null;
            nameText.text = currentDeployableInfo.operatorData.entityName;
            UpdateOperatorInfo();
        }
        else // deployableUnitEntity ���� ������Ʈ
        {

            currentDeployable = currentDeployableInfo.prefab.GetComponent<DeployableUnitEntity>();
            currentOperator = null;
            nameText.text = currentDeployableInfo.deployableUnitData.entityName;
            HideOperatorPanels();
        }
    }

    // ��ġ�� ���� Ŭ�� �� �г� ���� ����
    public void UpdateDeployedInfo(DeployableUnitEntity deployableUnitEntity)
    { 
        // ���� �̺�Ʈ ����
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
            currentOperator.OnStatsChanged -= UpdateOperatorInfo;
        }

        if (deployableUnitEntity is Operator op)
        {
            currentOperator = op;
            currentDeployable = null;
            nameText.text = currentDeployableInfo.operatorData.entityName;
            UpdateOperatorInfo();
        }
        else
        {
            HideOperatorPanels();

            currentDeployable = deployableUnitEntity;
            currentOperator = null;
            nameText.text = currentDeployableInfo.deployableUnitData.entityName;
        }
    }

    private void UpdateOperatorInfo()
    {
        if (currentOperator == null) return;

        OwnedOperator ownedOperator = currentDeployableInfo.ownedOperator;
        operatorSkill = ownedOperator.StageSelectedSkill;

        // ������ �̹��� �г� Ȱ��ȭ �� �Ҵ�
        ShowOperatorPanels();
        OperatorIconHelper.SetClassIcon(classIconImage, ownedOperator.BaseData.operatorClass);
        OperatorIconHelper.SetElitePhaseIcon(promotionIconImage, ownedOperator.currentPhase);

        // ���� ���� ���̰� �ϱ�
        statsContainer.SetActive(true);

        levelText.text = $"{ownedOperator.currentLevel}";

        if (currentOperator.IsDeployed)
        {
            currentOperator.OnHealthChanged += UpdateHealthText;
            currentOperator.OnStatsChanged += UpdateOperatorInfo;
        }

        var currentUnitStates = DeployableManager.Instance.UnitStates[currentDeployableInfo];

        UpdateStatText();
        UpdateSkillInfo();
    }

    private void UpdateStatText()
    {
        // ��ġ�� ��� - �ǽð� ������ ������
        if (currentOperator.IsDeployed)
        {
            UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
            attackText.text = $"���ݷ�: {Mathf.Ceil(currentOperator.currentStats.AttackPower)}";
            defenseText.text = $"����: {Mathf.Ceil(currentOperator.currentStats.Defense)}";
            magicResistanceText.text = $"�������׷�: {Mathf.Ceil(currentOperator.currentStats.MagicResistance)}";
            blockCountText.text = $"������: {Mathf.Ceil(currentOperator.currentStats.MaxBlockableEnemies)}";
        }
        // ��ġ���� ���� ��� - ownedOperator�� ������ ������
        else
        {
            OperatorStats ownedOperatorStats = currentDeployableInfo.ownedOperator.CurrentStats;

            // ��ġ���� ���� ��� : OwnedOperator�� ������ ������
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
        if (operatorSkill != null)
        {
            skillIconImage.sprite = operatorSkill.skillIcon;
            skillNameText.text = operatorSkill.skillName;
            skillDetailText.text = operatorSkill.description;
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
    }
}
