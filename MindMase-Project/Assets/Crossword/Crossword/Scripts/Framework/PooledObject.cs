using UnityEngine;
using System.Collections;

public class PooledObject : MonoBehaviour
{
	#region Properties

	/// <summary>
	/// The ObjectPool that this object belongs to
	/// </summary>
	public ObjectPool Pool { get; set; }

	#endregion
}
