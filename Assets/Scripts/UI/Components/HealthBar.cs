using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // 체력바 UI 관리
    private Slider slider = default!;
    [SerializeField] private Image healthFill = default!;
    [SerializeField] private Image shieldFill = default!;
    [SerializeField] private Image damageOverlayImage = default!;
    [SerializeField] private float damageFadeTime = 0.5f;

    [Header("체력 게이지인 경우 체크")]
    [SerializeField] private bool showDamageEffect;

    [Header("GaugeColor")]
    [SerializeField] private Color healthFillColor;
    [SerializeField] private Color shieldFillColor; // 체력 게이지 전용
    [SerializeField] private Color damageOverlayColor;  // 체력 게이지 전용

    private float currentAmount;
    private float maxAmount; // Health, SP 등 게이지가 주로 나타내는 값의 최대 수치
    private float totalAmount; // 추가로 반영되는 값까지 포함한 수치
    private Coroutine? damageCoroutine; 

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

    /// <summary>
    /// 게이지 업데이트
    /// </summary>
    public void UpdateHealthBar(float newValue, float maxValue, float currentShield = 0)
    {
        maxAmount = maxValue;
        float previousTotalAmount = totalAmount;
        totalAmount = maxValue + currentShield;

        slider.maxValue = totalAmount; 

        // 이전 값 저장 후 갱신
        float previousAmount = currentAmount;
        currentAmount = newValue;
        
        // 메인 게이지 비율 갱신
        float valueRatio = newValue / totalAmount;
        healthFill.fillAmount = valueRatio;
        healthFill.rectTransform.anchorMin = new Vector2(0, 0);
        healthFill.rectTransform.anchorMax = new Vector2(valueRatio, 1);

        // 보호막 게이지 시각화
        if (currentShield > 0)
        {
            // 시작점은 체력의 끝
            float shieldStartRatio = valueRatio;
            float shieldEndRatio = (newValue + currentShield) / totalAmount;
            float shieldRatio = currentShield / totalAmount;

            // x축으로 shieldStartRatio 부분부터 shieldEndRatio까지, y축은 게이지 전체를 채우는 구조
            shieldFill.gameObject.SetActive(true);
            shieldFill.rectTransform.anchorMin = new Vector2(shieldStartRatio, 0); 
            shieldFill.rectTransform.anchorMax = new Vector2(shieldEndRatio, 1);

            shieldFill.fillAmount = currentShield / totalAmount;
        }
        else
        {
            shieldFill.gameObject.SetActive(false);
        }

        // damageOverlay : 서서히 감소하는 체력 구현
        // 체력이 회복되거나 보호막이 깎이는 등, 게이지에서 체력의 비율이 올라가는 상황
        if (previousAmount < currentAmount || totalAmount != previousTotalAmount)
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }

            damageOverlayImage.fillAmount = currentAmount / totalAmount; 
        }
        //  체력이 닳는 상황
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

            // anchor로 게이지 크기 서서히 감소
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
