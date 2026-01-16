using UnityEngine;

[CreateAssetMenu(fileName = "New Unit Data", menuName = "Game/Unit Data")]
public class UnitData : ScriptableObject, IUnitData
{
    [SerializeField] protected string entityID = string.Empty; // 고유 식별자. 다른 에셋과 중복되면 안됨.
    [SerializeField] protected string entityNameLocalizationKey; // 화면에 표시될 이름을 로컬라이제이션 테이블에서 찾기 위한 키. 해당하는 언어를 불러오기 위한 키값이다.
    [SerializeField] protected UnitStats stats = default!;
    [SerializeField] protected GameObject prefab = default!;
    [SerializeField] protected Color primaryColor;
    [SerializeField] protected Color secondaryColor; // 필요한 경우만 사용

    public string EntityID => entityID;
    public string EntityNameLocalizationKey => entityNameLocalizationKey;
    public UnitStats Stats => stats;
    public GameObject Prefab => prefab;
    public Color PrimaryColor => primaryColor;
    public Color SecondaryColor => secondaryColor;

    // IUnitData 인터페이스 관련
    public UnitStats GetUnitStats() { return stats; }
}
