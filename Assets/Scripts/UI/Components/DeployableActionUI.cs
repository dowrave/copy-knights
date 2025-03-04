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

        // UI�� ���� ���� ����
        if (mainCamera != null)
        {
            transform.rotation = Quaternion.Euler(90, 0, 0); // ī�޶� 70��, �ǵ������� �ʰ� �����ϰ� ����
        }

        maskedOverlay.Initialize(darkPanelAlpha); // ���� 0���� ����

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
        // ��ư ��ġ�� �ν����Ϳ��� ����
        // ��ư �̺�Ʈ ����
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);

        if (deployable is Operator) 
        { 
            skillButton.onClick.AddListener(OnSkillButtonClicked);
        }
    }

    private void UpdateSkillIcon()
    {
        // ��ų �������� �ִٸ� �������� ��ư �̹�����
        if (currentOperator.CurrentSkill.skillIcon != null)
        {
            skillImage.sprite = currentOperator.CurrentSkill.skillIcon;
        }
        else
        {
            Debug.LogError("��ų �����ܿ� �̹����� ����!");
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

            // �ڵ� �ߵ� ��ų ó��
            if (currentOperator.CurrentSkill.autoActivate)
            {
                spIndicatorImage.color = normalColor;
                spIndicatorText.text = $"{currentSP} / {currentOperator.MaxSP}";
            }
            // ���� �ߵ� ��ų ó��
            else
            {
                // ��ų�� �� �� ���� ��
                if (currentOperator.CanUseSkill())
                {
                    spIndicatorImage.color = canActivateColor;
                    spIndicatorText.text = "Activate";
                }
                // ��ų�� ������ ��
                else if (currentOperator.IsSkillOn)
                {
                    spIndicatorImage.color = normalColor;
                    spIndicatorText.text = $"0 / {currentOperator.MaxSP}";
                }
                // ������ : ��ų�� ���� + ��� �Ұ���(SP�� ä��� ��Ȳ)
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
        // ���۷������� ��ų�� �ڵ� �ߵ��̶�� Ȱ��ȭ�� �ʿ� ����
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