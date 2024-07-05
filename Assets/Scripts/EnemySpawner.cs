using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public GameObject enemyPrefab;
        public float spawnTime;
    }

    public List<EnemySpawnInfo> enemySpawnList; // 생성되는 적들

    private bool isInitialized = false; // 맵 생성 후에 true로 변경됨
    private Vector3 startPoint;
    private Vector3 endPoint;

    private MapManager mapManager;
    private PathFindingManager pathfindingManager;

    private void Start()
    {
        mapManager = FindObjectOfType<MapManager>();
        pathfindingManager = FindObjectOfType<PathFindingManager>();   
        // MapManager가 SetSpawnPosition을 호출할 때까지 대기
        StartCoroutine(WaitForInitialization());
    }

    // 맵 생성을 기다림
    private IEnumerator WaitForInitialization()
    {
        while (!isInitialized)
        {
            yield return null;
        }
        StartCoroutine(SpawnEnemies());
    }

    public void SetPathPoints(Vector3 start, Vector3 end)
    {
        startPoint = start;
        endPoint = end;
        transform.position = startPoint; // 시작점에 스포너 배치
        isInitialized = true;
    }

    private IEnumerator SpawnEnemies()
    {
        float startTime = Time.time;

        foreach (var spawnInfo in enemySpawnList)
        {
            yield return new WaitForSeconds(spawnInfo.spawnTime - (Time.time - startTime));
            SpawnEnemy(spawnInfo.enemyPrefab);
        }
    }

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        GameObject enemyObject = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        Enemy enemy = enemyObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            UnitStats stats = GenerateEnemyStats();
            float movementSpeed = 1f;

            enemy.Initialize(stats, movementSpeed, startPoint, endPoint);
        }
        else
        {
            Destroy(enemyObject);
        }
    }

    private UnitStats GenerateEnemyStats()
    {
        return new UnitStats
        {
            Health = 100,
            AttackPower = 10,
            Defense = 5,
            MagicResistance = 2,
            AttackSpeed = 1f
        };
    }
}
