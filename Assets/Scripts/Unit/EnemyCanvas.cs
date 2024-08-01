using UnityEngine;

public class EnemyCanvas : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        healthBar.UpdateHealthBar(currentHealth, maxHealth);
    }

    public void SetHealthBarVisible(bool isVisible)
    {
        healthBar.SetVisible(isVisible);
    }

    public void SetHealthBarColor(Color color)
    {
        healthBar.SetColor(color);
    }
}