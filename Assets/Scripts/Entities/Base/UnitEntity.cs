using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static ICombatEntity;

// Operator, Enemy, Barricade ���� Ÿ�� ���� ���ֵ�� ���õ� ��ƼƼ
public abstract class UnitEntity : MonoBehaviour, ITargettable, IFactionMember
{
    public Faction Faction { get; protected set; }

    public GameObject Prefab { get; protected set; } = default!;
    public ShieldSystem shieldSystem = default!;

    // ���� ����
    private float _currentHealth;
    public float CurrentHealth
    {
        get => _currentHealth;
        protected set
        {
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth); // 0 ~ �ִ� ü�� ���̷� �� ����
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth, shieldSystem.CurrentShield);
        }
    }

    public float MaxHealth { get; protected set; }

    // �������̽��� �ִ� ��쿡�� �� ��
    protected List<CrowdControl> activeCC = new List<CrowdControl>();

    // �� ��ü�� �����ϴ� ��ƼƼ ���
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // �ڽ� �ݶ��̴�
    protected BoxCollider boxCollider = default!;

    // �̺�Ʈ
    public event System.Action<float, float, float> OnHealthChanged = delegate { };
    public event System.Action<UnitEntity> OnDestroyed = delegate { };
    public event System.Action<CrowdControl, bool> OnCrowdControlChanged = delegate { };

    protected virtual void Awake()
    {
        // �ݶ��̴� �Ҵ�
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogError($"{gameObject.name}�� BoxCollider�� ����!");
        }
        SetColliderState();

        // ���� �ý��� ����
        shieldSystem = new ShieldSystem();
        shieldSystem.OnShieldChanged += (shield, onShieldDepleted) =>
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shield);
        };

    }


    public virtual void AddAttackingEntity(ICombatEntity attacker)
    {
        if (!attackingEntities.Contains(attacker))
        {
            attackingEntities.Add(attacker);
        }
    }

    // �� ��ü�� �����ϴ� ���� ����
    public virtual void RemoveAttackingEntity(ICombatEntity attacker)
    {
        attackingEntities.Remove(attacker);
    }


    protected virtual void Die()
    {

        OnDestroyed?.Invoke(this);
        RemoveAllCrowdControls();
        Destroy(gameObject);
    }

    protected abstract void InitializeHP();

    public virtual void TakeHeal(UnitEntity healer, AttackSource attackSource, float healAmount)
    {
        float oldHealth = CurrentHealth;
        CurrentHealth += healAmount; 
        float actualHealAmount = Mathf.FloorToInt(CurrentHealth - oldHealth); // ���� ����

        if (healer is MedicOperator medic && medic.OperatorData.hitEffectPrefab != null)
        {
            PlayGetHitEffect(medic, attackSource);
        }
        
        ObjectPoolManager.Instance!.ShowFloatingText(transform.position, actualHealAmount, true);


        if (healer is Operator healerOperator)
        {
            StatisticsManager.Instance!.UpdateHealingDone(healerOperator.OperatorData, actualHealAmount);
        }
    }

    protected virtual void PlayGetHitEffect(UnitEntity attacker, AttackSource attackSource)
    {
        GameObject hitEffectPrefab = attackSource.HitEffectPrefab;
        string hitEffectTag = attackSource.HitEffectTag;
        string attackerName;

        if (attacker is Operator op)
        {
            OperatorData opData = op.OperatorData;
            attackerName = opData.entityName;
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = opData.hitEffectPrefab;
            }
        }
        else if (attacker is Enemy enemy)
        {
            EnemyData enemyData = enemy.BaseData;
            attackerName = enemyData.entityName;
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = enemyData.hitEffectPrefab;
            }
        }
        else
        {
            Debug.LogError("����Ʈ ����");
            return;
        }

        if (hitEffectPrefab != null)
        {
            Vector3 effectPosition = transform.position;

            // Ǯ���� ����Ʈ ������Ʈ ��������
            if (hitEffectTag == string.Empty)
            {
                hitEffectTag = attackerName + hitEffectPrefab.name;
            }
            GameObject? hitEffect = ObjectPoolManager.Instance!.SpawnFromPool(hitEffectTag, effectPosition, Quaternion.identity);
            if (hitEffect != null)
            {
                CombatVFXController hitVFXController = hitEffect.GetComponent<CombatVFXController>();
                hitVFXController.Initialize(attackSource, this, hitEffectTag);
            }
        }
    }

    public virtual void AddCrowdControl(CrowdControl newCC)
    {
        // ���� Ÿ���� CC Ȯ��
        CrowdControl existingCC = activeCC.FirstOrDefault(cc => cc.GetType() == newCC.GetType());
        if (existingCC != null)
        {
            RemoveCrowdControl(existingCC);
        }

        activeCC.Add(newCC);

        // CC �߰� �̺�Ʈ -> UI ������Ʈ � ���
        OnCrowdControlChanged?.Invoke(newCC, true);
    }

    public virtual void RemoveCrowdControl(CrowdControl cc)
    {
        if (activeCC.Remove(cc))
        {
            cc.ForceRemove();
            OnCrowdControlChanged?.Invoke(cc, false);
        }
    }

    // CC ȿ�� ����
    protected virtual void UpdateCrowdControls()
    {
        for (int i = activeCC.Count - 1; i>=0; i--)
        {
            var cc = activeCC[i];
            cc.Update();

            if (cc.IsExpired)
            {
                OnCrowdControlChanged?.Invoke(cc, false);
                activeCC.RemoveAt(i);
            }
        }
    }

    protected virtual void RemoveAllCrowdControls()
    {
        foreach (var cc in activeCC.ToList())
        {
            RemoveCrowdControl(cc);
        }
    }

    // ��ų ������ ���� ���� ü�� ���� �� �� �޼��带 ���
    public void ChangeCurrentHealth(float newCurrentHealth)
    {
        CurrentHealth = Mathf.Floor(newCurrentHealth);
    }
    public void ChangeMaxHealth(float newMaxHealth)
    {
        MaxHealth = Mathf.Floor(newMaxHealth);
    }


    public virtual void TakeDamage(UnitEntity attacker, AttackSource attackSource, float damage, bool playGetHitEffect = true)
    {
        float actualDamage = 0f;

        if (attacker is ICombatEntity iCombatEntity && CurrentHealth > 0)
        {
            // ��� / ���� ���׷��� ����� ���� ������ �����
            actualDamage = Mathf.Floor(CalculateActualDamage(iCombatEntity.AttackType, damage));

            // ���带 ��� ���� �����
            float remainingDamage = shieldSystem.AbsorbDamage(actualDamage);

            // ü�� ���
            CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shieldSystem.CurrentShield);

            // �ǰ� ����Ʈ ���
            if (playGetHitEffect)
            {
                PlayGetHitEffect(attacker, attackSource);
            }
        }

        OnDamageTaken(attacker, actualDamage);

        if (CurrentHealth <= 0)
        {
            Die(); // �������̵� �޼���
        }
    }

    protected virtual void OnDamageTaken(UnitEntity attacker, float actualDamage) { } // �ǰ� �ÿ� �߰��� ������ �� ���� �� ����� �޼��� 

    // �ݶ��̴��� Ȱ��ȭ ���� ����
    protected virtual void SetColliderState() { } // Enemy, DeployableUnitEntity���� �� ����(abstract���� �ϸ� �ݵ�� ���ܿ��� �����ؾ� ��)

    protected abstract float CalculateActualDamage(AttackType attacktype, float incomingDamage);
    public void ActivateShield(float amount) => shieldSystem.ActivateShield(amount);
    public void DeactivateShield() => shieldSystem.DeactivateShield();
    public float GetCurrentShield() => shieldSystem.CurrentShield;


}
