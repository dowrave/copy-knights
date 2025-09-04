using Skills.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// ������������ ���Ǵ�, ���۷������� ������ ǥ���ϴ� �г�.
public class InStageInfoPanel : MonoBehaviour
{

    [Header("Base Info Fields")]
    [SerializeField] private Image classIconImage = default!;
    [SerializeField] private GameObject operatorLevelTextBox = default!;
    [SerializeField] private Image promotionIconImage = default!;
    [SerializeField] private TextMeshProUGUI nameText = default!;
    [SerializeField] private TextMeshProUGUI levelText = default!;

    [Header("Stat Fields")]
    [SerializeField] private GameObject statsContainer = default!;
    [SerializeField] private TextMeshProUGUI healthText = default!;
    [SerializeField] private TextMeshProUGUI attackText = default!;
    [SerializeField] private TextMeshProUGUI defenseText = default!;
    [SerializeField] private TextMeshProUGUI magicResistanceText = default!;
    [SerializeField] private TextMeshProUGUI blockCountText = default!;

    [Header("Tabs")]
    //[SerializeField] private Button SkillTab = default!; // �Ƹ� �̰͸� �� �� ���� �ѵ�

    [Header("Ability Panel")]
    [SerializeField] private GameObject abilityContainer = default!; 
    [SerializeField] private Image skillIconImage = default!;
    [SerializeField] private TextMeshProUGUI skillNameText = default!;
    [SerializeField] private TextMeshProUGUI skillDetailText = default!;

    [Header("Cancel Panel")]
    [SerializeField] private Button cancelPanel = default!;


    // ��ġ ��� ����
    private DeployableManager.DeployableInfo currentDeployableInfo = default!;
    private DeployableUnitState? currentDeployableUnitState;

    // ���۷����Ϳ����� ���
    private Operator? currentOperator;
    private OperatorSkill operatorSkill = default!;

    private void Awake()
    {
        cancelPanel.gameObject.SetActive(false);
    }

    public void UpdateInfo(DeployableManager.DeployableInfo deployableInfo, bool IsClickDeployed)
    {
        currentDeployableInfo = deployableInfo;
        currentDeployableUnitState = DeployableManager.Instance!.UnitStates[currentDeployableInfo];

        if (currentDeployableUnitState == null)
        {
            Debug.LogError("���� ��ġ ����� ������ ����");
            return;
        }

        // �ڽ����� ������ ����� ���, ���� ���� �κ��� Ŭ���ϸ� ���� ������ ����� �� ����
        if (!IsClickDeployed)
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
        nameText.text = currentDeployableInfo.operatorData?.entityName ?? string.Empty;

        // ����
        levelText.text = $"{currentDeployableInfo.ownedOperator?.currentLevel}";

        // ��ų
        UpdateSkillInfo();

        // ����
        UpdateStatInfo();
    }

    private void UpdateDeployableInfo()
    {
        HideOperatorPanels();

        nameText.text = currentDeployableInfo.deployableUnitData?.entityName ?? string.Empty;
    }

    private void UpdateStatInfo()
    {
        // ���� ���۷����Ͱ� �־��ٸ� ���� ����
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
            currentOperator.OnStatsChanged -= UpdateOperatorInfo;
        }

        if (currentDeployableUnitState != null && currentDeployableUnitState.IsDeployed)
        {
            // ��ġ�� ��� �ǽð� ������ ������
            currentOperator = currentDeployableInfo.deployedOperator;
            if (currentOperator != null)
            {
                UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
                attackText.text = $"���ݷ�: {Mathf.FloorToInt(currentOperator.currentOperatorStats.AttackPower)}";
                defenseText.text = $"����: {Mathf.FloorToInt(currentOperator.currentOperatorStats.Defense)}";
                magicResistanceText.text = $"�������׷�: {Mathf.FloorToInt(currentOperator.currentOperatorStats.MagicResistance)}";
                blockCountText.text = $"������: {Mathf.FloorToInt(currentOperator.currentOperatorStats.MaxBlockableEnemies)}";

                // �̺�Ʈ ����
                currentOperator.OnHealthChanged += UpdateHealthText;
                currentOperator.OnStatsChanged += UpdateOperatorInfo;
            }
        }
        else
        {
            OperatorStats ownedOperatorStats = currentDeployableInfo.ownedOperator!.CurrentStats;

            float initialHealth = ownedOperatorStats.Health;
            UpdateHealthText(initialHealth, initialHealth);
            attackText.text = $"���ݷ�: {Mathf.FloorToInt(ownedOperatorStats.AttackPower)}";
            defenseText.text = $"����: {Mathf.FloorToInt(ownedOperatorStats.Defense)}";
            magicResistanceText.text = $"�������׷�: {Mathf.FloorToInt(ownedOperatorStats.MagicResistance)}";
            blockCountText.text = $"������: {Mathf.FloorToInt(ownedOperatorStats.MaxBlockableEnemies)}";
        }
    }

    private void UpdateHealthText(float currentHealth, float maxHealth, float currentShield = 0)
    {
        healthText.text = $"ü�� <color=#ff6666>{Mathf.FloorToInt(currentHealth)} / {Mathf.FloorToInt(maxHealth)}</color>";
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
        // operatorData�� ownedOperator�� null�� �ƴ� ���� ����
        if (currentDeployableInfo.operatorData == null || currentDeployableInfo.ownedOperator == null)
        {
            return;
        }

        int skillIndex = GameManagement.Instance!.UserSquadManager.GetCurrentSkillIndex(currentDeployableInfo.ownedOperator);
        operatorSkill = currentDeployableInfo.ownedOperator.UnlockedSkills[skillIndex];

        if (operatorSkill != null)
        {
            ShowOperatorPanels();
            OperatorIconHelper.SetClassIcon(classIconImage, currentDeployableInfo.operatorData.operatorClass);

            // 0 ����ȭ�� �����ܿ� �̹����� ���� : ������Ʈ ��Ȱ��ȭ -> ���� ��Ұ� �������� �̵�
            if (currentDeployableInfo.ownedOperator.currentPhase == OperatorGrowthSystem.ElitePhase.Elite0)
            {
                promotionIconImage.gameObject.SetActive(false);
            }
            else
            {
                promotionIconImage.gameObject.SetActive(true);
                OperatorIconHelper.SetElitePhaseIcon(promotionIconImage, currentDeployableInfo.ownedOperator.currentPhase);
            }

            skillIconImage.sprite = operatorSkill.skillIcon;
            skillNameText.text = operatorSkill.SkillName;
            skillDetailText.text = operatorSkill.description;
        }
    }

    private void OnCancelPanelClicked()
    {
        if (DeployableManager.Instance!.IsDeployableSelecting)
        {
            DeployableManager.Instance!.CancelDeployableSelection();
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
