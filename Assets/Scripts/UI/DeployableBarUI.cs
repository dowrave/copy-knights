using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;


public class DeployableBarUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private HealthBar spBar;

    private float backOffset = 0; // UI�� ���� ������
    private Camera mainCamera;
    private Canvas canvas;
    private IDeployable deployable;
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

    public void Initialize(IDeployable deployable)
    {
        // ���۷������� ���� Ȱ��ȭ
        if (deployable is Operator op)
        {
            this.op = op;
            op.OnHealthChanged += UpdateHealthBar;

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
            UpdateSPBar(op.CurrentSP, op.Data.maxSP);
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