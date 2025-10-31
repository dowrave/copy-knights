using Unity.VisualScripting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NUnit.Framework.Constraints;
using System;
using Skills.Base;


public class DeployableActionUI : MonoBehaviour
{
    [Header("Skill Button")]
    [SerializeField] private Button skillButton = default!;
    [SerializeField] private Image skillImage = default!;
    [SerializeField] private GameObject inactiveImage = default!;
    [SerializeField] private Image skillSPImage = default!; // 스킬 아이콘 위에 나타남

    [Header("spIndicator")]
    [SerializeField] private Image spIndicatorImage = default!; // 스킬 버튼 하단의 직사각형인 SP 표시 / Activate 이미지
    [SerializeField] private TextMeshProUGUI spIndicatorText = default!;

    [Header("Retreat Button")]
    [SerializeField] private Button retreatButton = default!;

    [Header("Skill Indicator Image Color")]
    [SerializeField] private Color normalColor = new Color(119, 233, 100, 255);
    private Color canActivateColor; // 초록

    [Header("Diamond Mask")]
    [SerializeField] private MaskedDiamondOverlay maskedOverlay = default!;

    [Header("Manual Stop UI")]
    [SerializeField] private GameObject manualStopUI = default!; // 수동 종료가 가능한 스킬에만 나타남

    private Canvas canvas = default!;

    private float darkPanelAlpha = 0f;
    
    private Operator? currentOperator;
    private bool isSPImageActive;

    private float SPImageColorAlpha = 0.3f;
    private Color skillOnColorWithAlpha;
    private Color skillOffColorWithAlpha;

    private DeployableUnitEntity deployable = default!;
    public DeployableUnitEntity Deployable => deployable;

    public void Initialize(DeployableUnitEntity deployable)
    {
        // 동일한 요소인 경우는 동작하지 않음
        if (this.deployable == deployable) return;

        // 상태 초기화
        ResetState();

        this.deployable = deployable;

        if (deployable is Operator)
        {
            currentOperator = deployable as Operator;
            if (currentOperator != null)
            {
                // 스킬 아이콘 관련 설정
                skillButton.gameObject.SetActive(true);

                UpdateSkillButton();
                isSPImageActive = currentOperator.CanUseSkill();


                InitializeSkillIcon();  // 스킬 아이콘 부분에 이미지 할당
                InitializeColors();

                // SP 변화에 따른 이벤트 등록
                currentOperator.OnSPChanged += HandleSPChange;
            }
        }
        else
        {
            currentOperator = null;
            skillButton.gameObject.SetActive(false);
        }

        // 레이캐스트 감지를 위한 이벤트 카메라 할당
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;

        // 버튼들에 이벤트 리스너 설정
        SetUpButtons();

        // 맵과 나란한 방향으로 설정
        transform.rotation = Quaternion.Euler(90, 0, 0);

        maskedOverlay.Initialize(darkPanelAlpha); // 알파 0으로 조정
        manualStopUI.SetActive(false); // 수동 종료 UI는 기본적으로 비활성화
    }

    private void InitializeSkillIcon()
    {
        // 스킬 아이콘이 있다면 아이콘을 버튼 이미지로
        if (currentOperator != null &&
            currentOperator.CurrentSkill.skillIcon != null)
        {
            skillImage.sprite = currentOperator.CurrentSkill.skillIcon;
        }
    }

    // ResourceManager에서 가져온 색상들을 다듬거나 할당함
    private void InitializeColors()
    {
        if (GameManagement.Instance! == null) throw new InvalidOperationException("GameManagement가 초기화되지 않음");

        Color tempOnColor = GameManagement.Instance!.ResourceManager.OnSkillColor;
        Color tempOffColor = GameManagement.Instance!.ResourceManager.OffSkillColor;

        skillOnColorWithAlpha = new Color(tempOnColor.r, tempOnColor.g, tempOnColor.b, SPImageColorAlpha);
        skillOffColorWithAlpha = new Color(tempOffColor.r, tempOffColor.g, tempOffColor.b, SPImageColorAlpha);

        canActivateColor = GameManagement.Instance!.ResourceManager.OffSkillColor;
    }


    private void SetUpButtons()
    {
        // 버튼 위치는 인스펙터에서 설정
        // 버튼 이벤트 설정
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);
        Logger.Log("DeployableActionUI - 퇴각 버튼에 OnClick 리스너 등록");

