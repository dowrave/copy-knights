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


        // Operator 특정 정보 업데이트
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
        // 이전 오퍼레이터의 구독 해제
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
        }

        currentOperator = op;

        // 오퍼레이터가 배치되었는지 확인
        if (op.IsDeployed)
        {
            currentOperator.OnHealthChanged += UpdateHealthText;
            UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
        }
        else
        {
            // 배치되지 않은 경우 초기 체력값을 가져옴
            float initialHealth = op.Data.stats.Health; 
            UpdateHealthText(initialHealth, initialHealth);
        }

        attackText.text = $"공격력: {op.currentStats.AttackPower}";
        defenseText.text = $"방어력: {op.currentStats.Defense}";
        magicResistanceText.text = $"마법저항력: {op.currentStats.MagicResistance}";
        blockCountText.text = $"저지수: {op.currentStats.MaxBlockableEnemies}";
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
