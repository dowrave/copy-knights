using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // ü�¹� UI ����
    private Slider slider;
    [SerializeField] private Image fillImage; // HealthBar�� ����� �����ϱ� ���� ������Ʈ�� ����
    [SerializeField] private bool showDamageEffect; 
    [SerializeField] private Image damageOverlayImage;
    [SerializeField] private float damageFadeTime = 0.5f;

    private float currentAmount;
    private float maxAmount;
    private Coroutine damageCoroutine; 

    private void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>(); 
        if (fillImage == null) fillImage = transform.Find("Fill Area/Fill").GetComponent<Image>();
        if (showDamageEffect)
        {
            if (damageOverlayImage == null)
            {
                damageOverlayImage = transform.Find("DamageOverlay").GetComponent<Image>();
            }
             damageOverlayImage.color = GetSofterColor(fillImage.color);
        }
        else
        {
            damageOverlayImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// �� ��� ���� ���� ��ȯ��
    /// </summary>
    private Color GetSofterColor(Color originalColor, float saturationAmount = 0.7f, float valueAmount = 0.1f)
    {
        float h, s, v;
        Color.RGBToHSV(originalColor, out h, out s, out v);

        // 0 ~ 1 ���̷� ���� ����
        s = Mathf.Clamp01(s - saturationAmount);
        v = Mathf.Clamp01(v + valueAmount);

        return Color.HSVToRGB(h, s, v);
    }

    /// <summary>
    /// ü�� ������ ������Ʈ(���� ü��, �ִ� ü��)
    /// </summary>
    public void UpdateHealthBar(float newAmount, float maxAmount)
    {
        this.maxAmount = maxAmount;
        float previousAmount = currentAmount;
        currentAmount = newAmount;

        // Fill Area/Fill �κ��� ���� �����
        slider.maxValue = maxAmount;
        slider.value = currentAmount;

        // ����Ʈ �����ֱ� : damageOverlayImage ����
        // ��Ȳ) ü�� ȸ��
        if (previousAmount < currentAmount)
        {
            // ü�� ��� ����Ʈ�� �����ְ� �ִ� ��Ȳ�̶�� �̸� ����
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }

            damageOverlayImage.fillAmount = currentAmount / maxAmount; 
        }

        // ��Ȳ) ü�� �ս� �� ������ ��� ȿ�� ����
        else if (showDamageEffect && previousAmount > currentAmount)
        {
            ShowDamageEffect(previousAmount);
        }
    }

    private void ShowDamageEffect(float previousAmount)
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
        }

        //damageOverlayImage.fillAmount = previousAmount / maxAmount;
        damageCoroutine = StartCoroutine(FadeDamageOverlay());
    }

    private IEnumerator FadeDamageOverlay()
    {
        float elapsedTime = 0f;
        float startFillAmount = damageOverlayImage.fillAmount;
        float targetFillAmount = currentAmount / maxAmount;

        while (elapsedTime < damageFadeTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / damageFadeTime;
            damageOverlayImage.fillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, t);
            yield return null;
        }
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public Color GetColor()
    {
        return fillImage != null ? fillImage.color : Color.white;
    }

    public void SetColor(Color color)
    {
        if (fillImage != null)
        {
            fillImage.color = color;
            damageOverlayImage.color = GetSofterColor(color);
        }
    }
}
