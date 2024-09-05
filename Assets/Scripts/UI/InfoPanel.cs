
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

    public void UpdateInfo(IDeployable deployable)
    {
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
        }

        // 기본 정보 업데이트
        nameText.text = deployable.GetType().Name;

        // Operator 특정 정보 업데이트
        if (deployable is Operator op)
        {
            UpdateOperatorInfo(op);
        }

    }

    private void UpdateOperatorInfo(Operator op)
    {
        // 이전 오퍼레이터의 구독 해제
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
        }

        currentOperator = op;
        currentOperator.OnHealthChanged += UpdateHealthText;

        UpdateHealthText(currentOperator.currentHealth, currentOperator.MaxHealth);
        attackText.text = $"공격력: {op.data.stats.attackPower}";
        defenseText.text = $"방어력: {op.data.stats.defense}";
        magicResistanceText.text = $"마법저항력: {op.data.stats.magicResistance}";
        blockCountText.text = $"저지수: {op.data.maxBlockableEnemies}";
    }

    private void UpdateHealthText(float currentHealth, float maxHealth)
    {
        healthText.text = $"체력 : {currentHealth} / {maxHealth}";
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
