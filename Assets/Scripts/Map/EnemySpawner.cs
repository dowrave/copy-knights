using System.Collections;
using System.Collections.Generic;
using System; 
using UnityEngine;
using System.Linq;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Config")]
    [SerializeField] private EnemySpawnerConfig spawnerConfig;

    // config에서 가져와서 spawner에서 관리하는 리스트
    private List<SpawnData> spawnList;
    public IReadOnlyList<SpawnData> SpawnList => spawnList;

    private bool isInitialized = false;

    public void Initialize()
    {
        spawnList = new List<SpawnData>(spawnerConfig.SpawnedEnemies);
        InitializeSpawnList();
        isInitialized = true;
        // Logger.Log("Spawner 초기화 동작 완료");
    }

    public void StartSpawning()
    {
        if (isInitialized)
        {
            StartCoroutine(SpawnEntities());
        }
    }

    private void InitializeSpawnList()
    {
        if (spawnerConfig.SpawnedEnemies == null || spawnerConfig.SpawnedEnemies.Count == 0)
        {
            Logger.LogWarning("스폰 리스트가 비어있음");
            return;
        }

        // 스폰 시간 기준으로 정렬 (SO를 구성할 때 대체로 시간 순서로 구현하지만 혹시나 해서)
        spawnList = spawnerConfig.SpawnedEnemies
            .OrderBy(spawnData => spawnData.SpawnTime)
            .ToList();
    }

    private IEnumerator SpawnEntities()
    {
        if (spawnList.Count == 0)
        {
            yield break;
        }
        float startTime = Time.time;


        foreach (var spawnData in spawnList)
        {
            yield return new WaitForSeconds(spawnData.SpawnTime - (Time.time - startTime));

            Spawn(spawnData);
        }
    }

    private void Spawn(SpawnData spawnData)
    {
        if (spawnData.SpawnType == SpawnType.None)
        {
            throw new InvalidOperationException("스포너 : spawnData에 할당된 스폰 타입이 None임");
        }
        else
        {
            string tag;
            GameObject spawnedObject;

            // 태그를 가져오는 위치가 달라서 구분함
            if (spawnData.SpawnType == SpawnType.PathIndicator)
            {
                tag = ObjectPoolManager.PathIndicatorTag;
                spawnedObject = ObjectPoolManager.Instance!.SpawnFromPool(tag, transform.position, Quaternion.identity);
                if (spawnedObject.TryGetComponent(out PathIndicator pathIndicator))
                {
                    pathIndicator.Initialize(spawnData.PathData);
                }
            }
            else if (spawnData.SpawnType == SpawnType.Enemy)
            {
                tag = spawnData.EnemyData.UnitTag;
                spawnedObject = ObjectPoolManager.Instance!.SpawnFromPool(tag, transform.position, Quaternion.identity);
                if (spawnedObject.TryGetComponent(out Enemy enemy))
                {
                    enemy.Initialize(spawnData.EnemyData, spawnData.PathData);
                }
            }
            else
            {
                throw new InvalidOperationException("[EnemySpawner]tag값이 정상적으로 설정되지 않음");
            }
        }
    }
}