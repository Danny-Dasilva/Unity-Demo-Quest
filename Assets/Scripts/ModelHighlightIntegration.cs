using UnityEngine;
using System.Collections;

namespace QuestCameraTools.App
{
    [RequireComponent(typeof(StepBasedModelSwitcher))]
    public class ModelHighlightIntegration : MonoBehaviour
    {
        [Header("Highlight Configuration")]
        [SerializeField] private bool autoAddHighlighter = true;
        [SerializeField] private bool highlightFirstPartOnLoad = true;
        [SerializeField] private Color defaultHighlightColor = Color.red;
        [SerializeField] private float defaultTransparency = 0.5f;
        
        [Header("Step-Specific Highlights")]
        [SerializeField] private bool useStepSpecificHighlights = false;
        [System.Serializable]
        public class StepHighlightConfig
        {
            public int stepNumber;
            public string partNameToHighlight;
            public Color customHighlightColor = Color.red;
        }
        [SerializeField] private StepHighlightConfig[] stepHighlights;
        
        [Header("References")]
        [SerializeField] private StepBasedModelSwitcher modelSwitcher;
        [SerializeField] private StepCounter stepCounter;
        [SerializeField] private HighlightButton highlightButton;
        
        private PartHighlighter currentHighlighter;
        private int lastProcessedStep = -1;
        
        private void Start()
        {
            if (modelSwitcher == null)
            {
                modelSwitcher = GetComponent<StepBasedModelSwitcher>();
            }
            
            if (stepCounter == null)
            {
                stepCounter = FindObjectOfType<StepCounter>();
            }
            
            if (highlightButton == null)
            {
                highlightButton = FindObjectOfType<HighlightButton>();
            }
            
            StartCoroutine(MonitorModelChanges());
        }
        
        private IEnumerator MonitorModelChanges()
        {
            yield return new WaitForSeconds(0.5f);
            
            while (true)
            {
                if (stepCounter != null)
                {
                    int currentStep = stepCounter.GetCurrentStep();
                    
                    if (currentStep != lastProcessedStep)
                    {
                        Debug.Log($"[ModelHighlightIntegration] Step changed to {currentStep}");
                        yield return new WaitForSeconds(0.1f);
                        ProcessCurrentModel();
                        lastProcessedStep = currentStep;
                    }
                }
                
                yield return new WaitForSeconds(0.2f);
            }
        }
        
        private void ProcessCurrentModel()
        {
            GameObject activeModel = null;
            
            foreach (Transform child in modelSwitcher.transform)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    MeshRenderer[] renderers = child.GetComponentsInChildren<MeshRenderer>();
                    if (renderers.Length > 0)
                    {
                        activeModel = child.gameObject;
                        Debug.Log($"[ModelHighlightIntegration] Found active model: {activeModel.name}");
                        break;
                    }
                }
            }
            
            if (activeModel != null && autoAddHighlighter)
            {
                currentHighlighter = activeModel.GetComponent<PartHighlighter>();
                
                if (currentHighlighter == null)
                {
                    currentHighlighter = activeModel.AddComponent<PartHighlighter>();
                    Debug.Log($"[ModelHighlightIntegration] Added PartHighlighter to {activeModel.name}");
                    
                    ConfigureHighlighter(currentHighlighter);
                }
                
                if (highlightButton != null)
                {
                    highlightButton.SetTargetHighlighter(currentHighlighter);
                }
                
                if (useStepSpecificHighlights && stepCounter != null)
                {
                    ApplyStepSpecificHighlight(stepCounter.GetCurrentStep());
                }
                else if (highlightFirstPartOnLoad)
                {
                    StartCoroutine(DelayedHighlight());
                }
            }
        }
        
        private void ConfigureHighlighter(PartHighlighter highlighter)
        {
            var highlightColorField = highlighter.GetType().GetField("highlightColor", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (highlightColorField != null)
            {
                highlightColorField.SetValue(highlighter, defaultHighlightColor);
            }
            
            var transparencyField = highlighter.GetType().GetField("transparentAlpha", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (transparencyField != null)
            {
                transparencyField.SetValue(highlighter, defaultTransparency);
            }
        }
        
        private IEnumerator DelayedHighlight()
        {
            yield return new WaitForSeconds(0.2f);
            
            if (currentHighlighter != null)
            {
                currentHighlighter.HighlightPart(0);
                Debug.Log("[ModelHighlightIntegration] Highlighted first part");
            }
        }
        
        private void ApplyStepSpecificHighlight(int stepNumber)
        {
            if (stepHighlights == null || currentHighlighter == null) return;
            
            foreach (var config in stepHighlights)
            {
                if (config.stepNumber == stepNumber)
                {
                    if (!string.IsNullOrEmpty(config.partNameToHighlight))
                    {
                        StartCoroutine(DelayedStepHighlight(config));
                    }
                    break;
                }
            }
        }
        
        private IEnumerator DelayedStepHighlight(StepHighlightConfig config)
        {
            yield return new WaitForSeconds(0.3f);
            
            if (currentHighlighter != null)
            {
                var highlightColorField = currentHighlighter.GetType().GetField("highlightColor", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (highlightColorField != null)
                {
                    highlightColorField.SetValue(currentHighlighter, config.customHighlightColor);
                }
                
                currentHighlighter.HighlightPartByName(config.partNameToHighlight);
                Debug.Log($"[ModelHighlightIntegration] Applied step-specific highlight for step {config.stepNumber}: {config.partNameToHighlight}");
            }
        }
        
        public void TriggerHighlightRefresh()
        {
            ProcessCurrentModel();
        }
        
        public PartHighlighter GetCurrentHighlighter()
        {
            return currentHighlighter;
        }
        
        [ContextMenu("Force Process Current Model")]
        private void ForceProcessCurrentModel()
        {
            ProcessCurrentModel();
        }
        
        [ContextMenu("Debug Current State")]
        private void DebugCurrentState()
        {
            Debug.Log($"=== ModelHighlightIntegration Debug ===");
            Debug.Log($"Current Step: {stepCounter?.GetCurrentStep()}");
            Debug.Log($"Last Processed Step: {lastProcessedStep}");
            Debug.Log($"Current Highlighter: {currentHighlighter?.gameObject.name}");
            Debug.Log($"Auto Add Highlighter: {autoAddHighlighter}");
            Debug.Log($"Use Step Specific: {useStepSpecificHighlights}");
            
            if (currentHighlighter != null)
            {
                Debug.Log($"Highlighter Part Count: {currentHighlighter.GetPartCount()}");
                Debug.Log($"Current Highlight Index: {currentHighlighter.GetCurrentHighlightIndex()}");
            }
            
            Debug.Log($"=== End Debug ===");
        }
    }
}