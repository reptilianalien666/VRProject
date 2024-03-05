using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;


namespace AudioAlchemy.AudioTools
{
    // ScriptableObject class to store a list of audio clips.
    [CreateAssetMenu(fileName = "AudioClipListData", menuName = "AudioAlchemyLab/AudioClipListData", order = 1)]
    public class AudioClipListData : ScriptableObject
    {
        public List<AudioClip> audioClips = new List<AudioClip>(); // List to hold audio clips.
    }

    // Main class for the AudioAlchemyLite Editor Window.
    public class AudioAlchemyLab : EditorWindow
    {
        // Singleton instance to ensure only one window is open at a time.
        public static AudioAlchemyLab instance;

        // List to store all audio clips found in the project.
        private List<AudioClip> audioClips = new List<AudioClip>();

        // Tracks the index of the currently selected audio clip in the list.
        private int currentClipIndex = -1;

        // Position for the scroll view in the UI.
        private Vector2 scrollPosition;

        // String to hold the name of the currently selected audio clip.
        private string selectedClipName = "";

        // Float to control the volume of the audio source.
        private float volume = 1.0f;

        // Reference to an AudioSource component for playing audio clips.
        public AudioSource audioSource;

        // Reference to the custom ScriptableObject that holds the list of audio clips.
        private AudioClipListData audioClipListData;

        // String to display the version number of the editor window.
        private string versionNumber = "v3.0";

        // Float to control the pitch of the audio source.
        private float pitch = 1.0f;

        // Boolean to toggle looping for audio playback.
        private bool isLooping = false;
        private float loopDelay = 1f; // Delay time for looping audio.
        private bool isPaused = false; // Tracks if the audio is currently paused.

        [SerializeField] private bool playOnAwakeEnabled = false; // tracks enabled status.

        private Color selectedTextColor = Color.green; // Text color when selected
        private Color defaultTextColor = Color.black; // Default text color
        private Texture2D selectedButtonTexture; // Selected button texture
        private Texture2D unselectedButtonTexture; // Unselected button texture

        // Float to track the next time to play when looping.
        private float nextPlayTime;

        // MenuItem attribute to add an option to Unity's menu bar for opening this window.
        [MenuItem("Tools/Audio Alchemy Lab")]
        public static void ShowWindow()
        {
            // Instantiate and show the window. Ensures it persists across Unity scenes.
            instance = GetWindow<AudioAlchemyLab>("Audio Alchemy Lab");
            DontDestroyOnLoad(instance);
        }

        // Called when the window is enabled (e.g., when entering the editor)
        private void OnEnable()
        {
            GatherAudioClips(Application.dataPath); // Load audio clips from the entire Assets folder
            LoadAudioSourceReference(); // Load the AudioSource reference from EditorPrefs or create a new one if it's null
            playOnAwakeEnabled = EditorPrefs.GetBool("PlayOnAwakeState", false);

            // Ensure the audioSource is not null
            if (audioSource == null)
            {
                // Create a new AudioSource component
                GameObject audioSourceObject = new GameObject("AudioSource");
                audioSource = audioSourceObject.AddComponent<AudioSource>();
            }

            minSize = new Vector2(200, 600);
        }


        // Called when the window is disabled (e.g., when exiting the editor)
        private void OnDisable()
        {
            // Stop the audio when the editor window is closed
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }

            SaveAudioSourceReference(); // Save the reference to EditorPrefs
            SaveAudioClipListData(); // Save AudioClipListData
            StopAudio(); // Stop audio playback

            if (isLooping)
            {
                isLooping = false;
                StopLooping(); // Stop audio looping
            }

            // Save the "Play On Awake" state to EditorPrefs
            EditorPrefs.SetBool("PlayOnAwakeState", playOnAwakeEnabled);
        }

        // Initialization and GUI drawing
        private void OnGUI()
        {
            GUILayout.BeginHorizontal();

            // Display the selected clip's name on the top left
            GUIStyle selectedClipNameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.UpperLeft, // Align the text to the upper left corner
                fontSize = 14, // Set the font size to 14
            };
            GUILayout.Label(selectedClipName, selectedClipNameStyle); // Display the selected clip's name using the specified style

