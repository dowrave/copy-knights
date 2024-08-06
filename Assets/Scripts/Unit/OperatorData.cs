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
    public GameObject prefab; // ���۷������� ������ ����
    public GameObject projectilePrefab; // ����ü ������ ����. null�̾ ����.

    // SP
    public float maxSP = 30f; // ��ų�� SP�� ��ġ�ϴ� �� ���� ��?
    public float initialSP = 0f; // �ʱ� SP
    public bool autoRecoverSP = true; // SP �ڵ� ȸ�� ����. ���� ������ ���� ȸ���ǰ� �� ���� �ִ�.
    public float SpRecoveryRate = 1f; // ���۷����͸��� �ٸ�

    public Sprite icon;

}