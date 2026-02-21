using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core
{
    public class KeyboardLayoutCreator : MonoBehaviour
    {
        [SerializeField] private KeySpritesData spritesData;
        [SerializeField] private Image keyPrefab;
        [SerializeField] private RectTransform keyboardContainer;

        private readonly string[][] _keyboardLayout = new string[][]
        {
            new[] { "BackQuote", "Alpha1", "Alpha2", "Alpha3", "Alpha4", "Alpha5", "Alpha6", "Alpha7", "Alpha8", "Alpha9", "Alpha0", "Minus", "Equals", "Backspace" },
            new[] { "Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "LeftBracket", "RightBracket", "Backslash" },
            new[] { "CapsLock", "A", "S", "D", "F", "G", "H", "J", "K", "L", "Semicolon", "Quote", "Return" },
            new[] { "LeftShift", "Z", "X", "C", "V", "B", "N", "M", "Comma", "Period", "Slash", "RightShift" },
            new[] { "LeftControl", "LeftAlt", "Space", "RightAlt", "RightControl" }
        };

        private void Start()
        {
            CreateKeyboardLayout();
            SaveKeyboardAsPrefab();
        }

        private void CreateKeyboardLayout()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            float keyboardWidth = screenWidth * 0.8f;
            float keyboardHeight = screenHeight * 0.4f;
            
            float baseKeyWidth = keyboardWidth / 15f;
            float baseKeyHeight = keyboardHeight / 5f;
            
            float bottomMargin = screenHeight * 0.2f;
            keyboardContainer.anchoredPosition = new Vector2(0, bottomMargin);
            
            float currentY = 0;
            
            for (int row = 0; row < _keyboardLayout.Length; row++)
            {
                string[] keys = _keyboardLayout[row];
                List<RectTransform> rowKeys = new List<RectTransform>();
                float rowWidth = 0;

                for (int col = 0; col < keys.Length; col++)
                {
                    string keyName = keys[col];
                    float keyWidthMultiplier = GetKeyWidthMultiplier(keyName);

                    Image keyImage = Instantiate(keyPrefab, keyboardContainer);
                    keyImage.preserveAspect = true;
                    RectTransform keyRect = keyImage.rectTransform;
                    
                    keyRect.sizeDelta = new Vector2(baseKeyWidth * keyWidthMultiplier, baseKeyHeight);
                    
                    if (spritesData.KeySprites.Any(s => s.name == keyName))
                    {
                        Sprite keySprite = spritesData.KeySprites.First(s => s.name == keyName);
                        keyImage.sprite = keySprite;
                        
                        AdjustKeySize(keyRect, keySprite, baseKeyWidth * keyWidthMultiplier, baseKeyHeight);
                    }

                    keyImage.name = keyName;
                    rowWidth += keyRect.sizeDelta.x;
                    rowKeys.Add(keyRect);
                }

                float currentX = -rowWidth / 2;
                foreach (RectTransform keyRect in rowKeys)
                {
                    keyRect.anchoredPosition = new Vector2(currentX + (keyRect.sizeDelta.x / 2), -currentY - (keyRect.sizeDelta.y / 2));
                    currentX += keyRect.sizeDelta.x;
                }
                
                currentY += baseKeyHeight;
            }

            keyboardContainer.sizeDelta = new Vector2(keyboardWidth, currentY);
            keyboardContainer.anchoredPosition = new Vector2(0, bottomMargin);
        }

        private float GetKeyWidthMultiplier(string keyName)
        {
            switch (keyName.ToLower())
            {
                case "space": return 6.2f;
                case "backspace":
                case "capslock":
                case "leftshift":
                case "rightshift": return 2f;
                case "tab":
                case "return": return 1.5f;
                default: return 1f;
            }
        }

        private void AdjustKeySize(RectTransform keyRect, Sprite keySprite, float maxWidth, float maxHeight)
        {
            float spriteAspect = keySprite.rect.width / keySprite.rect.height;
            float keyAspect = maxWidth / maxHeight;
            
            if (spriteAspect > keyAspect)
            {
                keyRect.sizeDelta = new Vector2(maxWidth, maxWidth / spriteAspect);
            }
            else
            {
                keyRect.sizeDelta = new Vector2(maxHeight * spriteAspect, maxHeight);
            }
        }

        private void SaveKeyboardAsPrefab()
        {
#if UNITY_EDITOR
            string localPath = "Assets/KeyboardPrefab.prefab";
            localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
            PrefabUtility.SaveAsPrefabAsset(keyboardContainer.gameObject, localPath);
            Debug.Log("Keyboard prefab created at " + localPath);
#endif
        }
    }
}
