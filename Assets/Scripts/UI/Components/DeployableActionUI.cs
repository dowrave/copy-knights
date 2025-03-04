using Unity.VisualScripting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NUnit.Framework.Constraints;


public class DeployableActionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private MaskedDiamondOverlay maskedOverlay;
    [SerializeField] private Button skillButton;
    [SerializeField] private Image skillImage;
    [SerializeField] private Button retreatButton;
    [SerializeField] private GameObject inactivePanel;
    [SerializeField] private Image spIndicatorImage;
    [SerializeField] private TextMeshProUGUI spIndicatorText;
    [SerializeField] private Image skillDurationImage;

    [Header("Skill Indicator Image Color")]
    [SerializeField] private Color canActivateColor = new Color(119, 233, 100, 255);
    [SerializeField] private Color normalColor = new Color(119, 233, 100, 255);

    private float darkPanelAlpha = 0f;

    private Camera mainCamera;
    private IDeployable deployable;
    private Operator currentOperator;
    private bool isDurationImageActive = false;

    public void Initialize(IDeployable deployable)
    {
        this.deployable = deployable;

        if (deployable is Operator)
        {
            currentOperator = deployable as Operator;
            UpdateSkillButton();
            currentOperator.OnSPChanged += UpdateInactivePanel;
        }
        else
        {
            skillButton.gameObject.SetActive(false);
        }


        mainCamera = Camera.main;
        SetUpButtons();
        UpdateSkillIcon();

        // UI가 보는 방향 설정
        if (mainCamera != null)
        {
            transform.rotation = Quaternion.Euler(90, 0, 0); // 카메라가 70도, 의도적으로 맵과 평행하게 구현
        }

        maskedOverlay.Initialize(darkPanelAlpha); // 알파 0으로 조정

        gameObject.SetActive(true);

    }
    private void Update()
    {
        if (currentOperator != null)
        {
            UpdateSkillButton();
        }
    }

    private void SetUpButtons()
    {
        // 버튼 위치는 인스펙터에서 설정
        // 버튼 이벤트 설정
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);

        if (deployable is Operator) 
        { 
            skillButton.onClick.AddListener(OnSkillButtonClicked);
        }
    }

    private void UpdateSkillIcon()
    {
        // 스킬 아이콘이 있다면 아이콘을 버튼 이미지로
        if (currentOperator.CurrentSkill.skillIcon != null)
        {
            skillImage.sprite = currentOperator.CurrentSkill.skillIcon;
        }
        else
        {
            Debug.LogError("스킬 아이콘에 이미지가 없음!");
        }
    }


    private void OnSkillButtonClicked()
    {
        if (deployable is Operator op)
        {
            if (op.CurrentSkill.autoActivate == false)
            {
                op.UseSkill();
                UpdateSkillButton();
            }
            Hide();
        }
    }


    private void OnRetreatButtonClicked()
    {
        deployable.Retreat();
        Hide();
    }

    public void Show()
    {
        maskedOverlay.Show();
        gameObject.SetActive(true);
        if (currentOperator != null)
        {
            UpdateSkillButton();
        }
    }

    public void Hide()
    {
        maskedOverlay.Hide();
        gameObject.SetActive(false);
    }

    private void UpdateSkillButton()
    {
        if (!currentOperator || !currentOperator.CurrentSkill) return;

        UpdateInactivePanel(currentOperator.CurrentSP, currentOperator.MaxSP);
        UpdateSPIndicator();
        UpdateSkillDurationImage();
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
                // 스킬을 쓸 수 있을 때
                if (currentOperator.CanUseSkill())
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

    private void UpdateInactivePanel(float currentSP, float maxSP)
    {
        // 오퍼레이터의 스킬이 자동 발동이라면 활성화될 필요 없음
        if (deployable is Operator op && op.CurrentSkill.autoActivate)
        {
            inactivePanel.SetActive(true);
            return;
        }

        bool canUseSkill = currentSP >= maxSP;

        skillButton.interactable = canUseSkill;
        if (inactivePanel != null)
        {
            inactivePanel.SetActive(!canUseSkill);
        }
        else
        {
            Debug.LogWarning("inactivePanel is null");
        }
    }

    private void UpdateSkillDurationImage()
    {
        if (currentOperator.IsSkillOn)
        {
            if (!isDurationImageActive)
            {
                skillDurationImage.gameObject.SetActive(true);
                isDurationImageActive = true;
            }
            skillDurationImage.fillAmount = currentOperator.CurrentSP / currentOperator.MaxSP;
        }
        else
        {
            skillDurationImage.gameObject.SetActive(false);
            isDurationImageActive = false;
        }
    }

    private void OnDestroy()
    {
        if (currentOperator != null)
        {
            currentOperator.OnSPChanged -= UpdateInactivePanel;
        }
    }
}