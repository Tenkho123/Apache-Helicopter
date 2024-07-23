using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{   
    ///<Summary>
    /// This script belong to "Sasquatch B Studios"
    /// More details in this video <see href="https://www.youtube.com/watch?v=9O7uqbEe-xc">HERE</see>
    ///</Summary>
    
    public static List<PooledObjectInfo> objectPools = new();

    private GameObject _objectPoolEmptyHolder;

    private static GameObject _gameObjectsEmpty;

    public enum PoolType
    {
        Gameobject,
        None
    }
    public static PoolType poolType;

    private void Awake()
    {
        SetupEmpties();
    }   

    private void SetupEmpties()
    {
        _objectPoolEmptyHolder = new GameObject("Pool Objects");

        _gameObjectsEmpty = new GameObject("GameObjects");
        _gameObjectsEmpty.transform.SetParent(_objectPoolEmptyHolder.transform);
    }

    public static GameObject SpawnObject(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, PoolType poolType = PoolType.None)
    {
        PooledObjectInfo pool = objectPools.Find(p => p.lookupString == objectToSpawn.name);

        if (pool == null) {
            pool = new PooledObjectInfo() {lookupString = objectToSpawn.name};
            objectPools.Add(pool);
        }

        // Find if any available object in pool. If not, then create new one.
        GameObject spawnableObject = pool.inactiveObjects.FirstOrDefault();

        if (spawnableObject == null) {
            GameObject parentObject = SetParentObject(poolType);

            spawnableObject = Instantiate(objectToSpawn, spawnPosition, spawnRotation);

            if (spawnableObject != null) spawnableObject.transform.SetParent(parentObject.transform);
        }

        else {
            spawnableObject.transform.position = spawnPosition;
            spawnableObject.transform.rotation = spawnRotation;
            pool.inactiveObjects.Remove(spawnableObject);
            spawnableObject.SetActive(true);
        }

        return spawnableObject;
    }

    public static void ReturnObjectPool(GameObject obj)
    {
        string goName = obj.name.Substring(0, obj.name.Length - 7);

        PooledObjectInfo pool = objectPools.Find(p => p.lookupString == goName);

        if (pool == null) Debug.Log("No pool found to return object");
        else {
            obj.SetActive(false);
            pool.inactiveObjects.Add(obj);
        }
    }

    private static GameObject SetParentObject(PoolType poolType)
    {
        switch (poolType) {
            case PoolType.Gameobject:
                return _gameObjectsEmpty;
            
            case PoolType.None:
                return null;
            
            default:
                return null;
        }
    }
}

public class PooledObjectInfo
{
    public string lookupString;
    public List<GameObject> inactiveObjects = new();
}
