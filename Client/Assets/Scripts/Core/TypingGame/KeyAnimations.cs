using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

namespace Core
{
    public class KeyAnimations : MonoBehaviour
    {
        [SerializeField] private Image[] keyImages;
        private Dictionary<string, Image> _keyImageDict = new Dictionary<string, Image>();
        private Vector3 _baseScale;

        private void Start()
        {
            foreach (Image keyImage in keyImages)
            {
                _keyImageDict[keyImage.name] = keyImage;
            }

            if (keyImages.Length > 0)
            {
                _baseScale = keyImages[0].transform.localScale;
            }
        }

        private void OnGUI()
        {
            Event ev = Event.current;
            if (ev.isKey)
            {
                string keyName = ev.keyCode.ToString();

                if (_keyImageDict.TryGetValue(keyName, out Image keyImage))
                {
                    if (ev.type == EventType.KeyDown)
                    {
                        keyImage.transform
                            .DOScale(_baseScale * 1.2f, 0.05f)
                            .SetEase(Ease.OutQuad);
                    }
                    else if (ev.type == EventType.KeyUp)
                    {
                        keyImage.transform
                            .DOScale(_baseScale, 0.05f)
                            .SetEase(Ease.InQuad);
                    }
                }
            }
        }
    }
}