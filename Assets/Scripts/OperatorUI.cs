using UnityEngine;
using UnityEngine.UI;


public class OperatorUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private HealthBar spBar;
    private Canvas canvas; 
    private Operator target;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
    }

    public void SetTarget(Operator op)
    {
        target = op;
        UpdatePosition();
        UpdateUI();
    }

    private void Update()
    {
        if (target != null)
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
        if (target != null)
        {
            UpdateHealthBar(target.Stats.Health, target.MaxHealth);
            UpdateSPBar(target.CurrentSP, target.data.maxSP);
        }
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
        spBar.SetColor(color);
    }

    private void UpdatePosition()
    {
        if (target != null)
        {
            transform.position = target.transform.position + new Vector3(0, 0, -0.4f); // UI를 Operator 아래에 배치
            transform.rotation = Camera.main.transform.rotation; // UI가 항상 카메라를 향하도록 함
        }
    }
}
