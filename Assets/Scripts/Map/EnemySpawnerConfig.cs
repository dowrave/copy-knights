using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Enemy Spawner Config", menuName ="Game/Enemy Spawner Config")]
public class EnemySpawnerConfig : ScriptableObject
{
    public List<EnemySpawnData> spawnedEnemies = new List<EnemySpawnData>();
}


[System.Serializable]
public class EnemySpawnData
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

