using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace QuestCameraTools.App
{
    public class HighlightButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Highlight Settings")]
        [SerializeField] private bool cycleForward = true;
        [SerializeField] private bool findHighlighterAutomatically = true;
        
        [Header("References")]
        [SerializeField] private PartHighlighter targetHighlighter;
        [SerializeField] private StepBasedModelSwitcher modelSwitcher;
        
        [Header("UI Feedback")]
        [SerializeField] private TMP_Text partNameText;
        [SerializeField] private AudioSource clickAudio;
        [SerializeField] private AudioClip clickSound;
        
        [Header("Visual Feedback")]
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Material pressedMaterial;
        [SerializeField] private Material normalMaterial;
        
        private bool isPressed = false;
        private float pressedDuration = 0.2f;
        private float pressedTimer = 0f;
        
        private void Start()
        {
            if (modelSwitcher == null)
            {
                modelSwitcher = FindObjectOfType<StepBasedModelSwitcher>();
            }
            
            if (buttonRenderer == null)
            {
                buttonRenderer = GetComponent<Renderer>();
            }
            
            if (buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
            }
            
            if (clickAudio == null)
            {
                clickAudio = GetComponent<AudioSource>();
            }
            
            if (findHighlighterAutomatically)
            {
                FindHighlighter();
            }
            
            UpdatePartNameDisplay();
        }
        
        private void Update()
        {
            if (findHighlighterAutomatically && targetHighlighter == null)
            {
                FindHighlighter();
            }
            
            if (isPressed)
            {
                pressedTimer -= Time.deltaTime;
                if (pressedTimer <= 0)
                {
                    isPressed = false;
                    RestoreButtonVisual();
                }
            }
        }
        
        private void FindHighlighter()
        {
            if (modelSwitcher != null)
            {
                GameObject currentModel = modelSwitcher.transform.GetChild(modelSwitcher.transform.childCount - 1).gameObject;
                
                foreach (Transform child in modelSwitcher.transform)
                {
                    if (child.gameObject.activeInHierarchy)
                    {
                        PartHighlighter highlighter = child.GetComponent<PartHighlighter>();
                        
                        if (highlighter == null)
                        {
                            highlighter = child.gameObject.AddComponent<PartHighlighter>();
                            Debug.Log($"[HighlightButton] Added PartHighlighter to {child.gameObject.name}");
                        }
                        
                        targetHighlighter = highlighter;
                        Debug.Log($"[HighlightButton] Found/Created highlighter on {child.gameObject.name}");
                        break;
                    }
                }
            }
            else
            {
                PartHighlighter[] highlighters = FindObjectsOfType<PartHighlighter>();
                if (highlighters.Length > 0)
                {
                    targetHighlighter = highlighters[0];
                    Debug.Log($"[HighlightButton] Found highlighter: {targetHighlighter.gameObject.name}");
                }
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
                FindHighlighter();
                if (targetHighlighter == null)
                {
                    Debug.LogWarning("[HighlightButton] No PartHighlighter found!");
                    return;
                }
            }
            
            if (cycleForward)
            {
                targetHighlighter.HighlightNextPart();
            }
            else
            {
                targetHighlighter.HighlightPreviousPart();
            }
            
            PlayClickFeedback();
            UpdatePartNameDisplay();
            
            Debug.Log($"[HighlightButton] Highlighted part {targetHighlighter.GetCurrentHighlightIndex()} of {targetHighlighter.GetPartCount()}");
        }
        
        private void PlayClickFeedback()
        {
            if (clickAudio != null && clickSound != null)
            {
                clickAudio.PlayOneShot(clickSound);
            }
            
            isPressed = true;
            pressedTimer = pressedDuration;
            ApplyPressedVisual();
        }
        
        private void ApplyPressedVisual()
        {
            if (pressedMaterial != null)
            {
                if (buttonRenderer != null)
                {
                    buttonRenderer.material = pressedMaterial;
                }
                
                if (buttonImage != null)
                {
                    buttonImage.material = pressedMaterial;
                }
            }
        }
        
        private void RestoreButtonVisual()
        {
            if (normalMaterial != null)
            {
                if (buttonRenderer != null)
                {
                    buttonRenderer.material = normalMaterial;
                }
                
                if (buttonImage != null)
                {
                    buttonImage.material = normalMaterial;
                }
            }
        }
        
        private void UpdatePartNameDisplay()
        {
            if (partNameText != null && targetHighlighter != null)
            {
                var partNames = targetHighlighter.GetPartNames();
                int currentIndex = targetHighlighter.GetCurrentHighlightIndex();
                
                if (currentIndex >= 0 && currentIndex < partNames.Count)
                {
                    string partName = partNames[currentIndex];
                    partNameText.text = $"Part: {partName} ({currentIndex + 1}/{partNames.Count})";
                }
            }
        }
        
        public void SetTargetHighlighter(PartHighlighter highlighter)
        {
            targetHighlighter = highlighter;
            UpdatePartNameDisplay();
        }
        
        public void SetCycleDirection(bool forward)
        {
            cycleForward = forward;
        }
        
        public void HighlightSpecificPart(int index)
        {
            if (targetHighlighter != null)
            {
                targetHighlighter.HighlightPart(index);
                UpdatePartNameDisplay();
            }
        }
        
        public void HighlightPartByName(string partName)
        {
            if (targetHighlighter != null)
            {
                targetHighlighter.HighlightPartByName(partName);
                UpdatePartNameDisplay();
            }
        }
        
        [ContextMenu("Test Highlight")]
        private void TestHighlight()
        {
            PerformHighlight();
        }
    }
}