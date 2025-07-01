using UnityEngine;
using UnityEngine.UI;

public class TrailTester : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private float toggleInterval = 5f;
    [SerializeField] private float testIntensity = 3f;
    
    private Image imageComponent;
    private Material materialInstance;
    private bool trailEnabled = false;
    private float timer = 0f;
    
    void Start()
    {
        // Get the image component
        imageComponent = GetComponent<Image>();
        
        if (imageComponent != null && imageComponent.material != null)
        {
            // Create material instance
            materialInstance = new Material(imageComponent.material);
            imageComponent.material = materialInstance;
            
            // Start with trail disabled
            materialInstance.SetFloat("_TrailEnabled", 0f);
            materialInstance.SetFloat("_TrailIntensity", 0f);
            
            Debug.Log("TrailTester initialized - Trail OFF");
        }
        else
        {
            Debug.LogError("No Image component or material found!");
        }
    }
    
    void Update()
    {
        if (materialInstance == null) return;
        
        timer += Time.deltaTime;
        
        if (timer >= toggleInterval)
        {
            timer = 0f;
            ToggleTrail();
        }
    }
    
    void ToggleTrail()
    {
        trailEnabled = !trailEnabled;
        
        if (trailEnabled)
        {
            // Turn ON
            materialInstance.SetFloat("_TrailEnabled", 1f);
            materialInstance.SetFloat("_TrailIntensity", testIntensity);
            Debug.Log("Trail turned ON");
        }
        else
        {
            // Turn OFF
            materialInstance.SetFloat("_TrailEnabled", 0f);
            materialInstance.SetFloat("_TrailIntensity", 0f);
            Debug.Log("Trail turned OFF");
        }
    }
    
    void OnDestroy()
    {
        // Clean up material instance
        if (materialInstance != null)
        {
            DestroyImmediate(materialInstance);
        }
    }
}
