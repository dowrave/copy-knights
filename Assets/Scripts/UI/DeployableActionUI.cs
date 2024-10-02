using Unity.VisualScripting;
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

        mainCamera = Camera.main;
        SetUpButtons();
        UpdateSkillIcon();

        // UI가 보는 방향 설정
        if (mainCamera != null)
        {
            //transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
            transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        maskedOverlay.Initialize(darkPanelAlpha); // 알파 0으로 조정

        gameObject.SetActive(true);

        if (IsOperator())
        {
            currentOperator = deployable as Operator;
            UpdateSkillButton();
            currentOperator.OnSPChanged += UpdateSkillButtonState;
            Debug.Log("SP 변화 이벤트 UI에 등록 완료");
        }
        else
        {
            skillButton.gameObject.SetActive(false);
        }
    }

    private void SetUpButtons()
    {
        // 버튼 위치는 인스펙터에서 설정
        // 버튼 이벤트 설정
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);

        if (IsOperator()) 
        { 
            skillButton.onClick.AddListener(OnSkillButtonClicked);
        }
    }

    private void UpdateSkillIcon()
    {
        // 스킬 아이콘 업데이트 로직
    }


    private void OnSkillButtonClicked()
    {
        if (deployable is Operator op)
        {
            Debug.Log("스킬 버튼 클릭됨");
            op.UseSkill();
            UpdateSkillButton();
            Hide();
        }
    }
    private void OnRetreatButtonClicked()
    {
        Debug.Log("퇴각 버튼 클릭됨");
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
        
        // 스킬 아이콘이 있다면 아이콘을 버튼 이미지로 설정
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