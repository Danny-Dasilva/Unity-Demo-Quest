using UnityEngine;
using UnityEngine.UI;

namespace QuestCameraTools.App
{
    /// <summary>
    /// Simplified audio toggle that works with colliders and direct method calls.
    /// Suitable for Meta Interaction SDK or button components.
    /// </summary>
    public class AudioToggleSimple : MonoBehaviour
    {
        [Header("Audio Materials")]
        [SerializeField] private Material listenMaterial;
        [SerializeField] private Material mutedMaterial;
        
        [Header("Target Components")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Image targetImage;
        [SerializeField] private RawImage targetRawImage;
        
        [Header("Options")]
        [SerializeField] private bool useColliderInteraction = true;
        [SerializeField] private bool requireTrigger = false;
        [SerializeField] private bool enableDebugLogs = true;
        
        private bool isMuted = false;
        
        private void Start()
        {
            // Auto-detect components
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (targetRawImage == null) targetRawImage = GetComponent<RawImage>();
            
            // Ensure we have a collider if using collider interaction
            if (useColliderInteraction)
            {
                Collider col = GetComponent<Collider>();
                if (col == null)
                {
                    // Add a box collider if none exists
                    col = gameObject.AddComponent<BoxCollider>();
                    if (requireTrigger) col.isTrigger = true;
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log("[AudioToggleSimple] Added BoxCollider to enable interaction");
                    }
                }
            }
            
            ValidateSetup();
            UpdateMaterial();
        }
        
        private void ValidateSetup()
        {
            if (listenMaterial == null || mutedMaterial == null)
            {
                Debug.LogError("[AudioToggleSimple] Materials not assigned! Please assign Listen and Muted materials.");
            }
            
            if (targetRenderer == null && targetImage == null && targetRawImage == null)
            {
                Debug.LogWarning("[AudioToggleSimple] No target component found! Please assign a Renderer, Image, or RawImage.");
            }
            
            if (enableDebugLogs)
            {
                string targets = "";
                if (targetRenderer != null) targets += "Renderer ";
                if (targetImage != null) targets += "Image ";
                if (targetRawImage != null) targets += "RawImage ";
                Debug.Log($"[AudioToggleSimple] Initialized with targets: {targets}");
            }
        }
        
        // Public method for UI Button OnClick or Meta Interaction SDK
        public void Toggle()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[AudioToggleSimple] Toggle called");
            }
            
            isMuted = !isMuted;
            UpdateMaterial();
            AudioListener.volume = isMuted ? 0f : 1f;
        }
        
        // Alternative public methods
        public void SetMuted(bool muted)
        {
            if (isMuted != muted)
            {
                isMuted = muted;
                UpdateMaterial();
                AudioListener.volume = isMuted ? 0f : 1f;
            }
        }
        
        public void Mute() => SetMuted(true);
        public void Unmute() => SetMuted(false);
        public bool IsMuted() => isMuted;
        
        // Collider-based interactions
        private void OnMouseDown()
        {
            if (useColliderInteraction && !requireTrigger)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[AudioToggleSimple] OnMouseDown triggered");
                }
                Toggle();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (useColliderInteraction && requireTrigger)
            {
                // Check for hand or controller tags
                if (other.CompareTag("Hand") || other.CompareTag("Controller") || 
                    other.name.Contains("Hand") || other.name.Contains("Controller"))
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[AudioToggleSimple] Triggered by: {other.name}");
                    }
                    Toggle();
                }
            }
        }
        
        private void UpdateMaterial()
        {
            Material mat = isMuted ? mutedMaterial : listenMaterial;
            
            if (mat == null)
            {
                Debug.LogError($"[AudioToggleSimple] Material is null! isMuted: {isMuted}");
                return;
            }
            
            // Update 3D Renderer
            if (targetRenderer != null)
            {
                targetRenderer.material = mat;
                if (enableDebugLogs)
                {
                    Debug.Log($"[AudioToggleSimple] Renderer material set to: {mat.name}");
                }
            }
            
            // Update UI Image
            if (targetImage != null)
            {
                targetImage.material = mat;
                if (enableDebugLogs)
                {
                    Debug.Log($"[AudioToggleSimple] Image material set to: {mat.name}");
                }
            }
            
            // Update UI RawImage
            if (targetRawImage != null)
            {
                targetRawImage.material = mat;
                if (enableDebugLogs)
                {
                    Debug.Log($"[AudioToggleSimple] RawImage material set to: {mat.name}");
                }
            }
        }
    }
}