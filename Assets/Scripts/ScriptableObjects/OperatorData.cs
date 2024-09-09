using UnityEngine;

[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : DeployableUnitData
{
    //ICombatEntity ����
    public AttackType attackType;
    public AttackRangeType attackRangeType;
    public float attackPower;
    public float attackSpeed;
    public float attackRange; // ���Ÿ� ���� ��

    // Operator �Ӽ�
    public Vector2Int[] attackableTiles = { Vector2Int.zero };
    public int maxBlockableEnemies = 1;
    public GameObject projectilePrefab; // ����ü ������ ����. null�̾ ����.

    // SP
    public float maxSP = 30f; // ��ų�� SP�� ��ġ�ϴ� �� ���� ��?
    public float initialSP = 0f; // �ʱ� SP
    public bool autoRecoverSP = true; // SP �ڵ� ȸ�� ����. ���� ������ ���� ȸ���ǰ� �� ���� �ִ�.
    public float SpRecoveryRate = 1f; // ���۷����͸��� �ٸ�
}