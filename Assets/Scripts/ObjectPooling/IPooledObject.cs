using UnityEngine;

public interface IPooledObject
{
    public void SetPool(ObjectPool pool) { }

    public void ReturnToPoll() { }

    public void StartObject() { }
    public void StopObject() { }
}