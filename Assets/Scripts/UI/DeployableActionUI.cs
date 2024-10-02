using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class DeployableActionUI : MonoBehaviour
{
    [SerializeField] private MaskedDiamondOverlay maskedOverlay;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button retreatButton;
    [SerializeField] private GameObject skillButtonInactivePanel;
    private Image skillIconImage; // ��ų �������� ǥ���ϴ� �̹��� ������Ʈ

    private float darkPanelAlpha = 0f;

    private Camera mainCamera;
    private IDeployable deployable;
    private Operator currentOperator;

    public void Initialize(IDeployable deployable)
    {
        this.deployable = deployable;

        mainCamera = Camera.main;
        SetUpButtons();
        UpdateSkillIcon();

        // UI�� ���� ���� ����
        if (mainCamera != null)
        {
            //transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
            transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        maskedOverlay.Initialize(darkPanelAlpha); // ���� 0���� ����

        gameObject.SetActive(true);

        if (IsOperator())
        {
            currentOperator = deployable as Operator;
            UpdateSkillButton();
            currentOperator.OnSPChanged += UpdateSkillButtonState;
            Debug.Log("SP ��ȭ �̺�Ʈ UI�� ��� �Ϸ�");
        }
        else
        {
            skillButton.gameObject.SetActive(false);
        }
    }

    private void SetUpButtons()
    {
        // ��ư ��ġ�� �ν����Ϳ��� ����
        // ��ư �̺�Ʈ ����
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);

        if (IsOperator()) 
        { 
            skillButton.onClick.AddListener(OnSkillButtonClicked);
        }
    }

    private void UpdateSkillIcon()
    {
        // ��ų ������ ������Ʈ ����
    }


    private void OnSkillButtonClicked()
    {
        if (deployable is Operator op)
        {
            Debug.Log("��ų ��ư Ŭ����");
            op.UseSkill();
            UpdateSkillButton();
            Hide();
        }
    }
    private void OnRetreatButtonClicked()
    {
        Debug.Log("�� ��ư Ŭ����");
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

    private bool IsOperator()
    {
        return deployable is Operator;
    }

    private void UpdateSkillButton()
    {
        if (!currentOperator || !currentOperator.ActiveSkill) return;
        
        // ��ų �������� �ִٸ� �������� ��ư �̹����� ����
        if (currentOperator.ActiveSkill.SkillIcon != null)
        {
            skillIconImage.sprite = currentOperator.ActiveSkill.SkillIcon;
            skillIconImage.gameObject.SetActive(true);
            skillButton.GetComponentInChildren<Text>().gameObject.SetActive(false);
        }

        UpdateSkillButtonState(currentOperator.CurrentSP, currentOperator.MaxSP);
    }

    private void UpdateSkillButtonState(float currentSP, float maxSP)
    {
        bool canUseSkill = currentSP >= maxSP;

        skillButton.interactable = canUseSkill;
        if (skillButtonInactivePanel != null)
        {
            skillButtonInactivePanel.SetActive(!canUseSkill);
        }
        else
        {
            Debug.LogWarning("skillButtonInactivePanel is null");
        }
    }

    private void OnDestroy()
    {
        if (currentOperator != null)
        {
            currentOperator.OnSPChanged -= UpdateSkillButtonState;
        }
    }
}