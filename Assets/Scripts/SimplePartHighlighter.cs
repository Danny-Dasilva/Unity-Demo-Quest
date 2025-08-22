using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace QuestCameraTools.App
{
    public class SimplePartHighlighter : MonoBehaviour
    {
        [Header("Highlight Settings")]
        [SerializeField] private Color highlightColor = new Color(1f, 0f, 0f, 1f); // Red
        [SerializeField] private float transparentAlpha = 0.5f;
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool applyHighlightOnStart = false; // Don't modify materials on start
        
        [Header("Part Selection")]
        [SerializeField] private int currentHighlightIndex = 0;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private List<string> partNames = new List<string>();
        
        private List<MeshRenderer> partRenderers = new List<MeshRenderer>();
        private Dictionary<MeshRenderer, Material[]> originalMaterials = new Dictionary<MeshRenderer, Material[]>();
        private Material transparentMaterial;
        private Material highlightMaterial;
        private bool isHighlightActive = false;
        
        private void Start()
        {
            if (autoInitialize)
            {
                Initialize();
            }
        }
        
        public void Initialize()
        {
            if (showDebugLogs) Debug.Log($"[SimplePartHighlighter] Initializing on {gameObject.name}");
            
            // Find all child renderers
            CollectRenderers();
            
            if (partRenderers.Count == 0)
            {
                Debug.LogWarning($"[SimplePartHighlighter] No child renderers found. Looking in object itself.");
                // Try to get renderer on this object
                MeshRenderer selfRenderer = GetComponent<MeshRenderer>();
                if (selfRenderer != null)
                {
                    partRenderers.Add(selfRenderer);
                }
            }
            
            if (partRenderers.Count > 0)
            {
                CreateMaterials();
                CacheOriginalMaterials();
                
                // Only apply highlight if explicitly requested
                if (applyHighlightOnStart)
                {
                    ApplyHighlight(currentHighlightIndex);
                    isHighlightActive = true;
                }
                
                if (showDebugLogs) Debug.Log($"[SimplePartHighlighter] Initialized with {partRenderers.Count} parts");
            }
            else
            {
                Debug.LogError($"[SimplePartHighlighter] No MeshRenderers found!");
            }
        }
        
        private void CollectRenderers()
        {
            partRenderers.Clear();
            partNames.Clear();
            
            // Get all child renderers (not including this object)
            MeshRenderer[] allRenderers = GetComponentsInChildren<MeshRenderer>();
            
            foreach (var renderer in allRenderers)
            {
                // Skip the parent object itself
                if (renderer.gameObject != gameObject)
                {
                    partRenderers.Add(renderer);
                    partNames.Add(renderer.gameObject.name);
                    
                    if (showDebugLogs) 
                        Debug.Log($"[SimplePartHighlighter] Found part: {renderer.gameObject.name}");
                }
            }
        }
        
        private void CreateMaterials()
        {
            // Create transparent material
            transparentMaterial = new Material(Shader.Find("Standard"));
            transparentMaterial.name = "SimpleTransparent";
            
            // Set to transparent rendering mode
            transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMaterial.SetInt("_ZWrite", 0);
            transparentMaterial.DisableKeyword("_ALPHATEST_ON");
            transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
            transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            transparentMaterial.renderQueue = 3000;
            
            Color transparentColor = Color.white;
            transparentColor.a = transparentAlpha;
            transparentMaterial.SetColor("_Color", transparentColor);
            
            // Create highlight material (opaque red)
            highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.name = "SimpleHighlight";
            
            // Ensure it's set to opaque rendering mode
            highlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            highlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            highlightMaterial.SetInt("_ZWrite", 1);
            highlightMaterial.DisableKeyword("_ALPHATEST_ON");
            highlightMaterial.DisableKeyword("_ALPHABLEND_ON");
            highlightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            highlightMaterial.renderQueue = 2000; // Opaque queue
            
            // Set the red color with full opacity
            highlightMaterial.SetColor("_Color", highlightColor);
            highlightMaterial.SetFloat("_Metallic", 0.2f);
            highlightMaterial.SetFloat("_Glossiness", 0.8f);
            
            // Enable the material
            highlightMaterial.EnableKeyword("_EMISSION");
            highlightMaterial.SetColor("_EmissionColor", highlightColor * 0.3f); // Slight emission for visibility
            
            if (showDebugLogs) 
                Debug.Log($"[SimplePartHighlighter] Created materials - Transparent: {transparentAlpha} alpha, Highlight: {highlightColor}");
        }
        
        private void CacheOriginalMaterials()
        {
            originalMaterials.Clear();
            
            foreach (var renderer in partRenderers)
            {
                if (renderer != null)
                {
                    // Store a copy of the original materials
                    originalMaterials[renderer] = renderer.sharedMaterials.ToArray();
                    
                    if (showDebugLogs)
                        Debug.Log($"[SimplePartHighlighter] Cached {renderer.sharedMaterials.Length} material(s) for {renderer.gameObject.name}");
                }
            }
        }
        
        public void CycleHighlight()
        {
            if (partRenderers.Count == 0) return;
            
            if (!isHighlightActive)
            {
                // First click - apply highlight to first part
                ApplyHighlight(currentHighlightIndex);
                isHighlightActive = true;
            }
            else
            {
                // Subsequent clicks - cycle through parts
                currentHighlightIndex = (currentHighlightIndex + 1) % partRenderers.Count;
                ApplyHighlight(currentHighlightIndex);
            }
            
            if (showDebugLogs)
                Debug.Log($"[SimplePartHighlighter] Highlighting part {currentHighlightIndex}: {partNames[currentHighlightIndex]}");
        }
        
        public void HighlightPart(int index)
        {
            if (partRenderers.Count == 0 || index < 0 || index >= partRenderers.Count) return;
            
            currentHighlightIndex = index;
            ApplyHighlight(index);
            
            if (showDebugLogs)
                Debug.Log($"[SimplePartHighlighter] Highlighting part {index}: {partNames[index]}");
        }
        
        private void ApplyHighlight(int highlightIndex)
        {
            for (int i = 0; i < partRenderers.Count; i++)
            {
                if (partRenderers[i] == null) continue;
                
                Material[] newMaterials;
                
                if (i == highlightIndex)
                {
                    // Apply highlight material to all slots
                    newMaterials = new Material[partRenderers[i].sharedMaterials.Length];
                    for (int j = 0; j < newMaterials.Length; j++)
                    {
                        newMaterials[j] = highlightMaterial;
                    }
                }
                else
                {
                    // Apply 50% transparent version of original materials
                    if (originalMaterials.ContainsKey(partRenderers[i]))
                    {
                        newMaterials = new Material[originalMaterials[partRenderers[i]].Length];
                        for (int j = 0; j < newMaterials.Length; j++)
                        {
                            Material origMat = originalMaterials[partRenderers[i]][j];
                            if (origMat != null)
                            {
                                // Create a copy of the original material
                                Material transMat = new Material(origMat);
                                transMat.name = origMat.name + "_Transparent";
                                
                                // Enable transparency on the material
                                transMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                                transMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                                transMat.SetInt("_ZWrite", 0);
                                transMat.DisableKeyword("_ALPHATEST_ON");
                                transMat.EnableKeyword("_ALPHABLEND_ON");
                                transMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                                transMat.renderQueue = 3000;
                                
                                // Set the alpha while preserving the original color
                                if (transMat.HasProperty("_Color"))
                                {
                                    Color color = transMat.GetColor("_Color");
                                    color.a = transparentAlpha;
                                    transMat.SetColor("_Color", color);
                                }
                                
                                newMaterials[j] = transMat;
                            }
                            else
                            {
                                // Fallback if original material is null
                                newMaterials[j] = transparentMaterial;
                            }
                        }
                    }
                    else
                    {
                        // Fallback to transparent material
                        newMaterials = new Material[] { transparentMaterial };
                    }
                }
                
                partRenderers[i].sharedMaterials = newMaterials;
            }
        }
        
        public void RestoreOriginalMaterials()
        {
            foreach (var kvp in originalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.sharedMaterials = kvp.Value;
                }
            }
            
            isHighlightActive = false;
            
            if (showDebugLogs) Debug.Log("[SimplePartHighlighter] Restored original materials");
        }
        
        public int GetPartCount()
        {
            return partRenderers.Count;
        }
        
        public int GetCurrentIndex()
        {
            return currentHighlightIndex;
        }
        
        public List<string> GetPartNames()
        {
            return partNames;
        }
        
        private void OnDestroy()
        {
            // Clean up created materials
            if (transparentMaterial != null)
                DestroyImmediate(transparentMaterial);
            
            if (highlightMaterial != null)
                DestroyImmediate(highlightMaterial);
        }
        
        [ContextMenu("Refresh Parts")]
        public void RefreshParts()
        {
            Initialize();
        }
        
        [ContextMenu("Restore Original Materials")]
        public void RestoreOriginals()
        {
            RestoreOriginalMaterials();
        }
        
        [ContextMenu("Test Cycle")]
        public void TestCycle()
        {
            CycleHighlight();
        }
        
        [ContextMenu("Debug Info")]
        private void DebugInfo()
        {
            Debug.Log($"=== SimplePartHighlighter Debug ===");
            Debug.Log($"GameObject: {gameObject.name}");
            Debug.Log($"Part Count: {partRenderers.Count}");
            Debug.Log($"Current Index: {currentHighlightIndex}");
            
            for (int i = 0; i < partNames.Count; i++)
            {
                string status = (i == currentHighlightIndex) ? " [HIGHLIGHTED]" : " [TRANSPARENT]";
                Debug.Log($"  Part {i}: {partNames[i]}{status}");
            }
            
            Debug.Log($"=== End Debug ===");
        }
    }
}