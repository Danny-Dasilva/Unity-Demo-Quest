using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuestCameraTools.App
{
    public class AudioToggle : MonoBehaviour, IPointerClickHandler
    {
        [Header("Audio Materials")]
        [SerializeField] private Material listenMaterial;
        [SerializeField] private Material mutedMaterial;
        
        [Header("References")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Image targetImage;
        
        private bool isMuted = false;
        
        private void Start()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();
            
            if (targetImage == null)
                targetImage = GetComponent<Image>();
            
            if (listenMaterial == null || mutedMaterial == null)
            {
                Debug.LogError("[AudioToggle] Please assign both Listen and Muted materials!");
                return;
            }
            
            UpdateMaterial();
            Debug.Log($"[AudioToggle] Initialized on {gameObject.name}");
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("[AudioToggle] Clicked!");
            ToggleAudio();
        }
        
        public void ToggleAudio()
        {
            isMuted = !isMuted;
            UpdateMaterial();
            AudioListener.volume = isMuted ? 0f : 1f;
            Debug.Log($"[AudioToggle] Audio is now {(isMuted ? "MUTED" : "ACTIVE")}");
        }
        
        private void UpdateMaterial()
        {
            Material newMaterial = isMuted ? mutedMaterial : listenMaterial;
            
            if (targetRenderer != null)
            {
                targetRenderer.material = newMaterial;
                Debug.Log($"[AudioToggle] Updated Renderer material to {newMaterial.name}");
            }
            
            if (targetImage != null)
            {
                targetImage.material = newMaterial;
                Debug.Log($"[AudioToggle] Updated Image material to {newMaterial.name}");
            }
        }
    }
}