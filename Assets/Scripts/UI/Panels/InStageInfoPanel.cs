using Skills.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// 스테이지에서 사용되는, 오퍼레이터의 정보를 표시하는 패널.
public class InStageInfoPanel : MonoBehaviour
{

    [Header("Base Info Fields")]
    [SerializeField] private Image classIconImage = default!;
    [SerializeField] private GameObject operatorLevelTextBox = default!;
    [SerializeField] private Image promotionIconImage = default!;
    [SerializeField] private TextMeshProUGUI nameText = default!;
    [SerializeField] private TextMeshProUGUI levelText = default!;

    [Header("Stat Fields")]
    [SerializeField] private GameObject statsContainer = default!;
    [SerializeField] private TextMeshProUGUI healthText = default!;
    [SerializeField] private TextMeshProUGUI attackText = default!;
    [SerializeField] private TextMeshProUGUI defenseText = default!;
    [SerializeField] private TextMeshProUGUI magicResistanceText = default!;
    [SerializeField] private TextMeshProUGUI blockCountText = default!;

    [Header("Tabs")]
    //[SerializeField] private Button SkillTab = default!; // 아마 이것만 쓸 것 같긴 한데

    [Header("Ability Panel")]
    [SerializeField] private GameObject abilityContainer = default!; 
    [SerializeField] private Image skillIconImage = default!;
    [SerializeField] private TextMeshProUGUI skillNameText = default!;
    [SerializeField] private TextMeshProUGUI skillDetailText = default!;

    [Header("Cancel Panel")]
    [SerializeField] private Button cancelPanel = default!;


    // 배치 요소 정보
    private DeployableManager.DeployableInfo currentDeployableInfo = default!;
    private DeployableUnitState? currentDeployableUnitState;

    // 오퍼레이터에서만 사용
    private Operator? currentOperator;
    private BaseSkill operatorSkill = default!;

    private void Awake()
    {
        cancelPanel.gameObject.SetActive(false);
    }

    public void UpdateInfo(DeployableManager.DeployableInfo deployableInfo, bool IsClickDeployed)
    {
        currentDeployableInfo = deployableInfo;
        currentDeployableUnitState = DeployableManager.Instance!.UnitStates[currentDeployableInfo];

        if (currentDeployableUnitState == null)
        {
            Debug.LogError("현재 배치 요소의 정보가 없음");
            return;
        }

        // 박스에서 꺼내는 요소인 경우, 맵의 남은 부분을 클릭하면 현재 동작을 취소할 수 있음
        if (!IsClickDeployed)
        {
            cancelPanel.gameObject.SetActive(true);
            cancelPanel.onClick.AddListener(OnCancelPanelClicked);
        }

        if (currentDeployableUnitState.IsOperator)
        {
            UpdateOperatorInfo();
        }
        else
        {
            UpdateDeployableInfo();
        }
    }

    private void UpdateOperatorInfo()
    {
        ShowOperatorPanels();

        // 이름
        nameText.text = currentDeployableInfo.operatorData?.entityName ?? string.Empty;

        // 레벨
        levelText.text = $"{currentDeployableInfo.ownedOperator?.currentLevel}";

        // 스킬
        UpdateSkillInfo();

        // 스탯
        UpdateStatInfo();
    }

    private void UpdateDeployableInfo()
    {
        HideOperatorPanels();

        nameText.text = currentDeployableInfo.deployableUnitData?.entityName ?? string.Empty;
    }

    private void UpdateStatInfo()
    {
        // 기존 오퍼레이터가 있었다면 구독 해제
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
            currentOperator.OnStatsChanged -= UpdateOperatorInfo;
        }

        if (currentDeployableUnitState != null && currentDeployableUnitState.IsDeployed)
        {
            // 배치된 경우 실시간 정보를 가져옴
            currentOperator = currentDeployableInfo.deployedOperator;
            if (currentOperator != null)
            {
                UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
                attackText.text = $"공격력: {Mathf.FloorToInt(currentOperator.currentOperatorStats.AttackPower)}";
                defenseText.text = $"방어력: {Mathf.FloorToInt(currentOperator.currentOperatorStats.Defense)}";
                magicResistanceText.text = $"마법저항력: {Mathf.FloorToInt(currentOperator.currentOperatorStats.MagicResistance)}";
                blockCountText.text = $"저지수: {Mathf.FloorToInt(currentOperator.currentOperatorStats.MaxBlockableEnemies)}";

                // 이벤트 구독
                currentOperator.OnHealthChanged += UpdateHealthText;
                currentOperator.OnStatsChanged += UpdateOperatorInfo;
            }
        }
        else
        {
            OperatorStats ownedOperatorStats = currentDeployableInfo.ownedOperator!.CurrentStats;

            float initialHealth = ownedOperatorStats.Health;
            UpdateHealthText(initialHealth, initialHealth);
            attackText.text = $"공격력: {Mathf.FloorToInt(ownedOperatorStats.AttackPower)}";
            defenseText.text = $"방어력: {Mathf.FloorToInt(ownedOperatorStats.Defense)}";
            magicResistanceText.text = $"마법저항력: {Mathf.FloorToInt(ownedOperatorStats.MagicResistance)}";
            blockCountText.text = $"저지수: {Mathf.FloorToInt(ownedOperatorStats.MaxBlockableEnemies)}";
        }
    }

    private void UpdateHealthText(float currentHealth, float maxHealth, float currentShield = 0)
    {
        healthText.text = $"체력 <color=#ff6666>{Mathf.FloorToInt(currentHealth)} / {Mathf.FloorToInt(maxHealth)}</color>";
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
        // operatorData와 ownedOperator가 null이 아닐 때만 진행
        if (currentDeployableInfo.operatorData == null || currentDeployableInfo.ownedOperator == null)
        {
            return;
        }

        int skillIndex = GameManagement.Instance!.UserSquadManager.GetCurrentSkillIndex(currentDeployableInfo.ownedOperator);
        operatorSkill = currentDeployableInfo.ownedOperator.UnlockedSkills[skillIndex];

        if (operatorSkill != null)
        {
            ShowOperatorPanels();
            OperatorIconHelper.SetClassIcon(classIconImage, currentDeployableInfo.operatorData.operatorClass);

            // 0 정예화는 아이콘에 이미지가 없음 : 오브젝트 비활성화 -> 레벨 요소가 왼쪽으로 이동
            if (currentDeployableInfo.ownedOperator.currentPhase == OperatorGrowthSystem.ElitePhase.Elite0)
            {
                promotionIconImage.gameObject.SetActive(false);
            }
            else
            {
                promotionIconImage.gameObject.SetActive(true);
                OperatorIconHelper.SetElitePhaseIcon(promotionIconImage, currentDeployableInfo.ownedOperator.currentPhase);
            }

            skillIconImage.sprite = operatorSkill.skillIcon;
            skillNameText.text = operatorSkill.skillName;
            skillDetailText.text = operatorSkill.description;
        }
    }

    private void OnCancelPanelClicked()
    {
        if (DeployableManager.Instance!.IsDeployableSelecting)
        {
            DeployableManager.Instance!.CancelDeployableSelection();
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

        cancelPanel.onClick.RemoveAllListeners();
        cancelPanel.gameObject.SetActive(false);
    }
}
