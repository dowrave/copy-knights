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
        canvas.worldCamera = Camera.main; // 메인 카메라 설정, UI 요소가 3D 공간에서 올바르게 렌더링되고 카메라를 향하게 한다.
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
