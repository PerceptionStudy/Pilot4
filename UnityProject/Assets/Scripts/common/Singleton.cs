using UnityEngine;

/// <summary>
/// Be aware this will not prevent a non singleton constructor
///   such as `T myT = new T();`
/// To prevent that, add `protected T () {}` to your singleton class.
/// 
/// As a note, this is made as MonoBehaviour because we need Coroutines.
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T _instance;
	
	private static object _lock = new object();

	public static T Instance
	{
		get
		{
			if (applicationIsQuitting) 
			{
				Debug.LogWarning("Singleton object: " + _instance.GetInstanceID() + " was already destroyed on application quit");
				return null;
			}
			
			lock(_lock)
			{
				if (_instance == null)
				{
					GameObject gameObject = GameObject.Find("(singleton) "+ typeof(T).ToString());

					if (gameObject == null)
					{
						gameObject = new GameObject();
						gameObject.name = "(singleton) "+ typeof(T).ToString();
						_instance = gameObject.AddComponent<T>();

						Debug.Log("Create Singleton object: " + _instance.GetInstanceID());
					}
					else
					{
						_instance = gameObject.GetComponent<T>();
						Debug.Log("Retreive Singleton object: " + _instance.GetInstanceID());
					}
				}
				
				return _instance;
			}
		}
	}
	
	protected static bool applicationIsQuitting = false;
	/// <summary>
	/// When Unity quits, it destroys objects in a random order.
	/// In principle, a Singleton is only destroyed when application quits.
	/// If any script calls Instance after it have been destroyed, 
	///   it will create a buggy ghost object that will stay on the Editor scene
	///   even after stopping playing the Application. Really bad!
	/// So, this was made to be sure we're not creating that buggy ghost object.
	/// </summary>
	public void OnDestroy () 
	{
		applicationIsQuitting = true;
	}
}