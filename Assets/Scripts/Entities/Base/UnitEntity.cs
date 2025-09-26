using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using System;
using Skills.Base;

// Operator, Enemy, Barricade ���� Ÿ�� ���� ���ֵ�� ���õ� ��ƼƼ
public abstract class UnitEntity : MonoBehaviour, ITargettable, IFactionMember, ICrowdControlTarget
{
    public Faction Faction { get; protected set; }

    protected GameObject prefab;
    public GameObject Prefab => prefab;
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

    // �� ��ü�� �����ϴ� ��ƼƼ ���
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // �ݶ��̴��� �ڽ����� �и�
    // �θ� ������Ʈ���� �ݶ��̴��� ������ ���, ���� ���� �ݶ��̴� ó�� �ÿ� ������ �����
    // �ݶ��̴��� �뵵�� ���� �ڽ� ������Ʈ�� ���� �־� �Ѵ�.
    [SerializeField] protected BodyColliderController bodyColliderController;

    // �� ��ü�� ���� �ִ� �޽� ��������
    [SerializeField] protected Renderer primaryRenderer; 
    [SerializeField] protected Renderer secondaryRenderer; 
    protected MaterialPropertyBlock propBlock; // ��� �������� ���� ����

    // ���� ����
    protected List<Buff> activeBuffs = new List<Buff>();

    public ActionRestriction Restrictions { get; private set; } = ActionRestriction.None;

    // ICrowdControlTarget �������̽� ����
    // public virtual 
    public virtual float MovementSpeed { get; }
    public virtual void SetMovementSpeed(float newMovementSpeed) {}

    // ��ų ����
    protected HashSet<Vector2Int> currentSkillRange = new HashSet<Vector2Int>(); // ��ų ����
    public Vector2Int LastSkillCenter { get; protected set; } // ���������� ����� ��ų�� �߽� ��ġ. ���� ���� ���θ� �����ϱ� ���� �ʵ�.

    // ����Ʈ �±�
    protected string? meleeAttackEffectTag;
    protected string hitEffectTag = string.Empty;
    protected string muzzleTag = string.Empty;
    public string HitEffectTag => hitEffectTag;

    // �̺�Ʈ
    public event Action<float, float, float> OnHealthChanged = delegate { };
    public event Action<Buff, bool> OnBuffChanged = delegate { }; // onCrowdControlChanged ��ü 
    public event Action<UnitEntity> OnDeathAnimationCompleted = delegate { }; // ü���� ���� �׾��� �� �������� �̺�Ʈ ���� 
    public event Action<UnitEntity> OnDestroyed = delegate { }; // � ��ηε� �� ��ü�� �ı��� �� ����, ���� ���� ���ڴٸ� ��ø�� ������ �÷��׸� ����.

