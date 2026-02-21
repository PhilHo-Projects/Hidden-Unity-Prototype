using System.Collections;
using UnityEngine;

namespace Core
{
    public class LavaLampAnimation : MonoBehaviour
    {
        private readonly int _color1ID = Shader.PropertyToID("_Color1");
    
        private Material _material;
        private UnityEngine.UI.Image _image;
    
        [Header("Color Animation Settings")]
        [Range(0.1f, 20f)]
        public float colorTransitionDuration = 2f;
        [Range(0f, 20f)]
        public float delayBetweenTransitions = 1f;
    
        [Header("Color Constraints")]
        [Range(0f, 1f)]
        public float minSaturation = 0.3f;
        [Range(0f, 1f)]
        public float maxSaturation = 0.8f;
        
        [Range(0f, 1f)]
        public float minBrightness = 0.2f;
        [Range(0f, 1f)]
        public float maxBrightness = 0.8f;

        private void Awake()
        {
            _image = GetComponent<UnityEngine.UI.Image>();
            _material = new Material(_image.material);
            _image.material = _material;
        }

        private void Start()
        {
            StartCoroutine(ColorAnimation());
        }

        private Color GenerateConstrainedRandomColor()
        {
            float hue = Random.value;
            float saturation = Random.Range(minSaturation, maxSaturation);
            float value = Random.Range(minBrightness, maxBrightness);
            Color baseColor = Color.HSVToRGB(hue, saturation, value);
            return baseColor;
        }

        private IEnumerator ColorAnimation()
        {
            while (true)
            {
                Color startColor = _material.GetColor(_color1ID);
                Color targetColor = GenerateConstrainedRandomColor();
                float elapsedTime = 0f;

                while (elapsedTime < colorTransitionDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / colorTransitionDuration;
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);
                    _material.SetColor(_color1ID, Color.Lerp(startColor, targetColor, smoothT));
                    yield return null;
                }

                yield return new WaitForSeconds(delayBetweenTransitions);
            }
        }
    }
}
