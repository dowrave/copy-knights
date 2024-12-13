using TMPro;
using UnityEngine;

/// <summary>
/// 스테이지에서 사용되는, 오퍼레이터의 정보를 표시하는 패널입니다.
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

        // Operator 정보 업데이트
        if (currentDeployableInfo.ownedOperator != null)
        {
            currentOperator = currentDeployableInfo.prefab.GetComponent<Operator>();
            currentDeployable = null;
            nameText.text = currentDeployableInfo.operatorData.entityName;
            statsContainer.SetActive(true);
            UpdateOperatorInfo();
        }
        else // deployableUnitEntity 정보 업데이트
        {
            currentDeployable = currentDeployableInfo.prefab.GetComponent<DeployableUnitEntity>();
            currentOperator = null;
            nameText.text = currentDeployableInfo.deployableUnitData.entityName;
            statsContainer.SetActive(false);
        }
    }

    /// <summary>
    /// 배치된 유닛을 클릭했을 때의 패널 정보 갱신
    /// </summary>
    public void UpdateDeployedInfo(DeployableUnitEntity deployableUnitEntity)
    {
        // 기존에 currentOperator가 있었다면 이벤트 해제
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

        // 오퍼레이터가 배치되었는지 확인
        if (currentOperator.IsDeployed)
        {
            currentOperator.OnHealthChanged += UpdateHealthText;
            currentOperator.OnStatsChanged += UpdateOperatorInfo;

            UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
            attackText.text = $"공격력: {currentOperator.currentStats.AttackPower}";
            defenseText.text = $"방어력: {currentOperator.currentStats.Defense}";
            magicResistanceText.text = $"마법저항력: {currentOperator.currentStats.MagicResistance}";
            blockCountText.text = $"저지수: {currentOperator.currentStats.MaxBlockableEnemies}";
        }

        // 배치되지 않은 경우
        else
        {
            OperatorStats ownedOperatorStats = currentDeployableInfo.ownedOperator.currentStats;

            // 배치되지 않은 경우 : OwnedOperator의 정보를 가져옴
            float initialHealth = ownedOperatorStats.Health; 
            UpdateHealthText(initialHealth, initialHealth);
            attackText.text = $"공격력: {ownedOperatorStats.AttackPower}";
            defenseText.text = $"방어력: {ownedOperatorStats.Defense}";
            magicResistanceText.text = $"마법저항력: {ownedOperatorStats.MagicResistance}";
            blockCountText.text = $"저지수: {ownedOperatorStats.MaxBlockableEnemies}";
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
