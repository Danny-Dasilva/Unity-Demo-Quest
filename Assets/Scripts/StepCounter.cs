using TMPro;
using UnityEngine;

namespace QuestCameraTools.App
{
    public class StepCounter : MonoBehaviour
    {
        [Header("Step Display")]
        [SerializeField] private TMP_Text stepText;
        
        [Header("Step Configuration")]
        [SerializeField] private int minStep = 1;
        [SerializeField] private int maxStep = 20;
        [SerializeField] private int currentStep = 1;
        
        private void Start()
        {
            if (stepText == null)
            {
                stepText = GetComponent<TMP_Text>();
            }
            
            UpdateStepDisplay();
        }
        
        public void NextStep()
        {
            if (currentStep < maxStep)
            {
                currentStep++;
                UpdateStepDisplay();
                Debug.Log($"[StepCounter] Next step: {currentStep}");
            }
            else
            {
                Debug.Log($"[StepCounter] Already at max step: {maxStep}");
            }
        }
        
        public void BackStep()
        {
            if (currentStep > minStep)
            {
                currentStep--;
                UpdateStepDisplay();
                Debug.Log($"[StepCounter] Back step: {currentStep}");
            }
            else
            {
                Debug.Log($"[StepCounter] Already at min step: {minStep}");
            }
        }
        
        public void SetStep(int step)
        {
            if (step >= minStep && step <= maxStep)
            {
                currentStep = step;
                UpdateStepDisplay();
                Debug.Log($"[StepCounter] Set step to: {currentStep}");
            }
            else
            {
                Debug.LogWarning($"[StepCounter] Invalid step: {step}. Must be between {minStep} and {maxStep}");
            }
        }
        
        public void ResetToStart()
        {
            currentStep = minStep;
            UpdateStepDisplay();
            Debug.Log($"[StepCounter] Reset to step: {currentStep}");
        }
        
        private void UpdateStepDisplay()
        {
            if (stepText != null)
            {
                stepText.text = $"Step {currentStep} of {maxStep}";
            }
            else
            {
                Debug.LogError("[StepCounter] No TMP_Text component assigned!");
            }
        }
        
        public int GetCurrentStep()
        {
            return currentStep;
        }
        
        public bool IsAtMinStep()
        {
            return currentStep <= minStep;
        }
        
        public bool IsAtMaxStep()
        {
            return currentStep >= maxStep;
        }
    }
}