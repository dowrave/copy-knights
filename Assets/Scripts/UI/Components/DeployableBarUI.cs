using UnityEngine;


public class DeployableBarUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar = default!;
    [SerializeField] private HealthBar spBar = default!;

    private float backOffset = 0; // UI의 높이 오프셋
    private Operator? op;

    private Camera mainCamera = default!;
    private Canvas canvas = default!;

    private void Awake()
    {
        if (healthBar == null)
        {
            healthBar = transform.Find("HealthBar").GetComponent<HealthBar>();
        }
        if (spBar == null)
        {
            spBar = transform.Find("SPBar").GetComponent<HealthBar>();
        }

        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        mainCamera = Camera.main;
        canvas.worldCamera = mainCamera;

        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }
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
            UpdatePosition();
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

        // Canvas가 항상 카메라를 향하도록 회전 설정
        //transform.rotation = Camera.main.transform.rotation;
    }
}