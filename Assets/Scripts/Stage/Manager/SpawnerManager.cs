using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance { get; private set; }
    public List<EnemySpawner> spawners = new List<EnemySpawner>();
    private Map currentMap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
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

    private void FindAllSpawners()
    {
        spawners.Clear();
        EnemySpawner[] foundSpawners = FindObjectsOfType<EnemySpawner>();
        foreach (EnemySpawner spawner in foundSpawners)
        {
            spawners.Add(spawner);
            spawner.Initialize(currentMap); // 각 스포너에 mapManager 전달
        }
    }


    // 스테이지 매니저에서 실행
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
