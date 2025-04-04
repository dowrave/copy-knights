using Unity.VisualScripting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NUnit.Framework.Constraints;
using System;


public class DeployableActionUI : MonoBehaviour
{
    [Header("Skill Button")]
    [SerializeField] private Button skillButton = default!;
    [SerializeField] private Image skillImage = default!;
    [SerializeField] private GameObject inactiveImage = default!;
    [SerializeField] private Image skillSPImage = default!; // ��ų ������ ���� ��Ÿ��

    [Header("spIndicator")]
    [SerializeField] private Image spIndicatorImage = default!; // ��ų ��ư �ϴ��� ���簢���� SP ǥ�� / Activate �̹���
    [SerializeField] private TextMeshProUGUI spIndicatorText = default!;

    [Header("Retreat Button")]
    [SerializeField] private Button retreatButton = default!;

    [Header("Skill Indicator Image Color")]
    [SerializeField] private Color normalColor = new Color(119, 233, 100, 255);
    private Color canActivateColor; // �ʷ�

    [Header("Diamond Mask")]
    [SerializeField] private MaskedDiamondOverlay maskedOverlay = default!;

    private Canvas canvas = default!;

    private float darkPanelAlpha = 0f;
    private IDeployable deployable = default!;
    private Operator? currentOperator;
    private bool isSPImageActive;

    private float SPImageColorAlpha = 0.3f;
    private Color skillOnColorWithAlpha;
    private Color skillOffColorWithAlpha;

    public void Initialize(IDeployable deployable)
    {
        this.deployable = deployable;

        if (deployable is Operator)
        {
            currentOperator = deployable as Operator;
            if (currentOperator != null)
            {
                UpdateSkillButton();
                currentOperator.OnSPChanged += HandleSPChange;
                isSPImageActive = currentOperator.CanUseSkill();
            }
        }
        else
        {
            skillButton.gameObject.SetActive(false);
        }

        // ����ĳ��Ʈ ������ ���� �̺�Ʈ ī�޶� �Ҵ�
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;

        SetUpButtons();
        InitializeSkillIcon();
        InitializeColors();

        transform.rotation = Quaternion.Euler(90, 0, 0); // ī�޶� 70��, �ǵ������� �ʰ� �����ϰ� ����

        maskedOverlay.Initialize(darkPanelAlpha); // ���� 0���� ����

        gameObject.SetActive(true);
    }

    private void InitializeSkillIcon()
    {
        // ��ų �������� �ִٸ� �������� ��ư �̹�����
        if (currentOperator != null && 
            currentOperator.CurrentSkill.skillIcon != null)
        {
            skillImage.sprite = currentOperator.CurrentSkill.skillIcon;
        }
    }

    // ResourceManager���� ������ ������� �ٵ�ų� �Ҵ���
    private void InitializeColors()
    {
        if (GameManagement.Instance! == null) throw new InvalidOperationException("GameManagement�� �ʱ�ȭ���� ����");

        Color tempOnColor = GameManagement.Instance!.ResourceManager.OnSkillColor;
        Color tempOffColor = GameManagement.Instance!.ResourceManager.OffSkillColor;

        skillOnColorWithAlpha = new Color(tempOnColor.r, tempOnColor.g, tempOnColor.b, SPImageColorAlpha);
        skillOffColorWithAlpha = new Color(tempOffColor.r, tempOffColor.g, tempOffColor.b, SPImageColorAlpha);

        canActivateColor = GameManagement.Instance!.ResourceManager.OffSkillColor;
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

    private void OnSkillButtonClicked()
    {
        if (deployable is Operator op)
        {
            if (op.CurrentSkill.autoActivate == false)
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
    }

    private void OnRetreatButtonClicked()
    {
        deployable.Retreat();
        ClickDetectionSystem.Instance!.OnButtonClicked();
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
        //maskedOverlay.Hide();
        DeployableManager.Instance!.CancelCurrentAction();
        //gameObject.SetActive(false);
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


    // ��ų ��� ��ư�� Ȱ�� ���� ���θ� ǥ���ϴ� �г��� ����
    private void UpdateInactivePanel()
    {
        if (currentOperator == null) return;

        // ��ư�� ���� �� �ִ� ��Ȳ���� ��Ȱ��ȭ
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

    // SP ��ȭ �̺�Ʈ�� ���� ���
    private void HandleSPChange(float currentSP, float maxSP)
    {
        UpdateInactivePanel();
    }

    private void UpdateSkillSPImage()
    {
        if (currentOperator == null) return;

        // ���ӽð��� �ִ� ��Ƽ�� ��ų�� ���� ����
        if (currentOperator.IsSkillOn)
        {
            SetSPImageActive(true);
            skillSPImage.color = skillOnColorWithAlpha;
            skillSPImage.fillAmount = currentOperator.CurrentSP / currentOperator.MaxSP;
        }
        // ��ų�� ������ �ʾҰ�, ��ų ��� ������ ����
        else if (currentOperator.CanUseSkill())
        {
            SetSPImageActive(false);
        }
        // ��ų�� ������ �ʾҰ�, SP�� �������� ����
        else
        {
            SetSPImageActive(true);
            skillSPImage.color = skillOffColorWithAlpha;
            skillSPImage.fillAmount = currentOperator.CurrentSP / currentOperator.MaxSP;
        }
    }

    // ��ų ��ư ���� SP�� ��Ÿ���� ������ �̹����� ǥ������ ���θ� �����մϴ�.
    private void SetSPImageActive(bool activate)
    {
        if (isSPImageActive == !activate)
        {
            skillSPImage.gameObject.SetActive(activate);
            isSPImageActive = activate;
        }
    }

    private void OnDestroy()
    {
        if (currentOperator != null)
        {
            currentOperator.OnSPChanged -= HandleSPChange;
        }
    }
}