using UnityEngine;
using UnityEngine.UI;


public class EnemyUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;
    private Canvas canvas;
    private Enemy target;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
    }

    public void SetTarget(Enemy enemy)
    {
        target = enemy;
        UpdatePosition();
        UpdateUI();
    }

    private void Update()
    {
        if (target != null)
        {
            UpdatePosition();
        } else 
        {
            Destroy(gameObject);
        }
    }

    public void UpdateUI()
    {
        if (target != null)
        {
            UpdateHealthBar(target.Stats.Health, target.MaxHealth);
        }
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

    private void UpdatePosition()
    {
        if (target != null)
        {
            transform.position = target.transform.position + new Vector3(0, 0, -0.4f);
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}
