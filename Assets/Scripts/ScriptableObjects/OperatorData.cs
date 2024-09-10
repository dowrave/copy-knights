using UnityEngine;

[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : DeployableUnitData
{
    public new OperatorStats stats; 

    //ICombatEntity 관련(일부는 stats에 포함)
    public AttackType attackType;
    public AttackRangeType attackRangeType;

    // Operator 속성
    public Vector2Int[] attackableTiles = { Vector2Int.zero }; // 참조형은 안전상 구조체에 넣지 않음(변할 수 있어도)
    
    public GameObject projectilePrefab; // 투사체 프리팹 정보. null이어도 무관.

    // SP
    public float maxSP = 30f; // 스킬의 SP와 일치하는 게 맞을 듯?
    public float initialSP = 0f; // 초기 SP
    public bool autoRecoverSP = true; // SP 자동 회복 여부. 적을 공격할 때만 회복되게 할 수도 있다.
}