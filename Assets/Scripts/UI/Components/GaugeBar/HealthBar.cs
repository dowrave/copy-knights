using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // ü�¹� UI ����
    [SerializeField] private Slider slider = default!;
    [SerializeField] private Image healthFill = default!;
    [SerializeField] private Image shieldFill = default!;
    [SerializeField] private Image damageOverlayImage = default!;
    [SerializeField] private float damageFadeTime = 0.5f;

    [Header("ü�� �������� ��� üũ")]
    [SerializeField] private bool showDamageEffect;

    [Header("GaugeColor")]
    [SerializeField] private Color healthFillColor;
    [SerializeField] private Color shieldFillColor; // ü�� ������ ����
    [SerializeField] private Color damageOverlayColor;  // ü�� ������ ����

    private float currentAmount;
    private float maxAmount; // Health, SP �� �������� �ַ� ��Ÿ���� ���� �ִ� ��ġ
    private float totalAmount; // �߰��� �ݿ��Ǵ� ������ ������ ��ġ
    private Coroutine? damageCoroutine;

    // źȯ ��� ���� ����
    private bool isAmmoMode = false;
    private int ammoCount = 0;
    private int maxAmmoCount = 0;

    private void Awake()
    {
        slider = GetComponent<Slider>();

        if (showDamageEffect)
        {
            if (damageOverlayImage == null)
            {
                damageOverlayImage = transform.Find("DamageOverlay").GetComponent<Image>();
            }
            damageOverlayImage.color = damageOverlayColor;
            shieldFill.gameObject.SetActive(true);
        }
        else
        {
            damageOverlayImage?.gameObject.SetActive(false);
            shieldFill.gameObject.SetActive(false);
        }

        healthFill.color = healthFillColor;
        shieldFill.color = shieldFillColor;
    }

    public void UpdateHealthBar(float newValue, float maxValue, float currentShield = 0)
    {
        maxAmount = maxValue;
        float previousTotalAmount = totalAmount;
        totalAmount = maxValue + currentShield;

        slider.maxValue = totalAmount; 

        // ���� �� ���� �� ����
        float previousAmount = currentAmount;
        currentAmount = newValue;
        
        // ���� ������ ���� ����
        float valueRatio = newValue / totalAmount;
        healthFill.fillAmount = valueRatio;
        healthFill.rectTransform.anchorMin = new Vector2(0, 0);
        healthFill.rectTransform.anchorMax = new Vector2(valueRatio, 1);

        // ��ȣ�� ������ �ð�ȭ
        if (currentShield > 0)
        {
            // �������� ü���� ��
            float shieldStartRatio = valueRatio;
            float shieldEndRatio = (newValue + currentShield) / totalAmount;
            float shieldRatio = currentShield / totalAmount;

            // x������ shieldStartRatio �κк��� shieldEndRatio����, y���� ������ ��ü�� ä��� ����
            shieldFill.gameObject.SetActive(true);
            shieldFill.rectTransform.anchorMin = new Vector2(shieldStartRatio, 0); 
            shieldFill.rectTransform.anchorMax = new Vector2(shieldEndRatio, 1);

            shieldFill.fillAmount = currentShield / totalAmount;
        }
        else
        {
            shieldFill.gameObject.SetActive(false);
        }

        // damageOverlay : ������ �����ϴ� ü�� ����
        // ü���� ȸ���ǰų� ��ȣ���� ���̴� ��, ���������� ü���� ������ �ö󰡴� ��Ȳ
        if (previousAmount < currentAmount || totalAmount != previousTotalAmount)
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }

            damageOverlayImage.fillAmount = currentAmount / totalAmount; 
        }
        //  ü���� ��� ��Ȳ
        else if (showDamageEffect && previousAmount > currentAmount)
        {
            ShowDamageEffect(previousAmount);
        }
    }

    public void SwitchToAmmoMode(int maxAmmo, int currentAmmo)
    {
        isAmmoMode = true;
        maxAmmoCount = maxAmmo;
        ammoCount = currentAmmo;

        // �ٸ� UI ��ҵ� ��Ȱ��ȭ
        if (shieldFill != null) shieldFill.gameObject.SetActive(false);
        if (damageOverlayImage != null) damageOverlayImage.gameObject.SetActive(false);

        UpdateAmmoDisplay();
    }

    public void UpdateAmmoCount(int currentAmmo)
    {
        if (!isAmmoMode) return;

        ammoCount = currentAmmo;
        UpdateAmmoDisplay();
    }

    private void UpdateAmmoDisplay()
    {
        if (maxAmmoCount <= 0) return;

        // Sliced Ÿ������ ����
        healthFill.type = Image.Type.Sliced;
        
        // ���� źȯ ���� ���
        float ammoRatio = (float)ammoCount / maxAmmoCount;
        
        // �̻����� ȿ���� ���� ������ �ܰ躰�� ����
        float discreteRatio = Mathf.Floor(ammoRatio * maxAmmoCount) / maxAmmoCount;
        
        healthFill.fillAmount = discreteRatio;
    }

    public void SwitchToNormalMode()
    {
        isAmmoMode = false;

        // HealthFill ���� ���·� ����
        healthFill.fillAmount = currentAmount / totalAmount;
        healthFill.rectTransform.anchorMin = new Vector2(0, 0);
        healthFill.rectTransform.anchorMax = new Vector2(currentAmount / totalAmount, 1);
    }

    private void ShowDamageEffect(float previousAmount)
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
        }

        damageCoroutine = StartCoroutine(FadeDamageOverlay());
    }

    private IEnumerator FadeDamageOverlay()
    {
        float elapsedTime = 0f;

        float startRatio = damageOverlayImage.rectTransform.anchorMax.x;
        float targetRatio = currentAmount / totalAmount;
        
        while (elapsedTime < damageFadeTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / damageFadeTime;

            // anchor�� ������ ũ�� ������ ����
            float currentRatio = Mathf.Lerp(startRatio, targetRatio, t);
            damageOverlayImage.rectTransform.anchorMax = new Vector2(currentRatio, 1);

            yield return null;
        }
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public Color GetColor()
    {
        return healthFill != null ? healthFill.color : Color.white;
    }

    public void SetColor(Color color)
    {
        if (healthFill != null)
        {
            healthFill.color = color;
        }
    }
}
