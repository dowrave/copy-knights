using UnityEngine;
using System;

// UnitEntity의 방어적인 요소들
// 체력, 방어력, 마법 저항력, 쉴드에 관한 처리를 다룬다.
public class HealthController: IHealthReadOnly
{
    private StatController _statController; 
    public ShieldSystem Shield { get; private set; } // 내부 로직용

    // 인터페이스 구현
    public float CurrentHealth { get; private set; }
    public float MaxHealth => _statController.GetStat(StatType.MaxHP);
    public float CurrentShield { get => Shield.CurrentShield; } // 외부 프로퍼티용

    // 변경 전 maxHealth를 캐싱해두는 변수(최대 체력 증가 시에 현재 체력의 비율을 유지해주기 위함)
    private float _lastMaxHealth; 

    public event Action<float, float, float> OnHealthChanged = delegate { };
    public event Action OnDeath = delegate { }; // 사망 이벤트

    // 생성자
    public HealthController(StatController statController)
    {
        _statController = statController;
        Shield = new ShieldSystem();

        _statController.OnStatChanged += HandleMaxHPChanged; 
    }

    public void Initialize()
    {
        _lastMaxHealth = MaxHealth;
        CurrentHealth = _lastMaxHealth;

        // 쉴드 초기화
        Shield.DeactivateShield();

        // 초기 상태 알림
        NotifyHealthChanged();
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
            NotifyHealthChanged();
        }

        // 사망 체크
        if (CurrentHealth <= 0)
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
                float defense = _statController.GetStat(StatType.Defense);
                reducedDamage = damage - defense;
                break;
            case AttackType.Magical:
                float magicResistance = _statController.GetStat(StatType.MagicResistance);
                reducedDamage = damage * (1 - magicResistance / 100);
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
        if (CurrentHealth <= 0) return 0f;

        float oldHealth = CurrentHealth;
        
        // 최대 체력을 넘기면 안됨
        CurrentHealth = Mathf.Min(CurrentHealth + attackSource.Damage, MaxHealth);
        
        // 실제 적용된 힐량
        float actualHealAmount = Mathf.FloorToInt(CurrentHealth - oldHealth); 

        // UI 업데이트 알림
        if (actualHealAmount > 0)
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, Shield.CurrentShield);
        }

        return actualHealAmount; 
    } 

    private void HandleMaxHPChanged(StatType type)
    {
        if (type == StatType.MaxHP)
        {
            // 변경된 후의 최대 체력
            float newMaxHealth = MaxHealth;

            // 비율 계산 및 적용 
            // 이전 최대 체력이 0보다 클 때만 수행해서 0나누기 방지
            if (_lastMaxHealth > 0 && newMaxHealth != _lastMaxHealth)
            {
               // 비율 : 현재 체력 / 변경 전 최대 체력
               float healthRatio = CurrentHealth / _lastMaxHealth;

               // 새로운 현재 체력 : 변경 후 최대 체력 * 비율
               CurrentHealth = newMaxHealth * healthRatio; 
            }

            // 캐시된 최대 체력값을 최신값으로 갱신
            _lastMaxHealth = newMaxHealth; 

            // 안전장치 - 계산 오차로 Max를 넘거나 0보다 작아지는 경우를 방지함
            // 
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, newMaxHealth);

            NotifyHealthChanged();
        }
    }  

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, Shield.CurrentShield);
    }

    public void ActivateShield(float amount) 
    {
        Shield.ActivateShield(amount);
        NotifyHealthChanged(); // 쉴드가 생기면 체력바 갱신
    }
    public void DeactivateShield()
    {
        Shield.DeactivateShield();
        NotifyHealthChanged();
    }

    public void Reset()
    {
        OnHealthChanged = delegate { };
        OnDeath = delegate { };

        _statController.OnStatChanged -= HandleMaxHPChanged; 
        // Shield는 계속 사용하므로 구독해제하지 않아도 무방함 - HealthSystem과 수명 동일함
    }

}