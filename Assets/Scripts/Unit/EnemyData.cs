using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "enemyDefault";
    public UnitStats baseStats; // Unit Ŭ������ baseStats�� ������ ����
    public float movementSpeed = 1f;
    public int blockCount = 1; // �����ϴ� ���� ��
    public GameObject prefab; // ���� ������

    // �� Ư���� �߰� �Ӽ���
    public float damageMultiplier = 1f;
    public float defenseMultiplier = 1f;
    // �ʿ��� �ٸ� Ư�� �Ӽ���...
}