using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;


public class OperatorUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private HealthBar spBar;

    private float backOffset = 0; // UI의 높이 오프셋
    private Camera mainCamera;
    private Canvas canvas;
    private Operator op;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        mainCamera = Camera.main;
        canvas.worldCamera = mainCamera;

        healthBar = transform.Find("HealthBar").GetComponent<HealthBar>();
        spBar = transform.Find("SPBar").GetComponent<HealthBar>();

        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }
    }

    public void Initialize(Operator op)
    {
        this.op = op;
        op.OnHealthChanged += UpdateHealthBar;

        UpdateUI();
        UpdatePosition();

        //gameObject.SetActive(true);

        Debug.Log("Operator UI 초기화 완료");
    }

    private void Update()
    {
        if (op != null)
        {
            UpdatePosition();
            //UpdateUI();
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

        // Canvas가 항상 카메라를 향하도록 회전 설정
        //transform.rotation = Camera.main.transform.rotation;
    }
}