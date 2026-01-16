using System.Collections.Generic;
using UnityEngine;
using Skills.Base;

// 필수 필드는 ?이 없고, 선택 필드는 ?이 있다 
[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : ScriptableObject, ICombatData, IDeployableData
{
    // UnitData
    [SerializeField] protected string entityID = string.Empty; // 고유 식별자. 다른 에셋과 중복되면 안됨.
    [SerializeField] protected string entityNameLocalizationKey; // 화면에 표시될 이름을 로컬라이제이션 테이블에서 찾기 위한 키. 해당하는 언어를 불러오기 위한 키값이다.
    [SerializeField] protected OperatorStats stats; 
    [SerializeField] protected GameObject prefab = default!;
    [SerializeField] protected Color primaryColor;
    [SerializeField] protected Color secondaryColor; // 필요한 경우만 사용

    // DeployableUnitData
    [SerializeField] protected Sprite? icon; // 오퍼레이터에 사용될 그림. null일 수 있다
    [SerializeField] protected bool canDeployOnGround = false;
    [SerializeField] protected bool canDeployOnHill = false;

    // OperatorData
    [SerializeField] protected OperatorClass operatorClass;
    [SerializeField] protected AttackType attackType;
    [SerializeField] protected AttackRangeType attackRangeType;
    [SerializeField] protected List<Vector2Int> attackableTiles = new List<Vector2Int>{ Vector2Int.zero };
    [SerializeField] protected OperatorSkill elite0Skill = default!; // 최초 스킬
    [SerializeField] protected float initialSP = 0f;

    [Header("VFXs")] 
    [SerializeField] protected GameObject deployEffectPrefab = default!; // 배치 이펙트
    [SerializeField] protected GameObject? meleeAttackEffectPrefab; // 근접 공격 이펙트(공격 시도 시 발생)
    [SerializeField] protected GameObject hitEffectPrefab = default!; // 공격이 적중했을 때의 이펙트
    [Tooltip("원거리 공격일 때 발사하는 객체에서 나오는 총구 효과. 거의 안 쓸 듯")]
    [SerializeField] protected GameObject muzzleVFXPrefab = default!; 

    [Header("For Ranged")]
    [SerializeField] protected GameObject? projectilePrefab;

    [Header("Level Up Stats")]
    [SerializeField] protected OperatorLevelStats levelStats = default!; // 레벨 1이 올라갈 때마다 상승하는 스탯량

    [Header("Elite Phase Settings")]
    [SerializeField] protected ElitePhaseUnlocks elite1Unlocks = default!;

    [Header("Promotion Required Items")]
    [SerializeField] protected List<ItemWithCount> promotionItems = default!;

    // 이 유닛 자체 풀은 StageManager에서 준비함, 여기선 유닛이 사용하는 오브젝트들만 준비함
    public virtual void CreateObjectPools()
    {
        if (projectilePrefab != null)
        {
            ObjectPoolManager.Instance.CreatePool(ProjectileTag, projectilePrefab, 5);
        }
        if (deployEffectPrefab != null)
        {
            ObjectPoolManager.Instance.CreatePool(DeployVFXTag, deployEffectPrefab, 1);
        }
        if (meleeAttackEffectPrefab != null)
        {
            ObjectPoolManager.Instance.CreatePool(MeleeAttackVFXTag, meleeAttackEffectPrefab, 3);
        }
        if (hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance.CreatePool(HitVFXTag, hitEffectPrefab, 3);
        }
        if (muzzleVFXPrefab != null)
        {
            ObjectPoolManager.Instance.CreatePool(MuzzleVFXTag, muzzleVFXPrefab, 3);
        }
    }

    protected string _unitTag;
    protected string _projectileTag;
    protected string _deployVFXTag;
    protected string _meleeAttackVFXTag;
    protected string _hitVFXTag;
    protected string _muzzleVFXTag;


    [System.Serializable]
    public class OperatorLevelStats
    {
        public float healthPerLevel;
        public float attackPowerPerLevel;
        public float defensePerLevel;
        public float magicResistancePerLevel;
    }

    [System.Serializable]
    public class ElitePhaseUnlocks
    {
        [Header("Attack Range Changes")]
        public List<Vector2Int>? additionalAttackTiles;

        [Header("New Skills")]
        public OperatorSkill? unlockedSkill;
    }

    // 프로퍼티들
    public string EntityID => entityID;
    public string EntityNameLocalizationKey => entityNameLocalizationKey;
    public OperatorStats Stats => stats;
    public GameObject Prefab => prefab;
    public Color PrimaryColor => primaryColor;
    public Color SecondaryColor => secondaryColor;

    public Sprite? Icon => icon;
    public bool CanDeployOnGround => canDeployOnGround;
    public bool CanDeployOnHill => canDeployOnHill;

    public OperatorClass OperatorClass => operatorClass;
    public AttackType AttackType => attackType;
    public AttackRangeType AttackRangeType => attackRangeType;
    public IReadOnlyList<Vector2Int> AttackableTiles => attackableTiles;
    public OperatorSkill Elite0Skill => elite0Skill;
    public float InitialSP => initialSP;

    public GameObject DeployEffectPrefab => deployEffectPrefab;
    public GameObject MeleeAttackEffectPrefab => meleeAttackEffectPrefab;
    public GameObject HitEffectPrefab => hitEffectPrefab;
    public GameObject MuzzleVFXPrefab => muzzleVFXPrefab;

    public GameObject ProjectilePrefab => projectilePrefab;

    public OperatorLevelStats LevelStats => levelStats;

    public ElitePhaseUnlocks Elite1Unlocks => elite1Unlocks;

    public IReadOnlyList<ItemWithCount> PromotionItems => promotionItems;

    // 태그 프로퍼티
    public string UnitTag
    {
        get
        {
            if (string.IsNullOrEmpty(_unitTag))
            {
                _unitTag = $"Operator_{entityID}";
            }
            return _unitTag;
        }
    }
    public string ProjectileTag
    {
        get
        {
            if (string.IsNullOrEmpty(_projectileTag))
            {
                _projectileTag = $"{entityID}_Projectile";
            }
            return _projectileTag;
        }
    }
    public string DeployVFXTag
    {
        get
        {
            if (string.IsNullOrEmpty(_deployVFXTag))
            {
                _deployVFXTag = $"{entityID}_DeployVFX";
            }
            return _deployVFXTag;
        }
    }
    public string MeleeAttackVFXTag
    {
        get
        {
            if (string.IsNullOrEmpty(_meleeAttackVFXTag))
            {
                _meleeAttackVFXTag = $"{entityID}_MeleeAttackVFX";
            }
            return _meleeAttackVFXTag;
        }
    }
    public string HitVFXTag
    {
        get
        {
            if (string.IsNullOrEmpty(_hitVFXTag))
            {
                _hitVFXTag = $"Operator_{entityID}_HitVFX";
            }
            return _hitVFXTag;
        }
    }
    public string MuzzleVFXTag
    {
        get
        {
            if (string.IsNullOrEmpty(_muzzleVFXTag))
            {
                _muzzleVFXTag = $"{entityID}_MuzzleVFX";
            }
            return _muzzleVFXTag;
        }
    }
}

