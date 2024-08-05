using UnityEngine;
using UnityEngine.UI;

// Enemy ������Ʈ�� UI�� �����ϴ� Ŭ����
public class EnemyUI : MonoBehaviour
{
    private float backOffset = 0; // UI�� ���� ������
    private HealthBar healthBar;
    private Canvas canvas;
    private Enemy enemy;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        healthBar = GetComponentInChildren<HealthBar>();
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
            //Debug.Log($"EnemyUI : ������Ʈ : {enemy.Stats.Health}, {enemy.MaxHealth}");
            UpdateHealthBar(enemy.Stats.Health, enemy.MaxHealth);
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

        transform.rotation = Quaternion.Euler(90, 0, 0);

        // Canvas�� �׻� ī�޶� ���ϵ��� ȸ�� ����
        //transform.rotation = Camera.main.transform.rotation;
    }
}