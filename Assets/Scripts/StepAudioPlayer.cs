using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QuestCameraTools.App
{
    public class StepAudioPlayer : MonoBehaviour
    {
        [System.Serializable]
        public class StepAudio
        {
            public int stepNumber;
            public AudioClip audioClip;
            [Tooltip("Override the default delay for this specific step")]
            public float customDelay = -1f; // -1 means use default
        }

        [Header("References")]
        [SerializeField] private StepCounter stepCounter;
        [SerializeField] private AudioSource audioSource;
        
        [Header("Audio Configuration")]
        [SerializeField] private float defaultAudioDelay = 1.0f;
        [SerializeField] private List<StepAudio> stepAudioClips = new List<StepAudio>();
        
        [Header("Playback Settings")]
        [SerializeField] private bool stopAudioOnStepChange = true;
        [SerializeField] private float fadeOutDuration = 0.2f;
        
        private int currentStep = -1;
        private Coroutine audioPlaybackCoroutine;
        private Coroutine fadeCoroutine;

        private void Start()
        {
            // Auto-find StepCounter if not assigned
            if (stepCounter == null)
            {
                stepCounter = GetComponent<StepCounter>();
                if (stepCounter == null)
                {
                    stepCounter = FindObjectOfType<StepCounter>();
                }
            }
            
            // Auto-find AudioSource if not assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    Debug.Log("[StepAudioPlayer] Created AudioSource component");
                }
            }
            
            if (stepCounter == null)
            {
                Debug.LogError("[StepAudioPlayer] No StepCounter found! Audio playback will not work.");
                return;
            }
            
            // Initialize with current step
            currentStep = stepCounter.GetCurrentStep();
            Debug.Log($"[StepAudioPlayer] Initialized with step {currentStep}");
        }

        private void Update()
        {
            if (stepCounter == null) return;
            
            int newStep = stepCounter.GetCurrentStep();
            if (newStep != currentStep)
            {
                Debug.Log($"[StepAudioPlayer] Step changed from {currentStep} to {newStep}");
                OnStepChanged(currentStep, newStep);
                currentStep = newStep;
            }
        }

        private void OnStepChanged(int oldStep, int newStep)
        {
            // Cancel any pending audio playback
            if (audioPlaybackCoroutine != null)
            {
                StopCoroutine(audioPlaybackCoroutine);
                audioPlaybackCoroutine = null;
                Debug.Log("[StepAudioPlayer] Cancelled pending audio playback");
            }
            
            // Stop current audio if configured
            if (stopAudioOnStepChange && audioSource.isPlaying)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }
                fadeCoroutine = StartCoroutine(FadeOutAndStop());
            }
            
            // Find audio for new step
            var stepAudio = stepAudioClips.FirstOrDefault(sa => sa.stepNumber == newStep);
            if (stepAudio != null && stepAudio.audioClip != null)
            {
                float delay = stepAudio.customDelay >= 0 ? stepAudio.customDelay : defaultAudioDelay;
                audioPlaybackCoroutine = StartCoroutine(PlayAudioWithDelay(stepAudio.audioClip, delay));
                Debug.Log($"[StepAudioPlayer] Scheduled audio '{stepAudio.audioClip.name}' for step {newStep} with {delay}s delay");
            }
            else
            {
                Debug.Log($"[StepAudioPlayer] No audio clip found for step {newStep}");
            }
        }

        private IEnumerator PlayAudioWithDelay(AudioClip clip, float delay)
        {
            Debug.Log($"[StepAudioPlayer] Waiting {delay} seconds before playing audio");
            yield return new WaitForSeconds(delay);
            
            if (audioSource != null && clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log($"[StepAudioPlayer] Playing audio clip: {clip.name}");
            }
            
            audioPlaybackCoroutine = null;
        }

        private IEnumerator FadeOutAndStop()
        {
            float startVolume = audioSource.volume;
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeOutDuration);
                yield return null;
            }
            
            audioSource.Stop();
            audioSource.volume = startVolume; // Reset volume for next playback
            fadeCoroutine = null;
            Debug.Log("[StepAudioPlayer] Audio faded out and stopped");
        }

        // Public method to manually trigger audio for current step
        public void PlayCurrentStepAudio()
        {
            if (stepCounter == null) return;
            
            int step = stepCounter.GetCurrentStep();
            var stepAudio = stepAudioClips.FirstOrDefault(sa => sa.stepNumber == step);
            
            if (stepAudio != null && stepAudio.audioClip != null)
            {
                if (audioPlaybackCoroutine != null)
                {
                    StopCoroutine(audioPlaybackCoroutine);
                }
                
                float delay = stepAudio.customDelay >= 0 ? stepAudio.customDelay : defaultAudioDelay;
                audioPlaybackCoroutine = StartCoroutine(PlayAudioWithDelay(stepAudio.audioClip, delay));
            }
        }

        // Method to sync with StepBasedModelSwitcher if needed
        public void SyncWithModelSwitcher(StepBasedModelSwitcher modelSwitcher)
        {
            if (modelSwitcher == null) return;
            
            // This method can be extended to automatically sync audio clips
            // from the model switcher's step configuration
            Debug.Log("[StepAudioPlayer] Synced with StepBasedModelSwitcher");
        }

        // Debug method to list all configured audio
        [ContextMenu("Debug Audio Configuration")]
        public void DebugAudioConfiguration()
        {
            Debug.Log("=== StepAudioPlayer Configuration ===");
            Debug.Log($"Default Delay: {defaultAudioDelay}s");
            Debug.Log($"Stop on Step Change: {stopAudioOnStepChange}");
            Debug.Log($"Fade Duration: {fadeOutDuration}s");
            Debug.Log($"Total Step Audio Clips: {stepAudioClips.Count}");
            
            foreach (var stepAudio in stepAudioClips)
            {
                string clipName = stepAudio.audioClip != null ? stepAudio.audioClip.name : "None";
                float delay = stepAudio.customDelay >= 0 ? stepAudio.customDelay : defaultAudioDelay;
                Debug.Log($"Step {stepAudio.stepNumber}: {clipName} (delay: {delay}s)");
            }
            
            Debug.Log($"Current Step: {currentStep}");
            Debug.Log($"Audio Source: {(audioSource != null ? "Found" : "Missing")}");
            Debug.Log($"Step Counter: {(stepCounter != null ? "Found" : "Missing")}");
            Debug.Log("=== End Configuration ===");
        }
    }
}