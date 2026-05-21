using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script contains the list of objects, which should be pooled. When receiving the command, it returns the object. If the object is not on the list, it creates the new object.
/// </summary>
[System.Serializable]
public class PoolingObjects
{
    public GameObject pooledPrefab;
    public int count;
}

public class PoolingController : MonoBehaviour {

    [Tooltip("Your 'pooling' objects. Add new element and add the prefab to create the object prefab")]
    public PoolingObjects[] poolingObjectsClass;

    //The list where 'pooling' objects will be stored
    List<GameObject> pooledObjectsList = new List<GameObject>();

    public static PoolingController instance; //unique class instance for the easy access

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        CreateNewList();        //Create the new list of 'pooling' objects
    }

    void CreateNewList()
    {
        for (int i = 0; i < poolingObjectsClass.Length; i++)    //for each prefab create the needed amount of objects and deactivate them
        {
            for (int k = 0; k < poolingObjectsClass[i].count; k++)
            {
                GameObject newObj = Instantiate(poolingObjectsClass[i].pooledPrefab, transform);
                pooledObjectsList.Add(newObj);
                newObj.SetActive(false);                
            }
        }
    }

    
    public GameObject GetPoolingObject(GameObject prefab)   //Looking for the needed object by prefab name and return it
    {
        // 파괴된 프리팹 참조가 넘어온 경우 방어
        if (prefab == null) return null;

        string cloneName = GetCloneName(prefab);
        for (int i = pooledObjectsList.Count - 1; i >= 0; i--)      
        {
            // 풀 안의 오브젝트가 Destroy()로 파괴되었을 경우 무효 참조 제거
            if (pooledObjectsList[i] == null)
            {
                pooledObjectsList.RemoveAt(i);
                continue;
            }
            if (!pooledObjectsList[i].activeSelf && pooledObjectsList[i].name == cloneName)
            {                
                return pooledObjectsList[i];
            }
        }
        return AddNewObject(prefab);                        //if there is no object needed create the new one
    }

    GameObject AddNewObject(GameObject prefab)              //create the new object and add it to the list
    {
        if (prefab == null) return null;
        GameObject newObj = Instantiate(prefab, transform);
        pooledObjectsList.Add(newObj);
        newObj.SetActive(false);
        return newObj;
    }

    string GetCloneName(GameObject prefab)                  
    {
        return prefab.name + "(Clone)";
    }
}
