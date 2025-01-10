using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using static ICombatEntity;

/// <summary>
/// Operator, Enemy, Barricade ���� Ÿ�� ���� ���ֵ�� ���õ� ��ƼƼ
/// </summary>
public abstract class UnitEntity : MonoBehaviour, ITargettable, IFactionMember
{
    public UnitData BaseData { get; private set; }

    private UnitStats currentStats; // ������Ƽ�� �������� ����. 
    public Faction Faction { get; protected set; }

    public Tile CurrentTile { get; protected set; }
    public GameObject Prefab { get; protected set; }

    public ShieldSystem shieldSystem;

    // ���� ����
    public float CurrentHealth
    {
        get => currentStats.Health;
        set
        {
            currentStats.Health = Mathf.Clamp(value, 0, MaxHealth); // 0 ~ �ִ� ü�� ���̷� �� ����
            OnHealthChanged?.Invoke(currentStats.Health, MaxHealth, shieldSystem.CurrentShield);
        }
    }
    public float MaxHealth { get; set; }

    protected List<CrowdControl> activeCC = new List<CrowdControl>();


    // �� ��ü�� �����ϴ� ��ƼƼ ���
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // �̺�Ʈ
    public event System.Action<float, float, float> OnHealthChanged;
    public event System.Action OnDestroyed;
    public event System.Action<CrowdControl, bool> OnCrowdControlChanged;

