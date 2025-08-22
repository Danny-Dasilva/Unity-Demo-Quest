using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace QuestCameraTools.App
{
    public class SimplerPartHighlighter : MonoBehaviour
    {
        [Header("Highlight Settings")]
        [SerializeField] private Color highlightColor = Color.red;
        [SerializeField] [Range(0f, 1f)] private float dimAlpha = 0.5f;
        [SerializeField] private bool useRendererToggle = false; // Simple on/off instead of transparency
        
        [Header("Part Selection")]
        [SerializeField] private int currentHighlightIndex = -1; // -1 means nothing highlighted
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        private List<MeshRenderer> partRenderers = new List<MeshRenderer>();
        private Dictionary<MeshRenderer, Material[]> originalMaterials = new Dictionary<MeshRenderer, Material[]>();
        private Dictionary<MeshRenderer, Color[]> originalColors = new Dictionary<MeshRenderer, Color[]>();
        private bool isInitialized = false;
        private Material redMaterial;
        
        private void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            if (showDebugLogs) Debug.Log($"[SimplerPartHighlighter] Initializing on {gameObject.name}");
            
            CreateRedMaterial();
            CollectRenderers();
            CacheOriginalMaterials();
            isInitialized = true;
            
            if (showDebugLogs) Debug.Log($"[SimplerPartHighlighter] Found {partRenderers.Count} parts");
        }
        
        private void CollectRenderers()
        {
            partRenderers.Clear();
            
            // Get all child renderers
            MeshRenderer[] allRenderers = GetComponentsInChildren<MeshRenderer>();
            
            foreach (var renderer in allRenderers)
            {
                // Skip the parent object itself if it has a renderer
                if (renderer.gameObject != gameObject)
                {
                    partRenderers.Add(renderer);
                    if (showDebugLogs) 
                        Debug.Log($"[SimplerPartHighlighter] Found part: {renderer.gameObject.name}");
                }
            }
            
            // If no children found, check if this object itself has parts
            if (partRenderers.Count == 0)
            {
                MeshRenderer selfRenderer = GetComponent<MeshRenderer>();
                if (selfRenderer != null)
                {
                    partRenderers.Add(selfRenderer);
                    if (showDebugLogs) 
                        Debug.Log($"[SimplerPartHighlighter] Using self as single part");
                }
            }
        }
        
        private void CreateRedMaterial()
        {
            // Create a simple red material that should work with any render pipeline
            redMaterial = new Material(Shader.Find("Standard"));
            if (redMaterial.shader == null)
            {
                // Fallback to other common shaders
                redMaterial.shader = Shader.Find("Universal Render Pipeline/Lit");
                if (redMaterial.shader == null)
                {
                    redMaterial.shader = Shader.Find("Mobile/Diffuse");
                }
            }
            
            redMaterial.name = "HighlightRed";
            redMaterial.color = highlightColor;
            
            if (showDebugLogs) Debug.Log($"[SimplerPartHighlighter] Created red material with shader: {redMaterial.shader.name}");
        }
        
        private void CacheOriginalMaterials()
        {
            originalMaterials.Clear();
            originalColors.Clear();
            
            foreach (var renderer in partRenderers)
            {
                if (renderer != null)
                {
                    // Store original materials
                    originalMaterials[renderer] = renderer.materials.ToArray();
                    
                    // Store original colors
                    Color[] colors = new Color[renderer.materials.Length];
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        if (renderer.materials[i] != null && renderer.materials[i].HasProperty("_Color"))
                        {
                            colors[i] = renderer.materials[i].GetColor("_Color");
                        }
                        else
                        {
                            colors[i] = Color.white;
                        }
                    }
                    originalColors[renderer] = colors;
                    
                    if (showDebugLogs)
                        Debug.Log($"[SimplerPartHighlighter] Cached {renderer.materials.Length} material(s) for {renderer.gameObject.name}");
                }
            }
        }
        
        public void CycleHighlight()
        {
            if (!isInitialized || partRenderers.Count == 0) return;
            
            // Increment index (start from -1 which means no highlight)
            currentHighlightIndex++;
            
            // If we've gone past the last part, cycle back to first
            if (currentHighlightIndex >= partRenderers.Count)
            {
                currentHighlightIndex = 0;
            }
            
            ApplyHighlight();
            
            if (showDebugLogs && currentHighlightIndex >= 0 && currentHighlightIndex < partRenderers.Count)
            {
                Debug.Log($"[SimplerPartHighlighter] Highlighting part {currentHighlightIndex}: {partRenderers[currentHighlightIndex].gameObject.name}");
            }
        }
        
        private void ApplyHighlight()
        {
            for (int i = 0; i < partRenderers.Count; i++)
            {
                MeshRenderer renderer = partRenderers[i];
                if (renderer == null) continue;
                
                if (i == currentHighlightIndex)
                {
                    // Highlight this part - replace all materials with red
                    Material[] redMats = new Material[renderer.materials.Length];
                    for (int j = 0; j < redMats.Length; j++)
                    {
                        redMats[j] = redMaterial;
                    }
                    renderer.materials = redMats;
                    renderer.enabled = true;
                }
                else
                {
                    if (useRendererToggle)
                    {
                        // Simple approach: just disable non-highlighted parts
                        renderer.enabled = false;
                    }
                    else
                    {
                        // Try transparency approach
                        renderer.enabled = true;
                        
                        if (originalMaterials.ContainsKey(renderer))
                        {
                            // Create transparent versions of original materials
                            Material[] origMats = originalMaterials[renderer];
                            Material[] dimMats = new Material[origMats.Length];
                            
                            for (int j = 0; j < origMats.Length; j++)
                            {
                                if (origMats[j] != null)
                                {
                                    dimMats[j] = new Material(origMats[j]);
                                    
                                    // Try to make it transparent
                                    if (dimMats[j].HasProperty("_Color"))
                                    {
                                        Color color = dimMats[j].color;
                                        color.a = dimAlpha;
                                        dimMats[j].color = color;
                                    }
                                    
                                    // Try common transparency properties
                                    if (dimMats[j].HasProperty("_Mode"))
                                    {
                                        dimMats[j].SetFloat("_Mode", 2); // Transparent
                                        dimMats[j].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                                        dimMats[j].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                                        dimMats[j].SetInt("_ZWrite", 0);
                                        dimMats[j].EnableKeyword("_ALPHABLEND_ON");
                                        dimMats[j].renderQueue = 3000;
                                    }
                                }
                                else
                                {
                                    dimMats[j] = origMats[j];
                                }
                            }
                            
                            renderer.materials = dimMats;
                        }
                    }
                }
            }
        }
        
        [ContextMenu("Toggle Renderer Mode")]
        public void ToggleRendererMode()
        {
            useRendererToggle = !useRendererToggle;
            if (showDebugLogs) Debug.Log($"[SimplerPartHighlighter] Switched to {(useRendererToggle ? "renderer toggle" : "transparency")} mode");
            
            if (currentHighlightIndex >= 0)
            {
                ApplyHighlight();
            }
        }
        
        public void RestoreOriginalMaterials()
        {
            foreach (var kvp in originalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.materials = kvp.Value;
                    kvp.Key.enabled = true; // Make sure renderer is enabled
                }
            }
            
            currentHighlightIndex = -1;
            
            if (showDebugLogs) Debug.Log("[SimplerPartHighlighter] Restored original materials");
        }
        
        public void HighlightSpecificPart(int index)
        {
            if (index >= 0 && index < partRenderers.Count)
            {
                currentHighlightIndex = index;
                ApplyHighlight();
            }
        }
        
        [ContextMenu("Test Cycle")]
        public void TestCycle()
        {
            CycleHighlight();
        }
        
        [ContextMenu("Restore Originals")]
        public void RestoreOriginals()
        {
            RestoreOriginalMaterials();
        }
        
        [ContextMenu("Debug Info")]
        private void DebugInfo()
        {
            Debug.Log($"=== SimplerPartHighlighter Debug ===");
            Debug.Log($"GameObject: {gameObject.name}");
            Debug.Log($"Part Count: {partRenderers.Count}");
            Debug.Log($"Current Highlight: {currentHighlightIndex}");
            Debug.Log($"Initialized: {isInitialized}");
            
            for (int i = 0; i < partRenderers.Count; i++)
            {
                string status = (i == currentHighlightIndex) ? " [HIGHLIGHTED]" : " [DIMMED]";
                Debug.Log($"  Part {i}: {partRenderers[i].gameObject.name}{status}");
            }
            
            Debug.Log($"=== End Debug ===");
        }
        
        private void OnDestroy()
        {
            // Clean up created materials
            if (redMaterial != null)
            {
                DestroyImmediate(redMaterial);
            }
            
            // Restore materials on destroy to avoid leaving modified materials
            if (isInitialized)
            {
                RestoreOriginalMaterials();
            }
        }
    }
}