using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace QuestCameraTools.App
{
    public class StepBasedModelSwitcher : MonoBehaviour
    {
        [System.Serializable]
        public class StepModel
        {
            public int stepNumber;
            public GameObject modelPrefab;
            public string stepName;
        }

        [SerializeField] private List<StepModel> stepModels = new List<StepModel>();
        [SerializeField] private StepCounter stepCounter;
        [SerializeField] private bool useAsyncLoading = false; // Simplified - disable async by default
        
        [Header("Position Management")]
        [SerializeField] private bool preserveOriginalPosition = true;
        [SerializeField] private bool useWorldPosition = false;
        [SerializeField] private bool debugPositionChanges = false;
        
        private GameObject currentModel;
        private int currentStep = -1;
        private Vector3 initialModelPosition = Vector3.zero;
        private Quaternion initialModelRotation = Quaternion.identity;
        private Vector3 initialModelScale = Vector3.one;
        private bool hasInitialPosition = false;
        
        // World position tracking for more reliable positioning
        private Vector3 initialWorldPosition = Vector3.zero;
        private Quaternion initialWorldRotation = Quaternion.identity;
        private bool hasInitialWorldPosition = false;
        private GameObject originalModel;
        private bool isInitialized = false;
        
        // Collider settings to copy to new models
        private Vector3 originalColliderSize = Vector3.one;
        private Vector3 originalColliderCenter = Vector3.zero;
        private PhysicsMaterial originalColliderMaterial = null;
        private bool hasOriginalCollider = false;
        private Vector3 originalModelScale = Vector3.one; // Store the original model's scale for comparison

        private void Start()
        {
            Debug.Log($"[StepBasedModelSwitcher] Starting on GameObject: {gameObject.name}");
            
            if (stepCounter == null)
            {
                stepCounter = FindObjectOfType<StepCounter>();
            }
            
            Debug.Log($"[StepBasedModelSwitcher] StepCounter found: {stepCounter != null}");
            if (stepCounter != null)
            {
                Debug.Log($"[StepBasedModelSwitcher] Initial step from counter: {stepCounter.GetCurrentStep()}");
            }
            
            Debug.Log($"[StepBasedModelSwitcher] Step Models configured: {stepModels.Count}");
            for (int i = 0; i < stepModels.Count; i++)
            {
                var model = stepModels[i];
                Debug.Log($"[StepBasedModelSwitcher] Step {i}: stepNumber={model.stepNumber}, prefab={model.modelPrefab?.name}, name={model.stepName}");
            }

            // Capture and hide the original manually placed model
            CaptureAndHideOriginalModel();
            
            // Initialize with the current step
            isInitialized = true;
            UpdateModel();
        }

        private void CaptureAndHideOriginalModel()
        {
            Debug.Log($"[StepBasedModelSwitcher] Looking for original model among {transform.childCount} children");
            
            GameObject actualModel = null;
            
            // List all children for debugging
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                Debug.Log($"[StepBasedModelSwitcher] Child {i}: {child.name}, active: {child.gameObject.activeInHierarchy}");
            }
            
            // First, try to find a child that looks like an actual model (not a Meta XR component)
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeInHierarchy && 
                    !child.name.Contains("BuildingBlock") && 
                    !child.name.Contains("HandGrab") &&
                    !child.name.Contains("Installation"))
                {
                    // Check if it has renderers (likely a visual model)
                    var renderers = child.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        actualModel = child.gameObject;
                        Debug.Log($"[StepBasedModelSwitcher] Found actual model: {actualModel.name} with {renderers.Length} renderers");
                        break;
                    }
                }
            }
            
            // If no actual model found, fall back to first active child
            if (actualModel == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.gameObject.activeInHierarchy)
                    {
                        actualModel = child.gameObject;
                        Debug.Log($"[StepBasedModelSwitcher] Fallback to first active child: {actualModel.name}");
                        break;
                    }
                }
            }
            
            if (actualModel != null)
            {
                originalModel = actualModel;
                
                // Capture both local and world positions for flexibility
                initialModelPosition = actualModel.transform.localPosition;
                initialModelRotation = actualModel.transform.localRotation;
                initialModelScale = actualModel.transform.localScale;
                hasInitialPosition = true;
                
                // Also capture world position as backup
                initialWorldPosition = actualModel.transform.position;
                initialWorldRotation = actualModel.transform.rotation;
                hasInitialWorldPosition = true;
                
                Debug.Log($"[StepBasedModelSwitcher] Captured original model: {originalModel.name}");
                Debug.Log($"[StepBasedModelSwitcher] Initial local position: {initialModelPosition}");
                Debug.Log($"[StepBasedModelSwitcher] Initial world position: {initialWorldPosition}");
                Debug.Log($"[StepBasedModelSwitcher] Initial rotation: {initialModelRotation.eulerAngles}");
                Debug.Log($"[StepBasedModelSwitcher] Initial scale: {initialModelScale}");
                
                // Check what the original model's bounds are
                var renderers = originalModel.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Debug.Log($"[StepBasedModelSwitcher] Original model renderer bounds: {renderers[0].bounds}");
                }
                
                // Capture BoxCollider settings from the original model
                CaptureOriginalColliderSettings();
                
                // Hide the original model - it will be managed by the step system
                originalModel.SetActive(false);
                Debug.Log($"[StepBasedModelSwitcher] Hidden original model - Active after hide: {originalModel.activeInHierarchy}");
                
                // Hide any other active children that are models
                foreach (Transform otherChild in transform)
                {
                    if (otherChild.gameObject != originalModel && 
                        otherChild.gameObject.activeInHierarchy &&
                        !otherChild.name.Contains("BuildingBlock") &&
                        !otherChild.name.Contains("HandGrab"))
                    {
                        Debug.Log($"[StepBasedModelSwitcher] Found additional model child: {otherChild.name}, hiding it");
                        otherChild.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[StepBasedModelSwitcher] No suitable original model found in children. Using default positioning.");
            }
        }

        private void Update()
        {
            if (!isInitialized || stepCounter == null) return;
            
            int newStep = stepCounter.GetCurrentStep();
            if (newStep != currentStep)
            {
                Debug.Log($"[StepBasedModelSwitcher] Step changed from {currentStep} to {newStep}");
                UpdateModel();
            }
        }

        private void UpdateModel()
        {
            if (stepCounter == null) 
            {
                Debug.LogWarning($"[StepBasedModelSwitcher] UpdateModel blocked - no stepCounter");
                return;
            }

            int targetStep = stepCounter.GetCurrentStep();
            Debug.Log($"[StepBasedModelSwitcher] UpdateModel called for step {targetStep}");
            
            // Find the step model configuration
            var stepModel = stepModels.FirstOrDefault(sm => sm.stepNumber == targetStep);
            Debug.Log($"[StepBasedModelSwitcher] Found stepModel for step {targetStep}: {stepModel != null}");
            if (stepModel != null)
            {
                Debug.Log($"[StepBasedModelSwitcher] StepModel details - prefab: {stepModel.modelPrefab?.name}, name: {stepModel.stepName}");
            }
            
            // Clean up current model
            if (currentModel != null)
            {
                Debug.Log($"[StepBasedModelSwitcher] Cleaning up current model: {currentModel.name}");
                
                // Save current position before destroying
                if (currentModel != originalModel)
                {
                    initialModelPosition = currentModel.transform.localPosition;
                    initialModelRotation = currentModel.transform.localRotation;
                    initialModelScale = currentModel.transform.localScale;
                    hasInitialPosition = true;
                    Debug.Log($"[StepBasedModelSwitcher] Saved position from current model: {initialModelPosition}");
                }
                
                // Handle cleanup
                if (currentModel == originalModel)
                {
                    // Just hide the original model
                    currentModel.SetActive(false);
                    Debug.Log($"[StepBasedModelSwitcher] Hidden original model");
                }
                else
                {
                    // Save current position before destroying if we're preserving positions
                    if (preserveOriginalPosition)
                    {
                        if (useWorldPosition && hasInitialWorldPosition)
                        {
                            // Don't update world position - keep original
                            if (debugPositionChanges)
                                Debug.Log($"[StepBasedModelSwitcher] Preserving original world position: {initialWorldPosition}");
                        }
                        else
                        {
                            // Don't update local position - keep original
                            if (debugPositionChanges)
                                Debug.Log($"[StepBasedModelSwitcher] Preserving original local position: {initialModelPosition}");
                        }
                    }
                    else
                    {
                        // Update positions from current model
                        initialModelPosition = currentModel.transform.localPosition;
                        initialModelRotation = currentModel.transform.localRotation;
                        initialModelScale = currentModel.transform.localScale;
                        hasInitialPosition = true;
                        
                        if (useWorldPosition)
                        {
                            initialWorldPosition = currentModel.transform.position;
                            initialWorldRotation = currentModel.transform.rotation;
                            hasInitialWorldPosition = true;
                        }
                        
                        if (debugPositionChanges)
                            Debug.Log($"[StepBasedModelSwitcher] Updated position from current model: {initialModelPosition}");
                    }
                    
                    // Destroy instantiated models
                    Destroy(currentModel);
                    Debug.Log($"[StepBasedModelSwitcher] Destroyed instantiated model");
                }
                currentModel = null;
            }
            
            // Set the new model
            if (stepModel != null && stepModel.modelPrefab != null)
            {
                // Check if this step uses the original model
                if (originalModel != null && AreModelsTheSame(stepModel.modelPrefab, originalModel))
                {
                    Debug.Log($"[StepBasedModelSwitcher] Using original model for step {targetStep}");
                    currentModel = originalModel;
                    currentModel.SetActive(true);
                    
                    // Apply position
                    ApplyModelTransform(currentModel);
                }
                else
                {
                    Debug.Log($"[StepBasedModelSwitcher] Instantiating new model: {stepModel.modelPrefab.name}");
                    
                    if (useAsyncLoading)
                    {
                        StartCoroutine(LoadModelAsync(stepModel.modelPrefab, targetStep));
                    }
                    else
                    {
                        InstantiateModel(stepModel.modelPrefab);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[StepBasedModelSwitcher] No model prefab found for step {targetStep}");
                // List all available steps for debugging
                Debug.Log($"[StepBasedModelSwitcher] Available steps:");
                foreach (var sm in stepModels)
                {
                    Debug.Log($"[StepBasedModelSwitcher] - Step {sm.stepNumber}: {sm.modelPrefab?.name}");
                }
            }
            
            currentStep = targetStep;
        }

        private void InstantiateModel(GameObject prefab)
        {
            // Calculate the desired position and rotation before instantiation
            Vector3 desiredPosition;
            Quaternion desiredRotation;
            
            if (useWorldPosition && hasInitialWorldPosition)
            {
                desiredPosition = initialWorldPosition;
                desiredRotation = initialWorldRotation;
            }
            else if (hasInitialPosition)
            {
                // Convert local position to world position for instantiation
                desiredPosition = transform.TransformPoint(initialModelPosition);
                desiredRotation = transform.rotation * initialModelRotation;
            }
            else
            {
                desiredPosition = transform.position;
                desiredRotation = transform.rotation;
            }
            
            if (debugPositionChanges)
            {
                Debug.Log($"[StepBasedModelSwitcher] Instantiating model at desired position: {desiredPosition}");
            }
            
            // Instantiate with the correct position and rotation immediately
            currentModel = Instantiate(prefab, desiredPosition, desiredRotation, transform);
            
            // Set the scale
            currentModel.transform.localScale = initialModelScale;
            
            if (debugPositionChanges)
            {
                Debug.Log($"[StepBasedModelSwitcher] Model instantiated at: {currentModel.transform.position}");
            }
            
            // Ensure the model has a BoxCollider for grabbing
            // Temporarily disabled to avoid collider alignment issues
            // EnsureModelHasCollider(currentModel);
            
            Debug.Log($"[StepBasedModelSwitcher] Model instantiated: {currentModel.name}");
            Debug.Log($"[StepBasedModelSwitcher] Model world position: {currentModel.transform.position}");
            Debug.Log($"[StepBasedModelSwitcher] Model local position: {currentModel.transform.localPosition}");
            Debug.Log($"[StepBasedModelSwitcher] Model scale: {currentModel.transform.localScale}");
            Debug.Log($"[StepBasedModelSwitcher] Model active: {currentModel.activeInHierarchy}");
            
            // Check collider status
            var collider = currentModel.GetComponent<Collider>();
            Debug.Log($"[StepBasedModelSwitcher] Model has collider: {collider != null}, type: {collider?.GetType().Name}");
            
            // Log visual components for debugging
            var renderers = currentModel.GetComponentsInChildren<Renderer>();
            Debug.Log($"[StepBasedModelSwitcher] Model has {renderers.Length} renderers");
            for (int i = 0; i < renderers.Length && i < 3; i++)
            {
                var renderer = renderers[i];
                Debug.Log($"[StepBasedModelSwitcher] Renderer {i}: {renderer.name}, enabled: {renderer.enabled}, bounds: {renderer.bounds}");
            }
            
            // Check if any other models are still visible
            Debug.Log($"[StepBasedModelSwitcher] Checking for other visible children:");
            foreach (Transform child in transform)
            {
                if (child.gameObject != currentModel)
                {
                    Debug.Log($"[StepBasedModelSwitcher] Other child: {child.name}, active: {child.gameObject.activeInHierarchy}");
                }
            }
        }

        private IEnumerator LoadModelAsync(GameObject prefab, int targetStep)
        {
            Debug.Log($"[StepBasedModelSwitcher] Starting async load for step {targetStep}");
            
            // Simple async load - just wait a frame then instantiate
            yield return null;
            
            // Verify we're still on the same step (user might have changed steps)
            if (stepCounter != null && stepCounter.GetCurrentStep() == targetStep)
            {
                InstantiateModel(prefab);
            }
            else
            {
                Debug.Log($"[StepBasedModelSwitcher] Step changed during async load, cancelling");
            }
        }

        private bool AreModelsTheSame(GameObject prefab, GameObject instance)
        {
            if (prefab == null || instance == null) return false;
            
            // Compare by name (simple heuristic)
            string prefabName = prefab.name.Replace("(Clone)", "").Trim();
            string instanceName = instance.name.Replace("(Clone)", "").Trim();
            
            bool isSame = prefabName == instanceName;
            Debug.Log($"[StepBasedModelSwitcher] Comparing models: '{prefabName}' vs '{instanceName}' = {isSame}");
            return isSame;
        }

        public void SetStepModels(List<StepModel> models)
        {
            stepModels = models;
            if (isInitialized)
            {
                UpdateModel();
            }
        }

        // Public method to force refresh (for debugging)
        public void ForceRefresh()
        {
            Debug.Log("[StepBasedModelSwitcher] Force refresh requested");
            UpdateModel();
        }

        // Debug method to show current state
        [ContextMenu("Debug Current State")]
        public void DebugCurrentState()
        {
            Debug.Log($"=== StepBasedModelSwitcher Debug State ===");
            Debug.Log($"Current Step: {currentStep}");
            Debug.Log($"Step Counter: {stepCounter?.GetCurrentStep()}");
            Debug.Log($"Current Model: {currentModel?.name}");
            Debug.Log($"Original Model: {originalModel?.name}");
            Debug.Log($"Has Initial Position: {hasInitialPosition}");
            Debug.Log($"Initial Position: {initialModelPosition}");
            Debug.Log($"Child Count: {transform.childCount}");
            
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                Debug.Log($"Child {i}: {child.name}, active: {child.gameObject.activeInHierarchy}");
            }
            Debug.Log($"=== End Debug State ===");
        }
        
        private struct ColliderInfo
        {
            public Vector3 size;
            public Vector3 center;
            public Vector3 rendererCenter;
        }
        
        private ColliderInfo CalculateOptimalColliderInfo(GameObject model)
        {
            var info = new ColliderInfo();
            
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                // Calculate combined bounds in world space first
                Bounds combinedWorldBounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    combinedWorldBounds.Encapsulate(renderer.bounds);
                }
                
                // Convert world bounds to local space relative to the model
                Vector3 worldMin = combinedWorldBounds.min;
                Vector3 worldMax = combinedWorldBounds.max;
                Vector3 worldCenter = combinedWorldBounds.center;
                
                // Transform to model's local space
                Vector3 localMin = model.transform.InverseTransformPoint(worldMin);
                Vector3 localMax = model.transform.InverseTransformPoint(worldMax);
                Vector3 localCenter = model.transform.InverseTransformPoint(worldCenter);
                
                // Calculate size and ensure positive values
                Vector3 localSize = localMax - localMin;
                localSize.x = Mathf.Abs(localSize.x);
                localSize.y = Mathf.Abs(localSize.y);
                localSize.z = Mathf.Abs(localSize.z);
                
                info.size = localSize;
                info.center = localCenter;
                info.rendererCenter = worldCenter;
                
                Debug.Log($"[StepBasedModelSwitcher] Calculated optimal collider info:");
                Debug.Log($"[StepBasedModelSwitcher] - World bounds: Center={combinedWorldBounds.center}, Size={combinedWorldBounds.size}");
                Debug.Log($"[StepBasedModelSwitcher] - Local center: {localCenter}");
                Debug.Log($"[StepBasedModelSwitcher] - Local size: {localSize}");
            }
            else
            {
                // Fallback to default values
                info.size = Vector3.one;
                info.center = Vector3.zero;
                info.rendererCenter = model.transform.position;
                
                Debug.LogWarning($"[StepBasedModelSwitcher] No renderers found on {model.name}, using default collider info");
            }
            
            return info;
        }
        
        private void CaptureOriginalColliderSettings()
        {
            if (originalModel == null) return;
            
            // Store the original model's scale for reference
            originalModelScale = originalModel.transform.localScale;
            
            // Check for BoxCollider on the original model
            var boxCollider = originalModel.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                // Store the raw collider size (we'll apply scale when needed)
                originalColliderSize = boxCollider.size;
                originalColliderCenter = boxCollider.center;
                originalColliderMaterial = boxCollider.material;
                hasOriginalCollider = true;
                
                Debug.Log($"[StepBasedModelSwitcher] Captured BoxCollider settings - Raw Size: {originalColliderSize}, Center: {originalColliderCenter}");
                Debug.Log($"[StepBasedModelSwitcher] Original model scale: {originalModelScale}");
                
                // Calculate and log the effective collider size
                var effectiveSize = Vector3.Scale(originalColliderSize, originalModelScale);
                Debug.Log($"[StepBasedModelSwitcher] Effective collider size (size × scale): {effectiveSize}");
            }
            else
            {
                Debug.LogWarning($"[StepBasedModelSwitcher] Original model {originalModel.name} does not have a BoxCollider. Will auto-generate for new models.");
                
                // Calculate bounds from renderers as fallback
                var renderers = originalModel.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds combinedBounds = renderers[0].bounds;
                    foreach (var renderer in renderers)
                    {
                        combinedBounds.Encapsulate(renderer.bounds);
                    }
                    
                    // Convert world bounds to local bounds
                    var worldToLocal = originalModel.transform.worldToLocalMatrix;
                    var localSize = worldToLocal.MultiplyVector(combinedBounds.size);
                    var localCenter = worldToLocal.MultiplyPoint(combinedBounds.center);
                    
                    // Make sure size components are positive
                    originalColliderSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
                    originalColliderCenter = localCenter;
                    hasOriginalCollider = false; // Mark as auto-generated
                    
                    Debug.Log($"[StepBasedModelSwitcher] Auto-calculated collider from renderers - Size: {originalColliderSize}, Center: {originalColliderCenter}");
                }
            }
        }
        
        private void EnsureModelHasCollider(GameObject model)
        {
            if (model == null) return;
            
            var existingCollider = model.GetComponent<Collider>();
            if (existingCollider != null)
            {
                Debug.Log($"[StepBasedModelSwitcher] Model {model.name} already has collider: {existingCollider.GetType().Name}");
                return;
            }
            
            Debug.Log($"[StepBasedModelSwitcher] Adding BoxCollider to model: {model.name}");
            
            // Calculate the optimal collider center and size based on renderer bounds
            var colliderInfo = CalculateOptimalColliderInfo(model);
            
            var boxCollider = model.AddComponent<BoxCollider>();
            
            if (hasOriginalCollider)
            {
                // Use original collider size but calculate center from renderer bounds
                var currentModelScale = model.transform.localScale;
                
                Vector3 scaledColliderSize;
                if (originalModelScale != Vector3.zero)
                {
                    // Calculate relative scale difference
                    var scaleRatio = new Vector3(
                        currentModelScale.x / originalModelScale.x,
                        currentModelScale.y / originalModelScale.y,
                        currentModelScale.z / originalModelScale.z
                    );
                    
                    scaledColliderSize = Vector3.Scale(originalColliderSize, scaleRatio);
                }
                else
                {
                    scaledColliderSize = originalColliderSize;
                }
                
                // Use the calculated optimal center instead of original center
                boxCollider.size = scaledColliderSize;
                boxCollider.center = colliderInfo.center;
                boxCollider.material = originalColliderMaterial;
                
                Debug.Log($"[StepBasedModelSwitcher] Applied hybrid collider settings:");
                Debug.Log($"[StepBasedModelSwitcher] - Model Scale: {currentModelScale}");
                Debug.Log($"[StepBasedModelSwitcher] - Original Scale: {originalModelScale}");
                Debug.Log($"[StepBasedModelSwitcher] - Original Size: {originalColliderSize}");
                Debug.Log($"[StepBasedModelSwitcher] - Final Collider Size: {boxCollider.size}");
                Debug.Log($"[StepBasedModelSwitcher] - Calculated Center: {boxCollider.center}");
                Debug.Log($"[StepBasedModelSwitcher] - Renderer Center: {colliderInfo.rendererCenter}");
                Debug.Log($"[StepBasedModelSwitcher] - Effective World Size: {Vector3.Scale(boxCollider.size, currentModelScale)}");
            }
            else
            {
                // Use the calculated optimal size and center from renderer bounds
                boxCollider.size = colliderInfo.size;
                boxCollider.center = colliderInfo.center;
                
                Debug.Log($"[StepBasedModelSwitcher] Auto-calculated collider:");
                Debug.Log($"[StepBasedModelSwitcher] - Local Size: {boxCollider.size}");
                Debug.Log($"[StepBasedModelSwitcher] - Local Center: {boxCollider.center}");
                Debug.Log($"[StepBasedModelSwitcher] - Renderer Center: {colliderInfo.rendererCenter}");
                Debug.Log($"[StepBasedModelSwitcher] - Model Scale: {model.transform.localScale}");
                Debug.Log($"[StepBasedModelSwitcher] - Effective World Size: {Vector3.Scale(boxCollider.size, model.transform.localScale)}");
            }
            
            Debug.Log($"[StepBasedModelSwitcher] Successfully added BoxCollider to {model.name}");
        }
        
        private void ApplyModelTransform(GameObject model)
        {
            if (model == null) return;
            
            Vector3 positionBefore = model.transform.position;
            
            if (useWorldPosition && hasInitialWorldPosition)
            {
                // Use world position (more reliable for objects that shouldn't move)
                model.transform.position = initialWorldPosition;
                model.transform.rotation = initialWorldRotation;
                model.transform.localScale = initialModelScale;
                
                if (debugPositionChanges)
                {
                    Debug.Log($"[StepBasedModelSwitcher] Applied world position: {initialWorldPosition}");
                    Debug.Log($"[StepBasedModelSwitcher] Position change: {positionBefore} -> {model.transform.position}");
                }
            }
            else if (hasInitialPosition)
            {
                // Use local position (original behavior)
                model.transform.localPosition = initialModelPosition;
                model.transform.localRotation = initialModelRotation;
                model.transform.localScale = initialModelScale;
                
                if (debugPositionChanges)
                {
                    Debug.Log($"[StepBasedModelSwitcher] Applied local position: {initialModelPosition}");
                    Debug.Log($"[StepBasedModelSwitcher] Position change: {positionBefore} -> {model.transform.position}");
                }
            }
            else
            {
                // Fallback to default positioning
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                
                if (debugPositionChanges)
                {
                    Debug.Log($"[StepBasedModelSwitcher] Applied default position: Vector3.zero");
                    Debug.Log($"[StepBasedModelSwitcher] Position change: {positionBefore} -> {model.transform.position}");
                }
            }
        }
        
        // Public method to reset position to original
        [ContextMenu("Reset To Original Position")]
        public void ResetToOriginalPosition()
        {
            if (currentModel != null)
            {
                if (useWorldPosition && hasInitialWorldPosition)
                {
                    currentModel.transform.position = initialWorldPosition;
                    currentModel.transform.rotation = initialWorldRotation;
                    Debug.Log($"[StepBasedModelSwitcher] Reset to original world position: {initialWorldPosition}");
                }
                else if (hasInitialPosition)
                {
                    currentModel.transform.localPosition = initialModelPosition;
                    currentModel.transform.localRotation = initialModelRotation;
                    Debug.Log($"[StepBasedModelSwitcher] Reset to original local position: {initialModelPosition}");
                }
                currentModel.transform.localScale = initialModelScale;
            }
        }
    }
}