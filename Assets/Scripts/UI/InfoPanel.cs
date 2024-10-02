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


        // Operator 특정 정보 업데이트
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
        // 오퍼레이터가 배치되었는지 확인
        if (currentOperator.IsDeployed)
        {
            currentOperator.OnHealthChanged += UpdateHealthText;
            currentOperator.OnStatsChanged += UpdateOperatorInfo; 

            // 배치된 경우 현재의 값을 사용
            UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
            attackText.text = $"공격력: {currentOperator.currentStats.AttackPower}";
            defenseText.text = $"방어력: {currentOperator.currentStats.Defense}";
            magicResistanceText.text = $"마법저항력: {currentOperator.currentStats.MagicResistance}";
            blockCountText.text = $"저지수: {currentOperator.currentStats.MaxBlockableEnemies}";
        }


        else
        {
            // 배치되지 않은 경우 Data의 값을 가져옴
            float initialHealth = currentOperator.Data.stats.Health; 
            UpdateHealthText(initialHealth, initialHealth);
            attackText.text = $"공격력: {currentOperator.Data.stats.AttackPower}";
            defenseText.text = $"방어력: {currentOperator.Data.stats.Defense}";
            magicResistanceText.text = $"마법저항력: {currentOperator.Data.stats.MagicResistance}";
            blockCountText.text = $"저지수: {currentOperator.Data.stats.MaxBlockableEnemies}";
        }
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
            currentOperator.OnStatsChanged -= UpdateOperatorInfo;
            currentOperator = null;
        }
    }
}