    protected virtual void Awake()
    {
        // �޽� ���� ����
        propBlock = new MaterialPropertyBlock();

        // ���� �ý��� ����
        shieldSystem = new ShieldSystem();
        shieldSystem.OnShieldChanged += (shield, onShieldDepleted) =>
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shield);
        };

        // �ݶ��̴��� ������ ������ �ڽ� Ŭ�����鿡�� �������� ������
    }

    public virtual void SetPrefab() { }

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
        if (primaryRenderer == null)
        {
            OnDeathAnimationCompleted?.Invoke(this);
            Destroy(gameObject);
            return;
        }

        // �������� ����� ���� ���� �ִϸ��̼��� �ϳ��� �׷����� ��� �����Ѵ�
        Sequence deathSequence = DOTween.Sequence();

        List<Material> materialInstances = new List<Material>();
        List<Renderer> renderers = new List<Renderer>();
        if (primaryRenderer != null) renderers.Add(primaryRenderer);
        if (secondaryRenderer != null) renderers.Add(secondaryRenderer);

        foreach (Renderer renderer in renderers)
        {
            // 1. ��Ƽ���� �ν��Ͻ��� ����� ������ ��Ƽ������ ����ϴ� �ٸ� ��ü�� ������ ���� �ʰ� �Ѵ�
            Material materialInstance = new Material(renderer.material);
            renderer.material = materialInstance;
            materialInstances.Add(materialInstance);

            // 2. ���� ������ ���� ��ȯ
            SetMaterialToTransparent(materialInstance);

            // 3. �� �������� ��Ƽ���� ���� ���̵� �ƿ� Ʈ���� ����
            Tween fadeTween = materialInstance.DOFade(0f, 0.2f);

            // 4. �������� ������. 
            deathSequence.Join(fadeTween);
        }

        deathSequence.OnComplete(() =>
        {
            OnDeathAnimationCompleted?.Invoke(this);

            foreach (Material mat in materialInstances)
            {
                Destroy(mat);
            }

            Destroy(gameObject);
        });

        // �������� ���� ������ ������ ��� �ִϸ��̼��� ��ϵ� -> DOTween�� �������� ������ -> �����Ŵ�̶�
        // �� �޼��尡 ȣ��Ǹ� ������ �����ų �ʿ� ���� ���� �����Ӻ��� ���۵ȴ�.
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
            
            if (actualHealAmount > 0)
            {
                ObjectPoolManager.Instance!.ShowFloatingText(transform.position, actualHealAmount, true);
            }
        }



        if (attackSource.Attacker is Operator healerOperator)
        {
            StatisticsManager.Instance!.UpdateHealingDone(healerOperator.OperatorData, actualHealAmount);
        }
    }

    protected virtual void AssignColorToRenderers(Color primaryColor, Color secondaryColor)
    {
        if (primaryRenderer != null)
        {
            primaryRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", primaryColor); // URP Lit ����
            primaryRenderer.SetPropertyBlock(propBlock);
        }
            
        if (secondaryRenderer != null)
        {
            secondaryRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", secondaryColor); // URP Lit ����
            secondaryRenderer.SetPropertyBlock(propBlock);
        }
    }

    protected virtual void PlayGetHitEffect(AttackSource attackSource)
    {
        GameObject sourceHitEffectPrefab = attackSource.HitEffectPrefab;
        string sourceHitEffectTag = attackSource.HitEffectTag;

        if (sourceHitEffectPrefab == null || sourceHitEffectTag == string.Empty) return;

        string attackerName;

        if (attackSource.Attacker is Operator op)
        {
            OperatorData opData = op.OperatorData;
            attackerName = opData.entityName;
            if (sourceHitEffectPrefab == null)
            {
                sourceHitEffectPrefab = opData.hitEffectPrefab;
            }
        }
        else if (attackSource.Attacker is Enemy enemy)
        {
            EnemyData enemyData = enemy.BaseData;
            attackerName = enemyData.EntityName;
            if (sourceHitEffectPrefab == null)
            {
                sourceHitEffectPrefab = enemyData.HitEffectPrefab;
            }
        }
        else
        {
            Debug.LogError("����Ʈ ����");
            return;
        }

        if (sourceHitEffectPrefab != null)
        {
            Vector3 effectPosition = transform.position;

            // Ǯ���� ����Ʈ ������Ʈ ��������
            if (sourceHitEffectTag == string.Empty)
            {
                Debug.LogWarning("[TakeDamage] hitEffectTag ���� null��");
            }

            GameObject? hitEffect = ObjectPoolManager.Instance!.SpawnFromPool(sourceHitEffectTag, effectPosition, Quaternion.identity);

            if (hitEffect != null)
            {
                CombatVFXController hitVFXController = hitEffect.GetComponent<CombatVFXController>();
                hitVFXController.Initialize(attackSource, this, sourceHitEffectTag);
            }
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
        // ������ ��� ���� �ɸ��� ���� ������ ���ŵ�
        if (buff is StunBuff stunBuff)
        {
            Buff existingStun = activeBuffs.FirstOrDefault(b => b is StunBuff);
            if (existingStun != null)
            {
                RemoveBuff(existingStun);
            }
        }

        activeBuffs.Add(buff);
        buff.OnApply(this, buff.caster);
        OnBuffChanged?.Invoke(buff, true); // �̺�Ʈ ȣ�� 
    }

    public void RemoveBuff(Buff buff)
    {
        if (activeBuffs.Contains(buff))
        {
            buff.OnRemove(); // ���� ����� �ٸ� �������� �ִٸ� ���⼭ ���� ���ŵ�
            if (activeBuffs.Remove(buff))
            {
                OnBuffChanged?.Invoke(buff, false);
            }
        }
    }

    // ���� �ߺ� ���� ������ ���� ���� Ÿ�� ���� �޼��� �߰�
    public bool HasBuff<T>() where T : Buff
    {
        return activeBuffs.Any(b => b is T);
    }

    public T? GetBuff<T>() where T : Buff
    {
        return activeBuffs.FirstOrDefault(b => b is T) as T;
    }

    protected virtual void RemoveAllBuffs()
    {
        foreach (var buff in activeBuffs.ToList())
        {
            RemoveBuff(buff);
        }
    }

    public virtual void RemoveBuffFromSourceSkill(OperatorSkill sourceSkill)
    {
        var buffsToRemove = activeBuffs.Where(b => b.SourceSkill == sourceSkill).ToList();
        foreach (var buff in activeBuffs.ToList())
        {
            RemoveBuff(buff);
        }
    }


    public virtual void TakeDamage(AttackSource source)
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

        // �ǰ� ����Ʈ ��� - ������, �±� ��� ���� ���� �����
        PlayGetHitEffect(source);

        // ����� �˾�
        if (source.ShowDamagePopup)
        {
            ObjectPoolManager.Instance.ShowFloatingText(transform.position, remainingDamage, false);
        }

        // �ǰ� ���� �߰� ����
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

    public void AddRestriction(ActionRestriction restirction)
    {
        Restrictions |= restirction; // ��Ʈ OR �������� �÷��� �߰�
    }

    public void RemoveRestriction(ActionRestriction restirction)
    {
        Restrictions &= ~restirction; // AND, NOT �������� �÷��� ����
    }
    public bool HasRestriction(ActionRestriction restirction)
    {
        return (Restrictions & restirction) != 0; // ��ġ�� ��Ʈ�� ������ true, ������ false.
    }

    // �ؽ� ������ �ްڴٴ� ����� �ִٸ� ���� �������̽��� ��ǲ�� ���� �ʿ�� ����
    public void SetCurrentSkillRange(HashSet<Vector2Int> range)
    {
        this.currentSkillRange = new HashSet<Vector2Int>(range);
    }

    public void SetLastSkillCenter(Vector2Int center)
    {
        LastSkillCenter = center;
    }

    public IReadOnlyCollection<Vector2Int> GetCurrentSkillRange()
    {
        return currentSkillRange;
    }

    public void ExecuteSkillSequence(IEnumerator skillCoroutine)
    {
        StartCoroutine(skillCoroutine);
    }


    public virtual void OnBodyTriggerEnter(Collider other) { }
    public virtual void OnBodyTriggerExit(Collider other) {}


    protected abstract float CalculateActualDamage(AttackType attacktype, float incomingDamage);
    public void ActivateShield(float amount) => shieldSystem.ActivateShield(amount);
    public void DeactivateShield() => shieldSystem.DeactivateShield();
    public float GetCurrentShield() => shieldSystem.CurrentShield;

    protected virtual void OnDestroy()
    {
        OnDestroyed?.Invoke(this);
    }

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
