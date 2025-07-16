using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using System;

// Operator, Enemy, Barricade ���� Ÿ�� ���� ���ֵ�� ���õ� ��ƼƼ
public abstract class UnitEntity : MonoBehaviour, ITargettable, IFactionMember, ICrowdControlTarget
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

    // �� ��ü�� �����ϴ� ��ƼƼ ��� : �� ��ü�� ��ȭ�� ������ �� �˸��� ���� �ʿ���(���, ���� ���)
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // �ݶ��̴��� �ڽ����� �и�
    // �θ� ������Ʈ���� �ݶ��̴��� ������ ���, ���� ���� �ݶ��̴� ó�� �ÿ� ������ �����
    // �ݶ��̴��� �뵵�� ���� �ڽ� ������Ʈ�� ���� �־� �Ѵ�.
    [SerializeField] protected BodyColliderController bodyColliderController;

    // ���� ����
    protected List<Buff> activeBuffs = new List<Buff>();

    // ICrowdControlTarget �������̽� ����
    // public virtual 
    public virtual float MovementSpeed { get; }
    public virtual void SetMovementSpeed(float newMovementSpeed) {}

    // �̺�Ʈ
    public event Action<float, float, float> OnHealthChanged = delegate { };
    public event Action<CrowdControl, bool> OnCrowdControlChanged = delegate { };
    // public event Action<UnitEntity> OnDestroyed = delegate { };
    public event Action<UnitEntity> OnDeathAnimationCompleted = delegate { };

    protected virtual void Awake()
    {
        // �ݶ��̴��� ������ ������ �ڽ� Ŭ�����鿡�� �������� ������

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

    protected virtual void Update()
    {
        foreach (var buff in activeBuffs.ToArray())
        {
            buff.OnUpdate();
        }
    }

    // �� ��ü�� �����ϴ� ���� ����
    public virtual void RemoveAttackingEntity(ICombatEntity attacker)
    {
        attackingEntities.Remove(attacker);
    }


    protected virtual void Die()
    {
        PlayDeathAnimation();
    }

    protected void PlayDeathAnimation()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();

        // ������ ��Ƽ������ ����ϴ� ��� ��ü�� ����Ǵ� �� ������ ��Ƽ���� �ν��Ͻ��� ����� �����Ѵ�.
        if (renderer != null)
        {
            Material materialInstance = new Material(renderer.material);
            renderer.material = materialInstance;

            SetMaterialToTransparent(materialInstance);

            // DOTween ����Ͽ� �������� ���� �� ���������� �ִϸ��̼� ����
            // materialInstance.DOColor(Color.black, 0f);
            materialInstance.DOFade(0f, 0.2f).OnComplete(() =>
            {
                OnDeathAnimationCompleted?.Invoke(this); // ����� ������ �˸��� �̺�Ʈ
                // OnDestroyed?.Invoke(this); // ���� �̺�Ʈ�� ����
                Destroy(materialInstance); // �޸� ���� ����
                RemoveAllCrowdControls();
                Destroy(gameObject);
            });
        }
        else
        {
            // �������� ��� �ݹ�� �ı��� ����ȴ�.
            OnDeathAnimationCompleted?.Invoke(this);
            // OnDestroyed?.Invoke(this);
            RemoveAllCrowdControls();
            Destroy(gameObject);
        }
    }

    // ��Ƽ������ �����ϰ� �����ϴ� �޼��� (URP Lit�� ���ٰ� ����)
    private void SetMaterialToTransparent(Material material)
    {
        // URP Lit ���̴��� Transparent ���� ����
        material.SetFloat("_Surface", 1f);      // 1 = Transparent
        material.SetFloat("_Blend", 0f);        // 0 = Alpha
        material.SetFloat("_AlphaClip", 0f);    // ���� Ŭ���� ��Ȱ��ȭ

        // ���� ��� ����
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);       // ���� ���� ��Ȱ��ȭ

        // ���� ť�� ���� ��ü������ ����
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // Ű���� ����
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        material.DisableKeyword("_ALPHATEST_ON");
    }

    protected abstract void InitializeHP();

    public virtual void TakeHeal(AttackSource attackSource)
    {
        float oldHealth = CurrentHealth;
        CurrentHealth += attackSource.Damage;
        float actualHealAmount = Mathf.FloorToInt(CurrentHealth - oldHealth); // ���� ����

        if (attackSource.Attacker is MedicOperator medic && medic.OperatorData.hitEffectPrefab != null)
        {
            PlayGetHitEffect(attackSource);
        }

        ObjectPoolManager.Instance!.ShowFloatingText(transform.position, actualHealAmount, true);


        if (attackSource.Attacker is Operator healerOperator)
        {
            StatisticsManager.Instance!.UpdateHealingDone(healerOperator.OperatorData, actualHealAmount);
        }
    }

    protected virtual void PlayGetHitEffect(AttackSource attackSource)
    {
        GameObject hitEffectPrefab = attackSource.HitEffectPrefab;
        string hitEffectTag = attackSource.HitEffectTag;
        string attackerName;

        if (attackSource.Attacker is Operator op)
        {
            OperatorData opData = op.OperatorData;
            attackerName = opData.entityName;
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = opData.hitEffectPrefab;
            }
        }
        else if (attackSource.Attacker is Enemy enemy)
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
        for (int i = activeCC.Count - 1; i >= 0; i--)
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

    public void AddBuff(Buff buff)
    {
        activeBuffs.Add(buff);
        buff.OnApply(this, buff.caster);
    }

    public void RemoveBuff(Buff buff)
    {
        buff.OnRemove();
        activeBuffs.Remove(buff);
    }

    // ���� �ߺ� ���� ������ ���� ���� Ÿ�� ���� �޼��� �߰�
    public bool HasBuff<T>() where T : Buff
    {
        return activeBuffs.Any(b => b is T);
    }


    public virtual void TakeDamage(AttackSource source, bool playGetHitEffect = true)
    {
        // ���� ü���� 0 ���϶�� ������� �ʴ´�
        // �ߺ��ؼ� ����Ǵ� ��츦 ������
        if (CurrentHealth <= 0) return;

        // ���� / ���� ���׷��� ����� ���� ������ �����
        float actualDamage = Mathf.Floor(CalculateActualDamage(source.Type, source.Damage));

        // ���带 ��� ���� �����
        float remainingDamage = shieldSystem.AbsorbDamage(actualDamage);

        // ü�� ���
        CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shieldSystem.CurrentShield);

        // �ǰ� ����Ʈ ���
        if (playGetHitEffect)
        {
            PlayGetHitEffect(source);
        }

        OnDamageTaken(source.Attacker, actualDamage);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void OnDamageTaken(UnitEntity attacker, float actualDamage) { } // �ǰ� �ÿ� �߰��� ������ �� ���� �� ����� �޼��� 

    // �ݶ��̴��� Ȱ��ȭ ���� ����
    protected virtual void SetColliderState(bool enabled)
    {
        if (bodyColliderController != null) bodyColliderController.SetColliderState(enabled);
    } 

    public virtual void OnBodyTriggerEnter(Collider other) {}
    public virtual void OnBodyTriggerExit(Collider other) {}


    protected abstract float CalculateActualDamage(AttackType attacktype, float incomingDamage);
    public void ActivateShield(float amount) => shieldSystem.ActivateShield(amount);
    public void DeactivateShield() => shieldSystem.DeactivateShield();
    public float GetCurrentShield() => shieldSystem.CurrentShield;

    // �ڽ� Ŭ�������� ������ ���� 
    // ������ ����ȯ��Ű�� �ʱ� ���ؼ� UnitEntity���� �����ص�
    public virtual float AttackPower
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public virtual float AttackSpeed // ���� ��ٿ�
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public virtual float Defense
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public virtual float MagicResistance
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public virtual AttackType AttackType
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
}