            // Create some flexible space to push the version number to the right
            GUILayout.FlexibleSpace(); // Create flexible space to push content to the right

            // Display version number on the top right
            GUIStyle versionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperRight, // Align the text to the upper right corner
                fontSize = 12, // Set the font size to 12
            };
            GUILayout.Label(versionNumber, versionStyle); // Display the version number using the specified style

            GUILayout.EndHorizontal();

            // Draw the logo first to ensure it appears at the top of the window
            Texture2D logoTexture = EditorGUIUtility.Load("Assets/Editor/AudioAlchemyLab/AALogo.png") as Texture2D;
            if (logoTexture != null)
            {
                float logoWidth = 75f; // Adjust the width as needed
                float logoHeight = 75f; // Adjust the height as needed
                                        // Calculate the position for the logo at the top center
                Rect logoRect = new Rect(position.width / 2 - logoWidth / 2, 5, logoWidth, logoHeight); // Added some space from the top
                GUI.DrawTexture(logoRect, logoTexture);
            }

            // Add some vertical space after the logo if needed
            GUILayout.Space(60);

            GUILayout.BeginHorizontal();

            // Select Folder button with folder icon and white text, scalable width
            GUIStyle folderButtonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
            float folderButtonMinWidth = 100f; // Set the minimum width for the button
            GUIContent folderButtonContent = new GUIContent(" Select Folder", EditorGUIUtility.IconContent("FolderOpened Icon").image); // Attempt to use a folder icon
            if (GUILayout.Button(folderButtonContent, folderButtonStyle, GUILayout.Height(25), GUILayout.ExpandWidth(true), GUILayout.MinWidth(folderButtonMinWidth)))
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(folderPath))
                {
                    GatherAudioClips(folderPath);
                }
            }

            // Refresh button with icon, fixed size, directly after the Select Folder button
            if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), GUILayout.Height(25), GUILayout.Width(25)))
            {
                RefreshAudioClips();
            }

            GUILayout.EndHorizontal();


            // Declare sliderLabelStyle outside of the if block
            GUIStyle sliderLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10, // Set the font size for slider labels
            };


            // Create a GUIStyle for the "Loop" label with a specific font size
            GUIStyle loopLabelStyle = new GUIStyle(sliderLabelStyle)
            {
                fontSize = 12, // Set the font size for the "Loop" label
            };

            // Audio Source field
            GUILayout.BeginHorizontal(); // Start a horizontal layout group
            GUILayout.Label("Audio Source", GUILayout.Width(80)); // Display a label "Audio Source" with a specified width
            GUILayout.Space(0); // Remove space between label and ObjectField
            audioSource = EditorGUILayout.ObjectField(audioSource, typeof(AudioSource), true) as AudioSource;
            GUILayout.EndHorizontal(); // End the horizontal layout group

            // Volume slider with live update
            EditorGUILayout.BeginHorizontal(); // Start a horizontal layout group
            GUILayout.Label("Volume", GUILayout.Width(50)); // Display a label "Volume" with a specified width
            float newVolume = EditorGUILayout.Slider(volume, 0.0f, 1.0f); // Create a slider for adjusting the volume
            if (newVolume != volume) // Check if the slider value has changed
            {
                volume = newVolume; // Update the 'volume' variable with the new value
                UpdateVolume(); // Call a function to update the volume
            }
            EditorGUILayout.EndHorizontal(); // End the horizontal layout group

            // Pitch slider with label closer
            EditorGUILayout.BeginHorizontal(); // Start a horizontal layout group
            GUILayout.Label("Pitch", GUILayout.Width(50)); // Display a label "Pitch" with a specified width
            float newPitch = EditorGUILayout.Slider(pitch, 0.1f, 3.0f); // Create a slider for adjusting the pitch
            if (newPitch != pitch) // Check if the slider value has changed
            {
                pitch = newPitch; // Update the 'pitch' variable with the new value
                UpdatePitch(); // Call a function to update the pitch
            }
            EditorGUILayout.EndHorizontal(); // End the horizontal layout group

            // Loop toggle and loop delay slider
            GUILayout.BeginHorizontal();

            // Display the "Loop" label with the custom style
            GUILayout.Label("Loop", loopLabelStyle, GUILayout.ExpandWidth(false));

            // Use EditorPrefs to store and retrieve the looping state
            bool newLooping = EditorPrefs.GetBool("LoopingState", isLooping);
            newLooping = EditorGUILayout.Toggle(newLooping, GUILayout.Width(15));

            if (newLooping != isLooping)
            {
                isLooping = newLooping;
                if (isLooping)
                {
                    // Looping is enabled; schedule the next play if audio is playing
                    if (audioSource.isPlaying)
                    {
                        ScheduleNextPlay(); // Correctly schedule the next loop
                        EditorApplication.update += LoopAudio;
                    }
                }
                else
                {
                    // Looping is disabled; stop the LoopAudio update loop
                    EditorApplication.update -= LoopAudio;
                }

                // Store the updated looping state in EditorPrefs
                EditorPrefs.SetBool("LoopingState", isLooping);
            }

            if (isLooping)
            {
                float newLoopDelay = EditorGUILayout.Slider(loopDelay, 0f, 2f, GUILayout.ExpandWidth(true));

                // Here is where the new code snippet gets integrated
                if (newLoopDelay != loopDelay)
                {
                    loopDelay = newLoopDelay;
                    if (audioSource.isPlaying && isLooping)
                    {
                        ScheduleNextPlay(); // Update the schedule with the new delay
                    }
                }
            }
            else
            {
                GUILayout.Label("", sliderLabelStyle, GUILayout.ExpandWidth(true));
            }

            GUILayout.EndHorizontal();

            // Custom "Play On Awake" toggle
            GUILayout.BeginHorizontal();
            GUILayout.Label("Play On Awake", loopLabelStyle, GUILayout.ExpandWidth(false));
            bool newPlayOnAwakeState = EditorGUILayout.Toggle(playOnAwakeEnabled, GUILayout.Width(15));
            if (newPlayOnAwakeState != playOnAwakeEnabled)
            {
                playOnAwakeEnabled = newPlayOnAwakeState;
                EditorPrefs.SetBool("PlayOnAwakeState", playOnAwakeEnabled);
            }
            GUILayout.EndHorizontal();

            // Apply playOnAwake state to AudioSource
            if (audioSource != null)
            {
                audioSource.playOnAwake = playOnAwakeEnabled;
            }

            // Play, Pause, and Stop buttons
            GUILayout.BeginHorizontal(); // Start a horizontal layout group
            if (GUILayout.Button("Play", GUILayout.Height(30f))) PlayAudio(); // Create a button labeled "Play" with specified height
            if (GUILayout.Button("Pause", GUILayout.Height(30f))) PauseAudio(); // Create a button labeled "Pause" with specified height
            if (GUILayout.Button("Stop", GUILayout.Height(30f))) StopAudio(); // Create a button labeled "Stop" with specified height
            GUILayout.EndHorizontal(); // End the horizontal layout group
                                       // Display the list of audio clips
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition); // Begin a scroll view

            GUIStyle clipButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 32f,
                fixedWidth = 100f
            };

            // Define the style for selected clip buttons
            GUIStyle selectedClipButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 32f,
                fixedWidth = 100f,
                normal = { background = MakeTexture(2, 2, new Color(0.2f, 0.2f, 0.2f)) }, // Dark grey
                active = { background = MakeTexture(2, 2, new Color(0.2f, 0.0f, 0.4f)) } // Even darker grey for active state
            };

            // Define a fixed height for the buttons
            float buttonHeight = 32f;

            // Calculate the button width based on the window width
            float buttonWidth = Mathf.Max((position.width - 64f) / 3f, 100f); // Adjust the minimum width (200f) as needed

            foreach (var clip in audioClips)
            {
                EditorGUILayout.BeginHorizontal(); // Start a horizontal layout group

                bool isSelected = (audioClips.IndexOf(clip) == currentClipIndex); // Check if this clip is currently selected

                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.normal.background = isSelected ? selectedButtonTexture : unselectedButtonTexture;
                buttonStyle.normal.textColor = isSelected ? selectedTextColor : defaultTextColor;
                buttonStyle.hover.textColor = Color.yellow;

                string buttonText = " \u266B " + (clip != null ? clip.name : "No Clip Selected");

                // Ensure all buttons have the same height and scale evenly
                if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Height(buttonHeight), GUILayout.ExpandWidth(true), GUILayout.MinWidth(buttonWidth)))
                {
                    SelectAudioClip(audioClips.IndexOf(clip));
                    PlayAudio();
                }

                // Get the folder icon
                Texture2D folderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;

                // Create a GUIContent with just the folder icon and no text or tooltip
                GUIContent content = new GUIContent(folderIcon);

                // Set the fixed size for the button
                float buttonFolderWidth = 32;
                float buttonFolderHeight = 32;

                // Display the button with the folder icon
                if (GUI.Button(GUILayoutUtility.GetRect(content, GUI.skin.button, GUILayout.Width(buttonFolderWidth), GUILayout.Height(buttonFolderHeight)), content, GUI.skin.button))
                {
                    Selection.activeObject = clip;
                    EditorGUIUtility.PingObject(clip);
                }

                EditorGUILayout.EndHorizontal(); // End the horizontal layout group
            }

            EditorGUILayout.EndScrollView(); // End the scroll view
        }

        // Gather audio clips from a selected folder
        private void GatherAudioClips(string rootPath)
        {
            audioClips.Clear(); // Clear the list of audio clips
            string[] audioFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".mp3") || s.EndsWith(".wav"))
                .Select(s => s.Replace("\\", "/").Replace(Application.dataPath, "Assets")) // Convert to asset path
                .ToArray();

            foreach (string assetPath in audioFiles)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath); // Load an AudioClip from the asset path
                if (clip != null)
                {
                    audioClips.Add(clip); // Add the loaded clip to the list
                }
            }
        }

        // Play the selected audio clip
        private void PlayAudio()
        {
            if (currentClipIndex >= 0 && currentClipIndex < audioClips.Count)
            {
                AudioClip selectedClip = audioClips[currentClipIndex];

                if (audioSource != null)
                {
                    audioSource.enabled = true; // Enable the audio source

                    // Set the AudioSource's Play On Awake property based on the custom toggle
                    audioSource.playOnAwake = playOnAwakeEnabled; // Update playOnAwake

                    if (isPaused)
                    {
                        audioSource.UnPause(); // Unpause the audio playback
                        isPaused = false;
                    }
                    else
                    {
                        audioSource.clip = selectedClip; // Set the audio source's clip
                        audioSource.Play(); // Start playing the audio
                        selectedClipName = selectedClip.name; // Update the selected clip name
                    }

                    if (isLooping)
                    {
                        ScheduleNextPlay(); // Schedule the next play for looping
                        EditorApplication.update += LoopAudio; // Subscribe to the update event for looping control
                    }
                }
            }
        }

        // Pause the audio playback
        private void PauseAudio()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause(); // Pause the audio playback
                isPaused = true;
            }
            else if (audioSource != null && isLooping)
            {
                // Toggle the pause state only if looping is enabled
                if (!isPaused)
                {
                    audioSource.Pause(); // Pause the audio playback
                    isPaused = true;
                }
                else
                {
                    audioSource.UnPause(); // Unpause the audio playback
                    isPaused = false;
                }
            }
        }


        // Stop the audio playback
        private void StopAudio()
        {
            if (audioSource != null)
            {
                audioSource.Stop(); // Stop the audio playback
                audioSource.enabled = false; // Disable the audio source
                isPaused = false;
                EditorApplication.update -= LoopAudio; // Ensure loop is stopped by unsubscribing from the update event
            }
        }

        // Handle looping audio playback
        private void LoopAudio()
        {
            if (audioSource != null && audioSource.enabled && isLooping && !audioSource.isPlaying && !isPaused && Time.realtimeSinceStartup >= nextPlayTime)
            {
                audioSource.Play(); // Start playing the audio for looping
                ScheduleNextPlay(); // Schedule the next play for looping
            }
            else if (isPaused)
            {
                //skipping loop when paused
            }
        }

        // Stop the audio looping
        private void StopLooping()
        {
            if (audioSource != null)
            {
                EditorApplication.update -= LoopAudio; // Unsubscribe from the update event for looping control
            }
        }

        // Select an audio clip by index
        private void SelectAudioClip(int index)
        {
            if (index >= 0 && index < audioClips.Count)
            {
                currentClipIndex = index; // Set the current clip index
                selectedClipName = audioClips[index].name; // Update the selected clip name
            }
        }

        private void RefreshAudioClips()
        {
            // Implement logic to refresh audio clips (e.g., re-scan the folder and update the list)
            // For now, I'll assume you want to refresh from the same folder path used before
            GatherAudioClips(Application.dataPath);
        }


        // Create a texture for styling buttons
        private Texture2D MakeTexture(int width, int height, Color col)
        {
            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // Update the volume of the audio source
        private void UpdateVolume()
        {
            if (audioSource != null)
            {
                audioSource.volume = volume; // Set the volume of the audio source
            }
        }

        // Update the pitch of the audio source
        private void UpdatePitch()
        {
            if (audioSource != null)
            {
                audioSource.pitch = pitch; // Set the pitch of the audio source
            }
        }

        // Save the AudioSource reference to EditorPrefs
        private void SaveAudioSourceReference()
        {
            if (audioSource != null)
            {
                // Save the instance ID of the AudioSource to EditorPrefs
                int audioSourceID = audioSource.GetInstanceID();
                EditorPrefs.SetInt("SavedAudioSourceID", audioSourceID);
            }
        }

        // Load the AudioSource reference from EditorPrefs
        private void LoadAudioSourceReference()
        {
            // Load the instance ID of the AudioSource from EditorPrefs
            int audioSourceID = EditorPrefs.GetInt("SavedAudioSourceID", -1);

            if (audioSourceID != -1)
            {
                // Find the AudioSource by its instance ID
                audioSource = EditorUtility.InstanceIDToObject(audioSourceID) as AudioSource;
            }
        }

        // Save changes to AudioClipListData ScriptableObject
        private void SaveAudioClipListData()
        {
            if (audioClipListData != null)
            {
                EditorUtility.SetDirty(audioClipListData); // Mark the asset as dirty
                AssetDatabase.SaveAssets(); // Save changes to the asset database
            }
        }

        // Adjust loop delay
        private void AdjustLoopDelay(float newLoopDelay)
        {
            if (audioSource != null && isLooping)
            {
                loopDelay = newLoopDelay;
                UpdateAudioSourceSettings(); // Update audio source settings with the new loop delay
            }
            else
            {
                loopDelay = newLoopDelay;
            }
        }

        // Update audio source settings for looping
        private void UpdateAudioSourceSettings()
        {
            if (audioSource != null)
            {
                if (isLooping && audioSource.isPlaying)
                {
                    // Calculate the time remaining in the current clip
                    double timeRemaining = (audioSource.clip.length - audioSource.time) / audioSource.pitch;

                    // Calculate the new time to schedule the loop start
                    double newStartTime = AudioSettings.dspTime + loopDelay - timeRemaining;

                    // Ensure the new start time is non-negative
                    if (newStartTime < 0)
                    {
                        newStartTime = 0;
                    }

                    // Schedule the loop start
                    audioSource.SetScheduledStartTime(newStartTime);
                }
                else
                {
                    audioSource.loop = isLooping;
                    audioSource.SetScheduledEndTime(0); // Stop any scheduled loop
                }
            }
        }

        // Schedule the next audio playback for looping
        private void ScheduleNextPlay()
        {
            if (audioSource != null && isLooping)
            {
                // Calculate the next play time
                nextPlayTime = Time.realtimeSinceStartup + audioSource.clip.length + loopDelay;
            }
        }

    }
}