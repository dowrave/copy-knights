using Skills.Base;
using TMPro;
using Unity.VisualScripting;
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

    // [Header("Tabs")]
    //[SerializeField] private Button SkillTab = default!; // 아마 이것만 쓸 것 같긴 한데

    [Header("Ability Panel")]
    [SerializeField] private GameObject abilityContainer = default!;
    [SerializeField] private Image skillIconImage = default!;
    [SerializeField] private TextMeshProUGUI skillNameText = default!;
    [SerializeField] private TextMeshProUGUI skillDetailText = default!;

    [Header("Cancel Panel")]
    // [SerializeField] private Button cancelPanel = default!;
    // [SerializeField] private GameObject cancelPanelObject = default!;

    // 배치 요소 정보
    private DeployableInfo currentDeployableInfo = default!;
    private DeployableUnitState? currentDeployableUnitState;

    // 오퍼레이터에서만 사용
    private Operator? currentOperator;
    private OperatorSkill operatorSkill = default!;

    public DeployableInfo CurrentDeployableInfo => currentDeployableInfo;

    private void Awake()
    {
        // Button cancelPanel = cancelPanelObject.GetComponent<Button>();
        // if (cancelPanel != null)
        // {
        //     cancelPanel.onClick.AddListener(OnCancelPanelClicked);
        // }
        // cancelPanelObject.SetActive(false);
    }

    private void Start()
    {
        // DeploymentInputHandler.Instance.OnDragStarted += DeactivateCancelPanelObject;
        // DeploymentInputHandler.Instance.OnDragEnded += ActivateCancelPanelObject;
    }

    // private void ActivateCancelPanelObject()
    // {
    //     cancelPanelObject.SetActive(true);
    // }
    
    // private void DeactivateCancelPanelObject()
    // {
    //     cancelPanelObject.SetActive(false);
    // }
    
    // 배치되지 않은 요소 처리
    public void UpdateUnDeployedInfo(DeployableInfo deployableInfo)
    {
        gameObject.SetActive(true);
        currentDeployableInfo = deployableInfo;
        currentDeployableUnitState = DeployableManager.Instance!.UnitStates[currentDeployableInfo];

        // 취소 패널 활성화
        // cancelPanelObject.SetActive(true);
        // cancelPanel.onClick.AddListener(OnCancelPanelClicked);

        if (currentDeployableInfo.ownedOperator != null)
        {
            OwnedOperator op = currentDeployableInfo.ownedOperator;
            CameraManager.Instance!.AdjustForDeployableInfo(true);
            UpdateOperatorInfo();
        }
        else if (currentDeployableInfo.deployableUnitData != null)
        {
            CameraManager.Instance!.AdjustForDeployableInfo(true);
            UpdateDeployableInfo();
        }
    }
    
    // 배치된 요소 처리
    public void UpdateDeployedInfo(DeployableUnitEntity deployedUnit)
    {
        gameObject.SetActive(true);
        currentDeployableInfo = deployedUnit.DeployableInfo;
        currentDeployableUnitState = DeployableManager.Instance!.UnitStates[currentDeployableInfo];

        // 취소 패널 비활성화
        // cancelPanelObject.SetActive(false);

        if (deployedUnit is Operator op)
        {
            op.OnDeathStarted += Hide;
            CameraManager.Instance!.AdjustForDeployableInfo(true, op);
            UpdateOperatorInfo();
        }
        else
        {
            deployedUnit.OnDeathStarted += Hide;
            CameraManager.Instance!.AdjustForDeployableInfo(true, deployedUnit);
            UpdateDeployableInfo();
        }
    }

    public void Hide(UnitEntity unit)
    {
        // 이게 null이 되는 경우는 이미 1번 사라진 상황
        // 왜인지 모르겠지만 이 메서드가 2번 실행되는 현상이 있고 이 조건문이 없다면 null 예외가 뜬다
        if (currentDeployableInfo == null) return;

        if (currentDeployableInfo.deployedOperator != null)
        {
            currentDeployableInfo.deployedOperator.OnDeathStarted -= Hide;
        }
        else if (currentDeployableInfo.deployedDeployable != null)
        {
            currentDeployableInfo.deployedDeployable.OnDeathStarted -= Hide;
        }

        Hide();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        CameraManager.Instance!.AdjustForDeployableInfo(false);
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
            skillNameText.text = operatorSkill.SkillName;
            skillDetailText.text = operatorSkill.description;
        }
    }

    public bool IsCurrentlyDisplaying(DeployableUnitEntity entity)
    {
        // 패널이 비활성화거나, 표시중인 정보가 없거나, 표시 중인 배치된 유닛이 없다면 false
        if (!gameObject.activeSelf ||
            CurrentDeployableInfo == null ||
            CurrentDeployableInfo.deployedDeployable == null ||
            CurrentDeployableInfo.deployedOperator == null)
        {
            return false;
        }

        if (entity is Operator op)
        {
            return CurrentDeployableInfo.deployedOperator == op;
        }

        return CurrentDeployableInfo.deployedDeployable == entity;
    }

    private void OnCancelPanelClicked()
    {
        Logger.Log("OnCancelPanelClicked 동작");

        if (DeploymentInputHandler.Instance == null) return;

        // 방금 드래그를 끝내고 타일에 배치된 시점에는 동작하지 않음
        if (DeploymentInputHandler.Instance.JustEndedDrag) return;

        if (DeploymentInputHandler.Instance!.CurrentState == DeploymentInputHandler.InputState.SelectingBox ||
            DeploymentInputHandler.Instance!.IsSelectingDeployedUnit)
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

        currentDeployableInfo = null;

        // if (cancelPanelObject != null)
        // {
        //     cancelPanelObject.SetActive(false);
        // }
        // cancelPanel.onClick.RemoveAllListeners();
        // cancelPanel.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // DeploymentInputHandler.Instance.OnDragStarted -= DeactivateCancelPanelObject;
        // DeploymentInputHandler.Instance.OnDragEnded -= ActivateCancelPanelObject;
    }
}
