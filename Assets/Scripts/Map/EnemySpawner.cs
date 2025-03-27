using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public EnemySpawnerConfig spawnList;
    private bool isInitialized = false;

    public void Initialize()
    {
        isInitialized = true;
    }


    public void StartSpawning()
    {
        if (isInitialized)
        {
            StartCoroutine(SpawnEntities());
        }
    }

    private IEnumerator SpawnEntities()
    {
        if (spawnList.spawnedEnemies.Count == 0)
        {
            yield break;
        }
        float startTime = Time.time;


        foreach (var spawnData in spawnList.spawnedEnemies)
        {
            yield return new WaitForSeconds(spawnData.spawnTime - (Time.time - startTime));

            Spawn(spawnData);
        }
    }

    private void Spawn(EnemySpawnData spawnData)
    {
        if (spawnData.prefab == null) return;

        GameObject spawnedObject = Instantiate(spawnData.prefab, transform.position, Quaternion.identity);

        if (spawnedObject.TryGetComponent(out Enemy enemy))
        {
            if (spawnData.spawnType != SpawnType.Enemy)
            {
                Debug.LogError("spawnType이 Enemy로 지정되어 있지 않음");
                return;
            }
            EnemyData enemyData = enemy.BaseData;
            enemy.Initialize(enemyData, spawnData.pathData);
        }
        else if (spawnedObject.TryGetComponent(out PathIndicator pathIndicator))
        {
            if (spawnData.spawnType != SpawnType.PathIndicator)
            {
                Debug.LogError("spawnType이 pathIndicator로 지정되어 있지 않음");
                return;
            }
            pathIndicator.Initialize(spawnData.pathData);
        }
        else
        {
            Debug.LogError("프리팹에 Enemy나 PathIndicator 컴포넌트가 없음");
            Destroy(gameObject);
        }
    }
}