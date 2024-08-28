using UnityEngine;
using UnityEngine.UI;


public class OperatorActionUI : MonoBehaviour
{
    [SerializeField] private DiamondImage diamondImage;
    [SerializeField] private float lineWidth = 0.1f;

    [SerializeField] private Button skillButton;
    [SerializeField] private Button retreatButton;
    private Camera mainCamera;
    private Operator op;

    public void Initialize(Operator _operator)
    {
        op = _operator;
        mainCamera = Camera.main;

        SetupDiamondIndicator();
        SetUpButtons();
        UpdateSkillIcon();

        // UI가 보는 방향 설정
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        gameObject.SetActive(true);
    }

    private void SetupDiamondIndicator()
    {
        //diamondRect.sizeDelta = new Vector2(diamondSize, diamondSize);
        diamondImage.LineWidth = lineWidth;
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
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

}
