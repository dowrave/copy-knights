using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public SpawnType spawnType;
    public float spawnTime = 0f;
    public PathData pathData = default!; // StageData������ ��� �����͸� ���� �����ϵ��� ����
    public GameObject prefab = default!; // �����Ǵ� ������ �پ��� �� �ֱ� ������ EnemyData�� ������� ����

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