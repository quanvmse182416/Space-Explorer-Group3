using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class MenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Header("Options Panel")]
    [SerializeField] private GameObject optionsCanvas; // Drag your options panel here
    [SerializeField] private Canvas optionsCanvasComponent; // Reference to the Canvas component
    [SerializeField] private CanvasGroup optionsCanvasGroup; // For smooth transitions
    [SerializeField] private Button closeOptionsButton; // Close button for options
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider shootingVolumeSlider;
    [SerializeField] private Slider explosionVolumeSlider;
    [SerializeField] private Slider starCollectingVolumeSlider;

    [Header("Settings")]
    [SerializeField] private string gameplaySceneName = "GamePlay";
    [SerializeField] private string saveFileName = "gamestate.json";

    [Header("Audio Settings")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;
    private static AudioSource musicAudioSource;
    private static bool musicInitialized = false;

    // Static volume variables that can be accessed from other scripts
    private static float s_MasterVolume = 1f;
    private static float s_MusicVolume = 0.5f;
    private static float s_ShootingVolume = 0.8f;
    private static float s_ExplosionVolume = 0.7f;
    private static float s_StarCollectingVolume = 0.6f;

    private int highScore = 0;
    private bool hasSaveData = false;
    private bool playerHasLives = false;

    void Awake()
    {
        // Set up persistent background music
        if (!musicInitialized)
        {
            // Create a persistent GameObject for music
            GameObject musicObj = new GameObject("MusicPlayer");
            DontDestroyOnLoad(musicObj);
            
            // Add audio source
            musicAudioSource = musicObj.AddComponent<AudioSource>();
            musicAudioSource.clip = backgroundMusic;
            musicAudioSource.volume = musicVolume;
            musicAudioSource.loop = true;
            musicAudioSource.playOnAwake = true;
            musicAudioSource.Play();
            
            musicInitialized = true;
        }

        // Load saved volume settings if they exist
        LoadVolumeSettings();
    }

    void Start()
    {
        SetupButtons();
        LoadSaveDataInfo();
        UpdateHighScoreText();
        UpdateContinueButton();
        
        // Set initial slider values
        InitializeVolumeSliders();
        
        // Hide options panel on startup
        if (optionsCanvasComponent != null)
            optionsCanvasComponent.enabled = false;
        else if (optionsCanvas != null)
            optionsCanvas.SetActive(false);
    }

    private void SetupButtons()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);
        
        if (newGameButton != null)
            newGameButton.onClick.AddListener(StartNewGame);
        
        if (optionsButton != null)
            optionsButton.onClick.AddListener(ShowOptions);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
            
        if (closeOptionsButton != null)
            closeOptionsButton.onClick.AddListener(CloseOptions);
            
        // Set up slider listeners
        SetupVolumeSliders();
    }

    private void InitializeVolumeSliders()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = s_MasterVolume;
            
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = s_MusicVolume;
            
        if (shootingVolumeSlider != null)
            shootingVolumeSlider.value = s_ShootingVolume;
            
        if (explosionVolumeSlider != null)
            explosionVolumeSlider.value = s_ExplosionVolume;
            
        if (starCollectingVolumeSlider != null)
            starCollectingVolumeSlider.value = s_StarCollectingVolume;
    }
    
    private void SetupVolumeSliders()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            
        if (shootingVolumeSlider != null)
            shootingVolumeSlider.onValueChanged.AddListener(SetShootingVolume);
            
        if (explosionVolumeSlider != null)
            explosionVolumeSlider.onValueChanged.AddListener(SetExplosionVolume);
            
        if (starCollectingVolumeSlider != null)
            starCollectingVolumeSlider.onValueChanged.AddListener(SetStarCollectingVolume);
    }

    private void LoadSaveDataInfo()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        hasSaveData = File.Exists(path);

        if (hasSaveData)
        {
            try
            {
                string json = File.ReadAllText(path);
                GameState state = JsonUtility.FromJson<GameState>(json);
                
                highScore = state.highScore;
                playerHasLives = state.playerLives > 0;
                
                Debug.Log($"Loaded save data - High Score: {highScore}, Player Lives: {state.playerLives}");
            }
            catch (Exception e)
            {
                Debug.LogError("Error loading save data: " + e.Message);
                hasSaveData = false;
                playerHasLives = false;
            }
        }
    }

    // Load volume settings from PlayerPrefs
    private void LoadVolumeSettings()
    {
        s_MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        s_MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        s_ShootingVolume = PlayerPrefs.GetFloat("ShootingVolume", 0.8f);
        s_ExplosionVolume = PlayerPrefs.GetFloat("ExplosionVolume", 0.7f);
        s_StarCollectingVolume = PlayerPrefs.GetFloat("StarCollectingVolume", 0.6f);
        
        // Apply loaded music volume
        if (musicAudioSource != null)
            musicAudioSource.volume = s_MusicVolume * s_MasterVolume;
    }
    
    // Save volume settings to PlayerPrefs
    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", s_MasterVolume);
        PlayerPrefs.SetFloat("MusicVolume", s_MusicVolume);
        PlayerPrefs.SetFloat("ShootingVolume", s_ShootingVolume);
        PlayerPrefs.SetFloat("ExplosionVolume", s_ExplosionVolume);
        PlayerPrefs.SetFloat("StarCollectingVolume", s_StarCollectingVolume);
        PlayerPrefs.Save();
    }

    private void UpdateHighScoreText()
    {
        if (highScoreText != null)
        {
            highScoreText.text = $"High score: {highScore}";
        }
    }

    private void UpdateContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(hasSaveData && playerHasLives);
        }
    }

    public void ContinueGame()
    {
        // Just load the gameplay scene - it will automatically load the save data
        Debug.Log("Loading saved game...");
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void StartNewGame()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        int preservedHighScore = 0;
        
        // Variables to preserve volume settings
        float preservedMasterVolume = s_MasterVolume;
        float preservedMusicVolume = s_MusicVolume;
        float preservedShootingVolume = s_ShootingVolume;
        float preservedExplosionVolume = s_ExplosionVolume;
        float preservedStarCollectingVolume = s_StarCollectingVolume;
        
        // Try to preserve the high score from existing save if available
        if (File.Exists(path))
        {
            try
            {
                // Read existing save to get high score and volume settings
                string json = File.ReadAllText(path);
                GameState state = JsonUtility.FromJson<GameState>(json);
                preservedHighScore = state.highScore;
                Debug.Log($"Preserving high score: {preservedHighScore}");
                
                // Delete the existing save file
                File.Delete(path);
                Debug.Log("Previous save file deleted for new game");
            }
            catch (Exception e)
            {
                Debug.LogError("Error handling save file: " + e.Message);
            }
        }
        
        // Create a fresh GameState with preserved high score and volume settings
        try
        {
            GameState newState = new GameState();
            newState.playerLives = 3; // Default starting lives
            newState.score = 0;
            newState.highScore = preservedHighScore; // Preserve high score
            newState.saveTime = DateTime.Now;
            
            // Preserve volume settings
            newState.masterVolume = preservedMasterVolume;
            newState.musicVolume = preservedMusicVolume;
            newState.shootingVolume = preservedShootingVolume;
            newState.explosionVolume = preservedExplosionVolume;
            newState.starCollectingVolume = preservedStarCollectingVolume;
            
            // Save the new game state
            string json = JsonUtility.ToJson(newState, true);
            File.WriteAllText(path, json);
            Debug.Log("Created new game state with preserved high score and volume settings");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to create new game state: " + e.Message);
        }
        
        // Load the gameplay scene
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void ShowOptions()
    {
        // Enable canvas rendering
        if (optionsCanvas != null)
        {
            if (optionsCanvasComponent != null)
            {
                optionsCanvasComponent.enabled = true;
            }
            else
            {
                // Fallback to activate the GameObject if the Canvas component reference is missing
                optionsCanvas.SetActive(true);
            }
            Debug.Log("Options panel opened");
        }
    }

    public void CloseOptions()
    {
        // Disable canvas rendering and save settings
        if (optionsCanvas != null)
        {
            if (optionsCanvasComponent != null)
            {
                optionsCanvasComponent.enabled = false;
            }
            else
            {
                optionsCanvas.SetActive(false);
            }
            
            // Save volume settings to PlayerPrefs
            SaveVolumeSettings();
            
            // Also update the current game save file with new volume settings
            UpdateVolumeSettingsInSaveFile();
            
            Debug.Log("Options panel closed and settings saved to both PlayerPrefs and save file");
        }
    }

    // Add this method to update volume settings in the current save file
    private void UpdateVolumeSettingsInSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        if (File.Exists(path))
        {
            try
            {
                // Read existing save
                string json = File.ReadAllText(path);
                GameState state = JsonUtility.FromJson<GameState>(json);
                
                // Update only the volume settings
                state.masterVolume = s_MasterVolume;
                state.musicVolume = s_MusicVolume;
                state.shootingVolume = s_ShootingVolume;
                state.explosionVolume = s_ExplosionVolume;
                state.starCollectingVolume = s_StarCollectingVolume;
                
                // Save the updated state back to file
                json = JsonUtility.ToJson(state, true);
                File.WriteAllText(path, json);
                
                Debug.Log("Volume settings updated in save file");
            }
            catch (Exception e)
            {
                Debug.LogError("Error updating volume settings in save file: " + e.Message);
            }
        }
    }

    // Add this coroutine for smooth fading
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float targetAlpha, float duration, System.Action onComplete = null)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    public void QuitGame()
    {
        // Save settings before quitting
        SaveVolumeSettings();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // Volume setting methods
    public void SetMasterVolume(float volume)
    {
        s_MasterVolume = volume;
        
        // Apply master volume to music
        if (musicAudioSource != null)
            musicAudioSource.volume = s_MusicVolume * s_MasterVolume;
        
        Debug.Log($"Master volume set to: {volume}");
    }

    public void SetMusicVolume(float volume)
    {
        s_MusicVolume = volume;
        
        // Update the audio source
        if (musicAudioSource != null)
            musicAudioSource.volume = volume * s_MasterVolume;
        
        Debug.Log($"Music volume set to: {volume}");
    }

    public void SetShootingVolume(float volume)
    {
        s_ShootingVolume = volume;
        Debug.Log($"Shooting volume set to: {volume}");
    }

    public void SetExplosionVolume(float volume)
    {
        s_ExplosionVolume = volume;
        Debug.Log($"Explosion volume set to: {volume}");
    }

    public void SetStarCollectingVolume(float volume)
    {
        s_StarCollectingVolume = volume;
        Debug.Log($"Star collecting volume set to: {volume}");
    }

    // Static methods for other scripts to access volume settings
    public static float GetMasterVolume()
    {
        return s_MasterVolume;
    }

    public static float GetMusicVolume()
    {
        return s_MusicVolume;
    }

    public static float GetShootingVolume()
    {
        return s_ShootingVolume * s_MasterVolume;
    }

    public static float GetExplosionVolume()
    {
        return s_ExplosionVolume * s_MasterVolume;
    }

    public static float GetStarCollectingVolume()
    {
        return s_StarCollectingVolume * s_MasterVolume;
    }

    // Music control methods
    public static void SetMusicVolumeDirectly(float volume)
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = Mathf.Clamp01(volume);
        }
    }

    public static void ToggleMusic(bool isOn)
    {
        if (musicAudioSource != null)
        {
            if (isOn && !musicAudioSource.isPlaying)
            {
                musicAudioSource.Play();
            }
            else if (!isOn && musicAudioSource.isPlaying)
            {
                musicAudioSource.Pause();
            }
        }
    }

    // Method to set all volume settings from saved game state
    public static void SetVolumeSettingsFromSave(float master, float music, float shooting, float explosion, float starCollecting)
    {
        // Set the static variables
        s_MasterVolume = master;
        s_MusicVolume = music;
        s_ShootingVolume = shooting;
        s_ExplosionVolume = explosion;
        s_StarCollectingVolume = starCollecting;
        
        // Apply to audio source if it exists
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = s_MusicVolume * s_MasterVolume;
        }
        
        // Also save to PlayerPrefs for consistency
        PlayerPrefs.SetFloat("MasterVolume", s_MasterVolume);
        PlayerPrefs.SetFloat("MusicVolume", s_MusicVolume);
        PlayerPrefs.SetFloat("ShootingVolume", s_ShootingVolume);
        PlayerPrefs.SetFloat("ExplosionVolume", s_ExplosionVolume);
        PlayerPrefs.SetFloat("StarCollectingVolume", s_StarCollectingVolume);
        PlayerPrefs.Save();
        
        Debug.Log("Volume settings loaded from save file");
    }
}
