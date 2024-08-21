using UnityEngine;
using UnityEngine.UI;


public class OperatorActionUI : MonoBehaviour
{
    [SerializeField] private Button skillButton;
    [SerializeField] private Button retreatButton;
    private Operator op; // �����ڵ� C#���� operator�� ����ϹǷ� �������� op�� �����Ѵ�
    private Camera mainCamera; 
    public void Initialize(Operator _operator)
    {
        op = _operator;
        mainCamera = Camera.main;

        // ��ư ��ġ�� �ν����Ϳ��� ����
        // ��ư �̺�Ʈ ����
        skillButton.onClick.AddListener(OnSkillButtonClicked);
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);

        // UI�� ���� ���� ����
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        gameObject.SetActive(true);
    }

    private void OnSkillButtonClicked()
    {
        Debug.Log("��ų ��ư Ŭ����");
        op.UseSkill();
        Hide();
    }
    private void OnRetreatButtonClicked()
    {
        Debug.Log("�� ��ư Ŭ����");
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
