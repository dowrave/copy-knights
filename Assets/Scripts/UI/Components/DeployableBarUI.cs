using UnityEngine;


public class DeployableBarUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar = default!;
    [SerializeField] private SPBar spBar = default!;

    private float backOffset = 0; // UI의 높이 오프셋
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
        // 오퍼레이터일 때만 활성화
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

        // Canvas가 항상 카메라를 향하도록 회전 설정 - 삐딱하게 나타나는 거 방지해줌
        transform.rotation = Camera.main.transform.rotation;
    }
}