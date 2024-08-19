using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;


public class OperatorUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private HealthBar spBar;
    private float backOffset = 0; // UI�� ���� ������
    private Canvas canvas; 
    private Operator op;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        healthBar = transform.Find("HealthBar").GetComponent<HealthBar>();
        spBar = transform.Find("SPBar").GetComponent<HealthBar>();

        //gameObject.SetActive(false);
    }

    public void Initialize(Operator op)
    {
        this.op = op;
        UpdateUI();
        UpdatePosition();

        //gameObject.SetActive(true);

        Debug.Log("Operator UI �ʱ�ȭ �Ϸ�");
    }

    private void Update()
    {
        if (op != null)
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
        if (op != null)
        {
            UpdateHealthBar(op.Stats.Health, op.MaxHealth);
            UpdateSPBar(op.CurrentSP, op.data.maxSP);
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
        if (op != null)
        {
            transform.position = op.transform.position + Vector3.back * backOffset;
        }

        transform.rotation = Quaternion.Euler(90, 0, 0);

        // Canvas�� �׻� ī�޶� ���ϵ��� ȸ�� ����
        //transform.rotation = Camera.main.transform.rotation;
    }
}