        if (deployable is Operator)
        {
            skillButton.onClick.AddListener(OnSkillButtonClicked);
            Logger.Log("DeployableActionUI - 퇴각 버튼에 OnClick 리스너 등록");

        }
    }

    private void OnSkillButtonClicked()
    {
        Logger.Log("[DeployableActionUI]스킬 버튼 클릭 감지");
        if (deployable is Operator op)
        {
            if (op.CurrentSkill is AmmoBasedActiveSkill ammoSkill && op.IsSkillOn)
            {
                ammoSkill.TerminateSkill(op);
            } 
            else if (op.CurrentSkill.autoActivate == false)
            {
                op.UseSkill();
                UpdateSkillButton();
            }

            ClickDetectionSystem.Instance!.OnButtonClicked();
            Hide();
        }
    }

    private void Update()
    {
        if (currentOperator != null)
        {
            UpdateSkillButton();
        }
    }

    private void UpdateSkillButton()
    {
        if (!currentOperator || !currentOperator.CurrentSkill) return;

        UpdateInactivePanel();
        UpdateSPIndicator();
        UpdateSkillSPImage();
        UpdateManualStopUI();
    }

    private void UpdateManualStopUI()
    {
        if (currentOperator == null || currentOperator.CurrentSkill == null) return;

        // 수동 종료가 가능한 스킬이 켜져 있을 때
        if (currentOperator.CurrentSkill is AmmoBasedActiveSkill && currentOperator.IsSkillOn)
        {
            manualStopUI.SetActive(true);
        }
        else
        {
            manualStopUI.SetActive(false);
        }
    }

    private void OnRetreatButtonClicked()
    {
        Logger.Log("[DeployableActionUI]퇴각 버튼 클릭 감지");
        deployable.Retreat();
        ClickDetectionSystem.Instance!.OnButtonClicked();
        Hide();
    }

    public void Hide()
    {
        DeployableManager.Instance!.CancelCurrentAction(); // 결과적으로 이 오브젝트가 파괴됨
    }

    private void UpdateSPIndicator()
    {
        if (currentOperator != null)
        {
            string currentSP = Mathf.Floor(currentOperator.CurrentSP).ToString();

            // 자동 발동 스킬 처리
            if (currentOperator.CurrentSkill.autoActivate)
            {
                spIndicatorImage.color = normalColor;
                spIndicatorText.text = $"{currentSP} / {currentOperator.MaxSP}";
            }
            // 수동 발동 스킬 처리
            else
            {
                AttackCounterBuff attackCounterbuff = currentOperator.GetBuff<AttackCounterBuff>();
                if (attackCounterbuff != null)
                {
                    spIndicatorImage.color = normalColor;
                    spIndicatorText.text = $"{attackCounterbuff.CurrentAttacks} / {attackCounterbuff.MaxAttacks}";
                }
                // 스킬을 쓸 수 있을 때
                else if (currentOperator.CanUseSkill())
                {
                    spIndicatorImage.color = canActivateColor;
                    spIndicatorText.text = "Activate";
                }
                // 스킬이 켜졌을 때
                else if (currentOperator.IsSkillOn)
                {
                    spIndicatorImage.color = normalColor;
                    spIndicatorText.text = $"0 / {currentOperator.MaxSP}";
                }
                // 나머지 : 스킬이 꺼짐 + 사용 불가능(SP를 채우는 상황)
                else
                {
                    spIndicatorImage.color = normalColor;
                    spIndicatorText.text = $"{currentSP} / {currentOperator.MaxSP}";
                }
            }
        }
    }


    // 스킬 사용 버튼의 활성 가능 여부를 표시하는 패널의 동작
    private void UpdateInactivePanel()
    {
        if (currentOperator == null) return;

        // 버튼을 누를 수 있는 상황에는 비활성화
        if (!currentOperator.CurrentSkill.autoActivate &&
            currentOperator.CanUseSkill())
        {
            inactiveImage.SetActive(false);
        }
        else
        {
            inactiveImage.SetActive(true);
        }
    }

    // SP 변화 이벤트를 받을 경우
    private void HandleSPChange(float currentSP, float maxSP)
    {
        UpdateInactivePanel();
    }

    private void UpdateSkillSPImage()
    {
        if (currentOperator == null) return;

        // 지속시간이 있는 액티브 스킬이 켜진 상태
        if (currentOperator.IsSkillOn)
        {
            SetSPImageActive(true);
            skillSPImage.color = skillOnColorWithAlpha;
            skillSPImage.fillAmount = currentOperator.CurrentSP / currentOperator.MaxSP;
        }
        // 스킬이 켜지지 않았고, 스킬 사용 가능한 상태
        else if (currentOperator.CanUseSkill())
        {
            SetSPImageActive(false);
        }
        // 스킬이 켜지지 않았고, SP가 차오르는 상태
        else
        {
            SetSPImageActive(true);
            skillSPImage.color = skillOffColorWithAlpha;
            skillSPImage.fillAmount = currentOperator.CurrentSP / currentOperator.MaxSP;
        }
    }

    // 스킬 버튼 위의 SP를 나타내는 게이지 이미지를 표시할지 여부를 결정합니다.
    private void SetSPImageActive(bool activate)
    {
        if (isSPImageActive == !activate)
        {
            skillSPImage.gameObject.SetActive(activate);
            isSPImageActive = activate;
        }
    }

    private void ResetState()
    {
        retreatButton.onClick.RemoveAllListeners();
        if (currentOperator != null)
        {
            skillButton.onClick.RemoveAllListeners();
            currentOperator.OnSPChanged -= HandleSPChange;
            currentOperator = null;
        }

        deployable = null;
    }

    private void OnDisable()
    {
        ResetState();
    }

    private void OnDestroy()
    {

    }
}