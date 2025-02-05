using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public List<EnemySpawnInfo> enemySpawnList = new List<EnemySpawnInfo>(); // �����Ǵ� ��, �ð��� ���� ����
    private bool isInitialized = false;

    public void Initialize()
    {
        
        isInitialized = true;
    }


    public void StartSpawning()
    {
        Debug.Log("EnemySpawner : StartSpawning �޼��� ����");

        if (isInitialized)
        {
            Debug.Log("EnemySpawner : ���� ����");
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

        // ��ġ�� enemy �ʱ�ȭ���� �ٽ� �����
        GameObject enemyObject = Instantiate(spawnInfo.enemyPrefab, transform.position, Quaternion.identity);
        Enemy enemy = enemyObject.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            EnemyData enemyData = enemy.BaseData;
            enemy.Initialize(enemyData, spawnInfo.pathData);
        }
        else
        {
            Destroy(enemyObject);
        }
    }
}