
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

        // �⺻ ���� ������Ʈ
        nameText.text = deployable.Name;

        // Operator Ư�� ���� ������Ʈ
        if (deployable is Operator op)
        {
            statsContainer.SetActive(true);
            UpdateOperatorInfo(op);
        }
        else
        {
            statsContainer.SetActive(false);
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
            // ��ġ���� ���� ��� OperatorData���� ���� ���� ������
            float initialHealth = op.Data.stats.health; 
            UpdateHealthText(initialHealth, initialHealth);
        }

        attackText.text = $"���ݷ�: {op.AttackPower}";
        defenseText.text = $"����: {op.Defense}";
        magicResistanceText.text = $"�������׷�: {op.MagicResistance}";
        blockCountText.text = $"������: {op.MaxBlockableEnemies}";
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
