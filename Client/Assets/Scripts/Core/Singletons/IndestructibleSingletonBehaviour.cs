using UnityEngine;

namespace Core
{
	public abstract class IndestructibleSingletonBehaviour<T> : SingletonBehaviour<T> where T : IndestructibleSingletonBehaviour<T>
	{
		protected override void Awake() 
		{
			// Make sure we don't keep our parents alive with DontDestroyOnLoad
			ReparentToRoot();
			DontDestroyOnLoad(gameObject);		
			base.Awake();	
		}
	
		protected virtual void ReparentToRoot() 
		{
#if UNITY_4_5 || UNITY_4_4 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0
		transform.parent = null;
#else
			//Unity 4.6+ only
			if (transform is RectTransform)
			{
				transform.SetParent(null, false);
			}
			else
			{
				transform.parent = null;
			}
#endif
		}
	}
}
