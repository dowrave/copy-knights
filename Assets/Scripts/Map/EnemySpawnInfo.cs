using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public SpawnType spawnType;
    public float spawnTime;
    public PathData pathData; // StageData������ ��� �����͸� ���� �����ϵ��� ����
    public GameObject prefab; // �����Ǵ� ������ �پ��� �� �ֱ� ������ EnemyData�� ������� ����

    [Tooltip("spawnType = Enemy�� ���� ���")]
    public EnemyType enemyType;
}

public enum SpawnType
{
    Enemy,
    PathIndicator
}

public enum EnemyType
{
    Regular,
    Elite,
    Boss
}