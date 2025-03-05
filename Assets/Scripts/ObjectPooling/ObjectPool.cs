using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject objectToPoll;
    [SerializeField] private int objectPollAmount;

    private Queue<GameObject> objectPool;

    [Header("Debugging")]
    [SerializeField, Tooltip("Enable this to display debug messages from this script in the Console.")]
    private bool showDebugMessages = false;
    [SerializeField, Tooltip("If enabled, debug messages will include the object's name as an identifier."), ShowIf(nameof(showDebugMessages))]
    private bool identifyObject = true;

    private void Awake()
    {
        objectPool = new Queue<GameObject>();
    }

    private void Start()
    {
        for (int i = 0; i < objectPollAmount; i++)
        {
            CreateNewObject();
        }
    }

    private GameObject CreateNewObject()
    {
        GameObject newObject = Instantiate(objectToPoll, parent: transform);
        objectPool.Enqueue(newObject);
        newObject.SetActive(false);
        Log($"Created new object: {newObject.name}");
        return newObject;
    }

    public GameObject GetObject()
    {
        GameObject poolObject;

        if (objectPool.Count > 0)
        {
            poolObject = objectPool.Dequeue();
            Log($"Reusing object from pool: {poolObject.name}");
        }
        else
        {
            poolObject = CreateNewObject();
        }

        IPooledObject pooledObjComponent = poolObject.GetComponent<IPooledObject>();
        if (pooledObjComponent == null)
        {
            Debug.LogError("Object does not have a PooledObject component.");
            return null;
        }

        pooledObjComponent.SetPool(this);

        poolObject.transform.SetParent(null);
        poolObject.SetActive(true);

        pooledObjComponent.StartObject();
        Log($"Activated object: {poolObject.name}");

        return poolObject;
    }

    public void ReturnObject(GameObject poolObject)
    {
        IPooledObject pooledObjComponent = poolObject.GetComponent<IPooledObject>();
        if (pooledObjComponent == null)
        {
            Debug.LogError("Object does not have a PooledObject component.");
        }
        pooledObjComponent.StopObject();

        poolObject.transform.SetParent(transform);

        poolObject.transform.localPosition = Vector3.zero;

        poolObject.SetActive(false);

        objectPool.Enqueue(poolObject);
        Log($"Returned object to pool: {poolObject.name}");
    }

    /// <summary>
    /// Logs a debug message to the Console if debugging is enabled.
    /// Includes the object's name as an identifier if 'identifyObject' is true.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    private void Log(string message)
    {
        if (showDebugMessages)
        {
            if (identifyObject)
                Debug.Log(message, this); // Includes object name in the log message.
            else
                Debug.Log(message); // Logs without object name.
        }
    }
}
