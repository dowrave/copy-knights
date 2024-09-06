
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Timeline.TimelinePlaybackControls;


public class InfoPanel : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI magicResistanceText;
    [SerializeField] private TextMeshProUGUI blockCountText;

    private IDeployable currentDeployable;
    private Operator currentOperator;
    private GameObject statsContainer;

    private void Awake() 
    {
        statsContainer = transform.Find("OperatorInfoContent/StatsContainer").gameObject;
        statsContainer.SetActive(false);
    }

    public void UpdateInfo(IDeployable deployable)
    {
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
        }

        // �⺻ ���� ������Ʈ
        nameText.text = deployable.Transform.name;

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
            UpdateHealthText(currentOperator.currentHealth, currentOperator.MaxHealth);
        }
        else
        {
            // ��ġ���� ���� ��� OperatorData���� ���� ���� ������
            UpdateHealthText(op.data.stats.health, op.data.stats.health);
        }

        attackText.text = $"���ݷ�: {op.data.stats.attackPower}";
        defenseText.text = $"����: {op.data.stats.defense}";
        magicResistanceText.text = $"�������׷�: {op.data.stats.magicResistance}";
        blockCountText.text = $"������: {op.data.maxBlockableEnemies}";
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
