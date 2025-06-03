using System.Collections;
using System; 
using UnityEngine;
using System.Linq;

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
            SortSpawnListByTime();
            StartCoroutine(SpawnEntities());
        }
    }

    private void SortSpawnListByTime()
    {
        if (spawnList.spawnedEnemies == null || spawnList.spawnedEnemies.Count == 0)
        {
            Debug.LogWarning("���� ����Ʈ�� �������");
            return;
        }

        // ���� �ð� �������� ����
        spawnList.spawnedEnemies = spawnList.spawnedEnemies
            .OrderBy(spawnData => spawnData.spawnTime)
            .ToList();
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
        // PathIndicator�� ������ �ڵ� ����
        if (spawnData.spawnType == SpawnType.PathIndicator)
        {
            spawnData.prefab = GameManagement.Instance.ResourceManager.PathIndicator;
            if (spawnData.prefab == null)
            {
                throw new InvalidOperationException("������ : ��� ǥ�ñ⸦ ã�� �� ����");
            }
        }

        // �Ϲ� Enemy�� ������ ���� ��Ȳ
        if (spawnData.prefab == null)
        {
            throw new InvalidOperationException("������ : �� �������� ����");
        }


        GameObject spawnedObject = Instantiate(spawnData.prefab, transform.position, Quaternion.identity);

        if (spawnedObject.TryGetComponent(out Enemy enemy))
        {
            if (spawnData.spawnType != SpawnType.Enemy)
            {
                Debug.LogError("spawnType�� Enemy�� �����Ǿ� ���� ����");
                return;
            }
            EnemyData enemyData = enemy.BaseData;
            enemy.Initialize(enemyData, spawnData.pathData);
        }
        else if (spawnedObject.TryGetComponent(out PathIndicator pathIndicator))
        {
            if (spawnData.spawnType != SpawnType.PathIndicator)
            {
                Debug.LogError("spawnType�� pathIndicator�� �����Ǿ� ���� ����");
                return;
            }
            pathIndicator.Initialize(spawnData.pathData);
        }
        else
        {
            Debug.LogError("�����տ� Enemy�� PathIndicator ������Ʈ�� ����");
        }
    }
}