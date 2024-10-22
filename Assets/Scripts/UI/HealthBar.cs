using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // 체력바 UI 관리
    private Slider slider;
    [SerializeField] private Image fillImage; // HealthBar의 색깔로 접근하기 위한 컴포넌트를 설정
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
    /// 더 밝고 연한 색을 반환함
    /// </summary>
    private Color GetSofterColor(Color originalColor, float saturationAmount = 0.7f, float valueAmount = 0.1f)
    {
        float h, s, v;
        Color.RGBToHSV(originalColor, out h, out s, out v);

        // 0 ~ 1 사이로 값을 지정
        s = Mathf.Clamp01(s - saturationAmount);
        v = Mathf.Clamp01(v + valueAmount);

        return Color.HSVToRGB(h, s, v);
    }

    /// <summary>
    /// 체력 게이지 업데이트(현재 체력, 최대 체력)
    /// </summary>
    public void UpdateHealthBar(float newAmount, float maxAmount)
    {
        this.maxAmount = maxAmount;
        float previousAmount = currentAmount;
        currentAmount = newAmount;

        // Fill Area/Fill 부분의 값만 변경됨
        slider.maxValue = maxAmount;
        slider.value = currentAmount;

        // 이펙트 보여주기 : damageOverlayImage 관련
        // 상황) 체력 회복
        if (previousAmount < currentAmount)
        {
            // 체력 닳는 이펙트를 보여주고 있는 상황이라면 이를 멈춤
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }

            damageOverlayImage.fillAmount = currentAmount / maxAmount; 
        }

        // 상황) 체력 손실 시 서서히 닳는 효과 구현
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
