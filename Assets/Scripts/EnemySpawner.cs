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
        public GameObject enemyPrefab;
        public float spawnTime;
    }

    //[System.Serializable]
    //public class PathNode
    //{
    //    public Vector2Int position;
    //    public float waitTime;
    //}

    public List<EnemySpawnInfo> enemySpawnList = new List<EnemySpawnInfo>(); // �����Ǵ� ��, �ð��� ���� ����
    //public List<PathNode> path = new List<PathNode>();

    private bool isInitialized = false;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private PathFindingManager pathFindingManager;
    private MapManager mapManager;

    public void Initialize(MapManager manager)
    {
        Debug.Log("EnemySpawner Initialize �۵� ����");
        mapManager = manager;
        pathFindingManager = PathFindingManager.Instance;

        startPoint = transform.position;
        endPoint = mapManager.GetEndPoint(); 

        isInitialized = true;
    }


    public void StartSpawning()
    {
        Debug.Log("EnemySpawner : �� ���� ����");
        if (isInitialized)
        {
            Debug.Log("EnemySpawner : initialize��");
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
            Debug.Log("SpawnEnemies �۵� ��..");
            yield return new WaitForSeconds(spawnInfo.spawnTime - (Time.time - startTime));
            SpawnEnemy(spawnInfo.enemyPrefab);
        }
    }
    
    private void SpawnEnemy(GameObject enemyPrefab)
    {
        Debug.Log("SpawnEnemy �۵�");
        if (enemyPrefab == null) return;
        Debug.Log("EnemyPrefab�� Null�� �ƴ�");

        GameObject enemyObject = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        Enemy enemy = enemyObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            UnitStats Stats = GenerateEnemyStats();
            float movementSpeed = 1f;
            enemy.Initialize(Stats, movementSpeed, startPoint, endPoint);
            SetEnemyPathAuto(enemy); // PathFindingManager�� �̿��� �ڵ� ��ã�� 
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