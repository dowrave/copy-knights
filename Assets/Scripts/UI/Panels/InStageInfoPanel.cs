using Skills.Base;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


// ������������ ���Ǵ�, ���۷������� ������ ǥ���ϴ� �г�.
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
    //[SerializeField] private Button SkillTab = default!; // �Ƹ� �̰͸� �� �� ���� �ѵ�

    [Header("Ability Panel")]
    [SerializeField] private GameObject abilityContainer = default!;
    [SerializeField] private Image skillIconImage = default!;
    [SerializeField] private TextMeshProUGUI skillNameText = default!;
    [SerializeField] private TextMeshProUGUI skillDetailText = default!;

    [Header("Cancel Panel")]
    // [SerializeField] private Button cancelPanel = default!;
    // [SerializeField] private GameObject cancelPanelObject = default!;

    // ��ġ ��� ����
    private DeployableInfo currentDeployableInfo = default!;
    private DeployableUnitState? currentDeployableUnitState;

    // ���۷����Ϳ����� ���
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
    
    // ��ġ���� ���� ��� ó��
    public void UpdateUnDeployedInfo(DeployableInfo deployableInfo)
    {
        gameObject.SetActive(true);
        currentDeployableInfo = deployableInfo;
        currentDeployableUnitState = DeployableManager.Instance!.UnitStates[currentDeployableInfo];

        // ��� �г� Ȱ��ȭ
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
    
    // ��ġ�� ��� ó��
    public void UpdateDeployedInfo(DeployableUnitEntity deployedUnit)
    {
        gameObject.SetActive(true);
        currentDeployableInfo = deployedUnit.DeployableInfo;
        currentDeployableUnitState = DeployableManager.Instance!.UnitStates[currentDeployableInfo];

        // ��� �г� ��Ȱ��ȭ
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
        // �̰� null�� �Ǵ� ���� �̹� 1�� ����� ��Ȳ
        // ������ �𸣰����� �� �޼��尡 2�� ����Ǵ� ������ �ְ� �� ���ǹ��� ���ٸ� null ���ܰ� ���
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

        // �̸�
        nameText.text = currentDeployableInfo.operatorData?.entityName ?? string.Empty;

        // ����
        levelText.text = $"{currentDeployableInfo.ownedOperator?.currentLevel}";

        // ��ų
        UpdateSkillInfo();

        // ����
        UpdateStatInfo();
    }

    private void UpdateDeployableInfo()
    {
        HideOperatorPanels();
        nameText.text = currentDeployableInfo.deployableUnitData?.entityName ?? string.Empty;
    }

    private void UpdateStatInfo()
    {
        // ���� ���۷����Ͱ� �־��ٸ� ���� ����
        if (currentOperator != null)
        {
            currentOperator.OnHealthChanged -= UpdateHealthText;
            currentOperator.OnStatsChanged -= UpdateOperatorInfo;
        }

        if (currentDeployableUnitState != null && currentDeployableUnitState.IsDeployed)
        {
            // ��ġ�� ��� �ǽð� ������ ������
            currentOperator = currentDeployableInfo.deployedOperator;
            if (currentOperator != null)
            {
                UpdateHealthText(currentOperator.CurrentHealth, currentOperator.MaxHealth);
                attackText.text = $"���ݷ�: {Mathf.FloorToInt(currentOperator.currentOperatorStats.AttackPower)}";
                defenseText.text = $"����: {Mathf.FloorToInt(currentOperator.currentOperatorStats.Defense)}";
                magicResistanceText.text = $"�������׷�: {Mathf.FloorToInt(currentOperator.currentOperatorStats.MagicResistance)}";
                blockCountText.text = $"������: {Mathf.FloorToInt(currentOperator.currentOperatorStats.MaxBlockableEnemies)}";

                // �̺�Ʈ ����
                currentOperator.OnHealthChanged += UpdateHealthText;
                currentOperator.OnStatsChanged += UpdateOperatorInfo;
            }
        }
        else
        {
            OperatorStats ownedOperatorStats = currentDeployableInfo.ownedOperator!.CurrentStats;

            float initialHealth = ownedOperatorStats.Health;
            UpdateHealthText(initialHealth, initialHealth);
            attackText.text = $"���ݷ�: {Mathf.FloorToInt(ownedOperatorStats.AttackPower)}";
            defenseText.text = $"����: {Mathf.FloorToInt(ownedOperatorStats.Defense)}";
            magicResistanceText.text = $"�������׷�: {Mathf.FloorToInt(ownedOperatorStats.MagicResistance)}";
            blockCountText.text = $"������: {Mathf.FloorToInt(ownedOperatorStats.MaxBlockableEnemies)}";
        }
    }

    private void UpdateHealthText(float currentHealth, float maxHealth, float currentShield = 0)
    {
        healthText.text = $"ü�� <color=#ff6666>{Mathf.FloorToInt(currentHealth)} / {Mathf.FloorToInt(maxHealth)}</color>";
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
        // operatorData�� ownedOperator�� null�� �ƴ� ���� ����
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

            // 0 ����ȭ�� �����ܿ� �̹����� ���� : ������Ʈ ��Ȱ��ȭ -> ���� ��Ұ� �������� �̵�
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
        // �г��� ��Ȱ��ȭ�ų�, ǥ������ ������ ���ų�, ǥ�� ���� ��ġ�� ������ ���ٸ� false
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
        Logger.Log("OnCancelPanelClicked ����");

        if (DeploymentInputHandler.Instance == null) return;

        // ��� �巡�׸� ������ Ÿ�Ͽ� ��ġ�� �������� �������� ����
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
