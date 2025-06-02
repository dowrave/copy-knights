using UnityEngine;


public class DeployableBarUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar = default!;
    [SerializeField] private SPBar spBar = default!;

    private float backOffset = 0; // UI�� ���� ������
    private Operator? op;

    private Camera mainCamera = default!;
    private Canvas canvas = default!;

    public HealthBar HealthBar => healthBar;
    public SPBar SpBar => spBar;

    private void Awake()
    {
        if (healthBar == null)
        {
            healthBar = transform.Find("HealthBar").GetComponent<HealthBar>();
        }
        if (spBar == null)
        {
            spBar = transform.Find("SPBar").GetComponent<SPBar>();
        }

        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        mainCamera = Camera.main;
        canvas.worldCamera = mainCamera;

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
        }
    }

    private void Update()
    {   
        if (op == null)
        {
            Destroy(gameObject);
        }

        UpdatePosition();
    }

    public void UpdateUI()
    {
        if (op != null)
        {
            UpdateHealthBar(op.CurrentHealth, op.MaxHealth, op.GetCurrentShield());
            UpdateSPBar(op.CurrentSP, op.MaxSP);
        }
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth, float currentShield)
    {
        healthBar.UpdateHealthBar(currentHealth, maxHealth, currentShield);
    }

    public void UpdateSPBar(float currentSP, float maxSP)
    {
        if (spBar.IsAmmoMode) return;
        spBar.UpdateSPFill(currentSP, maxSP);
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

    public void SwitchSPBarToAmmoMode(int maxAmmo, int currentAmmo)
    {
        spBar.SwitchToAmmoMode(maxAmmo, currentAmmo);
    }

    public void SwitchSPBarToNormalMode()
    {
        spBar.SwitchToNormalMode();
    }

    public void UpdateAmmoDisplay(int currentAmmo)
    {
        spBar.UpdateAmmoCount(currentAmmo);
    }

    private void UpdatePosition()
    {
        //if (op != null)
        //{
        //    transform.position = op.transform.position + Vector3.back * backOffset;
        //}

        //transform.rotation = Quaternion.Euler(90, 0, 0);

        // Canvas�� �׻� ī�޶� ���ϵ��� ȸ�� ���� - �ߵ��ϰ� ��Ÿ���� �� ��������
        transform.rotation = Camera.main.transform.rotation;
    }
}