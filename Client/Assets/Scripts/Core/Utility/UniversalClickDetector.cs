using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class RaycastLayerDetector : MonoBehaviour
{
    public bool logClicks = true;
    
    // For UI detection
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;
    
    void Start()
    {
        // Get the EventSystem
        eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogWarning("No EventSystem found in the scene. UI click detection will not work.");
        }
    }
    
    void Update()
    {
        if (!logClicks) return;
        
        // Only detect on mouse down
        if (!Input.GetMouseButtonDown(0)) return;
        
        // Create a comprehensive report of everything under the click
        GenerateClickReport();
    }
    
    private void GenerateClickReport()
    {
        List<ClickedObject> allObjects = new List<ClickedObject>();
        
        // First, get all UI elements (sorted by render order)
        if (eventSystem != null)
        {
            pointerEventData = new PointerEventData(eventSystem);
            pointerEventData.position = Input.mousePosition;
            
            List<RaycastResult> uiResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, uiResults);
            
            foreach (RaycastResult result in uiResults)
            {
                // Get sorting info
                Canvas canvas = GetParentCanvas(result.gameObject);
                int sortingOrder = canvas ? canvas.sortingOrder : 0;
                int sortingLayerID = canvas ? canvas.sortingLayerID : 0;
                
                // Get graphic component to check if raycast blocking
                Graphic graphic = result.gameObject.GetComponent<Graphic>();
                bool blocksRaycast = graphic != null && graphic.raycastTarget;
                
                // Image transparency check
                Image image = result.gameObject.GetComponent<Image>();
                string transparency = "N/A";
                if (image != null)
                {
                    Color color = image.color;
                    transparency = $"Alpha: {color.a}";
                }
                
                allObjects.Add(new ClickedObject {
                    Name = result.gameObject.name,
                    Path = GetHierarchyPath(result.gameObject.transform),
                    Type = "UI",
                    Distance = result.distance,
                    SortingOrder = sortingOrder,
                    SortingLayerID = sortingLayerID,
                    IsActive = result.gameObject.activeInHierarchy,
                    IsRaycastTarget = blocksRaycast,
                    AdditionalInfo = transparency,
                    GameObject = result.gameObject
                });
            }
        }
        
        // Then, get all scene objects
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        
        foreach (RaycastHit hit in hits)
        {
            // Get renderer component for sorting info
            Renderer renderer = hit.collider.gameObject.GetComponent<Renderer>();
            int sortingOrder = renderer ? renderer.sortingOrder : 0;
            int sortingLayerID = renderer ? renderer.sortingLayerID : 0;
            
            allObjects.Add(new ClickedObject {
                Name = hit.collider.gameObject.name,
                Path = GetHierarchyPath(hit.collider.gameObject.transform),
                Type = "3D Object",
                Distance = hit.distance,
                SortingOrder = sortingOrder,
                SortingLayerID = sortingLayerID,
                IsActive = hit.collider.gameObject.activeInHierarchy,
                IsRaycastTarget = true, // 3D colliders always block raycasts
                AdditionalInfo = $"Hit point: {hit.point}",
                GameObject = hit.collider.gameObject
            });
        }
        
        // Sort all objects by distance/sorting order
        allObjects.Sort((a, b) => {
            // For UI, sorting order takes precedence
            if (a.Type == "UI" && b.Type == "UI")
            {
                // Higher sorting order is rendered on top (closer to viewer)
                if (a.SortingLayerID != b.SortingLayerID)
                    return b.SortingLayerID.CompareTo(a.SortingLayerID);
                if (a.SortingOrder != b.SortingOrder)
                    return b.SortingOrder.CompareTo(a.SortingOrder);
                // If same sorting order, use distance
                return a.Distance.CompareTo(b.Distance);
            }
            
            // For mix of UI and 3D, UI is generally on top
            if (a.Type == "UI" && b.Type == "3D Object")
                return -1;
            if (a.Type == "3D Object" && b.Type == "UI")
                return 1;
                
            // For 3D objects, sort by distance
            return a.Distance.CompareTo(b.Distance);
        });
        
        // Generate the report
        if (allObjects.Count > 0)
        {
            Debug.Log("========= CLICK RAYCAST REPORT (Foreground to Background) =========");
            
            for (int i = 0; i < allObjects.Count; i++)
            {
                ClickedObject obj = allObjects[i];
                Debug.Log($"[{i+1}] {obj.Type}: {obj.Name}\n" +
                          $"   Path: {obj.Path}\n" +
                          $"   Active: {obj.IsActive}, Blocks Raycast: {obj.IsRaycastTarget}\n" +
                          $"   Sorting Order: {obj.SortingOrder}, Distance: {obj.Distance}\n" +
                          $"   {obj.AdditionalInfo}");
                
                // For input fields, show additional info
                InputField inputField = obj.GameObject.GetComponent<InputField>();
                if (inputField != null)
                {
                    Debug.Log($"   InputField Properties: \n" +
                              $"   - Interactable: {inputField.interactable}\n" +
                              $"   - Navigation Mode: {inputField.navigation.mode}\n" +
                              $"   - Selectable: {(inputField as Selectable != null)}\n" +
                              $"   - Selected: {EventSystem.current.currentSelectedGameObject == obj.GameObject}");
                }
                
                // Show components
                Component[] components = obj.GameObject.GetComponents<Component>();
                Debug.Log($"   Components:");
                foreach (Component component in components)
                {
                    if (component is Graphic graphic)
                    {
                        Debug.Log($"   - {component.GetType().Name} (RaycastTarget: {graphic.raycastTarget}, Color Alpha: {graphic.color.a})");
                    }
                    else
                    {
                        Debug.Log($"   - {component.GetType().Name}");
                    }
                }
                
                // Add a separator
                if (i < allObjects.Count - 1)
                {
                    Debug.Log("   -----------------------------");
                }
            }
        }
        else
        {
            Debug.Log("========= CLICK RAYCAST REPORT =========\nNo objects found under click position.");
        }
    }
    
    // Helper method to get full path of object in the hierarchy
    private string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
    
    // Helper to get the parent canvas
    private Canvas GetParentCanvas(GameObject obj)
    {
        Canvas canvas = obj.GetComponent<Canvas>();
        if (canvas != null) return canvas;
        
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            canvas = parent.GetComponent<Canvas>();
            if (canvas != null) return canvas;
            parent = parent.parent;
        }
        
        return null;
    }
    
    // Class to store object data for sorting and reporting
    private class ClickedObject
    {
        public string Name;
        public string Path;
        public string Type; // "UI" or "3D Object"
        public float Distance;
        public int SortingOrder;
        public int SortingLayerID;
        public bool IsActive;
        public bool IsRaycastTarget;
        public string AdditionalInfo;
        public GameObject GameObject;
    }
    
    // Public method to toggle logging from other scripts if needed
    public void ToggleLogging(bool enable)
    {
        logClicks = enable;
    }
}
