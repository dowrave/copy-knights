using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab;
    public float spawnTime;
    public PathData pathData; // StageData에서는 경로 데이터를 직접 참조하도록 수정
}
