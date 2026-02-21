// using System.Collections.Generic;
// using UnityEngine;
//
// namespace Core.Utility
// {
//     [CreateAssetMenu(fileName = "AnimationPrefabsContainer", menuName = "Animation/Prefab Container")]
//     public class AnimationPrefabsContainer : ScriptableObject
//     {
//         [SerializeField] private List<GameObject> prefabs = new List<GameObject>();
//         [SerializeField] private List<GameObject> specialEffectPrefabs = new List<GameObject>();
//
//         public List<GameObject> Prefabs => prefabs;
//         public List<GameObject> SpecialEffectPrefabs => specialEffectPrefabs;
//
//         public GameObject GetRandomPrefab()
//         {
//             if (prefabs == null || prefabs.Count == 0)
//                 return null;
//                 
//             int randomIndex = Random.Range(0, prefabs.Count);
//             return prefabs[randomIndex];
//         }
//
//         public GameObject GetRandomSpecialEffectPrefab()
//         {
//             if (specialEffectPrefabs == null || specialEffectPrefabs.Count == 0)
//                 return null;
//                 
//             int randomIndex = Random.Range(0, specialEffectPrefabs.Count);
//             return specialEffectPrefabs[randomIndex];
//         }
//
//         public GameObject GetPrefabAt(int index)
//         {
//             if (prefabs == null || index < 0 || index >= prefabs.Count)
//                 return null;
//                 
//             return prefabs[index];
//         }
//     }
// }