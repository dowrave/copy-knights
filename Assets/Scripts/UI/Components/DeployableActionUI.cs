using Unity.VisualScripting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class DeployableActionUI : MonoBehaviour
{
    [SerializeField] private MaskedDiamondOverlay maskedOverlay;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button retreatButton;
    [SerializeField] private GameObject skillButtonInactivePanel;
    private Image skillIconImage; // 스킬 아이콘을 표시하는 이미지 컴포넌트

    private float darkPanelAlpha = 0f;

    private Camera mainCamera;
    private IDeployable deployable;
    private Operator currentOperator;

    public void Initialize(IDeployable deployable)
    {
        this.deployable = deployable;

        if (deployable is Operator)
        {
            currentOperator = deployable as Operator;
            UpdateSkillButton();
            currentOperator.OnSPChanged += UpdateSkillButtonState;
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
        skillIconImage = skillButton.GetComponent<Image>();

        // 스킬 아이콘이 있다면 아이콘을 버튼 이미지로

        if (currentOperator.CurrentSkill.skillIcon != null)
        {
            skillIconImage.gameObject.SetActive(true);
            skillIconImage.sprite = currentOperator.CurrentSkill.skillIcon;
            skillButton.GetComponentInChildren<TextMeshProUGUI>().gameObject.SetActive(false);
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

        UpdateSkillButtonState(currentOperator.CurrentSP, currentOperator.MaxSP);
    }

    private void UpdateSkillButtonState(float currentSP, float maxSP)
    {
        // 오퍼레이터의 스킬이 자동 발동이라면 활성화될 필요 없음
        if (deployable is Operator op && op.CurrentSkill.autoActivate)
        {
            skillButtonInactivePanel.SetActive(true);
            return;
        }

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