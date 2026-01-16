#nullable enable
using UnityEngine;

[CreateAssetMenu(fileName = "New Deployable Unit Data", menuName = "Game/Deployable Unit Data")]
public class DeployableUnitData : ScriptableObject, IDeployableData
{
    // UnitData
    [SerializeField] protected string entityID = string.Empty; // 고유 식별자. 다른 에셋과 중복되면 안됨.
    [SerializeField] protected string entityNameLocalizationKey; // 화면에 표시될 이름을 로컬라이제이션 테이블에서 찾기 위한 키. 해당하는 언어를 불러오기 위한 키값이다.
    [SerializeField] protected DeployableUnitStats stats;
    [SerializeField] protected GameObject prefab = default!;
    [SerializeField] protected Color primaryColor;
    [SerializeField] protected Color secondaryColor; // 필요한 경우만 사용

    // DeployableUnitData
    [SerializeField] protected Sprite? icon; // null일 수 있다고 하겠음
    [SerializeField] protected bool canDeployOnGround = false;
    [SerializeField] protected bool canDeployOnHill = false;
    [SerializeField] protected float cooldownTime = 0f; // 배치 후 쿨다운 시간

    protected string _unitTag;

    public void CreateObjectPools() { }
    // public string UnitTag => _unitTag ??= $"DeployableUnit_{entityID}";
    public string EntityID => entityID;
    public string EntityNameLocalizationKey => entityNameLocalizationKey;
    public DeployableUnitStats Stats => stats;
    public GameObject Prefab => prefab;
    public Color PrimaryColor => primaryColor;
    public Color SecondaryColor => secondaryColor;

    public Sprite? Icon => icon;
    public bool CanDeployOnGround => canDeployOnGround;
    public bool CanDeployOnHill => canDeployOnHill;
    public float CooldownTime => cooldownTime; 

    public string UnitTag
    {
        get
        {
            if (string.IsNullOrEmpty(_unitTag))
                _unitTag = $"DeployableUnit_{entityID}";

            return _unitTag;
        }
    }
}



#nullable restore