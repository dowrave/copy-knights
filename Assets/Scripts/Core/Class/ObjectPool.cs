using UnityEngine;
using System.Collections.Generic;
    

[System.Serializable]
public class ObjectPool
{
    public string? tag;
    public GameObject? prefab;
    public int size;

    // 런타임 상태 관리
    private Queue<GameObject> _inactiveObjects = new Queue<GameObject>();
    private HashSet<GameObject> _activeObjects = new HashSet<GameObject>();

    public ObjectPool(string tag, GameObject prefab, int size)
    {
        this.tag = tag;
        this.prefab = prefab;
        this.size = size;

        for (int i = 0; i < size; i++)
        {
            GameObject obj = GameObject.Instantiate(prefab);
            _inactiveObjects.Enqueue(obj);
            obj.SetActive(false);
        }
    }

    public void AddPoolSize(int additionalSize)
    {
        for (int i=0; i < additionalSize; i ++)
        {
            GameObject obj = GameObject.Instantiate(prefab);
            _inactiveObjects.Enqueue(obj);
            obj.SetActive(false);
        }
        size += additionalSize;
    }

    public GameObject? SpawnFromPool(string tag, Vector3 position, Quaternion rotation, Transform? parent = null)
    {
        GameObject obj;

        // (if) 풀을 모두 사용했다면 새로 생성 / (else) 풀의 내용 사용
        if (_inactiveObjects.Count == 0)
        {
            obj = GameObject.Instantiate(prefab);
            size++;
        }
        else
        {
            obj = _inactiveObjects.Dequeue();
        }

        _activeObjects.Add(obj);
        return SetupPooledObject(obj, tag, position, rotation, parent);
    }

    // 오브젝트 활성화 및 생성
    private GameObject SetupPooledObject(GameObject obj, string tag, Vector3 position, Quaternion rotation, Transform? parent = null)
    {
        obj.SetActive(true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        if (parent != null)
        {
            obj.transform.SetParent(parent);
        }

        IPooledObject pooledObj = obj.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnObjectSpawn(tag);
        }

        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        // 큐에 넣은 뒤 비활성화
        _inactiveObjects.Enqueue(obj);
        if (_activeObjects.Contains(obj))
        {
            _activeObjects.Remove(obj);
        }
        obj.SetActive(false);

        // 풀 자체가 이미 비활성화된 경우는 외부에서 파괴
        // 아마 클래스로도 들어오지 않을 것 같긴 함
    }

    // 실제로 실행시킬 일은 없지만
    // 만약 파괴할 일이 있다면 자체적으로 Destroy 작업을 거친 후 외부에서 연결을 끊어주면 됨
    public void Clear()
    {
        // 1. 비활성 큐에 있는 모든 오브젝트 파괴
        while (_inactiveObjects.Count > 0)
        {
            GameObject obj = _inactiveObjects.Dequeue();
            if (obj != null) 
                GameObject.Destroy(obj);
        }

        // 2. 현재 활성화되어 활동 중인 오브젝트 파괴
        foreach (GameObject obj in _activeObjects)
        {
            if (obj != null) 
                GameObject.Destroy(obj);
        }
        
        // 3. 컬렉션 비우기
        _activeObjects.Clear();
    }
}