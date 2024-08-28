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

        // UI�� ���� ���� ����
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
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

}
