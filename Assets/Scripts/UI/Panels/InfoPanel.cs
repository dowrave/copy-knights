using TMPro;
using UnityEngine;

/// <summary>
/// ������������ ���Ǵ�, ���۷������� ������ ǥ���ϴ� �г��Դϴ�.
/// </summary>
public class InfoPanel : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI nameText;
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

    public void UpdateUnDeployedInfo(DeployableManager.DeployableInfo deployableInfo)
    {
        currentDeployableInfo = deployableInfo;
        //Debug.Log($"currentDeployableInfo : {currentDeployableInfo}");
        //Debug.Log($"currentDeployableInfo.ownedOperator : {currentDeployableInfo.ownedOperator}");

        // Operator ���� ������Ʈ
        if (currentDeployableInfo.ownedOperator != null)
        {
            currentOperator = currentDeployableInfo.prefab.GetComponent<Operator>();
            currentDeployable = null;
            nameText.text = currentDeployableInfo.operatorData.entityName;
            statsContainer.SetActive(true);
            UpdateOperatorInfo();
        }
        else // deployableUnitEntity ���� ������Ʈ
        {
            currentDeployable = currentDeployableInfo.prefab.GetComponent<DeployableUnitEntity>();
            currentOperator = null;
            nameText.text = currentDeployableInfo.deployableUnitData.entityName;
            statsContainer.SetActive(false);
        }
    }

    /// <summary>
    /// ��ġ�� ������ Ŭ������ ���� �г� ���� ����
    /// </summary>
    public void UpdateDeployedInfo(DeployableUnitEntity deployableUnitEntity)
    {
        // ������ currentOperator�� �־��ٸ� �̺�Ʈ ����
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
            statsContainer.SetActive(true);
            UpdateOperatorInfo();
        }
        else
        {
            currentDeployable = deployableUnitEntity;
            currentOperator = null;

            nameText.text = currentDeployableInfo.deployableUnitData.entityName;
            statsContainer.SetActive(false);
        }
    }

    private void UpdateOperatorInfo()
    {
        if (currentOperator == null) return;

        // ���۷����Ͱ� ��ġ�Ǿ����� Ȯ��
        if (currentOperator.IsDeployed)
        {
            currentOperator.OnHealthChanged += UpdateHealthText;
            currentOperator.OnStatsChanged += UpdateOperatorInfo;

            UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
            attackText.text = $"���ݷ�: {currentOperator.currentStats.AttackPower}";
            defenseText.text = $"����: {currentOperator.currentStats.Defense}";
            magicResistanceText.text = $"�������׷�: {currentOperator.currentStats.MagicResistance}";
            blockCountText.text = $"������: {currentOperator.currentStats.MaxBlockableEnemies}";
        }

        // ��ġ���� ���� ���
        else
        {
            OperatorStats ownedOperatorStats = currentDeployableInfo.ownedOperator.currentStats;

            // ��ġ���� ���� ��� : OwnedOperator�� ������ ������
            float initialHealth = ownedOperatorStats.Health; 
            UpdateHealthText(initialHealth, initialHealth);
            attackText.text = $"���ݷ�: {ownedOperatorStats.AttackPower}";
            defenseText.text = $"����: {ownedOperatorStats.Defense}";
            magicResistanceText.text = $"�������׷�: {ownedOperatorStats.MagicResistance}";
            blockCountText.text = $"������: {ownedOperatorStats.MaxBlockableEnemies}";
        }
    }

    private void UpdateHealthText(float currentHealth, float maxHealth)
    {
        healthText.text = $"ü�� : {currentHealth} / {maxHealth}";
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
