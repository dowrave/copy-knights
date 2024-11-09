using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance { get; private set; }
    private List<EnemySpawner> spawners = new List<EnemySpawner>();
    private Map currentMap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(Map map)
    {
        currentMap = map;
        FindAllSpawners();
    }

    /// <summary>
    /// �ʿ� �ִ� �����ʵ��� ã�Ƽ� �ڵ����� �����
    /// </summary>
    private void FindAllSpawners()
    {
        spawners.Clear();

        // Map ã��
        Map currentMap = MapManager.Instance?.CurrentMap; 
        if (currentMap != null)
        {
            // FindObjectsOfType�̳� GetComponentsInChildren�̳� ��� ����Ʈ�� ã��
            EnemySpawner[] foundSpawners = currentMap.GetComponentsInChildren<EnemySpawner>();
            foreach (EnemySpawner spawner in foundSpawners)
            {
                spawners.Add(spawner);
                spawner.Initialize();
            }
        }

    }


    // �������� �Ŵ������� ����
    public void StartSpawning()
    {
        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.StartSpawning();
            }
            else
            {
                Debug.LogWarning("Null EnemySpawner found in the list.");
            }
        }
    }

}
