
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
        // 이전 오퍼레이터의 구독 해제
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
        }

        // currentOperator는 null일 수도 있음
        currentOperator = op; 

        // 배치된 오퍼레이터의 체력 표시
        if (currentOperator != null)
        {
            Debug.Log("배치된 오퍼레이터 체력 표시");
            currentOperator.OnHealthChanged += UpdateHealthText;
            UpdateHealthText(currentOperator.currentHealth, currentOperator.MaxHealth);
        }
        // 배치되지 않은 오퍼레이터의 체력 표시
        else
        {
            Debug.Log("배치되지 않은 오퍼레이터 체력 표시");

            UpdateHealthText(operatorData.baseStats.Health, operatorData.baseStats.Health);
        }

        nameText.text = operatorData.operatorName;

        attackText.text = $"공격력: {operatorData.baseStats.AttackPower}";
        defenseText.text = $"방어력: {operatorData.baseStats.Defense}";
        magicResistanceText.text = $"마법저항력: {operatorData.baseStats.MagicResistance}";
        blockCountText.text = $"저지수: {operatorData.maxBlockableEnemies}";
    }

    private void UpdateHealthText(float currentHealth, float maxHealth)
    {
        healthText.text = $"체력 : {currentHealth} / {maxHealth}";
    }
}
