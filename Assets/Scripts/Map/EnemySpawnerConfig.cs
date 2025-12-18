using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Enemy Spawner Config", menuName ="Game/Enemy Spawner Config")]
public class EnemySpawnerConfig : ScriptableObject
{
    [SerializeField] protected List<SpawnData> spawnedEnemies;

    public IReadOnlyList<SpawnData> SpawnedEnemies => spawnedEnemies;
}


[System.Serializable]
public class SpawnData
{
    [SerializeField] protected SpawnType spawnType;
    [SerializeField] protected float spawnTime = 0f;
    [SerializeField] protected PathData pathData = default!; // StageData에서는 경로 데이터를 직접 참조하도록 수정

    [Header("Enemy일 때만 사용")]
    [SerializeField] protected EnemyData? enemyData; 

    public SpawnType SpawnType => spawnType;
    public float SpawnTime => spawnTime; 
    public PathData PathData => pathData;
    public EnemyData? EnemyData => enemyData;
}

public enum SpawnType
{
    None,
    Enemy,
    PathIndicator
}

