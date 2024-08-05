using UnityEngine;
using UnityEngine.UI;


public class OperatorCanvas : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private HealthBar spBar;
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main; // ���� ī�޶� ����, UI ��Ұ� 3D �������� �ùٸ��� �������ǰ� ī�޶� ���ϰ� �Ѵ�.
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        healthBar.UpdateHealthBar(currentHealth, maxHealth);
    }

    public void UpdateSPBar(float currentSP, float maxSP)
    {
        spBar.UpdateHealthBar(currentSP, maxSP);
    }

    public void SetHealthBarColor(Color color)
    {
        healthBar.SetColor(color);
    }

    public void SetSPBarColor(Color color)
    {
        healthBar.SetColor(color);
    }
}
