using UnityEngine;
using UnityEngine.UI;


public class DeployableActionUI : MonoBehaviour
{
    [SerializeField] private MaskedDiamondOverlay maskedOverlay;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button retreatButton;

    private float darkPanelAlpha = 0f;

    private Camera mainCamera;
    private Operator op;

    public void Initialize(IDeployable deployable)
    {
        if (deployable is Operator op)
        {
            this.op = op;
        }

        // 오퍼레이터가 아니라면 스킬 버튼은 비활성화
        if (this.op == null)
        {
            skillButton.gameObject.SetActive(false);
        }

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
    }

    private void SetUpButtons()
    {
        // 버튼 위치는 인스펙터에서 설정
        // 버튼 이벤트 설정
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);

        if (op != null) 
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
        if (op != null)
        {
            Debug.Log("스킬 버튼 클릭됨");
            op.UseSkill();
            Hide();
        }
    }
    private void OnRetreatButtonClicked()
    {
        Debug.Log("퇴각 버튼 클릭됨");
        op.Retreat();
        Hide();
    }

    public void Show()
    {
        maskedOverlay.Show();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        maskedOverlay.Hide();
        gameObject.SetActive(false);
    }

}