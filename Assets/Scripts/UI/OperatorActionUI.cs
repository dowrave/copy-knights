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

        // UI�� ���� ���� ����
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        maskedOverlay.Initialize(darkPanelAlpha); // ���� 0���� ����

        gameObject.SetActive(true);
    }
  
    private void SetUpButtons()
    {
        // ��ư ��ġ�� �ν����Ϳ��� ����
        // ��ư �̺�Ʈ ����
        skillButton.onClick.AddListener(OnSkillButtonClicked);
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);
    }
    
    private void UpdateSkillIcon()
    {
        // ��ų ������ ������Ʈ ����
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
        maskedOverlay.Show();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        maskedOverlay.Hide();
        gameObject.SetActive(false);
    }

}
