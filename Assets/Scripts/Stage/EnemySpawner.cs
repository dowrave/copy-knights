using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public EnemyData enemyData;
        public float spawnTime;
    }

    public List<EnemySpawnInfo> enemySpawnList = new List<EnemySpawnInfo>(); // 생성되는 적, 시간이 적혀 있음

    private bool isInitialized = false;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private PathFindingManager pathFindingManager;
    private Map currentMap;

    public void Initialize(Map map)
    {
        currentMap = map;
        pathFindingManager = PathFindingManager.Instance;
        startPoint = transform.position;
        endPoint = currentMap.FindEndPoint(); 
        isInitialized = true;
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
        GameObject enemyObject = Instantiate(spawnInfo.enemyData.prefab, startPoint, Quaternion.identity);
        Enemy enemy = enemyObject.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            enemy.Initialize(spawnInfo.enemyData, startPoint, endPoint);
            SetEnemyPathAuto(enemy); // PathFindingManager를 이용한 자동 길찾기 
        }
        else
        {
            Destroy(enemyObject);
        }
    }

    private void SetEnemyPathAuto(Enemy enemy)
    {
        List<Vector3> path = pathFindingManager.FindPath(startPoint, endPoint); 

        if (path != null && path.Count > 0)
        {
            enemy.SetPath(path, new List<float>(new float[path.Count]));
        }
        else
        {
            Destroy(enemy.gameObject);
        }
    }

    //private void SetEnemyPath(Enemy enemy)
    //{
    //    List<Vector3> worldPath = new List<Vector3>();
    //    foreach (var node in path)
    //    {
    //        Vector3 worldPos = mapManager.GetTilePosition(node.position.x, node.position.y);
    //        worldPath.Add(worldPos);
    //    }
    //    enemy.SetPath(worldPath, path.Select(node => node.waitTime).ToList());
    //}

}