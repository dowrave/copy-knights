using Skills.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// 스테이지에서 사용되는, 오퍼레이터의 정보를 표시하는 패널.
public class InStageInfoPanel : MonoBehaviour
{

    [Header("Base Info Fields")]
    [SerializeField] private Image classIconImage;
    [SerializeField] private GameObject operatorLevelTextBox;
    [SerializeField] private Image promotionIconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Stat Fields")]
    [SerializeField] private GameObject statsContainer;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI magicResistanceText;
    [SerializeField] private TextMeshProUGUI blockCountText;

    [Header("Tabs")]
    [SerializeField] private Button SkillTab; // 아마 이것만 쓸 것 같긴 한데

    [Header("Ability Panel")]
    [SerializeField] private GameObject abilityContainer; 
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillDetailText;

    // 배치 요소 정보
    private DeployableUnitEntity currentDeployable;
    private DeployableManager.DeployableInfo currentDeployableInfo;

    // 오퍼레이터에서만 사용
    private Operator currentOperator;
    private BaseSkill operatorSkill;

    private void Awake() 
    {
        statsContainer = transform.Find("OperatorInfoContent/StatsContainer").gameObject;
        statsContainer.SetActive(false);
    }

    // 배치되지 않은 유닛 클릭 시 패널 정보 갱신
    public void UpdateUnDeployedInfo(DeployableManager.DeployableInfo deployableInfo)
    {
        currentDeployableInfo = deployableInfo;

        // Operator 정보 업데이트
        if (currentDeployableInfo.ownedOperator != null)
        {
            currentOperator = currentDeployableInfo.prefab.GetComponent<Operator>();
            currentDeployable = null;
            nameText.text = currentDeployableInfo.operatorData.entityName;
            UpdateOperatorInfo();
        }
        else // deployableUnitEntity 정보 업데이트
        {

            currentDeployable = currentDeployableInfo.prefab.GetComponent<DeployableUnitEntity>();
            currentOperator = null;
            nameText.text = currentDeployableInfo.deployableUnitData.entityName;
            HideOperatorPanels();
        }
    }

    // 배치된 유닛 클릭 시 패널 정보 갱신
    public void UpdateDeployedInfo(DeployableUnitEntity deployableUnitEntity)
    { 
        // 기존 이벤트 해제
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
            UpdateOperatorInfo();
        }
        else
        {
            HideOperatorPanels();

            currentDeployable = deployableUnitEntity;
            currentOperator = null;
            nameText.text = currentDeployableInfo.deployableUnitData.entityName;
        }
    }

    private void UpdateOperatorInfo()
    {
        if (currentOperator == null) return;

        OwnedOperator ownedOperator = currentDeployableInfo.ownedOperator;
        operatorSkill = ownedOperator.StageSelectedSkill;

        // 아이콘 이미지 패널 활성화 및 할당
        ShowOperatorPanels();
        OperatorIconHelper.SetClassIcon(classIconImage, ownedOperator.BaseData.operatorClass);
        OperatorIconHelper.SetElitePhaseIcon(promotionIconImage, ownedOperator.currentPhase);

        // 스탯 정보 보이게 하기
        statsContainer.SetActive(true);

        levelText.text = $"{ownedOperator.currentLevel}";

        if (currentOperator.IsDeployed)
        {
            currentOperator.OnHealthChanged += UpdateHealthText;
            currentOperator.OnStatsChanged += UpdateOperatorInfo;
        }

        var currentUnitStates = DeployableManager.Instance.UnitStates[currentDeployableInfo];

        UpdateStatText();
        UpdateSkillInfo();
    }

    private void UpdateStatText()
    {
        // 배치된 경우 - 실시간 정보를 가져옴
        if (currentOperator.IsDeployed)
        {
            UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
            attackText.text = $"공격력: {Mathf.Ceil(currentOperator.currentStats.AttackPower)}";
            defenseText.text = $"방어력: {Mathf.Ceil(currentOperator.currentStats.Defense)}";
            magicResistanceText.text = $"마법저항력: {Mathf.Ceil(currentOperator.currentStats.MagicResistance)}";
            blockCountText.text = $"저지수: {Mathf.Ceil(currentOperator.currentStats.MaxBlockableEnemies)}";
        }
        // 배치되지 않은 경우 - ownedOperator의 정보를 가져옴
        else
        {
            OperatorStats ownedOperatorStats = currentDeployableInfo.ownedOperator.CurrentStats;

            // 배치되지 않은 경우 : OwnedOperator의 정보를 가져옴
            float initialHealth = ownedOperatorStats.Health;
            UpdateHealthText(initialHealth, initialHealth);
            attackText.text = $"공격력: {Mathf.Ceil(ownedOperatorStats.AttackPower)}";
            defenseText.text = $"방어력: {Mathf.Ceil(ownedOperatorStats.Defense)}";
            magicResistanceText.text = $"마법저항력: {Mathf.Ceil(ownedOperatorStats.MagicResistance)}";
            blockCountText.text = $"저지수: {Mathf.Ceil(ownedOperatorStats.MaxBlockableEnemies)}";
        }
    }

    private void UpdateHealthText(float currentHealth, float maxHealth, float currentShield = 0)
    {
        healthText.text = $"체력 <color=#ff6666>{Mathf.Ceil(currentHealth)} / {Mathf.Ceil(maxHealth)}</color>";
    }

    private void ShowOperatorPanels()
    {
        classIconImage.gameObject.SetActive(true);
        operatorLevelTextBox.gameObject.SetActive(true);
        statsContainer.SetActive(true);
        abilityContainer.SetActive(true);
    }

    private void HideOperatorPanels()
    {
        classIconImage.gameObject.SetActive(false);
        operatorLevelTextBox.gameObject.SetActive(false);
        statsContainer.SetActive(false);
        abilityContainer.SetActive(false);
    }

    private void UpdateSkillInfo()
    {
        if (operatorSkill != null)
        {
            skillIconImage.sprite = operatorSkill.skillIcon;
            skillNameText.text = operatorSkill.skillName;
            skillDetailText.text = operatorSkill.description;
        }
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
