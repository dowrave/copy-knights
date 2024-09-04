
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class OperatorInfoPanel : MonoBehaviour
{
    private Operator currentOperator;  

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI magicResistanceText;
    [SerializeField] private TextMeshProUGUI blockCountText;

    public void UpdateInfo(OperatorData operatorData, Operator op = null)
    {
        // ���� ���۷������� ���� ����
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
        }

        // currentOperator�� null�� ���� ����
        currentOperator = op; 

        // ��ġ�� ���۷������� ü�� ǥ��
        if (currentOperator != null)
        {
            Debug.Log("��ġ�� ���۷����� ü�� ǥ��");
            currentOperator.OnHealthChanged += UpdateHealthText;
            UpdateHealthText(currentOperator.currentHealth, currentOperator.MaxHealth);
        }
        // ��ġ���� ���� ���۷������� ü�� ǥ��
        else
        {
            Debug.Log("��ġ���� ���� ���۷����� ü�� ǥ��");

            UpdateHealthText(operatorData.stats.health, operatorData.stats.health);
        }

        nameText.text = operatorData.operatorName;

        attackText.text = $"���ݷ�: {operatorData.stats.attackPower}";
        defenseText.text = $"����: {operatorData.stats.defense}";
        magicResistanceText.text = $"�������׷�: {operatorData.stats.magicResistance}";
        blockCountText.text = $"������: {operatorData.maxBlockableEnemies}";
    }

    private void UpdateHealthText(float currentHealth, float maxHealth)
    {
        healthText.text = $"ü�� : {currentHealth} / {maxHealth}";
    }
}
