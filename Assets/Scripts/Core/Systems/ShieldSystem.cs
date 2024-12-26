using UnityEngine;

/// <summary>
/// ��ȣ���� ���� ������ ��ü�鿡�� ���˴ϴ�
/// ��ȣ�� ���� ������ ����մϴ�
/// </summary>
public class ShieldSystem
{
    private float currentShield;
    private float maxShield;
    private bool hasShield;

    public float CurrentShield => currentShield;
    public float MaxShield => maxShield;

    public event System.Action<float> OnShieldChanged;

    public void ActivateShield(float amount)
    {
        maxShield = amount;
        SetShield(amount);
        hasShield = true;
    }

    public void DeactivateShield()
    {
        SetShield(0);
        maxShield = 0;
        hasShield = false;
    }

    public float AbsorbDamage(float damage)
    {
        if (!hasShield) return damage;

        if (currentShield >= damage)
        {
            SetShield(currentShield - damage);
            return 0;
        }
        // ���尡 ������ ��Ȳ
        else
        {
            float remainingDamage = damage - currentShield;
            SetShield(0);
            return remainingDamage;
        }

    }

    public void SetShield(float value)
    {
        currentShield = Mathf.Clamp(value, 0f, maxShield);
        OnShieldChanged?.Invoke(currentShield);

        if (currentShield <= 0)
        {
            hasShield = false;
        }
    }
}