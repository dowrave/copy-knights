using UnityEngine;


public class DeployableBarUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private HealthBar spBar;

    private float backOffset = 0; // UI�� ���� ������
    private Operator op;
    private void Awake()
    {
        healthBar = transform.Find("HealthBar").GetComponent<HealthBar>();
        spBar = transform.Find("SPBar").GetComponent<HealthBar>();
    }

    public void Initialize(IDeployable deployable)
    {
        // ���۷������� ���� Ȱ��ȭ
        if (deployable is Operator op)
        {
            this.op = op;
            op.OnHealthChanged += UpdateHealthBar;
            op.OnSPChanged += UpdateSPBar;

            UpdateUI();
            UpdatePosition();

            Debug.Log("Bar UI �ʱ�ȭ �Ϸ�");
        }
    }

    private void Update()
    {
        if (op != null)
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
        if (op != null)
        {
            UpdateHealthBar(op.CurrentHealth, op.MaxHealth);
            UpdateSPBar(op.CurrentSP, op.MaxSP);
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

    public Color GetHealthBarColor()
    {
        return healthBar.GetColor();
    }
    public void SetHealthBarColor(Color color)
    {
        healthBar.SetColor(color);
    }

    public Color GetSPBarColor()
    {
        return spBar.GetColor();
    }

    public void SetSPBarColor(Color color)
    {
        if (color != spBar.GetColor())
        {
            spBar.SetColor(color);
        }
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