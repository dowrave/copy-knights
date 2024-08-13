using UnityEngine;
using UnityEngine.UI;


public class OperatorActionUI : MonoBehaviour
{
    [SerializeField] private Button skillButton;
    [SerializeField] private Button retreatButton;
    private Operator op; // �����ڵ� C#���� operator�� ����ϹǷ� �������� op�� �����Ѵ�

    public void Initialize(Operator _operator)
    {
        op = _operator;

        // ��ư ��ġ�� �ν����Ϳ��� ����

        // ��ư �̺�Ʈ ����
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
