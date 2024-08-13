using UnityEngine;
using UnityEngine.UI;


public class OperatorActionUI : MonoBehaviour
{
    [SerializeField] private Button skillButton;
    [SerializeField] private Button retreatButton;
    private Operator op; // 연산자도 C#에서 operator로 사용하므로 변수명은 op로 지정한다

    public void Initialize(Operator _operator)
    {
        op = _operator;

        // 버튼 위치는 인스펙터에서 설정

        // 버튼 이벤트 설정
        skillButton.onClick.AddListener(op.UseSkill);
        retreatButton.onClick.AddListener(op.Retreat);

        gameObject.SetActive(true);
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
