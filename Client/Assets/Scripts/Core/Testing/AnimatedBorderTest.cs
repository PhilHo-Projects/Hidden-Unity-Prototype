using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AnimatedBorderTest : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool loopAnimation = true;
    
    [Header("Dot Settings")]
    [SerializeField] private Color dotColor = Color.white;
    [SerializeField] private float dotSize = 0.1f;
    [SerializeField] private float dotIntensity = 3f;
    [SerializeField] private float borderOffset = 0.05f;
    
    private Image imageComponent;
    private Material materialInstance;
    private Tweener currentTween;
    
    void Start()
    {
        // Get the Image component
        imageComponent = GetComponent<Image>();
        if (imageComponent == null)
        {
            Debug.LogError("No Image component found on " + gameObject.name);
            return;
        }
        
        // Create a material instance to avoid affecting other objects
        if (imageComponent.material != null)
        {
            materialInstance = new Material(imageComponent.material);
            imageComponent.material = materialInstance;
        }
        else
        {
            Debug.LogError("No material assigned to Image component");
            return;
        }
        
        // Set initial shader properties
        UpdateShaderProperties();
        
        // Start animation if auto-start is enabled
        if (autoStart)
        {
            StartAnimation();
        }
    }
    
    void UpdateShaderProperties()
    {
        if (materialInstance != null)
        {
            materialInstance.SetColor("_DotColor", dotColor);
            materialInstance.SetFloat("_DotSize", dotSize);
            materialInstance.SetFloat("_DotIntensity", dotIntensity);
            materialInstance.SetFloat("_BorderOffset", borderOffset);
        }
    }
    
    public void StartAnimation()
    {
        if (materialInstance == null) return;
        
        // Stop any existing animation
        StopAnimation();
        
        // Start the dot progress animation
        if (loopAnimation)
        {
            currentTween = materialInstance.DOFloat(1f, "_DotProgress", animationSpeed)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear);
        }
        else
        {
            currentTween = materialInstance.DOFloat(1f, "_DotProgress", animationSpeed)
                .SetEase(Ease.Linear);
        }
    }
    
    public void StopAnimation()
    {
        if (currentTween != null)
        {
            currentTween.Kill();
            currentTween = null;
        }
        
        // Reset progress to 0
        if (materialInstance != null)
        {
            materialInstance.SetFloat("_DotProgress", 0f);
        }
    }
    
    void OnValidate()
    {
        // Update shader properties when values change in inspector
        if (materialInstance != null)
        {
            UpdateShaderProperties();
        }
    }
    
    void OnDestroy()
    {
        // Clean up
        StopAnimation();
        if (materialInstance != null)
        {
            DestroyImmediate(materialInstance);
        }
    }
}
