using TMPro;
using UnityEngine;


public class InfoPanel : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI magicResistanceText;
    [SerializeField] private TextMeshProUGUI blockCountText;

    private DeployableUnitEntity currentDeployable;
    private Operator currentOperator;
    private GameObject statsContainer;

    private void Awake() 
    {
        statsContainer = transform.Find("OperatorInfoContent/StatsContainer").gameObject;
        statsContainer.SetActive(false);
    }

    public void UpdateInfo(DeployableUnitEntity deployable)
    {
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
            currentOperator.OnStatsChanged -= UpdateOperatorInfo;
        }


        // Operator Ư�� ���� ������Ʈ
        if (deployable is Operator op)
        {
            currentOperator = op;
            nameText.text = op.Data.entityName;
            statsContainer.SetActive(true);
            UpdateOperatorInfo();
        }
        else
        {
            nameText.text = deployable.Data.entityName;
            //statsContainer.SetActive(false);
        }

    }

    private void UpdateOperatorInfo()
    {
        // ���۷����Ͱ� ��ġ�Ǿ����� Ȯ��
        if (currentOperator.IsDeployed)
        {
            currentOperator.OnHealthChanged += UpdateHealthText;
            currentOperator.OnStatsChanged += UpdateOperatorInfo; 

            // ��ġ�� ��� ������ ���� ���
            UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
            attackText.text = $"���ݷ�: {currentOperator.currentStats.AttackPower}";
            defenseText.text = $"����: {currentOperator.currentStats.Defense}";
            magicResistanceText.text = $"�������׷�: {currentOperator.currentStats.MagicResistance}";
            blockCountText.text = $"������: {currentOperator.currentStats.MaxBlockableEnemies}";
        }


        else
        {
            // ��ġ���� ���� ��� Data�� ���� ������
            float initialHealth = currentOperator.Data.stats.Health; 
            UpdateHealthText(initialHealth, initialHealth);
            attackText.text = $"���ݷ�: {currentOperator.Data.stats.AttackPower}";
            defenseText.text = $"����: {currentOperator.Data.stats.Defense}";
            magicResistanceText.text = $"�������׷�: {currentOperator.Data.stats.MagicResistance}";
            blockCountText.text = $"������: {currentOperator.Data.stats.MaxBlockableEnemies}";
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
