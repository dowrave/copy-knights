using UnityEngine;
using UnityEngine.UI;


public class OperatorActionUI : MonoBehaviour
{
    [SerializeField] private MaskedDiamondOverlay maskedOverlay;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button retreatButton;

    private float darkPanelAlpha = 0f;

    private Camera mainCamera;
    private Operator op;

    public void Initialize(Operator _operator)
    {
        op = _operator;
        mainCamera = Camera.main;

        SetUpButtons();
        UpdateSkillIcon();

        // UI가 보는 방향 설정
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        maskedOverlay.Initialize(darkPanelAlpha); // 알파 0으로 조정

        gameObject.SetActive(true);
    }
  
    private void SetUpButtons()
    {
        // 버튼 위치는 인스펙터에서 설정
        // 버튼 이벤트 설정
        skillButton.onClick.AddListener(OnSkillButtonClicked);
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);
    }
    
    private void UpdateSkillIcon()
    {
        // 스킬 아이콘 업데이트 로직
    }
    

    private void OnSkillButtonClicked()
    {
        Debug.Log("스킬 버튼 클릭됨");
        op.UseSkill();
        Hide();
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
