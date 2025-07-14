using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuestCameraTools.App
{
    public class GeminiVoiceToggleSimple : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Voice Materials")]
        [SerializeField] private Material idleMaterial;
        [SerializeField] private Material recordingMaterial;
        [SerializeField] private Material processingMaterial;
        [SerializeField] private Material playingMaterial;
        
        [Header("References")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Image targetImage;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private PassthroughScreenshotCapture screenshotCapture;
        [SerializeField] private GeminiTTSManager ttsManager;
        [SerializeField] private SimpleGeminiClient geminiClient;
        [SerializeField] private StepCounter stepCounter;
        
        [Header("Audio Settings")]
        [SerializeField] private int recordingFrequency = 44100;
        [SerializeField] private float maxRecordingTime = 30f;
        [SerializeField] private float audioVolume = 0.8f;
        
        [Header("API Settings")]
        [SerializeField] private string apiKey = "AIzaSyA7r_rDvnettdUnrORdmEkGGfmk4mak3Hc"; // Set in inspector or use environment variable
        [SerializeField] private string geminiModel = "gemini-1.5-flash";
        [SerializeField] private bool enableVoiceActivity = true;
        [SerializeField] private float voiceActivityThreshold = 0.01f;
        
        private enum VoiceState
        {
            Idle,
            Recording,
            Processing,
            Playing
        }
        
        private VoiceState currentState = VoiceState.Idle;
        private AudioClip recordedClip;
        private bool isRecording = false;
        private string microphoneDevice;
        
        private void Start()
        {
            // Auto-assign components if not set
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();
            
            if (targetImage == null)
                targetImage = GetComponent<Image>();
            
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    Debug.LogWarning("[GeminiVoiceToggle] No AudioSource component found on this GameObject. Audio playback will be disabled.");
                }
                else
                {
                    Debug.Log("[GeminiVoiceToggle] AudioSource component found and assigned.");
                    // Set initial volume
                    audioSource.volume = audioVolume;
                }
            }
            
            // Auto-assign Quest components
            if (screenshotCapture == null)
                screenshotCapture = FindObjectOfType<PassthroughScreenshotCapture>();
            
            if (ttsManager == null)
                ttsManager = FindObjectOfType<GeminiTTSManager>();
            
            if (geminiClient == null)
                geminiClient = FindObjectOfType<SimpleGeminiClient>();
            
            if (stepCounter == null)
                stepCounter = FindObjectOfType<StepCounter>();
            
            // Find and validate SimpleWebCamManager
            var webCamManager = FindObjectOfType<SimpleWebCamManager>();
            if (webCamManager == null)
            {
                Debug.LogError("[GeminiVoiceToggle] SimpleWebCamManager not found in scene! Camera capture will not work.");
            }
            else
            {
                Debug.Log($"[GeminiVoiceToggle] SimpleWebCamManager found: {webCamManager.gameObject.name}");
                Debug.Log($"[GeminiVoiceToggle] SimpleWebCamManager initialized: {webCamManager.IsInitialized}");
                Debug.Log($"[GeminiVoiceToggle] SimpleWebCamManager enabled: {webCamManager.enabled}");
            }
            
            // Ensure QuestCameraPermissions component exists
            var cameraPermissions = FindObjectOfType<QuestCameraPermissions>();
            if (cameraPermissions == null)
            {
                Debug.LogWarning("[GeminiVoiceToggle] QuestCameraPermissions not found in scene. Creating one...");
                var permissionObject = new GameObject("QuestCameraPermissions");
                cameraPermissions = permissionObject.AddComponent<QuestCameraPermissions>();
                Debug.Log("[GeminiVoiceToggle] QuestCameraPermissions component created");
            }
            else
            {
                Debug.Log($"[GeminiVoiceToggle] QuestCameraPermissions found: {cameraPermissions.gameObject.name}");
            }
            
            // Check materials
            if (idleMaterial == null || recordingMaterial == null || processingMaterial == null || playingMaterial == null)
            {
                Debug.LogError("[GeminiVoiceToggle] Please assign all materials (Idle, Recording, Processing, Playing)!");
                return;
            }
            
            // Validate Quest components (warn but don't fail - they're optional)
            if (screenshotCapture == null)
            {
                Debug.LogWarning("[GeminiVoiceToggle] PassthroughScreenshotCapture not found! Camera screenshots will be disabled.");
            }
            
            if (ttsManager == null)
            {
                Debug.LogWarning("[GeminiVoiceToggle] GeminiTTSManager not found! TTS will be disabled.");
            }
            else
            {
                // Initialize TTS manager with API key
                StartCoroutine(InitializeTTSManager());
            }
            
            if (geminiClient == null)
            {
                Debug.LogWarning("[GeminiVoiceToggle] SimpleGeminiClient not found! Gemini API will be disabled.");
            }
            
            if (stepCounter == null)
            {
                Debug.LogWarning("[GeminiVoiceToggle] StepCounter not found! Step awareness will be disabled.");
            }
            
            // Initialize microphone
            if (Microphone.devices.Length > 0)
            {
                microphoneDevice = Microphone.devices[0];
                Debug.Log($"[GeminiVoiceToggle] Using microphone: {microphoneDevice}");
            }
            else
            {
                Debug.LogError("[GeminiVoiceToggle] No microphone found!");
                return;
            }
            
            // Get API key
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = PlayerPrefs.GetString("GeminiAPIKey", "");
                }
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[GeminiVoiceToggle] No API key found! Set GEMINI_API_KEY environment variable or assign in inspector.");
            }
            
            UpdateMaterial();
            Debug.Log($"[GeminiVoiceToggle] Initialized on {gameObject.name}");
        }
        
        private System.Collections.IEnumerator InitializeTTSManager()
        {
            // Reduce initialization delay for faster startup
            yield return null;
            if (ttsManager != null && !string.IsNullOrEmpty(apiKey))
            {
                Debug.Log("[GeminiVoiceToggle] Setting TTS manager API key");
                ttsManager.SetAPIKey(apiKey);
                // InitializeTTS is called automatically in the TTS manager's Start method
                Debug.Log($"[GeminiVoiceToggle] TTS manager ready status: {ttsManager.IsReady()}");
            }
        }
        
        // Interface implementations for direct pointer events
        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log("[GeminiVoiceToggle] OnPointerDown - Button pressed");
            StartVoiceRecording();
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log("[GeminiVoiceToggle] OnPointerUp - Button released");
            StopVoiceRecording();
        }
        
        // Public methods for UnityEvents (like your AudioToggle.ToggleAudio)
        /// <summary>
        /// Start voice recording - call this from UnityEvents
        /// </summary>
        public void StartVoiceRecording()
        {
            Debug.Log($"[GeminiVoiceToggle] StartVoiceRecording called - Current state: {currentState}");
            
            if (currentState == VoiceState.Playing)
            {
                Debug.Log("[GeminiVoiceToggle] Currently playing audio, stopping playback first");
                StopAudioPlayback();
                return;
            }
            
            if (currentState == VoiceState.Idle)
            {
                Debug.Log("[GeminiVoiceToggle] State is Idle, starting recording");
                StartRecording();
            }
            else
            {
                Debug.LogWarning($"[GeminiVoiceToggle] Cannot start recording in state: {currentState}");
            }
        }
        
        /// <summary>
        /// Stop voice recording and process - call this from UnityEvents
        /// </summary>
        public void StopVoiceRecording()
        {
            Debug.Log($"[GeminiVoiceToggle] StopVoiceRecording called - Current state: {currentState}");
            
            if (currentState == VoiceState.Recording)
            {
                Debug.Log("[GeminiVoiceToggle] Currently recording, stopping recording");
                StopRecording();
            }
            else
            {
                Debug.LogWarning($"[GeminiVoiceToggle] Not recording, current state: {currentState}");
            }
        }
        
        /// <summary>
        /// Toggle between recording and stopping - alternative single-click method
        /// </summary>
        public void ToggleVoiceRecording()
        {
            if (currentState == VoiceState.Idle)
            {
                StartVoiceRecording();
            }
            else if (currentState == VoiceState.Recording)
            {
                StopVoiceRecording();
            }
            else if (currentState == VoiceState.Playing)
            {
                StopAudioPlayback();
            }
        }
        
        /// <summary>
        /// Stop any current audio playback - call this from UnityEvents
        /// </summary>
        public void StopAudioPlayback()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            // Stop any running coroutines
            StopAllCoroutines();
            
            currentState = VoiceState.Idle;
            UpdateMaterial();
            Debug.Log("[GeminiVoiceToggle] Audio playback stopped");
        }
        
        /// <summary>
        /// Test camera capture functionality - call this to debug camera issues
        /// </summary>
        public void TestCameraCapture()
        {
            Debug.Log("[GeminiVoiceToggle] TestCameraCapture called");
            
            // Find SimpleWebCamManager
            var webCamManager = FindObjectOfType<SimpleWebCamManager>();
            if (webCamManager == null)
            {
                Debug.LogError("[GeminiVoiceToggle] SimpleWebCamManager not found in scene!");
                return;
            }
            
            Debug.Log($"[GeminiVoiceToggle] SimpleWebCamManager found: {webCamManager.gameObject.name}");
            Debug.Log($"[GeminiVoiceToggle] IsInitialized: {webCamManager.IsInitialized}");
            Debug.Log($"[GeminiVoiceToggle] IsPlaying: {webCamManager.IsPlaying}");
            Debug.Log($"[GeminiVoiceToggle] IsCameraReady: {webCamManager.IsCameraReady()}");
            
            if (!webCamManager.IsInitialized)
            {
                Debug.LogError("[GeminiVoiceToggle] SimpleWebCamManager is not initialized. Check camera permissions and Quest device support.");
                return;
            }
            
            // Test direct capture
            var testImage = webCamManager.CaptureScreenshot(true);
            Debug.Log($"[GeminiVoiceToggle] Direct capture test result: {testImage.Length} bytes");
            
            // Test through PassthroughScreenshotCapture
            if (screenshotCapture != null)
            {
                Debug.Log("[GeminiVoiceToggle] Testing through PassthroughScreenshotCapture...");
                var screenshotData = screenshotCapture.CaptureScreenshot();
                Debug.Log($"[GeminiVoiceToggle] PassthroughScreenshotCapture test result: {screenshotData.pngData?.Length ?? 0} bytes");
            }
            else
            {
                Debug.LogError("[GeminiVoiceToggle] PassthroughScreenshotCapture not found!");
            }
        }
        
        /// <summary>
        /// Force camera initialization - for debugging camera issues
        /// </summary>
        public void ForceInitializeCamera()
        {
            Debug.Log("[GeminiVoiceToggle] ForceInitializeCamera called");
            
            var webCamManager = FindObjectOfType<SimpleWebCamManager>();
            if (webCamManager == null)
            {
                Debug.LogError("[GeminiVoiceToggle] SimpleWebCamManager not found in scene!");
                return;
            }
            
            // webCamManager.ForceInitializeCamera();
        }
        
        /// <summary>
        /// Try different camera for testing - cycles through available cameras
        /// </summary>
        public void TryNextCamera()
        {
            Debug.Log("[GeminiVoiceToggle] TryNextCamera called");
            
            var webCamManager = FindObjectOfType<SimpleWebCamManager>();
            if (webCamManager == null)
            {
                Debug.LogError("[GeminiVoiceToggle] SimpleWebCamManager not found in scene!");
                return;
            }
            
            // This will force a reinitialization which should select the next rear-facing camera
            
            // Test capture immediately
            StartCoroutine(TestCameraAfterDelay());
        }
        
        private System.Collections.IEnumerator TestCameraAfterDelay()
        {
            yield return new WaitForSeconds(1f); // Wait for camera to initialize
            
            TestCameraCapture();
        }
        
        /// <summary>
        /// Manually request camera permissions - for debugging permission issues
        /// </summary>
        public void RequestCameraPermissions()
        {
            Debug.Log("[GeminiVoiceToggle] RequestCameraPermissions called");
            
            var cameraPermissions = FindObjectOfType<QuestCameraPermissions>();
            if (cameraPermissions == null)
            {
                Debug.LogError("[GeminiVoiceToggle] QuestCameraPermissions not found in scene!");
                return;
            }
            
            Debug.Log("[GeminiVoiceToggle] Forcing camera permission request...");
            cameraPermissions.ForcePermissionCheck();
            
            // Also try to reinitialize the camera after a delay
            StartCoroutine(ReinitializeCameraAfterPermissions());
        }
        
        private System.Collections.IEnumerator ReinitializeCameraAfterPermissions()
        {
            yield return new WaitForSeconds(2f); // Give permissions time to process
            
            var webCamManager = FindObjectOfType<SimpleWebCamManager>();
            if (webCamManager != null)
            {
                Debug.Log("[GeminiVoiceToggle] Attempting to reinitialize camera after permission request");
            }
        }
        private IEnumerator EnsureTTSManagerReady()
        {
            // Wait for TTS manager to be ready
            float timeout = 5f;
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                if (ttsManager != null && ttsManager.IsReady())
                {
                    Debug.Log("[GeminiVoiceToggle] TTS Manager is ready");
                    yield return null;
                    yield break;
                }
                
                // If TTS manager exists but isn't ready, try to initialize it
                if (ttsManager != null && !string.IsNullOrEmpty(apiKey))
                {
                    Debug.Log("[GeminiVoiceToggle] Attempting to initialize TTS Manager");
                    ttsManager.SetAPIKey(apiKey);
                    
                    // Reduce initialization delay for faster processing
                    yield return new WaitForSeconds(0.1f);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Debug.LogError("[GeminiVoiceToggle] TTS Manager failed to initialize within timeout");
        }

        private void StartRecording()
        {
            if (string.IsNullOrEmpty(microphoneDevice))
            {
                Debug.LogError("[GeminiVoiceToggle] No microphone available!");
                return;
            }
            
            Debug.Log($"[GeminiVoiceToggle] Starting recording with device: {microphoneDevice}");
            Debug.Log($"[GeminiVoiceToggle] Recording settings - Frequency: {recordingFrequency}, Max time: {maxRecordingTime}s");
            
            currentState = VoiceState.Recording;
            UpdateMaterial();
            
            recordedClip = Microphone.Start(microphoneDevice, false, (int)maxRecordingTime, recordingFrequency);
            isRecording = true;
            
            if (recordedClip != null)
            {
                Debug.Log($"[GeminiVoiceToggle] Microphone started successfully. AudioClip created with {recordedClip.channels} channels, {recordedClip.frequency}Hz");
            }
            else
            {
                Debug.LogError("[GeminiVoiceToggle] Failed to create AudioClip from microphone!");
                currentState = VoiceState.Idle;
                UpdateMaterial();
                return;
            }
            
            // Auto-stop after max time
            StartCoroutine(AutoStopRecording());
            
            // Start voice activity detection if enabled
            if (enableVoiceActivity)
            {
                Debug.Log("[GeminiVoiceToggle] Starting voice activity monitoring");
                StartCoroutine(MonitorVoiceActivity());
            }
        }
        
        private void StopRecording()
        {
            if (!isRecording) 
            {
                Debug.LogWarning("[GeminiVoiceToggle] StopRecording called but isRecording is false");
                return;
            }
            
            Debug.Log("[GeminiVoiceToggle] Stopping recording...");
            isRecording = false;
            
            // Get microphone position before ending
            int micPosition = Microphone.GetPosition(microphoneDevice);
            Debug.Log($"[GeminiVoiceToggle] Microphone position at stop: {micPosition}");
            
            Microphone.End(microphoneDevice);
            Debug.Log("[GeminiVoiceToggle] Microphone.End() called");
            
            if (recordedClip != null)
            {
                Debug.Log($"[GeminiVoiceToggle] AudioClip info - Samples: {recordedClip.samples}, Length: {recordedClip.length}s, Channels: {recordedClip.channels}");
                
                // Check if we have actual audio data by examining the samples
                if (recordedClip.samples > 0 && micPosition > 0)
                {
                    // Get a small sample to check if there's actual audio
                    float[] testSamples = new float[Mathf.Min(1024, recordedClip.samples)];
                    recordedClip.GetData(testSamples, 0);
                    
                    float maxSample = 0f;
                    for (int i = 0; i < testSamples.Length; i++)
                    {
                        maxSample = Mathf.Max(maxSample, Mathf.Abs(testSamples[i]));
                    }
                    
                    Debug.Log($"[GeminiVoiceToggle] Audio data check - Max sample: {maxSample:F4}, Mic position: {micPosition}");
                    
                    if (maxSample > 0.0001f || micPosition > 100) // Either we have audio or mic moved
                    {
                        Debug.Log("[GeminiVoiceToggle] Audio recorded successfully, processing voice query");
                        ProcessVoiceQuery();
                    }
                    else
                    {
                        Debug.LogWarning("[GeminiVoiceToggle] Audio appears to be silent or very quiet!");
                        currentState = VoiceState.Idle;
                        UpdateMaterial();
                    }
                }
                else
                {
                    Debug.LogWarning($"[GeminiVoiceToggle] AudioClip has {recordedClip.samples} samples, mic position: {micPosition}");
                    currentState = VoiceState.Idle;
                    UpdateMaterial();
                }
            }
            else
            {
                Debug.LogError("[GeminiVoiceToggle] AudioClip is null!");
                currentState = VoiceState.Idle;
                UpdateMaterial();
            }
        }
        
        private IEnumerator AutoStopRecording()
        {
            yield return new WaitForSeconds(maxRecordingTime);
            if (isRecording)
            {
                Debug.Log($"[GeminiVoiceToggle] Auto-stopping recording after {maxRecordingTime}s");
                StopRecording();
            }
        }
        
        private IEnumerator MonitorVoiceActivity()
        {
            int voiceActivityCount = 0;
            while (isRecording)
            {
                if (recordedClip != null)
                {
                    float[] samples = new float[1024];
                    int micPosition = Microphone.GetPosition(microphoneDevice);
                    
                    if (micPosition > 0)
                    {
                        int startIndex = Mathf.Max(0, micPosition - 1024);
                        recordedClip.GetData(samples, startIndex);
                        
                        // Calculate RMS (Root Mean Square) for volume detection
                        float rms = 0f;
                        for (int i = 0; i < samples.Length; i++)
                        {
                            rms += samples[i] * samples[i];
                        }
                        rms = Mathf.Sqrt(rms / samples.Length);
                        
                        // Update visual feedback based on voice activity
                        if (rms > voiceActivityThreshold)
                        {
                            voiceActivityCount++;
                            // Voice detected - could add visual feedback here
                            Debug.Log($"[GeminiVoiceToggle] Voice activity detected: {rms:F3} (count: {voiceActivityCount})");
                        }
                        
                        // Log mic position periodically
                        if (voiceActivityCount % 10 == 0)
                        {
                            Debug.Log($"[GeminiVoiceToggle] Microphone position: {micPosition}/{recordedClip.samples}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[GeminiVoiceToggle] Microphone position is 0 or negative: {micPosition}");
                    }
                }
                else
                {
                    Debug.LogError("[GeminiVoiceToggle] recordedClip is null during voice activity monitoring");
                    break;
                }
                
                yield return new WaitForSeconds(0.1f); // Check every 100ms
            }
            
            Debug.Log($"[GeminiVoiceToggle] Voice activity monitoring stopped. Total voice detections: {voiceActivityCount}");
        }
        
        private void ProcessVoiceQuery()
        {
            Debug.Log("[GeminiVoiceToggle] ProcessVoiceQuery started");
            currentState = VoiceState.Processing;
            UpdateMaterial();
            
            StartCoroutine(ProcessVoiceCoroutine());
        }
        
        private IEnumerator ProcessVoiceCoroutine()
        {
            Debug.Log("[GeminiVoiceToggle] ProcessVoiceCoroutine started");
            
            // Convert AudioClip to WAV bytes
            var audioBytes = ConvertAudioClipToWAV(recordedClip);
            Debug.Log($"[GeminiVoiceToggle] Audio converted to WAV: {audioBytes?.Length ?? 0} bytes");
            
            // Capture screenshot
            var screenshotBytes = CaptureScreenshot();
            Debug.Log($"[GeminiVoiceToggle] Screenshot captured: {screenshotBytes?.Length ?? 0} bytes");
            
            // Send to Gemini API
            yield return StartCoroutine(SendGeminiRequest(audioBytes, screenshotBytes));
        }
        
        private byte[] ConvertAudioClipToWAV(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogError("[GeminiVoiceToggle] AudioClip is null in ConvertAudioClipToWAV");
                return new byte[0];
            }
            
            Debug.Log($"[GeminiVoiceToggle] Converting AudioClip to WAV:");
            Debug.Log($"  - Samples: {clip.samples}");
            Debug.Log($"  - Channels: {clip.channels}");
            Debug.Log($"  - Frequency: {clip.frequency}Hz");
            Debug.Log($"  - Length: {clip.length}s");
            
            // Simple WAV conversion
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            
            // Check if we actually have audio data
            float maxSample = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                maxSample = Mathf.Max(maxSample, Mathf.Abs(samples[i]));
            }
            Debug.Log($"[GeminiVoiceToggle] Max sample value: {maxSample:F4}");
            
            if (maxSample < 0.001f)
            {
                Debug.LogWarning("[GeminiVoiceToggle] Audio samples are very quiet or silent!");
            }
            
            // Convert to 16-bit PCM
            var pcmData = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                var sample = Mathf.Clamp(samples[i], -1f, 1f);
                var intSample = (short)(sample * 32767f);
                pcmData[i * 2] = (byte)(intSample & 0xFF);
                pcmData[i * 2 + 1] = (byte)((intSample >> 8) & 0xFF);
            }
            
            // Create WAV header
            var header = new byte[44];
            var hz = clip.frequency;
            var channels = clip.channels;
            var samples_per_channel = clip.samples;
            
            // WAV header
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(header, 0);
            System.BitConverter.GetBytes(36 + pcmData.Length).CopyTo(header, 4);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(header, 8);
            System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(header, 12);
            System.BitConverter.GetBytes(16).CopyTo(header, 16);
            System.BitConverter.GetBytes((short)1).CopyTo(header, 20);
            System.BitConverter.GetBytes((short)channels).CopyTo(header, 22);
            System.BitConverter.GetBytes(hz).CopyTo(header, 24);
            System.BitConverter.GetBytes(hz * channels * 2).CopyTo(header, 28);
            System.BitConverter.GetBytes((short)(channels * 2)).CopyTo(header, 32);
            System.BitConverter.GetBytes((short)16).CopyTo(header, 34);
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(header, 36);
            System.BitConverter.GetBytes(pcmData.Length).CopyTo(header, 40);
            
            // Combine header and data
            var wavData = new byte[header.Length + pcmData.Length];
            header.CopyTo(wavData, 0);
            pcmData.CopyTo(wavData, header.Length);
            
            Debug.Log($"[GeminiVoiceToggle] WAV file created: {wavData.Length} bytes total ({header.Length} header + {pcmData.Length} data)");
            
            return wavData;
        }
        
        private byte[] CaptureScreenshot()
        {
            // Use Quest passthrough camera screenshot capture
            if (screenshotCapture == null)
            {
                Debug.LogError("[GeminiVoiceToggle] PassthroughScreenshotCapture not available");
                return new byte[0];
            }
            
            Debug.Log($"[GeminiVoiceToggle] PassthroughScreenshotCapture found: {screenshotCapture.gameObject.name}");
            
            if (!screenshotCapture.IsCameraReady())
            {
                Debug.LogWarning("[GeminiVoiceToggle] Quest passthrough camera not ready");
                return new byte[0];
            }
            
            Debug.Log("[GeminiVoiceToggle] Quest passthrough camera is ready, capturing screenshot...");
            var screenshotData = screenshotCapture.CaptureScreenshot();
            
            if (screenshotData.isValid && screenshotData.pngData != null)
            {
                Debug.Log($"[GeminiVoiceToggle] Quest screenshot captured: {screenshotData.width}x{screenshotData.height}, {screenshotData.pngData.Length} bytes");
                
                // Validate image data - check if it's all black (common issue)
                if (screenshotData.pngData.Length > 100) // Basic size check
                {
                    // Check PNG header to ensure it's a valid PNG
                    if (screenshotData.pngData[0] == 0x89 && screenshotData.pngData[1] == 0x50 && 
                        screenshotData.pngData[2] == 0x4E && screenshotData.pngData[3] == 0x47)
                    {
                        Debug.Log("[GeminiVoiceToggle] PNG header validation passed");
                    }
                    else
                    {
                        Debug.LogWarning("[GeminiVoiceToggle] PNG header validation failed - may not be valid PNG data");
                    }
                    
                    // Sample some bytes to check for all-black image
                    int sampleSize = Mathf.Min(1000, screenshotData.pngData.Length - 50);
                    int nonZeroBytes = 0;
                    for (int i = 50; i < 50 + sampleSize; i++) // Skip PNG header
                    {
                        if (screenshotData.pngData[i] != 0) nonZeroBytes++;
                    }
                    
                    float nonZeroRatio = (float)nonZeroBytes / sampleSize;
                    Debug.Log($"[GeminiVoiceToggle] Image data analysis: {nonZeroBytes}/{sampleSize} non-zero bytes ({nonZeroRatio:P})");
                    
                    if (nonZeroRatio < 0.1f)
                    {
                        Debug.LogWarning("[GeminiVoiceToggle] Image appears to be mostly black/empty - passthrough may not be working");
                    }
                }
                
                return screenshotData.pngData;
            }
            else
            {
                Debug.LogError($"[GeminiVoiceToggle] Failed to capture Quest screenshot - isValid: {screenshotData.isValid}, pngData: {screenshotData.pngData?.Length ?? 0} bytes");
                return new byte[0];
            }
        }
        
        private IEnumerator SendGeminiRequest(byte[] audioBytes, byte[] imageBytes)
        {
            Debug.Log("[GeminiVoiceToggle] SendGeminiRequest started");
            
            if (geminiClient == null || !geminiClient.IsReady())
            {
                Debug.LogError("[GeminiVoiceToggle] Gemini client not ready!");
                currentState = VoiceState.Idle;
                UpdateMaterial();
                yield break;
            }
            
            Debug.Log($"[GeminiVoiceToggle] Gemini client is ready");
            
            // Handle missing image data gracefully
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Debug.LogWarning("[GeminiVoiceToggle] No image data available, sending audio-only request");
                imageBytes = null;
            }
            
            // Validate audio data
            if (audioBytes == null || audioBytes.Length == 0)
            {
                Debug.LogError("[GeminiVoiceToggle] No audio data available!");
                currentState = VoiceState.Idle;
                UpdateMaterial();
                yield break;
            }
            
            // Get current step information
            int currentStep = stepCounter != null ? stepCounter.GetCurrentStep() : 1;
            
            // Build step-aware prompt
            string stepPrompt = BuildStepAwarePrompt(currentStep - 1, imageBytes != null);
            
            var request = new SimpleGeminiClient.GeminiRequest
            {
                textPrompt = stepPrompt,
                imageData = imageBytes,
                audioData = audioBytes,
                imageMimeType = imageBytes != null ? "image/png" : null,
                audioMimeType = "audio/wav"
            };
            
            Debug.Log($"[GeminiVoiceToggle] Sending request:");
            Debug.Log($"[GeminiVoiceToggle]  - Text prompt: {request.textPrompt}");
            Debug.Log($"[GeminiVoiceToggle]  - Audio data: {request.audioData?.Length ?? 0} bytes");
            Debug.Log($"[GeminiVoiceToggle]  - Image data: {request.imageData?.Length ?? 0} bytes");
            Debug.Log($"[GeminiVoiceToggle]  - Audio MIME: {request.audioMimeType}");
            Debug.Log($"[GeminiVoiceToggle]  - Image MIME: {request.imageMimeType}");
            
            bool requestCompleted = false;
            SimpleGeminiClient.GeminiResponse response = null;
            
            geminiClient.SendRequest(request, (geminiResponse) => {
                response = geminiResponse;
                requestCompleted = true;
                Debug.Log($"[GeminiVoiceToggle] Gemini response received - Success: {geminiResponse.success}");
                if (!geminiResponse.success)
                {
                    Debug.LogError($"[GeminiVoiceToggle] Gemini error details: {geminiResponse.error}");
                }
            });
            
            // Wait for response
            while (!requestCompleted)
            {
                yield return null;
            }
            
            if (response.success && !string.IsNullOrEmpty(response.content))
            {
                Debug.Log($"[GeminiVoiceToggle] Gemini response: {response.content}");
                StartCoroutine(PlayTextToSpeech(response.content));
            }
            else
            {
                Debug.LogError($"[GeminiVoiceToggle] Gemini request failed: {response.error}");
                currentState = VoiceState.Idle;
                UpdateMaterial();
            }
        }
        
        
        private IEnumerator PlayTextToSpeech(string text)
        {
            Debug.Log($"[GeminiVoiceToggle] Converting text to speech: {text}");
            
            // Show processing state during TTS conversion
            currentState = VoiceState.Processing;
            UpdateMaterial();
            
            // Ensure TTS manager is ready
            yield return StartCoroutine(EnsureTTSManagerReady());
            
            if (ttsManager != null && ttsManager.IsReady())
            {
                bool ttsCompleted = false;
                AudioClip ttsClip = null;
                string ttsError = null;
                
                // Request TTS conversion
                ttsManager.ConvertTextToSpeech(text, (response) => {
                    ttsCompleted = true;
                    if (response.success)
                    {
                        ttsClip = response.audioClip;
                        Debug.Log($"[GeminiVoiceToggle] TTS conversion successful: {response.audioLength} samples");
                    }
                    else
                    {
                        ttsError = response.error;
                        Debug.LogError($"[GeminiVoiceToggle] TTS conversion failed: {response.error}");
                    }
                });
                
                // Wait for TTS completion
                while (!ttsCompleted)
                {
                    yield return null;
                }
                
                // Play the audio if successful
                if (ttsClip != null && audioSource != null)
                {
                    audioSource.clip = ttsClip;
                    audioSource.volume = audioVolume;
                    
                    // Now switch to playing state only when audio actually starts playing
                    currentState = VoiceState.Playing;
                    UpdateMaterial();
                    
                    audioSource.Play();
                    
                    // Wait for audio to finish playing
                    while (audioSource.isPlaying)
                    {
                        yield return null;
                    }
                }
                else
                {
                    Debug.LogError($"[GeminiVoiceToggle] TTS playback failed: {ttsError ?? "Unknown error"}");
                }
            }
            else
            {
                Debug.LogError("[GeminiVoiceToggle] TTS Manager not available or not ready after timeout");
                // Fallback: simulate TTS with shorter delay
                yield return new WaitForSeconds(0.5f);
            }
            
            currentState = VoiceState.Idle;
            UpdateMaterial();
        }
        
        
        private void UpdateMaterial()
        {
            Material newMaterial = currentState switch
            {
                VoiceState.Idle => idleMaterial,
                VoiceState.Recording => recordingMaterial,
                VoiceState.Processing => processingMaterial,
                VoiceState.Playing => playingMaterial,
                _ => idleMaterial
            };
            
            if (targetRenderer != null && newMaterial != null)
            {
                targetRenderer.material = newMaterial;
                Debug.Log($"[GeminiVoiceToggle] Updated Renderer material to {newMaterial.name} (State: {currentState})");
            }
            
            if (targetImage != null && newMaterial != null)
            {
                targetImage.material = newMaterial;
                Debug.Log($"[GeminiVoiceToggle] Updated Image material to {newMaterial.name} (State: {currentState})");
            }
        }
        
        private void OnDestroy()
        {
            if (isRecording)
            {
                Microphone.End(microphoneDevice);
            }
            
            if (recordedClip != null)
            {
                Destroy(recordedClip);
            }
        }
        
        private string BuildStepAwarePrompt(int currentStep, bool hasImage)
        {
            string basePrompt = @"You are an AI guide that helps a user replace the battery in a Polaroid Now Gen 2 instant camera. The user will send you a message indicating the current step number and any questions or concerns. Respond with clear, encouraging, and practical guidance for that step only, using everyday language that can be read aloud. If safety is involved, politely remind the user of any precautions. Always keep answers short enough to fit on a slide and easy to speak. Keep your answers short and concise.

Step 1 - Introduction
Welcome to the battery-replacement walkthrough for the Polaroid Now Gen 2. With the camera powered off and film removed, press 'Next' when you're ready to start.

Step 2 - Gather tools
Collect a long-shank Phillips #00 screwdriver for the deep screws and a thin plastic opening pick - like a guitar pick - to separate the shells without scratching them.

Step 3 - Open the film tray
Press the tray-release button on the left side; the film door will spring open. Remove any film pack so the inside of the camera is clear.

Step 4 - Remove the rear-shell screw
Insert the Phillips screwdriver into the deep recess on the back edge and remove the single screw that holds the rear shell in place. Take care not to drop it inside the body.

Step 5 - Release rear-shell clips
Slide the opening pick into the seam above the USB-C port, then sweep it along the top edge. You'll hear the plastic clips pop free as you go.

Step 6 - Lift off the rear shell
With the clips released, pull the rear shell straight back and set it aside where it won't be scratched.

Step 7 - Remove film-door screws
Flip the camera so its base faces up. Remove the two small screws - one at each hinge post - securing the film door.

Step 8 - Detach the film door
Slide each hinge sideways off its post and lift the door away. Keep the door and screws together for reassembly.

Step 9 - Remove front-shell screws
Turn the camera so the lens faces away from you. Unscrew the three Phillips screws that secure the front shell - two are deep on the sides and one sits near the top edge.

Step 10 - Release front-shell clips
Gently squeeze the front shell and run the pick along the seams to pop the remaining clips, then lift the shell off in one piece.

Step 11 - Remove the battery
Peel back the black tape holding the battery, grip the connector close to the socket, and pull straight out to unplug it. Slide the battery out of its cradle and lift it free.";
            
            string contextPrompt = $"\n\nCURRENT STEP: The user is currently on Step {currentStep}.";
            
            if (hasImage)
            {
                contextPrompt += " Analyze the image to see what they're working on and provide guidance specific to their current step and any issues you can observe.";
            }
            else
            {
                contextPrompt += " Respond to their audio query with guidance specific to their current step.";
            }
            
            return basePrompt + contextPrompt;
        }
    }
}