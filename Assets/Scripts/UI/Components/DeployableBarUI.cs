using UnityEngine;


public class DeployableBarUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar = default!;
    [SerializeField] private SPBar spBar = default!;

    private float backOffset = 0; // UI의 높이 오프셋

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
    }

    private void Update()
    {   
        UpdateRotation();
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

    private void UpdateRotation()
    {
        // Canvas가 항상 카메라를 향하도록 회전 설정 - 삐딱하게 나타나는 거 방지해줌
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}