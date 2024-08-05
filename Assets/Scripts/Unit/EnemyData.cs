using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "enemyDefault";
    public UnitStats baseStats; // Unit 클래스의 baseStats와 동일한 구조
    public float movementSpeed = 1f;
    public int blockCount = 1; // 차지하는 저지 수
    public GameObject prefab; // 적의 프리팹
    public Sprite icon; // UI에 표시될 아이콘

    // 적 특유의 추가 속성들
    public float damageMultiplier = 1f;
    public float defenseMultiplier = 1f;
    // 필요한 다른 특수 속성들...
}