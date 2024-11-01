using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Description")]
    public string stageId; // 1-1, 1-2, 1-3 등등
    public string stageName; // 스테이지 부제
    public string stageDetail; // 스테이지 설명

    [Header("Stage Functional Elements")]
    public GameObject mapPrefab;
    public List<EnemySpawnerData> spawnerData;
    public float playerHealthMultiplier = 1f;
    public float enemyStatMultiplier = 1f;

    [System.Serializable]
    public class EnemySpawnerData
    {
        public Vector3 position;
        public List<EnemySpawnInfo> enemySpawnList;
    }

    [System.Serializable]
    public class EnemySpawnInfo
    {
        public GameObject enemyPrefab;
        public float spawnTime;
        public string pathName; // 경로의 이름 (맵에서 정의된 경로와 연결)
    }
}