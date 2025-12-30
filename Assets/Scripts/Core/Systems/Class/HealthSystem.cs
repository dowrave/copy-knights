using UnityEngine;
using UnityEngine.Events;
using System;
using System.Threading;

// UnitEntity의 방어적인 요소들
// 체력, 방어력, 마법 저항력, 쉴드에 관한 처리를 다룬다.
public class HealthSystem
{

    // 스탯 정의
    // private float _currentHealth;
    // public float CurrentHealth
    // {
    //     get => _currentHealth;
    //     private set
    //     {
    //         _currentHealth = Mathf.Clamp(value, 0, MaxHealth); // 0 ~ 최대 체력 사이로 값 유지
    //         OnHealthChanged?.Invoke(_currentHealth, MaxHealth, Shield.CurrentShield);
    //     }
    // }
    public float CurrentHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public float Defense { get; private set; }
    public float MagicResistance { get; private set;}
    public ShieldSystem Shield { get; private set; }

    public event Action<float, float, float> OnHealthChanged = delegate { };
    public event Action OnDeath = delegate { }; // 사망 이벤트

    // 생성자
    public HealthSystem()
    {
        Shield = new ShieldSystem();
    }

    public void Initialize(float maxHealth, float defense, float magicResist)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        Defense = defense;
        MagicResistance = magicResist;

        // 쉴드 초기화
        Shield.DeactivateShield();

        // 초기 상태 알림
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, Shield.CurrentShield);

        // shieldSystem.OnShieldChanged += (shield, onShieldDepleted) =>
        // {
        //     OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shield);
        // };
    }

    // 대미지 처리
    public float ProcessDamage(AttackSource source)
    {
        if (CurrentHealth <= 0) return 0f;
        
        // 방어력 / 마법저항에 의해 감소된 피해량
        float actualDamage = Mathf.Floor(CalculateActualDamage(source.Type, source.Damage));

        // 쉴드에 의해 흡수되고 남은 피해량
        float remainingDamage = Shield.AbsorbDamage(actualDamage);

        // 체력 차감
        float previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);

        // 변경 알림
        if (previousHealth != CurrentHealth)
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, Shield.CurrentShield);
        }

        // 사망 체크
        if (CurrentHealth < 0)
        {
            OnDeath?.Invoke();
        }

        // UI 표시를 위해 리턴()
        return remainingDamage;
    }

    private float CalculateActualDamage(AttackType type, float damage)
    {
        if (type == AttackType.None) return 0; // 예외 처리 
        
        float reducedDamage = damage;
        switch (type)
        {
            case AttackType.Physical:
                reducedDamage = damage - Defense;
                break;
            case AttackType.Magical:
                reducedDamage = damage * (1 - MagicResistance / 100);
                break;
            case AttackType.True:
                reducedDamage = damage;
                break;
        }

        return Mathf.Max(reducedDamage, 0.05f * damage); // 들어온 대미지의 5%는 항상 들어가게끔 보장
    }

    // 힐 처리
    public float ProcessHeal(AttackSource attackSource)
    {
        float oldHealth = CurrentHealth;
        CurrentHealth += attackSource.Damage;
        float actualHealAmount = Mathf.FloorToInt(CurrentHealth - oldHealth); // 실제 힐량

        return actualHealAmount; 
    }    

    public void ChangeCurrentHealth(float newCurrentHealth)
    {
        CurrentHealth = Mathf.Floor(newCurrentHealth);
    }
    public void ChangeMaxHealth(float newMaxHealth)
    {
        MaxHealth = Mathf.Floor(newMaxHealth);
    }

    public void ActivateShield(float amount) => Shield.ActivateShield(amount);
    public void DeactivateShield() => Shield.DeactivateShield();
    public float GetCurrentShield() => Shield.CurrentShield;

    public void Reset()
    {
        OnHealthChanged = delegate { };
        OnDeath = delegate { };

        // Shield는 계속 사용하므로 구독해제하지 않아도 무방함 - HealthSystem과 수명 동일함
    }

}