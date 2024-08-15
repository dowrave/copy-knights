using UnityEngine;
using UnityEngine.UI;


public class OperatorActionUI : MonoBehaviour
{
    [SerializeField] private Button skillButton;
    [SerializeField] private Button retreatButton;
    private Operator op; // 연산자도 C#에서 operator로 사용하므로 변수명은 op로 지정한다
    private Camera mainCamera; 
    public void Initialize(Operator _operator)
    {
        op = _operator;
        mainCamera = Camera.main;

        // 버튼 위치는 인스펙터에서 설정

        // 버튼 이벤트 설정
        skillButton.onClick.AddListener(OnSkillButtonClicked);
        Debug.Log("Skill button listener added");
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);
        Debug.Log("Retreat button listener added");

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }
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
