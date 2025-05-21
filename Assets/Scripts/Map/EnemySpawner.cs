using System.Collections;
using System; 
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
        // PathIndicator는 프리팹 자동 지정
        if (spawnData.spawnType == SpawnType.PathIndicator)
        {
            spawnData.prefab = GameManagement.Instance.ResourceManager.PathIndicator;
            if (spawnData.prefab == null)
            {
                throw new InvalidOperationException("스포너 : 경로 표시기를 찾을 수 없음");
            }
        }

        // 일반 Enemy는 없으면 오류 상황
        if (spawnData.prefab == null)
        {
            throw new InvalidOperationException("스포너 : 적 프리팹이 없음");
        }


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
        }
    }
}