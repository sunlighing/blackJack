
using UnityEngine;

public abstract class SingletonDontDestroy<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T m_instance;

	public static T Instance
	{
		get
		{
			if ((Object)m_instance == (Object)null)
			{
				m_instance = (T)Object.FindObjectOfType(typeof(T));
				if (Object.FindObjectsOfType(typeof(T)).Length > 1)
				{
					Debug.LogError("[Singleton] Something went really wrong  - there should never be more than 1 singleton! Reopening the scene might fix it.");
					return m_instance;
				}
				if ((Object)m_instance == (Object)null)
				{
					m_instance = new GameObject(typeof(T).ToString()).AddComponent<T>();
				}
			}
			return m_instance;
		}
	}

	protected virtual void Awake()
	{
		if ((Object)m_instance != (Object)null && (Object)m_instance != (Object)(this as T))
		{
			Object.Destroy(base.gameObject);
			return;
		}
		m_instance = Instance;
		Object.DontDestroyOnLoad(base.gameObject);
	}
}
