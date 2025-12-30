using UnityEngine;
using UnityEngine.UI;

// Enemy 오브젝트의 UI를 관리하는 클래스
public class EnemyBarUI : MonoBehaviour
{
    private float backOffset = 0; // UI의 높이 오프셋

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
    }

    public void Initialize(Enemy enemy)
    {
        this.enemy = enemy;

        UnsubscribeEvents();
        SubscribeEvents();

        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        UpdateHealthBar(enemy.HealthSystem.CurrentHealth, enemy.HealthSystem.MaxHealth, enemy.GetCurrentShield());
    }
 

    // Enemy의 이동보다 늦게 동작
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


    protected void SubscribeEvents()
    {
        enemy.HealthSystem.OnHealthChanged += UpdateHealthBar;
    }

    protected void UnsubscribeEvents()
    {
        enemy.HealthSystem.OnHealthChanged -= UpdateHealthBar;
    }



    public void UpdateHealthBar(float currentHealth, float maxHealth, float currentShield = 0)
    {
        if (enemy != null && healthBar != null)
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

    protected virtual void OnDestroy()
    {
        UnsubscribeEvents();
    }
}