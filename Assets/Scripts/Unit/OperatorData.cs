using UnityEngine;

[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : ScriptableObject
{
    public string operatorName;
    public UnitStats baseStats;
    public Vector2Int[] attackableTiles = { Vector2Int.zero };
    public bool canDeployGround;
    public bool canDeployHill;
    public int maxBlockableEnemies = 1;

    public float deploymentCost;
    public float reDeployTime = 70f;

    public AttackRangeType attackRangeType;
    public GameObject prefab; // 오퍼레이터의 프리팹 정보
    public GameObject projectilePrefab; // 투사체 프리팹 정보. null이어도 무관.

    // SP
    public float maxSP = 30f; // 스킬의 SP와 일치하는 게 맞을 듯?
    public float initialSP = 0f; // 초기 SP
    public bool autoRecoverSP = true; // SP 자동 회복 여부. 적을 공격할 때만 회복되게 할 수도 있다.
    public float SpRecoveryRate = 1f; // 오퍼레이터마다 다름

    public Sprite icon;

}