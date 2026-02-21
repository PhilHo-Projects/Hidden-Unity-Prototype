using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "KeySpritesData", menuName = "Keyboard/Key Sprites Data")]
    public class KeySpritesData : ScriptableObject
    {
        public Sprite[] KeySprites;
    }
}