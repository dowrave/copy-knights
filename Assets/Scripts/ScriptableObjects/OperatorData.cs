using UnityEngine;

[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : ScriptableObject
{
    public string operatorName;
    public OperatorStats stats;

    // ICombatEntity 包访
    public AttackType attackType;
    public AttackRangeType attackRangeType;

    // Operator 加己
    public Vector2Int[] attackableTiles = { Vector2Int.zero };

    public GameObject projectilePrefab;

    // SP 包访
    public float maxSP = 30f;
    public float initialSP = 0f;
    public bool autoRecoverSP = true;

    public Sprite icon;
    public GameObject prefab;
}