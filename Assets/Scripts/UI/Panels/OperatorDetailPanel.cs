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
    [SerializeField] private float centerPositionOffset; // 타일 시각화 위치를 위한 중심 이동
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
    [SerializeField] private Button setDefaultSkillButton = default!;
    [SerializeField] private Image defaultSkillIndicator = default!;

    [Header("Set Needed Rect")]
    [SerializeField] private RectTransform? skillSelectedIndicatorRect = default!;
    [SerializeField] private RectTransform? defaultSkillIndicatorRect = default!;


    private OperatorSkill currentSelectedSkill = default!; // UI상에서 선택되고 있는 스킬
    private int currentSelectedSkillIndex;
    private OperatorData operatorData = default!;
    private OwnedOperator? currentOperator;


    private void Awake()
    {
        SetupButtons();
        SetRectComponents();
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
        

        setDefaultSkillButton.onClick.AddListener(HandleDefaultButtonClicked);
    }

    private void SetRectComponents()
    {
        if (skillSelectedIndicatorRect == null)
        {
            skillSelectedIndicatorRect = skillSelectedIndicator.GetComponent<RectTransform>();
        }

        if (defaultSkillIndicatorRect == null)
        {
            defaultSkillIndicatorRect = skillSelectedIndicator.GetComponent<RectTransform>();
        }
    }

    // Initialize가 실행되는 경우, Awake보다 먼저 실행될 수 있음에 유의
    public void Initialize(OwnedOperator ownedOp)
    {
        if (currentOperator != ownedOp)
        {
            SetRectComponents();
            ClearAttackRange();

            currentOperator = ownedOp;
            operatorData = ownedOp.OperatorData;

            // AttackRangeHelper 초기화
            attackRangeHelper = UIHelper.Instance!.CreateAttackRangeHelper(
                attackRangeContainer,
                centerPositionOffset,
                tileSize
            );

            currentSelectedSkill = ownedOp.DefaultSelectedSkill;

            UpdateAllUI();
        }
    }

    // OnEnable이 필요한 이유) 레벨업 / 정예화 후에 돌아오는 경우는 Initialize로 실행되지 않음
    private void OnEnable()
    {
        // 현재 오퍼레이터가 할당된 경우에만 UI 업데이트 실행
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
        InitializeSkillsUI();
        UpdateSkillsUI();
    }

    private void UpdateBasicInfo()
    {
        // 배경이 되는 오퍼레이터 이미지 설정
        if (operatorData.Icon != null)
        {
            operatorIconImage.sprite = operatorData.Icon;
            operatorIconImage.enabled = true;
        }
        else
        {
            operatorIconImage.enabled = false; 
        }

        operatorNameText.text = GameManagement.Instance!.LocalizationManager.GetText(operatorData.EntityNameLocalizationKey);

        // 클래스 아이콘 설정
        OperatorIconHelper.SetClassIcon(classIconImage, operatorData.OperatorClass);

        // 공격 범위 설정
        if (currentOperator != null)
        {
            attackRangeHelper.ShowBasicRange(currentOperator.BaseActionableOffsets);

        }
    }

    private void UpdateStats()
    {
        if (currentOperator != null)
        {
            OperatorStats currentStats = currentOperator.CurrentStats;

            healthText.text = Mathf.FloorToInt(currentStats.Health).ToString();
            attackPowerText.text = Mathf.FloorToInt(currentStats.AttackPower).ToString();
            defenseText.text = Mathf.FloorToInt(currentStats.Defense).ToString();
            magicResistanceText.text = Mathf.FloorToInt(currentStats.MagicResistance).ToString();
            deploymentCostText.text = Mathf.FloorToInt(currentStats.DeploymentCost).ToString();
            redeployTimeText.text = Mathf.FloorToInt(currentStats.RedeployTime).ToString();
            blockCountText.text = Mathf.FloorToInt(currentStats.MaxBlockableEnemies).ToString();
            attackSpeedText.text = currentStats.BaseAttackCooldown.ToString("F2");
        }
    }

    // 현재 경험치, 현재 레벨, 정예화 상태 업데이트
    private void UpdateGrowthInfo()
    {
        UpdateExpInfo();
        UpdatePromotionInfo();
    }


    // 경험치, 레벨 관련 정보 업데이트
    private void UpdateExpInfo()
    {
        if (currentOperator != null)
        {
            int currentLevel = currentOperator.CurrentLevel;
            int maxLevel = OperatorGrowthSystem.GetMaxLevel(currentOperator.CurrentPhase);
            int currentExp = currentOperator.CurrentExp;
            float maxExp = OperatorGrowthSystem.GetMaxExpForNextLevel(
                currentOperator.CurrentPhase,
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


    // 정예화 정보 업데이트
    private void UpdatePromotionInfo()
    {
        if (currentOperator != null)
        {
            phaseText.text = $"{(int)currentOperator.CurrentPhase}";
            OperatorIconHelper.SetElitePhaseIcon(promotionImage, currentOperator.CurrentPhase);

            if (currentOperator.CurrentPhase == OperatorElitePhase.Elite0)
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
                MainMenuManager.Instance!.ChangePanel(levelUpPanelObject, gameObject);
                levelUpPanel.Initialize(currentOperator);
            }
            else if (currentOperator.CurrentLevel == OperatorGrowthSystem.GetMaxLevel(currentOperator.CurrentPhase))
            {
                NotificationToastManager.Instance!.ShowNotification("현재 정예화에서의 최대 레벨입니다.");
            }
        }
    }

    private void OnPromoteClicked()
    {
        // 0정예화일 때에만 진입 가능
        if (currentOperator != null && 
            currentOperator.CurrentPhase == OperatorElitePhase.Elite0)
        {
            GameObject promotionPanelObject = MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.OperatorPromotion];
            MainMenuManager.Instance!.ChangePanel(promotionPanelObject, gameObject);
            OperatorPromotionPanel promotionPanel = promotionPanelObject.GetComponent<OperatorPromotionPanel>();
            promotionPanel.Initialize(currentOperator);
        }
        else if (currentOperator.CurrentPhase == OperatorElitePhase.Elite1)
        {
            NotificationToastManager.Instance!.ShowNotification($"최대 정예화에 도달했습니다.");
        }
    }

    private void InitializeSkillsUI()
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
        }

    }

    private void UpdateSkillsUI()
    {
        UpdateSkillSelectionUI();
        UpdateSkillDescriptionUI();
        UpdateDefaultSkillIndicator();
        UpdateSetDefaultSkillButton();
    }

    private void OnSkillButtonClicked(int skillIndex)
    {
        if (currentOperator == null) return;
        var skills = currentOperator.UnlockedSkills;

        if (skillIndex < skills.Count)
        {
            currentSelectedSkillIndex = skillIndex;
            currentSelectedSkill = skills[skillIndex];
        }

        UpdateSkillsUI();
    }

    // 스킬이 선택됐음을 보여주는 인디케이터 표시 로직
    private void UpdateSkillSelectionUI()
    {
        if (currentOperator == null) return;

        // 기본 선택 스킬이 없으면 인디케이터 숨김.
        if (currentOperator.DefaultSelectedSkill == null)
        {
            skillSelectedIndicator.gameObject.SetActive(false);
            return;
        }

        Transform targetButtonTransform = null;
        var unlockedSkills = currentOperator.UnlockedSkills;

        // 첫 번째 스킬이 기본 선택 스킬과 같으면 skillIconBox1의 버튼을 대상으로 함
        if (unlockedSkills.Count > 0 && currentSelectedSkill == unlockedSkills[0])
        {
            targetButtonTransform = skillIconBoxes[0].transform;
        }
        // 두 번째 스킬이 기본 선택 스킬과 같으면 skillIconBox2의 버튼을 대상으로 함
        else if (unlockedSkills.Count > 1 && currentSelectedSkill == unlockedSkills[1])
        {
            targetButtonTransform = skillIconBoxes[1].transform;
        }

        if (targetButtonTransform != null)
        {
            // 인디케이터를 선택된 스킬 버튼의 첫 번째 자식으로 재배치
            skillSelectedIndicator.transform.SetParent(targetButtonTransform, false);
            skillSelectedIndicator.transform.SetSiblingIndex(0);

            // 위치 이동
            skillSelectedIndicatorRect.anchoredPosition = Vector2.zero;

            // 활성화
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
        currentOperator.SetDefaultSelectedSkill(currentSelectedSkillIndex);
        NotificationToastManager.Instance!.ShowNotification($"기본 설정 스킬이 {currentSelectedSkill.SkillName}으로 변경되었습니다.");
        UpdateSkillsUI();
    }

    // currentDefaultSkill에 해당하는 인디케이터 업데이트
    private void UpdateDefaultSkillIndicator()
    {
        if (currentOperator == null || currentOperator.DefaultSelectedSkill == null)
        {
            defaultSkillIndicator.gameObject.SetActive(false);
            return;
        }

        // 이 인디케이터가 들어간 skillIconBox를 찾음
        Transform targetTransform = null;
        var unlockedSkills = currentOperator.UnlockedSkills;
        if (unlockedSkills.Count > 0 && currentOperator.DefaultSelectedSkill == unlockedSkills[0])
        {
            targetTransform = skillIconBoxes[0].transform;
        }
        else if (unlockedSkills.Count > 1 && currentOperator.DefaultSelectedSkill == unlockedSkills[1])
        {
            targetTransform = skillIconBoxes[1].transform;
        }

        // 인디케이터의 위치를 옮김
        if (targetTransform != null)
        {
            defaultSkillIndicator.transform.SetParent(targetTransform, false);
            defaultSkillIndicatorRect.anchoredPosition = Vector2.zero;

            defaultSkillIndicator.gameObject.SetActive(true);
        }
        else
        {
            defaultSkillIndicator.gameObject.SetActive(false);
        }
    }

    private void UpdateSetDefaultSkillButton()
    {
        if (currentSelectedSkill == currentOperator.DefaultSelectedSkill)
        {
            setDefaultSkillButton.interactable = false;
        }
        else
        {
            setDefaultSkillButton.interactable = true;
        }

    }
}
