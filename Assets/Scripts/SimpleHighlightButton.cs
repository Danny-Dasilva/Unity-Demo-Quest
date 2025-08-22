using UnityEngine;
using UnityEngine.EventSystems;

namespace QuestCameraTools.App
{
    public class SimpleHighlightButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Target Configuration")]
        [SerializeField] private SimplePartHighlighter targetHighlighter;
        [SerializeField] private GameObject targetObject;
        [SerializeField] private string targetObjectName = "456";
        
        [Header("Auto-Find Settings")]
        [SerializeField] private bool autoFindTarget = true;
        [SerializeField] private bool searchInBuildingBlock = true;
        
        [Header("Visual Feedback")]
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material pressedMaterial;
        [SerializeField] private Renderer buttonRenderer;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        private float pressAnimationTime = 0.2f;
        private float pressTimer = 0f;
        private bool isPressed = false;
        
        private void Start()
        {
            // Get button renderer if not assigned
            if (buttonRenderer == null)
            {
                buttonRenderer = GetComponent<Renderer>();
            }
            
            // Try to find the target
            if (autoFindTarget && targetHighlighter == null)
            {
                FindTarget();
            }
            
            // Ensure highlighter is set up
            if (targetHighlighter == null && targetObject != null)
            {
                SetupHighlighter(targetObject);
            }
        }
        
        private void Update()
        {
            // Handle button press animation
            if (isPressed)
            {
                pressTimer -= Time.deltaTime;
                if (pressTimer <= 0)
                {
                    isPressed = false;
                    RestoreButtonVisual();
                }
            }
        }
        
        private void FindTarget()
        {
            if (showDebugLogs) Debug.Log($"[SimpleHighlightButton] Searching for target: {targetObjectName}");
            
            // First try to find in BuildingBlock if specified
            if (searchInBuildingBlock)
            {
                GameObject[] buildingBlocks = GameObject.FindObjectsOfType<GameObject>();
                foreach (var obj in buildingBlocks)
                {
                    if (obj.name.Contains("BuildingBlock") || obj.name.Contains("Cube"))
                    {
                        // Search children for 456 object
                        Transform found456 = FindChildByName(obj.transform, targetObjectName);
                        if (found456 != null)
                        {
                            targetObject = found456.gameObject;
                            if (showDebugLogs) 
                                Debug.Log($"[SimpleHighlightButton] Found {targetObjectName} under {obj.name}");
                            break;
                        }
                    }
                }
            }
            
            // If not found, search globally
            if (targetObject == null)
            {
                GameObject found = GameObject.Find(targetObjectName);
                if (found != null)
                {
                    targetObject = found;
                    if (showDebugLogs) 
                        Debug.Log($"[SimpleHighlightButton] Found {targetObjectName} globally");
                }
            }
            
            // Set up highlighter on found object
            if (targetObject != null)
            {
                SetupHighlighter(targetObject);
            }
            else
            {
                Debug.LogWarning($"[SimpleHighlightButton] Could not find object named '{targetObjectName}'");
            }
        }
        
        private Transform FindChildByName(Transform parent, string name)
        {
            // Check direct match
            if (parent.name.Contains(name))
                return parent;
            
            // Search children recursively
            foreach (Transform child in parent)
            {
                Transform result = FindChildByName(child, name);
                if (result != null)
                    return result;
            }
            
            return null;
        }
        
        private void SetupHighlighter(GameObject target)
        {
            targetHighlighter = target.GetComponent<SimplePartHighlighter>();
            
            if (targetHighlighter == null)
            {
                targetHighlighter = target.AddComponent<SimplePartHighlighter>();
                if (showDebugLogs) 
                    Debug.Log($"[SimpleHighlightButton] Added SimplePartHighlighter to {target.name}");
            }
            else
            {
                if (showDebugLogs) 
                    Debug.Log($"[SimpleHighlightButton] Found existing SimplePartHighlighter on {target.name}");
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            PerformHighlight();
        }
        
        public void PerformHighlight()
        {
            if (targetHighlighter == null)
            {
                Debug.LogWarning("[SimpleHighlightButton] No target highlighter set!");
                
                // Try to find it one more time
                if (autoFindTarget)
                {
                    FindTarget();
                }
                
                if (targetHighlighter == null)
                    return;
            }
            
            // Cycle to next part
            targetHighlighter.CycleHighlight();
            
            // Visual feedback
            ShowPressAnimation();
            
            if (showDebugLogs)
            {
                int currentIndex = targetHighlighter.GetCurrentIndex();
                int totalParts = targetHighlighter.GetPartCount();
                var partNames = targetHighlighter.GetPartNames();
                
                if (currentIndex < partNames.Count)
                {
                    Debug.Log($"[SimpleHighlightButton] Highlighted part {currentIndex + 1}/{totalParts}: {partNames[currentIndex]}");
                }
            }
        }
        
        private void ShowPressAnimation()
        {
            isPressed = true;
            pressTimer = pressAnimationTime;
            
            if (buttonRenderer != null && pressedMaterial != null)
            {
                buttonRenderer.material = pressedMaterial;
            }
        }
        
        private void RestoreButtonVisual()
        {
            if (buttonRenderer != null && normalMaterial != null)
            {
                buttonRenderer.material = normalMaterial;
            }
        }
        
        public void SetTarget(GameObject target)
        {
            targetObject = target;
            SetupHighlighter(target);
        }
        
        public void SetTargetHighlighter(SimplePartHighlighter highlighter)
        {
            targetHighlighter = highlighter;
            targetObject = highlighter.gameObject;
        }
        
        [ContextMenu("Test Click")]
        private void TestClick()
        {
            PerformHighlight();
        }
        
        [ContextMenu("Find Target Now")]
        private void FindTargetNow()
        {
            FindTarget();
        }
        
        [ContextMenu("Debug State")]
        private void DebugState()
        {
            Debug.Log($"=== SimpleHighlightButton Debug ===");
            Debug.Log($"Target Object: {(targetObject != null ? targetObject.name : "null")}");
            Debug.Log($"Target Highlighter: {(targetHighlighter != null ? "Set" : "null")}");
            
            if (targetHighlighter != null)
            {
                Debug.Log($"Highlighter Part Count: {targetHighlighter.GetPartCount()}");
                Debug.Log($"Current Highlight Index: {targetHighlighter.GetCurrentIndex()}");
            }
            
            Debug.Log($"Auto Find: {autoFindTarget}");
            Debug.Log($"Search in BuildingBlock: {searchInBuildingBlock}");
            Debug.Log($"=== End Debug ===");
        }
    }
}