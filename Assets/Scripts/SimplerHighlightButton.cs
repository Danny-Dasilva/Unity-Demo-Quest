using UnityEngine;
using UnityEngine.EventSystems;

namespace QuestCameraTools.App
{
    public class SimplerHighlightButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Target")]
        [SerializeField] private GameObject targetObject;
        [SerializeField] private string targetName = "456";
        [SerializeField] private bool autoFind = true;
        
        private SimplerPartHighlighter highlighter;
        
        private void Start()
        {
            if (autoFind && targetObject == null)
            {
                FindTarget();
            }
            
            SetupHighlighter();
        }
        
        private void FindTarget()
        {
            // Try to find 456 object
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains(targetName))
                {
                    targetObject = obj;
                    Debug.Log($"[SimplerHighlightButton] Found target: {obj.name}");
                    break;
                }
            }
        }
        
        private void SetupHighlighter()
        {
            if (targetObject == null) return;
            
            highlighter = targetObject.GetComponent<SimplerPartHighlighter>();
            if (highlighter == null)
            {
                highlighter = targetObject.AddComponent<SimplerPartHighlighter>();
                Debug.Log($"[SimplerHighlightButton] Added SimplerPartHighlighter to {targetObject.name}");
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            PerformHighlight();
        }
        
        public void PerformHighlight()
        {
            if (highlighter == null)
            {
                SetupHighlighter();
            }
            
            if (highlighter != null)
            {
                highlighter.CycleHighlight();
            }
            else
            {
                Debug.LogWarning("[SimplerHighlightButton] No highlighter found!");
            }
        }
        
        [ContextMenu("Test Click")]
        private void TestClick()
        {
            PerformHighlight();
        }
    }
}