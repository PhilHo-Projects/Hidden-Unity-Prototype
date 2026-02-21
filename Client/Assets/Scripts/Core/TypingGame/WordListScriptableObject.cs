using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core
{
    [CreateAssetMenu(fileName = "New Word List", menuName = "Word List")]
    public class WordListScriptableObject : ScriptableObject
    {
        public List<string> words = new List<string>();
    }
}