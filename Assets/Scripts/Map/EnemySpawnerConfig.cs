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
    public PathData pathData = default!; // StageData에서는 경로 데이터를 직접 참조하도록 수정
    public GameObject prefab = default!; // 스폰되는 종류가 다양할 수 있기 때문에 EnemyData를 사용하지 않음

    [Tooltip("spawnType = Enemy일 때만 사용")]
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

