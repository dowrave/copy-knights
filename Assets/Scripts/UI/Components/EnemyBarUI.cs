using UnityEngine;
using UnityEngine.UI;

// Enemy ������Ʈ�� UI�� �����ϴ� Ŭ����
public class EnemyBarUI : MonoBehaviour
{
    private float backOffset = 0; // UI�� ���� ������

    private HealthBar? healthBar;
    private Camera? mainCamera;
    private Canvas? canvas;
    private Enemy? enemy;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;
        }

        healthBar = GetComponentInChildren<HealthBar>();

        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

    }

    public void Initialize(Enemy enemy)
    {
        this.enemy = enemy;
        UpdateUI();
    }

    private void LateUpdate()
    {
        if (enemy != null)
        {
            UpdatePosition();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateUI()
    {
        if (enemy != null)
        {
            UpdateHealthBar(enemy.CurrentHealth, enemy.MaxHealth, enemy.GetCurrentShield());
        }
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth, float currentShield = 0)
    {
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth, currentShield);
        }
    }

    public void SetHealthBarVisible(bool isVisible)
    {
        if (healthBar != null)
        {
            healthBar.SetVisible(isVisible);
        }
    }

    public void SetHealthBarColor(Color color)
    {
        if (healthBar != null) 
        {
            healthBar.SetColor(color);  
        }
    }

    private void UpdatePosition()
    {
        if (enemy != null)
        {
            transform.position = enemy.transform.position + Vector3.back * backOffset;
        }
    }
}