using System;
using System.Collections.Generic;
using UnityEngine;
using HoloLab.QuestCameraTools.AprilTagTracking;

public class AprilTagObjectSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject aprilTagObjectPrefab;

    private QuestAprilTagTracking aprilTagTracking;
    private readonly Dictionary<int, GameObject> aprilTagObjects = new Dictionary<int, GameObject>();

    private void Start()
    {
        // Force clear any leftover objects
        foreach (var obj in aprilTagObjects.Values)
        {
            if (obj != null) 
            {
                Debug.Log($"AprilTagObjectSpawner: Destroying leftover object at {obj.transform.position}");
                DestroyImmediate(obj);
            }
        }
        aprilTagObjects.Clear();
        Debug.Log("AprilTagObjectSpawner: Cleared all existing AprilTag objects");
        
        aprilTagTracking = FindFirstObjectByType<QuestAprilTagTracking>();
        if (aprilTagTracking == null)
        {
            Debug.LogError($"{nameof(QuestAprilTagTracking)} not found in the scene.");
            return;
        }

        aprilTagTracking.OnAprilTagDetected += OnAprilTagDetected;
        Debug.Log("AprilTagObjectSpawner: Successfully subscribed to OnAprilTagDetected event");
    }
    
    // Method to force spawn a test object for debugging
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceSpawnTestObject()
    {
        if (aprilTagObjectPrefab != null)
        {
            var testObj = Instantiate(aprilTagObjectPrefab);
            testObj.transform.position = new Vector3(0, 1.5f, 2f);
            testObj.transform.localScale = Vector3.one * 0.2f;
            Debug.Log($"AprilTagObjectSpawner: Force spawned test object at {testObj.transform.position}");
            
            var allComponents = testObj.GetComponents<Component>();
            Debug.Log($"AprilTagObjectSpawner: Test object has {allComponents.Length} components:");
            foreach (var comp in allComponents)
            {
                Debug.Log($"  Component: {comp.GetType().Name}");
            }
        }
    }

    private void OnAprilTagDetected(List<AprilTagDetectedInfo> infoList)
    {
        Debug.Log($"AprilTagObjectSpawner: OnAprilTagDetected called with {infoList.Count} tags");
        
        foreach (var info in infoList)
        {
            if (aprilTagObjects.ContainsKey(info.ID))
            {
                Debug.Log($"AprilTagObjectSpawner: Tag ID {info.ID} already exists - object should be tracked automatically");
                continue;
            }

            if (aprilTagObjectPrefab == null)
            {
                Debug.LogError("AprilTagObjectSpawner: aprilTagObjectPrefab is null!");
                continue;
            }

            Debug.Log($"AprilTagObjectSpawner: Instantiating object for tag ID {info.ID}");
            var aprilTagGameObject = Instantiate(aprilTagObjectPrefab);
            
            // Debug all components on the spawned object
            var allComponents = aprilTagGameObject.GetComponents<Component>();
            Debug.Log($"AprilTagObjectSpawner: Spawned object has {allComponents.Length} components:");
            foreach (var comp in allComponents)
            {
                Debug.Log($"  Component: {comp.GetType().Name}");
            }
            
            // Find the AprilTagObject component and set the info
            var aprilTagComponent = aprilTagGameObject.GetComponent<HoloLab.QuestCameraTools.AprilTagTracking.Samples.AprilTagObject>();
            if (aprilTagComponent != null)
            {
                aprilTagComponent.SetAprilTagDetectedInfo(info);
                Debug.Log($"AprilTagObjectSpawner: Set AprilTagDetectedInfo for tag ID {info.ID}");
            }
            else
            {
                Debug.LogError($"AprilTagObjectSpawner: No AprilTagObject component found on prefab for tag ID {info.ID}");
            }
            
            // Check for AprilTagTracker component
            var trackerComponent = aprilTagGameObject.GetComponent<HoloLab.QuestCameraTools.AprilTagTracking.AprilTagTracker>();
            if (trackerComponent != null)
            {
                Debug.Log($"AprilTagObjectSpawner: Found AprilTagTracker component, target ID: {trackerComponent.TargetAprilTagID}");
            }
            else
            {
                Debug.LogError($"AprilTagObjectSpawner: No AprilTagTracker component found on prefab for tag ID {info.ID}");
            }

            aprilTagObjects[info.ID] = aprilTagGameObject;
            Debug.Log($"AprilTagObjectSpawner: Successfully spawned object for tag ID {info.ID}");
        }
    }
}