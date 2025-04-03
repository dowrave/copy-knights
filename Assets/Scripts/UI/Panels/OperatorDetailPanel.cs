using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Skills.Base;

public class OperatorDetailPanel : MonoBehaviour
{
    [Header("Basic Info")]
    [SerializeField] private Image operatorIconImage = default!;
    [SerializeField] private Image classIconImage = default!;
    [SerializeField] private TextMeshProUGUI operatorNameText = default!;
    [SerializeField] private TextMeshProUGUI levelText = default!;
    [SerializeField] private TextMeshProUGUI maxLevelText = default!;
    [SerializeField] private TextMeshProUGUI phaseText = default!;

    [Header("Attack Range Visualization")]
    [SerializeField] private RectTransform attackRangeContainer = default!;
    [SerializeField] private float centerPositionOffset; // Ÿ�� �ð�ȭ ��ġ�� ���� �߽� �̵�
    [SerializeField] private float tileSize = 25f;

    private UIHelper.AttackRangeHelper attackRangeHelper = default!;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI healthText = default!;
    [SerializeField] private TextMeshProUGUI attackPowerText = default!;
    [SerializeField] private TextMeshProUGUI defenseText = default!;
    [SerializeField] private TextMeshProUGUI magicResistanceText = default!;
    [SerializeField] private TextMeshProUGUI deploymentCostText = default!;
    [SerializeField] private TextMeshProUGUI redeployTimeText = default!;
    [SerializeField] private TextMeshProUGUI blockCountText = default!;
    [SerializeField] private TextMeshProUGUI attackSpeedText = default!;

    [Header("Growth")]
    [SerializeField] private TextMeshProUGUI expText = default!;
    [SerializeField] private Slider expGauge = default!;
    [SerializeField] private Image promotionImage = default!;
    [SerializeField] private Button levelUpButton = default!;
    [SerializeField] private Button promoteButton = default!;

    [Header("Indicator")]
    [SerializeField] private TextMeshProUGUI maxLevelIndicator = default!;
    [SerializeField] private TextMeshProUGUI maxPromotionIndicator = default!;
    [SerializeField] private TextMeshProUGUI canLevelUpIndicator = default!;

    [Header("Skills")]
    [SerializeField] private List<SkillIconBox> skillIconBoxes = new List<SkillIconBox>();
    [SerializeField] private TextMeshProUGUI skillDetailText = default!;
    [SerializeField] private Image skillSelectedIndicator = default!;
    [SerializeField] private Button SetDefaultSkillButton = default!;

    private BaseSkill currentSelectedSkill = default!;
    private OperatorData operatorData = default!;
    private OwnedOperator? currentOperator;

    private void Awake()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        levelUpButton.interactable = true;
        promoteButton.interactable = true;

        levelUpButton.onClick.AddListener(OnLevelUpClicked);
        promoteButton.onClick.AddListener(OnPromoteClicked);


        for (int i = 0; i < skillIconBoxes.Count; i++)
        {
            int index = i;
            skillIconBoxes[i].OnButtonClicked += () => OnSkillButtonClicked(index);
        }
        

