using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace QuestCameraTools.App
{
    public class PartHighlighter : MonoBehaviour
    {
        [Header("Highlight Settings")]
        [SerializeField] private Color highlightColor = new Color(1f, 0f, 0f, 1f); // Red
        [SerializeField] private float transparentAlpha = 0.5f;
        
        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;
        [SerializeField] private int currentHighlightIndex = 0;
        
        private List<MeshRenderer> partRenderers = new List<MeshRenderer>();
        private Dictionary<MeshRenderer, Material[]> originalMaterials = new Dictionary<MeshRenderer, Material[]>();
        private Material transparentMaterial;
        private Material highlightMaterial;
        private bool isInitialized = false;
        
        private void Start()
        {
            Initialize();
        }
        
        private void OnEnable()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }
        
        private void Initialize()
        {
            if (debugLogs) Debug.Log($"[PartHighlighter] Initializing on {gameObject.name}");
            
            CollectPartRenderers();
            CreateMaterials();
            CacheOriginalMaterials();
            
            if (partRenderers.Count > 0)
            {
                ApplyHighlight(0);
                isInitialized = true;
            }
            else
            {
                Debug.LogWarning($"[PartHighlighter] No MeshRenderers found in {gameObject.name}");
            }
        }
        
        private void CollectPartRenderers()
        {
            partRenderers.Clear();
            
            MeshRenderer[] allRenderers = GetComponentsInChildren<MeshRenderer>();
            
            foreach (var renderer in allRenderers)
            {
                if (renderer.gameObject != gameObject)
                {
                    partRenderers.Add(renderer);
                    if (debugLogs) Debug.Log($"[PartHighlighter] Found part: {renderer.gameObject.name}");
                }
            }
            
            if (debugLogs) Debug.Log($"[PartHighlighter] Total parts found: {partRenderers.Count}");
        }
        
        private void CreateMaterials()
        {
            if (transparentMaterial == null)
            {
                transparentMaterial = new Material(Shader.Find("Standard"));
                transparentMaterial.name = "TransparentMaterial";
                
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
                
                if (debugLogs) Debug.Log("[PartHighlighter] Created transparent material");
            }
            
            if (highlightMaterial == null)
            {
                highlightMaterial = new Material(Shader.Find("Standard"));
                highlightMaterial.name = "HighlightMaterial";
                highlightMaterial.SetColor("_Color", highlightColor);
                
                highlightMaterial.SetFloat("_Metallic", 0.2f);
                highlightMaterial.SetFloat("_Glossiness", 0.8f);
                
                if (debugLogs) Debug.Log($"[PartHighlighter] Created highlight material with color: {highlightColor}");
            }
        }
        
        private void CacheOriginalMaterials()
        {
            originalMaterials.Clear();
            
            foreach (var renderer in partRenderers)
            {
                if (renderer != null && !originalMaterials.ContainsKey(renderer))
                {
                    originalMaterials[renderer] = renderer.sharedMaterials.ToArray();
                    if (debugLogs) Debug.Log($"[PartHighlighter] Cached {renderer.sharedMaterials.Length} materials for {renderer.gameObject.name}");
                }
            }
        }
        
        public void HighlightNextPart()
        {
            if (partRenderers.Count == 0) return;
            
            currentHighlightIndex = (currentHighlightIndex + 1) % partRenderers.Count;
            ApplyHighlight(currentHighlightIndex);
            
            if (debugLogs) Debug.Log($"[PartHighlighter] Highlighting part {currentHighlightIndex}: {partRenderers[currentHighlightIndex].gameObject.name}");
        }
        
        public void HighlightPreviousPart()
        {
            if (partRenderers.Count == 0) return;
            
            currentHighlightIndex--;
            if (currentHighlightIndex < 0) currentHighlightIndex = partRenderers.Count - 1;
            ApplyHighlight(currentHighlightIndex);
            
            if (debugLogs) Debug.Log($"[PartHighlighter] Highlighting part {currentHighlightIndex}: {partRenderers[currentHighlightIndex].gameObject.name}");
        }
        
        public void HighlightPart(int index)
        {
            if (partRenderers.Count == 0 || index < 0 || index >= partRenderers.Count) return;
            
            currentHighlightIndex = index;
            ApplyHighlight(index);
            
            if (debugLogs) Debug.Log($"[PartHighlighter] Highlighting part {index}: {partRenderers[index].gameObject.name}");
        }
        
        public void HighlightPartByName(string partName)
        {
            for (int i = 0; i < partRenderers.Count; i++)
            {
                if (partRenderers[i].gameObject.name.ToLower().Contains(partName.ToLower()))
                {
                    HighlightPart(i);
                    return;
                }
            }
            
            Debug.LogWarning($"[PartHighlighter] Part with name containing '{partName}' not found");
        }
        
        private void ApplyHighlight(int highlightIndex)
        {
            if (!isInitialized && partRenderers.Count == 0)
            {
                Initialize();
            }
            
            for (int i = 0; i < partRenderers.Count; i++)
            {
                if (partRenderers[i] == null) continue;
                
                if (i == highlightIndex)
                {
                    Material[] highlightMats = new Material[partRenderers[i].sharedMaterials.Length];
                    for (int j = 0; j < highlightMats.Length; j++)
                    {
                        highlightMats[j] = highlightMaterial;
                    }
                    partRenderers[i].sharedMaterials = highlightMats;
                }
                else
                {
                    if (originalMaterials.ContainsKey(partRenderers[i]))
                    {
                        Material[] transparentMats = new Material[originalMaterials[partRenderers[i]].Length];
                        for (int j = 0; j < transparentMats.Length; j++)
                        {
                            Material origMat = originalMaterials[partRenderers[i]][j];
                            if (origMat != null)
                            {
                                Material transMat = new Material(transparentMaterial);
                                
                                if (origMat.HasProperty("_MainTex"))
                                {
                                    transMat.SetTexture("_MainTex", origMat.GetTexture("_MainTex"));
                                }
                                
                                if (origMat.HasProperty("_Color"))
                                {
                                    Color origColor = origMat.GetColor("_Color");
                                    origColor.a = transparentAlpha;
                                    transMat.SetColor("_Color", origColor);
                                }
                                
                                transparentMats[j] = transMat;
                            }
                            else
                            {
                                transparentMats[j] = transparentMaterial;
                            }
                        }
                        partRenderers[i].sharedMaterials = transparentMats;
                    }
                }
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
            
            if (debugLogs) Debug.Log("[PartHighlighter] Restored original materials");
        }
        
        public void RefreshParts()
        {
            if (debugLogs) Debug.Log("[PartHighlighter] Refreshing parts");
            
            CollectPartRenderers();
            CacheOriginalMaterials();
            
            if (partRenderers.Count > 0 && currentHighlightIndex < partRenderers.Count)
            {
                ApplyHighlight(currentHighlightIndex);
            }
        }
        
        public List<string> GetPartNames()
        {
            return partRenderers.Select(r => r.gameObject.name).ToList();
        }
        
        public int GetPartCount()
        {
            return partRenderers.Count;
        }
        
        public int GetCurrentHighlightIndex()
        {
            return currentHighlightIndex;
        }
        
        private void OnDestroy()
        {
            if (transparentMaterial != null)
            {
                DestroyImmediate(transparentMaterial);
            }
            
            if (highlightMaterial != null)
            {
                DestroyImmediate(highlightMaterial);
            }
        }
        
        [ContextMenu("Debug Part List")]
        private void DebugPartList()
        {
            Debug.Log($"=== PartHighlighter Debug ===");
            Debug.Log($"GameObject: {gameObject.name}");
            Debug.Log($"Part Count: {partRenderers.Count}");
            Debug.Log($"Current Highlight Index: {currentHighlightIndex}");
            
            for (int i = 0; i < partRenderers.Count; i++)
            {
                string highlighted = (i == currentHighlightIndex) ? " [HIGHLIGHTED]" : "";
                Debug.Log($"Part {i}: {partRenderers[i].gameObject.name}{highlighted}");
            }
            Debug.Log($"=== End Debug ===");
        }
    }
}