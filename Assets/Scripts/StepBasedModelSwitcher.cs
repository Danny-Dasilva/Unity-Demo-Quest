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
            [Tooltip("Audio clip to play when this step is activated")]
            public AudioClip audioClip;
        }

        [SerializeField] private List<StepModel> stepModels = new List<StepModel>();
        [SerializeField] private StepCounter stepCounter;
        [SerializeField] private bool useAsyncLoading = false;
        
        [Header("Position Management")]
        [SerializeField] private bool preserveOriginalPosition = true;
        [SerializeField] private bool useWorldPosition = false;
        [SerializeField] private bool debugPositionChanges = false;
        
        [Header("Parent Collider Settings")]
        [SerializeField] private bool autoUpdateParentCollider = true;
        [SerializeField] private float colliderPadding = 0.02f;
        
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
        
        // Parent collider that will be dynamically sized
        private BoxCollider parentBoxCollider = null;

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
            
            // Ensure we have a parent collider
            EnsureParentCollider();

            // Capture and hide the original manually placed model
            CaptureAndHideOriginalModel();
            
            // Initialize with the current step
            isInitialized = true;
            UpdateModel();
        }

        private void EnsureParentCollider()
        {
            parentBoxCollider = GetComponent<BoxCollider>();
            if (parentBoxCollider == null)
            {
                parentBoxCollider = gameObject.AddComponent<BoxCollider>();
                Debug.Log($"[StepBasedModelSwitcher] Added BoxCollider to parent GameObject");
            }
            else
            {
                Debug.Log($"[StepBasedModelSwitcher] Found existing BoxCollider on parent GameObject");
            }
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
                
                // Remove any colliders from the original model (we use parent collider)
                RemoveChildColliders(originalModel);
                
                // Update parent collider based on original model
                if (autoUpdateParentCollider)
                {
                    UpdateParentCollider(originalModel);
                }
                
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
                
                // Save current position before destroying (only if not preserving original)
                if (!preserveOriginalPosition && currentModel != originalModel)
                {
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
            // Instantiate as child with local position/rotation/scale
            currentModel = Instantiate(prefab, transform);
            
            // Apply the saved transform
            ApplyModelTransform(currentModel);
            
            // Remove any colliders from the model (we use parent collider)
            RemoveChildColliders(currentModel);
            
            // Update parent collider to match new model
            if (autoUpdateParentCollider)
            {
                UpdateParentCollider(currentModel);
            }
            
            Debug.Log($"[StepBasedModelSwitcher] Model instantiated: {currentModel.name}");
            Debug.Log($"[StepBasedModelSwitcher] Model world position: {currentModel.transform.position}");
            Debug.Log($"[StepBasedModelSwitcher] Model local position: {currentModel.transform.localPosition}");
            Debug.Log($"[StepBasedModelSwitcher] Model scale: {currentModel.transform.localScale}");
            Debug.Log($"[StepBasedModelSwitcher] Model active: {currentModel.activeInHierarchy}");
            
            // Log visual components for debugging
            var renderers = currentModel.GetComponentsInChildren<Renderer>();
            Debug.Log($"[StepBasedModelSwitcher] Model has {renderers.Length} renderers");
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

        private void RemoveChildColliders(GameObject model)
        {
            if (model == null) return;
            
            // Remove all colliders from the model and its children
            var colliders = model.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                // Don't remove the parent collider
                if (col.gameObject == gameObject) continue;
                
                Debug.Log($"[StepBasedModelSwitcher] Removing collider from {col.gameObject.name}");
                Destroy(col);
            }
        }

        private void UpdateParentCollider(GameObject model)
        {
            if (parentBoxCollider == null || model == null) return;
            
            // Get all renderers in the model
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[StepBasedModelSwitcher] No renderers found in model: {model.name}");
                return;
            }
            
            // Calculate combined bounds in world space
            Bounds combinedBounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
            
            // Convert to local space relative to the parent transform
            Vector3 localCenter = transform.InverseTransformPoint(combinedBounds.center);
            Vector3 localSize = transform.InverseTransformVector(combinedBounds.size);
            
            // Ensure positive size values and add padding
            localSize = new Vector3(
                Mathf.Abs(localSize.x) + colliderPadding,
                Mathf.Abs(localSize.y) + colliderPadding,
                Mathf.Abs(localSize.z) + colliderPadding
            );
            
            // Update the parent collider
            parentBoxCollider.center = localCenter;
            parentBoxCollider.size = localSize;
            
            Debug.Log($"[StepBasedModelSwitcher] Updated parent collider - Center: {localCenter}, Size: {localSize}");
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

        public void SetStepModels(List<StepModel> models)
        {
            stepModels = models;
            if (isInitialized)
            {
                UpdateModel();
            }
        }

        // Public method to get audio clip for a specific step
        public AudioClip GetAudioClipForStep(int stepNumber)
        {
            var stepModel = stepModels.FirstOrDefault(sm => sm.stepNumber == stepNumber);
            return stepModel?.audioClip;
        }

        // Public method to get all step models (for syncing with audio player)
        public List<StepModel> GetStepModels()
        {
            return stepModels;
        }

        // Public method to force refresh (for debugging)
        [ContextMenu("Force Refresh")]
        public void ForceRefresh()
        {
            Debug.Log("[StepBasedModelSwitcher] Force refresh requested");
            currentStep = -1; // Force update
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
            Debug.Log($"Initial Scale: {initialModelScale}");
            Debug.Log($"Parent Collider: Center={parentBoxCollider?.center}, Size={parentBoxCollider?.size}");
            Debug.Log($"Child Count: {transform.childCount}");
            
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                Debug.Log($"Child {i}: {child.name}, active: {child.gameObject.activeInHierarchy}");
            }
            Debug.Log($"=== End Debug State ===");
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
        
        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            if (parentBoxCollider != null)
            {
                Gizmos.color = Color.green;
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(parentBoxCollider.center, parentBoxCollider.size);
                Gizmos.matrix = oldMatrix;
            }
        }
    }
}