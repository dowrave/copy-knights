using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public List<EnemySpawnInfo> enemySpawnList = new List<EnemySpawnInfo>(); // 생성되는 적, 시간이 적혀 있음

    private bool isInitialized = false;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private Map currentMap;

    private Dictionary<string, PathData> pathDataDict = new Dictionary<string, PathData>();

    public void Initialize(Map map)
    {
        currentMap = map;
        startPoint = transform.position;
        endPoint = currentMap.FindEndPoint(); 
        isInitialized = true;

        LoadAllPathData();
    }

    private void LoadAllPathData()
    {
        PathData[] allPaths = Resources.LoadAll<PathData>("Paths"); // Resources/Paths 에 PathData 에셋들이 저장되어야 함
        foreach (var path in allPaths)
        {
            pathDataDict[path.name] = path;
        }
    }

    public void StartSpawning()
    {
        if (isInitialized)
        {
            StartCoroutine(SpawnEnemies());
        }
    }

    private IEnumerator SpawnEnemies()
    {
        if (enemySpawnList.Count == 0)
        {
            yield break;
        }
        float startTime = Time.time;
        foreach (var spawnInfo in enemySpawnList)
        {
            yield return new WaitForSeconds(spawnInfo.spawnTime - (Time.time - startTime));
            SpawnEnemy(spawnInfo);
        }
    }
    
    private void SpawnEnemy(EnemySpawnInfo spawnInfo)
    {
        if (spawnInfo.enemyPrefab == null) return;

        GameObject enemyObject = Instantiate(spawnInfo.enemyPrefab, startPoint, Quaternion.identity);
        Enemy enemy = enemyObject.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            EnemyData enemyData = enemy.Data;
            enemy.Initialize(enemyData, spawnInfo.pathData);
        }
        else
        {
            Destroy(enemyObject);
        }
    }

    private PathData GetPathData(string pathName)
    {
        if (pathDataDict.TryGetValue(pathName, out PathData pathData))
        {
            return pathData;
        }
        return null;
    }
}