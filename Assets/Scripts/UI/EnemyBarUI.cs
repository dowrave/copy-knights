using UnityEngine;
using UnityEngine.UI;

// Enemy ������Ʈ�� UI�� �����ϴ� Ŭ����
public class EnemyBarUI : MonoBehaviour
{
    private float backOffset = 0; // UI�� ���� ������
    private HealthBar healthBar;
    private Camera mainCamera;

    private Canvas canvas;
    private Enemy enemy;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        mainCamera = Camera.main;
        canvas.worldCamera = mainCamera;

        healthBar = GetComponentInChildren<HealthBar>();

        
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
            UpdateUI();
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

            UpdateHealthBar(enemy.CurrentHealth, enemy.MaxHealth);
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
        if (enemy != null)
        {
            transform.position = enemy.transform.position + Vector3.back * backOffset;
        }

        //transform.rotation = Quaternion.Euler(90, 0, 0);

        // Canvas�� �׻� ī�޶� ���ϵ��� ȸ�� ����
        //transform.rotation = Camera.main.transform.rotation;
    }
}