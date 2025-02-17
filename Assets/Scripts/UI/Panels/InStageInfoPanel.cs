using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// ������������ ���Ǵ�, ���۷������� ������ ǥ���ϴ� �г��Դϴ�.
/// </summary>
public class InStageInfoPanel : MonoBehaviour
{

    [Header("Base Info Fields")]
    [SerializeField] private Image classIconImage;
    [SerializeField] private GameObject operatorLevelTextBox;
    [SerializeField] private Image promotionIconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Stat Fields")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI magicResistanceText;
    [SerializeField] private TextMeshProUGUI blockCountText;

    private Operator currentOperator;
    private DeployableUnitEntity currentDeployable;
    private GameObject statsContainer;
    private DeployableManager.DeployableInfo currentDeployableInfo;

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
            classIconImage.gameObject.SetActive(false);
            operatorLevelTextBox.gameObject.SetActive(false);

            currentDeployable = currentDeployableInfo.prefab.GetComponent<DeployableUnitEntity>();
            currentOperator = null;
            nameText.text = currentDeployableInfo.deployableUnitData.entityName;
            statsContainer.SetActive(false);
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
            // Ŭ���� ������, ���� ���� ����
            classIconImage.gameObject.SetActive(false);
            operatorLevelTextBox.gameObject.SetActive(false);

            currentDeployable = deployableUnitEntity;
            currentOperator = null;
            nameText.text = currentDeployableInfo.deployableUnitData.entityName;
            statsContainer.SetActive(false);
        }
    }

    private void UpdateOperatorInfo()
    {
        if (currentOperator == null) return;

        OwnedOperator ownedOperator = currentDeployableInfo.ownedOperator;

        // ������ �̹��� �г� Ȱ��ȭ �� �Ҵ�
        classIconImage.gameObject.SetActive(true);
        operatorLevelTextBox.gameObject.SetActive(true);
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
