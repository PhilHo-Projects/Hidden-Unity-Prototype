using System.Collections.Generic;
using UnityEngine;

namespace Core.Utility
{
    [CreateAssetMenu(fileName = "VFXPrefabContainer", menuName = "VFX/Prefab Container")]
    public class VFXPrefabContainer : ScriptableObject
    {
        [SerializeField] private List<GameObject> prefabs = new List<GameObject>();
        public List<GameObject> Prefabs => prefabs;

        public GameObject GetRandomPrefab()
        {
            if (prefabs == null || prefabs.Count == 0) return null;
            int randomIndex = UnityEngine.Random.Range(0, prefabs.Count);
            return prefabs[randomIndex];
        }
    }
}