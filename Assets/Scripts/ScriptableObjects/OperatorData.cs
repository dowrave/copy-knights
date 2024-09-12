using UnityEngine;

[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : ScriptableObject
{
    public string operatorName;
    public OperatorStats stats;

    // ICombatEntity ����
    public AttackType attackType;
    public AttackRangeType attackRangeType;

    // Operator �Ӽ�
    public Vector2Int[] attackableTiles = { Vector2Int.zero };

    public GameObject projectilePrefab;

    // SP ����
    public float maxSP = 30f;
    public float initialSP = 0f;
    public bool autoRecoverSP = true;

    public Sprite icon;
    public GameObject prefab;
}