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

    [Header("Manual Stop UI")]
    [SerializeField] private GameObject manualStopUI = default!; // ���� ���ᰡ ������ ��ų���� ��Ÿ��

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
        // ������ ����� ���� �������� ����
        if (this.deployable == deployable) return;

        // ���� �ʱ�ȭ
        ResetState();

        this.deployable = deployable;

        if (deployable is Operator)
        {
            currentOperator = deployable as Operator;
            if (currentOperator != null)
            {
                // ��ų ������ ���� ����
                skillButton.gameObject.SetActive(true);

                UpdateSkillButton();
                isSPImageActive = currentOperator.CanUseSkill();


                InitializeSkillIcon();  // ��ų ������ �κп� �̹��� �Ҵ�
                InitializeColors();

                // SP ��ȭ�� ���� �̺�Ʈ ���
                currentOperator.OnSPChanged += HandleSPChange;
            }
        }
        else
        {
            currentOperator = null;
            skillButton.gameObject.SetActive(false);
        }

        // ����ĳ��Ʈ ������ ���� �̺�Ʈ ī�޶� �Ҵ�
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;

        // ��ư�鿡 �̺�Ʈ ������ ����
        SetUpButtons();

        // �ʰ� ������ �������� ����
        transform.rotation = Quaternion.Euler(90, 0, 0);

        maskedOverlay.Initialize(darkPanelAlpha); // ���� 0���� ����
        manualStopUI.SetActive(false); // ���� ���� UI�� �⺻������ ��Ȱ��ȭ
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
        Logger.Log("DeployableActionUI - �� ��ư�� OnClick ������ ���");

        if (deployable is Operator)
        {
            skillButton.onClick.AddListener(OnSkillButtonClicked);
            Logger.Log("DeployableActionUI - �� ��ư�� OnClick ������ ���");

        }
    }

    private void OnSkillButtonClicked()
    {
        Logger.Log("[DeployableActionUI]��ų ��ư Ŭ�� ����");
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

        // ���� ���ᰡ ������ ��ų�� ���� ���� ��
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
        Logger.Log("[DeployableActionUI]�� ��ư Ŭ�� ����");
        deployable.Retreat();
        ClickDetectionSystem.Instance!.OnButtonClicked();
        Hide();
    }

    public void Hide()
    {
        DeployableManager.Instance!.CancelCurrentAction(); // ��������� �� ������Ʈ�� �ı���
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
                AttackCounterBuff attackCounterbuff = currentOperator.GetBuff<AttackCounterBuff>();
                if (attackCounterbuff != null)
                {
                    spIndicatorImage.color = normalColor;
                    spIndicatorText.text = $"{attackCounterbuff.CurrentAttacks} / {attackCounterbuff.MaxAttacks}";
                }
                // ��ų�� �� �� ���� ��
                else if (currentOperator.CanUseSkill())
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