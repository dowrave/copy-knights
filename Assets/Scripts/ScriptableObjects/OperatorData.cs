using UnityEngine;

[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : DeployableUnitData
{
    public new OperatorStats stats; 

    //ICombatEntity ����(�Ϻδ� stats�� ����)
    public AttackType attackType;
    public AttackRangeType attackRangeType;

    // Operator �Ӽ�
    public Vector2Int[] attackableTiles = { Vector2Int.zero }; // �������� ������ ����ü�� ���� ����(���� �� �־)
    
    public GameObject projectilePrefab; // ����ü ������ ����. null�̾ ����.

    // SP
    public float maxSP = 30f; // ��ų�� SP�� ��ġ�ϴ� �� ���� ��?
    public float initialSP = 0f; // �ʱ� SP
    public bool autoRecoverSP = true; // SP �ڵ� ȸ�� ����. ���� ������ ���� ȸ���ǰ� �� ���� �ִ�.
}