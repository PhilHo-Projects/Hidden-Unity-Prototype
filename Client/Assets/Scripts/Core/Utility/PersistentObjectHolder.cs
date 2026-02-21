using UnityEngine;

namespace Core.Utility
{
    public class PersistentObjectHolder : MonoBehaviour
    {
        void Start()
        {
            DontDestroyOnLoad(this);
        }
    }
}
