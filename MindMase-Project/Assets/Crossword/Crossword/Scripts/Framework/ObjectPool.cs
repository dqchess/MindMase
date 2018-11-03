using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPool
{
	#region Member Variables

	private GameObject			objectPrefab	= null;
	private List<PooledObject>	pooledObjects	= new List<PooledObject>();
	private Transform			poolContainer	= null;

	#endregion

	#region Public Methods

	public ObjectPool(GameObject objectPrefab, int initialSize, Transform parent = null)
	{
		this.objectPrefab = objectPrefab;

		poolContainer = new GameObject("object_pool").transform;

		poolContainer.SetParent(parent);

		for (int i = 0; i < initialSize; i++)
		{
			CreateObject();
		}
	}

	/// <summary>
	/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
	/// Objects are returned to the pool by setting their active state to false.
	/// </summary>
	public GameObject GetObject(Transform parent)
	{
		GameObject obj = null;

		if (poolContainer.childCount > 0)
		{
			obj = poolContainer.GetChild(0).gameObject;
		}

		if (obj == null)
		{
			obj = CreateObject();
		}

		obj.transform.SetParent(parent, false);

		return obj;
	}

	/// <summary>
	/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
	/// Objects are returned to the pool by setting their active state to false.
	/// </summary>
	public T GetObject<T>(Transform parent) where T : Component
	{
		return GetObject(parent).GetComponent<T>();
	}

	/// <summary>
	/// Sets all instantiated GameObjects to de-active
	/// </summary>
	public void ReturnAllObjectsToPool()
	{
		for (int i = 0; i < pooledObjects.Count; i++)
		{
			pooledObjects[i].transform.SetParent(poolContainer, false);
		}
	}

	/// <summary>
	/// Destroies all objects.
	/// </summary>
	public void DestroyAllObjects()
	{
		for (int i = 0; i < pooledObjects.Count; i++)
		{
			pooledObjects[i].Pool = null;
			GameObject.Destroy(pooledObjects[i].gameObject);
		}

		pooledObjects.Clear();
		GameObject.Destroy(poolContainer.gameObject);
	}

	/// <summary>
	/// Returns the GameObject to its ObjectPool
	/// </summary>
	public static void ReturnObjectToPool(GameObject gameObject)
	{
		PooledObject pooledObject = gameObject.GetComponent<PooledObject>();

		if (pooledObject != null)
		{
			pooledObject.Pool.ReturnObjectToPool(pooledObject);
		}
		else
		{
			Debug.LogError("[ObjectPool] This object is not part of any pool");
		}
	}

	/// <summary>
	/// Returns the GameObject to its ObjectPool
	/// </summary>
	public void ReturnObjectToPool(PooledObject pooledObject)
	{
		if (pooledObjects.Contains(pooledObject))
		{
			pooledObject.transform.SetParent(poolContainer, false);
		}
		else
		{
			Debug.LogError("[ObjectPool] This object is not part of this object pool");
		}
	}

	#endregion

	#region Private Methods

	private GameObject CreateObject()
	{
		PooledObject pooledObject = GameObject.Instantiate(objectPrefab).AddComponent<PooledObject>();

		pooledObject.Pool = this;
		pooledObject.transform.SetParent(poolContainer, false);
		pooledObjects.Add(pooledObject);

		return pooledObject.gameObject;
	}

	#endregion
}