    protected virtual void Awake()
    {
        shieldSystem = new ShieldSystem();
        shieldSystem.OnShieldChanged += (shield, onShieldDepleted) =>
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shield);
        };
    }

    public void Initialize(UnitData unitData)
    {
        this.BaseData = unitData;
        currentStats = unitData.stats;

        InitializeUnitProperties();
    }

    protected virtual void Update()
    {
        UpdateCrowdControls();
    }

    // Data, Stat�� ��ƼƼ���� �ٸ��� ������ �ڽ� �޼��忡�� ������ ���
    protected virtual void InitializeUnitProperties()
    {
        InitializeHP();

        // ���� ��ġ�� ������� �� Ÿ�� ����
        UpdateCurrentTile();

        Prefab = BaseData.prefab;
    }

    /// <summary>
    /// �ǰ� ����� ���, ü�� ����
    /// </summary>
    public virtual void TakeDamage(UnitEntity attacker, AttackSource attackSource, float damage)
    {
        if (attacker is ICombatEntity iCombatEntity)
        {
            // ��� / ���� ���׷��� ����� ���� ������ �����
            float actualDamage = CalculateActualDamage(iCombatEntity.AttackType, damage);

            // ���带 ��� ���� �����
            float remainingDamage = shieldSystem.AbsorbDamage(actualDamage);

            // ü�� ���
            CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shieldSystem.CurrentShield);

            PlayGetHitEffect(attacker, attackSource);

            if (CurrentHealth <= 0)
            {
                Die(); // �ڽ� �޼��忡�� �������̵��ߴٸ� �������̵��� �޼��尡 ȣ��
            }
        }
    }

    /// <summary>
    /// ����� ��� ����
    /// </summary>
    protected virtual float CalculateActualDamage(AttackType attacktype, float incomingDamage)
    {
        float actualDamage = 0; // �Ҵ��ؾ� return������ ������ �ȳ�

        switch (attacktype)
        {
            case AttackType.Physical:
                actualDamage = incomingDamage - currentStats.Defense;
                break;
            case AttackType.Magical: 
                actualDamage = incomingDamage * (1 - currentStats.MagicResistance / 100);
                break;
            case AttackType.True:
                actualDamage = incomingDamage;
                break;
        }

        return Mathf.Max(actualDamage, 0.05f * incomingDamage); // ���� ������� 5%�� ���Բ� ����
    }

    /// <summary>
    /// ���� ��ġ�� Ÿ�� ����
    /// </summary>
    protected virtual void UpdateCurrentTile()
    {
        Vector3 position = transform.position;
        Tile newTile = MapManager.Instance.GetTileAtPosition(position);

        if (newTile != CurrentTile)
        {
            CurrentTile = newTile;
        }
    }

    public virtual void AddAttackingEntity(ICombatEntity attacker)
    {
        if (!attackingEntities.Contains(attacker))
        {
            attackingEntities.Add(attacker);
        }
    }

    /// <summary>
    /// �� ��ü�� �����ϴ� ��ü ����Ʈ attackingEntites���� ��ü ����
    /// </summary>
    public virtual void RemoveAttackingEntity(ICombatEntity attacker)
    {
        attackingEntities.Remove(attacker);
    }


    protected virtual void Die()
    {
        // �������� ������ Ÿ�� ����
        foreach (ICombatEntity entity in attackingEntities)
        {
            entity.RemoveCurrentTarget();
        }
        OnDestroyed?.Invoke();
        RemoveAllCrowdControls();
        Destroy(gameObject);
    }

    protected virtual void InitializeHP()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
    }

    public virtual void TakeHeal(UnitEntity healer, AttackSource attackSource, float healAmount)
    {
        float oldHealth = CurrentHealth;
        CurrentHealth += healAmount; 
        float actualHealAmount = CurrentHealth - oldHealth; // ���� ����

        if (healer is MedicOperator medic && medic.BaseData.hitEffectPrefab != null)
        {
            PlayGetHitEffect(medic, attackSource);
        }
        
        ObjectPoolManager.Instance.ShowFloatingText(transform.position, actualHealAmount, true);


        if (healer is Operator healerOperator)
        {
            StatisticsManager.Instance.UpdateHealingDone(healerOperator, actualHealAmount);
        }
    }

    protected virtual void PlayGetHitEffect(UnitEntity attacker, AttackSource attackSource)
    {
        GameObject hitEffectPrefab;
        string attackerName;

        if (attacker is Operator op)
        {
            OperatorData opData = op.BaseData;
            hitEffectPrefab = opData.hitEffectPrefab;
            attackerName = opData.entityName;
        }
        else if (attacker is Enemy enemy)
        {
            EnemyData enemyData = enemy.BaseData;
            hitEffectPrefab = enemyData.hitEffectPrefab;
            attackerName = enemyData.entityName;
        }
        else
        {
            Debug.LogError("����Ʈ�� �߰����� ����");
            return;
        }

        if (hitEffectPrefab != null)
        {
            Vector3 effectPosition = transform.position;

            // Ǯ���� ����Ʈ ������Ʈ ��������
            string effectTag = attackerName + hitEffectPrefab.name;
            GameObject hitEffect = ObjectPoolManager.Instance.SpawnFromPool(effectTag, effectPosition, Quaternion.identity);

            // VFX ������Ʈ ���
            VisualEffect vfx = hitEffect.GetComponent<VisualEffect>();
            float effectLifetime = 1f;
            
            if (vfx != null)
            {
                // ���� ������Ƽ�� ����� ����Ʈ�� ������ ���
                if (vfx.HasVector3("AttackDirection"))
                {
                    Vector3 attackDirection = (transform.position - attackSource.Position).normalized;
                    vfx.SetVector3("AttackDirection", attackDirection);
                }

                if (vfx.HasFloat("LifeTime"))
                {
                    int lifeTimeID = Shader.PropertyToID("Lifetime");
                    effectLifetime = vfx.GetFloat(lifeTimeID);
                }

                vfx.Play();
            }

            StartCoroutine(ReturnEffectToPool(effectTag, hitEffect, effectLifetime));
        }
    }

    protected IEnumerator ReturnEffectToPool(string tag, GameObject effect, float lifeTime = 1f)
    {
        yield return new WaitForSeconds(lifeTime); // ����Ʈ�� ��Ÿ�� �ð��� ��� ��

        if (effect != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(tag, effect);
        }
    }

    public void ActivateShield(float amount) => shieldSystem.ActivateShield(amount);
    public void DeactivateShield() => shieldSystem.DeactivateShield();
    public float GetCurrentShield() => shieldSystem.CurrentShield;

    public virtual void AddCrowdControl(CrowdControl cc)
    {
        // �ϴ� ���� Ÿ���̸� ���� ����
        if (activeCC.Any(existing => existing.GetType() == cc.GetType())) return;

        activeCC.Add(cc);

        // CC �߰� �̺�Ʈ -> UI ������Ʈ � ���
        OnCrowdControlChanged?.Invoke(cc, true);
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
}
