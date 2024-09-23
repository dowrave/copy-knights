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
        }


        // Operator Ư�� ���� ������Ʈ
        if (deployable is Operator op)
        {
            nameText.text = op.Data.entityName;
            statsContainer.SetActive(true);
            UpdateOperatorInfo(op);
        }
        else
        {
            nameText.text = deployable.Data.entityName;
            //statsContainer.SetActive(false);
        }

    }

    private void UpdateOperatorInfo(Operator op)
    {
        // ���� ���۷������� ���� ����
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
        }

        currentOperator = op;

        // ���۷����Ͱ� ��ġ�Ǿ����� Ȯ��
        if (op.IsDeployed)
        {
            currentOperator.OnHealthChanged += UpdateHealthText;
            UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
        }
        else
        {
            // ��ġ���� ���� ��� �ʱ� ü�°��� ������
            float initialHealth = op.Data.stats.Health; 
            UpdateHealthText(initialHealth, initialHealth);
        }

        attackText.text = $"���ݷ�: {op.currentStats.AttackPower}";
        defenseText.text = $"����: {op.currentStats.Defense}";
        magicResistanceText.text = $"�������׷�: {op.currentStats.MagicResistance}";
        blockCountText.text = $"������: {op.currentStats.MaxBlockableEnemies}";
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
            currentOperator = null;
        }
    }
}