        SetDefaultSkillButton.onClick.AddListener(HandleDefaultButtonClicked);
    }


    public void Initialize(OwnedOperator ownedOp)
    {
        if (currentOperator != ownedOp)
        {
            ClearAttackRange();

            currentOperator = ownedOp;
            operatorData = ownedOp.OperatorProgressData;

            // AttackRangeHelper �ʱ�ȭ
            attackRangeHelper = UIHelper.Instance!.CreateAttackRangeHelper(
                attackRangeContainer,
                centerPositionOffset,
                tileSize
            );

            currentSelectedSkill = ownedOp.DefaultSelectedSkill;

            UpdateAllUI();
        }
    }

    // OnEnable�� �ʿ��� ����) ������ / ����ȭ �Ŀ� ���ƿ��� ���� Initialize�� ������� ����
    private void OnEnable()
    {
        // ���� ���۷����Ͱ� �Ҵ�� ��쿡�� UI ������Ʈ ����
        if (currentOperator != null)
        {
            UpdateAllUI();
        }
    }

    private void UpdateAllUI()
    {
        UpdateBasicInfo();
        UpdateStats();
        UpdateGrowthInfo();
        UpdateSkillsUI();
        //UpdateButtonStates();
    }


    private void UpdateBasicInfo()
    {
        // ����� �Ǵ� ���۷����� �̹��� ����
        if (operatorData.Icon != null)
        {
            operatorIconImage.sprite = operatorData.Icon;
            operatorIconImage.enabled = true;
        }
        else
        {
            operatorIconImage.enabled = false; 
        }

        operatorNameText.text = operatorData.entityName;

        // Ŭ���� ������ ����
        OperatorIconHelper.SetClassIcon(classIconImage, operatorData.operatorClass);

        // ���� ���� ����
        if (currentOperator != null)
        {
            attackRangeHelper.ShowBasicRange(currentOperator.CurrentAttackableGridPos);

        }
    }

    private void UpdateStats()
    {
        if (currentOperator != null)
        {
            OperatorStats currentStats = currentOperator.CurrentStats;

            healthText.text = Mathf.Floor(currentStats.Health).ToString();
            attackPowerText.text = Mathf.Floor(currentStats.AttackPower).ToString();
            defenseText.text = Mathf.Floor(currentStats.Defense).ToString();
            magicResistanceText.text = Mathf.Floor(currentStats.MagicResistance).ToString();
            deploymentCostText.text = Mathf.Floor(currentStats.DeploymentCost).ToString();
            redeployTimeText.text = Mathf.Floor(currentStats.RedeployTime).ToString();
            blockCountText.text = Mathf.Floor(currentStats.MaxBlockableEnemies).ToString();
            attackSpeedText.text = Mathf.Floor(currentStats.AttackSpeed).ToString();
        }
    }

    // ���� ����ġ, ���� ����, ����ȭ ���� ������Ʈ
    private void UpdateGrowthInfo()
    {
        UpdateExpInfo();
        UpdatePromotionInfo();
    }


    // ����ġ, ���� ���� ���� ������Ʈ
    private void UpdateExpInfo()
    {
        if (currentOperator != null)
        {
            int currentLevel = currentOperator.currentLevel;
            int maxLevel = OperatorGrowthSystem.GetMaxLevel(currentOperator.currentPhase);
            int currentExp = currentOperator.currentExp;
            float maxExp = OperatorGrowthSystem.GetMaxExpForNextLevel(
                currentOperator.currentPhase,
                currentLevel
            );

            expText.text = $"EXP\n<size=44><color=#FFE61A>{currentExp.ToString()}</color>/{maxExp.ToString()}</size>";
            levelText.text = $"LV\n<size=100><b>{currentLevel.ToString()}</b></size=100>";
            maxLevelText.text = $"/{maxLevel.ToString()}";
            expGauge.value = currentExp / maxExp;

            if (currentLevel < maxLevel)
            {
                maxLevelIndicator.gameObject.SetActive(false);
                canLevelUpIndicator.gameObject.SetActive(true);
            }
            else
            {
                maxLevelIndicator.gameObject.SetActive(true);
                canLevelUpIndicator.gameObject.SetActive(false);
            }
        }
    }


    // ����ȭ ���� ������Ʈ
    private void UpdatePromotionInfo()
    {
        if (currentOperator != null)
        {
            phaseText.text = $"{(int)currentOperator.currentPhase}";
            OperatorIconHelper.SetElitePhaseIcon(promotionImage, currentOperator.currentPhase);

            if (currentOperator.currentPhase == OperatorGrowthSystem.ElitePhase.Elite0)
            {
                maxPromotionIndicator.gameObject.SetActive(false);
            }
            else
            {
                maxPromotionIndicator.gameObject.SetActive(true);
            }
        }

    }

    private void ClearAttackRange()
    {
        if (attackRangeHelper != null)
        {
            attackRangeHelper.ClearTiles();
        }
    }


    private void OnLevelUpClicked()
    {
        if (currentOperator != null)
        {
            if (currentOperator.CanLevelUp)
            {
                GameObject levelUpPanelObject = MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.OperatorLevelUp];
                OperatorLevelUpPanel levelUpPanel = levelUpPanelObject.GetComponent<OperatorLevelUpPanel>();
                MainMenuManager.Instance!.FadeInAndHide(levelUpPanelObject, gameObject);
                levelUpPanel.Initialize(currentOperator);
            }
            else if (currentOperator.currentLevel == OperatorGrowthSystem.GetMaxLevel(currentOperator.currentPhase))
            {
                MainMenuManager.Instance!.ShowNotification("���� ����ȭ������ �ִ� �����Դϴ�.");
            }
        }
    }

    private void OnPromoteClicked()
    {
        // 0����ȭ�� ������ ���� ����
        if (currentOperator != null && 
            currentOperator.currentPhase == OperatorGrowthSystem.ElitePhase.Elite0)
        {
            GameObject promotionPanelObject = MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.OperatorPromotion];
            MainMenuManager.Instance!.FadeInAndHide(promotionPanelObject, gameObject);
            OperatorPromotionPanel promotionPanel = promotionPanelObject.GetComponent<OperatorPromotionPanel>();
            promotionPanel.Initialize(currentOperator);
        }
        else if (currentOperator.currentPhase == OperatorGrowthSystem.ElitePhase.Elite1)
        {
            MainMenuManager.Instance!.ShowNotification($"�ִ� ����ȭ�� �����߽��ϴ�.");
        }
    }

    private void UpdateSkillsUI()
    {
        if (currentOperator != null)
        { 
            var unlockedSkills = currentOperator.UnlockedSkills;

            skillIconBoxes[0].Initialize(unlockedSkills[0], true, true);

            if (unlockedSkills.Count > 1)
            {
                skillIconBoxes[1].Initialize(unlockedSkills[1], true, true);
                skillIconBoxes[1].SetButtonInteractable(true);
            }
            else
            {
                skillIconBoxes[1].ResetSkillIcon();
                skillIconBoxes[1].SetButtonInteractable(false);
            }

            UpdateSkillSelectionUI();
            UpdateSkillDescriptionUI();
        }
    }

    private void OnSkillButtonClicked(int skillIndex)
    {
        if (currentOperator == null) return;
        var skills = currentOperator.UnlockedSkills;

        if (skillIndex < skills.Count)
        {
            currentSelectedSkill = currentOperator.UnlockedSkills[skillIndex];
        }

        UpdateSkillSelectionUI();
        UpdateSkillDescriptionUI();
    }

    // ��ų�� ���õ����� �����ִ� �ε������� ǥ�� ����
    private void UpdateSkillSelectionUI()
    {
        if (currentOperator == null) return;

        // �⺻ ���� ��ų�� ������ �ε������� ����.
        if (currentOperator.DefaultSelectedSkill == null)
        {
            skillSelectedIndicator.gameObject.SetActive(false);
            return;
        }

        Transform targetButtonTransform = null;
        var unlockedSkills = currentOperator.UnlockedSkills;

        // ù ��° ��ų�� �⺻ ���� ��ų�� ������ skillIconBox1�� ��ư�� ������� ��
        if (unlockedSkills.Count > 0 && currentSelectedSkill == unlockedSkills[0])
        {
            targetButtonTransform = skillIconBoxes[0].transform;
        }
        // �� ��° ��ų�� �⺻ ���� ��ų�� ������ skillIconBox2�� ��ư�� ������� ��
        else if (unlockedSkills.Count > 1 && currentSelectedSkill == unlockedSkills[1])
        {
            targetButtonTransform = skillIconBoxes[1].transform;
        }

        if (targetButtonTransform != null)
        {
            // �ε������͸� ���õ� ��ų ��ư�� ù ��° �ڽ����� ���ġ
            skillSelectedIndicator.transform.SetParent(targetButtonTransform, false);
            skillSelectedIndicator.transform.SetSiblingIndex(0);

            // ��ġ �̵�
            skillSelectedIndicator.transform.localPosition = Vector3.zero; // ��ġ�� �ٲ���

            // Ȱ��ȭ
            skillSelectedIndicator.gameObject.SetActive(true);
        }
        else
        {
            skillSelectedIndicator.gameObject.SetActive(false);
        }
    }

    private void UpdateSkillDescriptionUI()
    {
        if (currentOperator == null || currentSelectedSkill == null) return;

        skillDetailText.text = currentSelectedSkill.description;
    }

    private void HandleDefaultButtonClicked()
    {
        currentOperator.SetDefaultSelectedSkills(currentSelectedSkill);
        MainMenuManager.Instance!.ShowNotification($"�⺻ ���� ��ų�� {currentSelectedSkill.skillName}���� ����Ǿ����ϴ�.");
    }
}
