using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuestCameraTools.App
{
    public class StepNavigationButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Navigation Type")]
        [SerializeField] private NavigationType navigationType = NavigationType.Next;
        
        [Header("References")]
        [SerializeField] private StepCounter stepCounter;
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Image buttonImage;
        
        [Header("Materials (Optional)")]
        [SerializeField] private Material enabledMaterial;
        [SerializeField] private Material disabledMaterial;
        
        public enum NavigationType
        {
            Next,
            Back
        }
        
        private void Start()
        {
            if (stepCounter == null)
            {
                stepCounter = FindObjectOfType<StepCounter>();
                if (stepCounter == null)
                {
                    Debug.LogError("[StepNavigationButton] No StepCounter found in scene!");
                }
            }
            
            if (buttonRenderer == null)
                buttonRenderer = GetComponent<Renderer>();
            
            if (buttonImage == null)
                buttonImage = GetComponent<Image>();
            
            UpdateButtonState();
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (stepCounter == null) return;
            
            switch (navigationType)
            {
                case NavigationType.Next:
                    stepCounter.NextStep();
                    Debug.Log("[StepNavigationButton] Next button clicked");
                    break;
                    
                case NavigationType.Back:
                    stepCounter.BackStep();
                    Debug.Log("[StepNavigationButton] Back button clicked");
                    break;
            }
            
            UpdateButtonState();
        }
        
        public void PerformNavigation()
        {
            if (stepCounter == null) return;
            
            switch (navigationType)
            {
                case NavigationType.Next:
                    stepCounter.NextStep();
                    break;
                    
                case NavigationType.Back:
                    stepCounter.BackStep();
                    break;
            }
            
            UpdateButtonState();
        }
        
        private void UpdateButtonState()
        {
            if (stepCounter == null) return;
            
            bool shouldDisable = false;
            
            switch (navigationType)
            {
                case NavigationType.Next:
                    shouldDisable = stepCounter.IsAtMaxStep();
                    break;
                    
                case NavigationType.Back:
                    shouldDisable = stepCounter.IsAtMinStep();
                    break;
            }
            
            if (enabledMaterial != null && disabledMaterial != null)
            {
                Material materialToUse = shouldDisable ? disabledMaterial : enabledMaterial;
                
                if (buttonRenderer != null)
                {
                    buttonRenderer.material = materialToUse;
                }
                
                if (buttonImage != null)
                {
                    buttonImage.material = materialToUse;
                }
            }
            
            if (shouldDisable)
            {
                Debug.Log($"[StepNavigationButton] {navigationType} button disabled at limit");
            }
        }
        
        public void SetStepCounter(StepCounter counter)
        {
            stepCounter = counter;
            UpdateButtonState();
        }
    }
}