using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Add this for scene management
using UnityEngine.InputSystem;

[Serializable]
public class GameState
{
    public int playerLives;
    public int score;
    public int highScore; // Added to track highest score
    public List<ObjectData> asteroids = new List<ObjectData>();
    public List<ObjectData> stars = new List<ObjectData>();
    public ObjectData player;
    public DateTime saveTime;
    
    // Add volume settings
    public float masterVolume = 1f;
    public float musicVolume = 0.5f;
    public float shootingVolume = 0.8f;
    public float explosionVolume = 0.7f;
    public float starCollectingVolume = 0.6f;

    [Serializable]
    public class ObjectData
    {
        public float posX;
        public float posY;
        public float size;
        public int health;

        public ObjectData() { }
        
        public ObjectData(Vector3 position, float size = 1.0f, int health = 1)
        {
            posX = position.x;
            posY = position.y;
            this.size = size;
            this.health = health;
        }
    }
}

public class GameController : MonoBehaviour
{
    // Add a field to track game over state
    private bool isGameOver = false;

    [Header("Spawning")]
    [SerializeField] private GameObject asteroidPrefab;
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private float asteroidSpawnRate = 2.0f;
    [SerializeField] private float starSpawnRate = 5.0f;
    [SerializeField] private float spawnHeight = 7.0f; // Above the camera view
    [SerializeField] private float spawnWidthMin = -8.0f;
    [SerializeField] private float spawnWidthMax = 8.0f;
    
    [Header("Difficulty Settings")]
    [SerializeField] private float minAsteroidSize = 0.5f;
    [SerializeField] private float maxAsteroidSize = 3.0f;
    [SerializeField] private float difficultyIncreaseRate = 0.1f;
    [SerializeField] private float maxDifficulty = 2.0f;
    
    [Header("References")]
    [SerializeField] private HealthManager healthManager;
    
    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button retryButton;
    
    [Header("Menu Navigation")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string mainMenuSceneName = "Menu"; // Scene to load when main menu is clicked
    
    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "gamestate.json";
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float autoSaveInterval = 60.0f; // Save every minute

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseCanvas; // Drag your pause menu canvas here
    [SerializeField] private Button resumeButton;    // Resume button
    [SerializeField] private Button pauseOptionsButton; // Options button on pause screen
    [SerializeField] private Button pauseMainMenuButton; // Main menu button on pause screen
    [SerializeField] private Button pauseQuitButton; // Quit button on pause screen
      [Header("Options Menu")]
    [SerializeField] private GameObject optionsCanvas; // Drag your options panel here
    [SerializeField] private Button closeOptionsButton; // Close button for options
    [SerializeField] private Slider masterVolumeSlider; // Master volume slider
    [SerializeField] private Slider musicVolumeSlider; // Music volume slider
    [SerializeField] private Slider shootingVolumeSlider; // Shooting sound volume slider
    [SerializeField] private Slider explosionVolumeSlider; // Explosion sound volume slider
    [SerializeField] private Slider starCollectingVolumeSlider; // Star collecting sound volume slider
    // Make isPaused public static so it can be accessed by other scripts
    public static bool IsPaused { get; private set; } = false;
    
    // Static volume variables that can be accessed from other scripts
    private static float s_MasterVolume = 1f;
    private static float s_MusicVolume = 0.5f;
    private static float s_ShootingVolume = 0.8f;
    private static float s_ExplosionVolume = 0.7f;
    private static float s_StarCollectingVolume = 0.6f;
    
    private float currentDifficulty = 1.0f;
    private float asteroidTimer;
    private float starTimer;
    private float saveTimer;
    private List<Asteroid> activeAsteroids = new List<Asteroid>();
    private List<Star> activeStars = new List<Star>();
    private Camera mainCamera;
    private int highScore = 0;

    // Add these new fields for tracking game time and uncapped difficulty
    private float gameTimeElapsed = 0f;
    private float uncappedDifficulty = 1.0f; // Will continue growing even past maxDifficulty
    
    private void Start()
    {
        mainCamera = Camera.main;
        asteroidTimer = asteroidSpawnRate;
        starTimer = starSpawnRate;
        saveTimer = autoSaveInterval;
        
        // Game starts without game over state
        isGameOver = false;
        
        // Hide game over UI at start
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Setup retry button - hide it initially
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetryGame);
            retryButton.gameObject.SetActive(false);
        }
        
        // Setup main menu button
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            // Initially hide if in game over panel, otherwise show
            mainMenuButton.gameObject.SetActive(false);
        }
        
        // Setup quit button
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
            // Initially hide if in game over panel, otherwise show
            quitButton.gameObject.SetActive(false);
        }
          // Setup pause menu buttons
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
            
        if (pauseOptionsButton != null)
            pauseOptionsButton.onClick.AddListener(ShowOptions);
            
        if (pauseMainMenuButton != null)
            pauseMainMenuButton.onClick.AddListener(GoToMainMenu);
            
        if (pauseQuitButton != null)
            pauseQuitButton.onClick.AddListener(QuitGame);
          // Setup options menu button
        if (closeOptionsButton != null)
            closeOptionsButton.onClick.AddListener(CloseOptions);
            
        // Make sure pause menu is hidden at start
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
            
        // Make sure options panel is hidden at start
        if (optionsCanvas != null)
            optionsCanvas.SetActive(false);
        
        // Try to load saved game state
        bool loadedGame = false;
        if (File.Exists(GetSavePath()))
        {
            loadedGame = TryLoadGame();
            if (loadedGame)
            {
                Debug.Log("Game state loaded successfully");
            }
            else
            {
                Debug.LogWarning("Failed to load game state. Starting new game.");
            }
        }
        
        // Only spawn a new player if game wasn't loaded OR if the player should be alive
        // (healthManager.Lives > 0 ensures we don't spawn when out of lives)
        GameObject player = GameObject.FindWithTag("Player");
        if (!loadedGame || (player == null && healthManager != null && healthManager.Lives > 0))
        {
            SpawnNewPlayer();
        }
    }
    
    // Method to spawn a new player at the start position
    private void SpawnNewPlayer()
    {
        if (healthManager == null)
        {
            Debug.LogError("Cannot spawn player - healthManager is not assigned!");
            return;
        }
        
        if (healthManager.PlayerPrefab == null)
        {
            Debug.LogError("Cannot spawn player - playerPrefab is not assigned on HealthManager!");
            return;
        }
        
        Vector3 spawnPosition = healthManager.RespawnPoint != null ? 
            healthManager.RespawnPoint.position : new Vector3(0, -4, 0);
                
        GameObject newPlayer = Instantiate(healthManager.PlayerPrefab, spawnPosition, Quaternion.identity);
        if (newPlayer == null)
        {
            Debug.LogError("Failed to instantiate player prefab!");
            return;
        }
        
        newPlayer.tag = "Player";
        
        // Update the reference in HealthManager
        healthManager.SetPlayerInstance(newPlayer);
    }
    
    private void Update()
    {
        // Only allow pausing if the game is not in game over state
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !isGameOver)
        {
            TogglePause();
            return; // Skip the rest of Update when toggling pause
        }
        
        // Skip game logic updates when paused
        if (IsPaused)
            return;
            
        // Track game time for uncapped difficulty
        gameTimeElapsed += Time.deltaTime;
        
        // Calculate both capped and uncapped difficulty
        uncappedDifficulty = 1.0f + (gameTimeElapsed * difficultyIncreaseRate / 100f);
        currentDifficulty = Mathf.Min(maxDifficulty, uncappedDifficulty);
        
        // Spawn asteroids
        asteroidTimer -= Time.deltaTime;
        if (asteroidTimer <= 0)
        {
            SpawnAsteroid();
            asteroidTimer = asteroidSpawnRate / currentDifficulty; // Faster spawning as difficulty increases
        }
        
        // Spawn stars
        starTimer -= Time.deltaTime;
        if (starTimer <= 0)
        {
            SpawnStar();
            starTimer = starSpawnRate;
        }
        
        // Autosave game
        if (autoSave)
        {
            saveTimer -= Time.deltaTime;
            if (saveTimer <= 0)
            {
                SaveGame();
                saveTimer = autoSaveInterval;
            }
        }
        
        // Clean up destroyed objects from our lists
        CleanupDestroyedObjects();
    }
    
    // Add a new helper method to consistently hide game over UI
    public void HideGameOverUI()
    {
        // Reset game over state
        isGameOver = false;
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }
        
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(false);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(false);
        }
        
        if (quitButton != null)
        {
            quitButton.gameObject.SetActive(false);
        }
    }

    // Modify the ShowGameOverUI method to include the new buttons
    public void ShowGameOverUI()
    {
        // Set game over state
        isGameOver = true;
        
        // Make sure game isn't paused when showing game over
        if (IsPaused)
        {
            ResumeGame();
        }
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = $"Game Over\nScore: {GameManager.Score}\nHigh Score: {highScore}";
        }
        
        // Show UI buttons
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(true);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
        }
        
        if (quitButton != null)
        {
            quitButton.gameObject.SetActive(true);
        }
    }
    
    // Retry button handler - reset game but keep high score
    public void RetryGame()
    {
        // Reset time tracking for difficulty
        gameTimeElapsed = 0f;
        uncappedDifficulty = 1.0f;
        
        // Save high score before resetting
        int currentScore = GameManager.Score;
        highScore = Mathf.Max(highScore, currentScore);
        
        // Hide all game over UI elements consistently
        HideGameOverUI();
        
        // Clear all game objects
        ClearAllGameObjects();
        
        // Reset score
        GameManager.ResetScore();
        
        // Reset health manager
        if (healthManager != null)
        {
            healthManager.ResetHealth();
        }
        
        // Spawn a new player if needed
        if (healthManager != null && healthManager.PlayerPrefab != null)
        {
            Vector3 spawnPosition = healthManager.RespawnPoint != null ? 
                healthManager.RespawnPoint.position : new Vector3(0, -4, 0);
            
            // Ensure we're spawning in the correct world space
            GameObject newPlayer = Instantiate(
                healthManager.PlayerPrefab, 
                spawnPosition, 
                Quaternion.identity,
                null); // Explicit null parent to ensure it's in world space
                
            newPlayer.tag = "Player";
            
            // Make sure the player is in the scene root, not under any canvas
            newPlayer.transform.SetParent(null);
            
            // Double check position is correct
            newPlayer.transform.position = spawnPosition;
            
            // Update the player reference in the HealthManager
            healthManager.SetPlayerInstance(newPlayer);
            Debug.Log($"New player spawned at position {spawnPosition} and reference updated in retry");
        }
        
        // Save the game with reset state but preserved high score
        SaveGame();
        
        Debug.Log("Game restarted with high score preserved: " + highScore);
    }
    
    // Modify the GoToMainMenu method to ensure pause state is reset
    public void GoToMainMenu()
    {
        // Make sure to unpause the game before returning to menu
        if (IsPaused)
        {
            IsPaused = false;
            Time.timeScale = 1f;
        }
        
        // Save game before returning to menu
        if (autoSave)
        {
            SaveGame();
        }
        
        // Load the main menu scene
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            // Check if scene exists in build settings
            if (SceneUtility.GetBuildIndexByScenePath("Scenes/" + mainMenuSceneName) >= 0 ||
                SceneUtility.GetBuildIndexByScenePath(mainMenuSceneName) >= 0)
            {
                Debug.Log("Loading main menu scene: " + mainMenuSceneName);
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                Debug.LogError($"Scene '{mainMenuSceneName}' not found in build settings. Make sure to add it!");
            }
        }
        else
        {
            Debug.LogError("Main menu scene name is not specified!");
        }
    }
    
    // Add this method to handle quit button click
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        
        // Save game before quitting
        if (autoSave)
        {
            SaveGame();
        }
        
        // Quit application
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in editor
        #else
        Application.Quit(); // Quit application in build
        #endif
    }
    
    private void SpawnAsteroid()
    {
        float randomX = UnityEngine.Random.Range(spawnWidthMin, spawnWidthMax);
        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, 0);
        
        GameObject asteroidObject = Instantiate(asteroidPrefab, spawnPosition, Quaternion.identity);
        Asteroid asteroid = asteroidObject.GetComponent<Asteroid>();
        
        if (asteroid != null)
        {
            // Set random size based on difficulty
            float randomSize = UnityEngine.Random.Range(minAsteroidSize, maxAsteroidSize);
            asteroid.SetSize(randomSize);
            
            // Calculate bonus health based on infinite difficulty scaling
            // This will continue to grow even after reaching max difficulty
            int bonusHealth = 0;
            
            // Start adding bonus health after reaching 80% of max difficulty
            if (uncappedDifficulty >= maxDifficulty * 0.8f)
            {
                // Base formula: every 50% increase in difficulty beyond max adds +1 health
                float excessDifficulty = uncappedDifficulty - (maxDifficulty * 0.8f);
                bonusHealth = Mathf.FloorToInt(excessDifficulty / (maxDifficulty * 0.5f));
                
                // Cap at a reasonable amount to prevent excessive health in very long games
                // Can be adjusted based on player's expected power growth
                bonusHealth = Mathf.Min(bonusHealth, 20);
                
                // Apply bonus health to the asteroid
                if (bonusHealth > 0)
                {
                    // Add this method to the Asteroid class
                    asteroid.SetBonusHealth(bonusHealth);
                    
                    // Visual debug
                    if (bonusHealth > 5)
                    {
                        Debug.Log($"Spawned reinforced asteroid with +{bonusHealth} bonus health (uncapped difficulty: {uncappedDifficulty:F2})");
                    }
                }
            }
            
            // Assign the star prefab if not already assigned
            if (asteroid.starPrefab == null && starPrefab != null)
            {
                asteroid.starPrefab = starPrefab;
            }
            
            activeAsteroids.Add(asteroid);
        }
    }
    
    private void SpawnStar()
    {
        float randomX = UnityEngine.Random.Range(spawnWidthMin, spawnWidthMax);
        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, 0);
        
        GameObject starObject = Instantiate(starPrefab, spawnPosition, Quaternion.identity);
        Star star = starObject.GetComponent<Star>();
        
        if (star != null)
        {
            activeStars.Add(star);
        }
    }
    
    private void CleanupDestroyedObjects()
    {
        activeAsteroids.RemoveAll(a => a == null);
        activeStars.RemoveAll(s => s == null);
    }
    
    public bool SaveGame()
    {
        try
        {
            GameState state = new GameState();
            
            // Save player lives and score
            state.playerLives = healthManager != null ? healthManager.Lives : 3;
            state.score = GameManager.Score;
            state.highScore = Mathf.Max(highScore, GameManager.Score); // Save high score
            state.saveTime = DateTime.Now;
            
            // Save player data if available
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                state.player = new GameState.ObjectData(player.transform.position);
            }
            
            // Save all active asteroids
            foreach (var asteroid in activeAsteroids)
            {
                if (asteroid != null)
                {
                    state.asteroids.Add(new GameState.ObjectData(
                        asteroid.transform.position, 
                        asteroid.Size, 
                        asteroid.Health
                    ));
                }
            }
            
            // Save all active stars
            foreach (var star in activeStars)
            {
                if (star != null)
                {
                    state.stars.Add(new GameState.ObjectData(
                        star.transform.position
                    ));
                }
            }
            
            // Convert to JSON and save to file
            string json = JsonUtility.ToJson(state, true);
            File.WriteAllText(GetSavePath(), json);
            
            Debug.Log("Game saved successfully");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save game: " + e.Message);
            return false;
        }
    }
    
    public bool TryLoadGame()
    {
        try
        {
            string path = GetSavePath();
            if (!File.Exists(path))
            {
                Debug.LogWarning("No save file found at " + path);
                return false;
            }
            
            string json = File.ReadAllText(path);
            GameState state = JsonUtility.FromJson<GameState>(json);
            
            // Clear existing objects
            ClearAllGameObjects();
              // Load high score
            highScore = state.highScore;
            
            // Load volume settings
            s_MasterVolume = state.masterVolume;
            s_MusicVolume = state.musicVolume;
            s_ShootingVolume = state.shootingVolume;
            s_ExplosionVolume = state.explosionVolume;
            s_StarCollectingVolume = state.starCollectingVolume;
            
            // Update global volume settings using MenuController to ensure consistency
            MenuController.SetVolumeSettingsFromSave(
                s_MasterVolume,
                s_MusicVolume,
                s_ShootingVolume,
                s_ExplosionVolume,
                s_StarCollectingVolume
            );
            
            Debug.Log("Volume settings loaded from save file");
            
            // Restore player lives and score
            if (healthManager != null)
            {
                // Use the new direct method instead of resetting and decrementing
                healthManager.SetLivesDirectly(state.playerLives);
            }
            
            GameManager.ResetScore();
            GameManager.AddScore(state.score);
            
            // Only restore player if they have lives remaining
            if (state.player != null && state.playerLives > 0)
            {
                // Find existing player
                GameObject player = GameObject.FindWithTag("Player");
                Vector3 savedPosition = new Vector3(state.player.posX, state.player.posY, 0);
                
                if (player != null)
                {
                    // Move existing player to saved position
                    player.transform.position = savedPosition;
                    Debug.Log($"Moved player to saved position: {savedPosition}");
                }
                else if (healthManager != null && healthManager.PlayerPrefab != null)
                {
                    // Spawn new player at saved position
                    GameObject newPlayer = Instantiate(healthManager.PlayerPrefab, savedPosition, Quaternion.identity);
                    newPlayer.tag = "Player";
                    healthManager.SetPlayerInstance(newPlayer);
                    Debug.Log($"Spawned player at saved position: {savedPosition}");
                }
                else
                {
                    Debug.LogWarning("Could not restore player position - no player or prefab found");
                }
                
                // Hide game over UI
                HideGameOverUI();
            }
            else if (state.playerLives <= 0)
            {
                // Ensure game over panel and retry button are shown if lives are 0
                Debug.Log("Loaded game with 0 lives - maintaining game over state");
                ShowGameOverUI();
            }
            
            // Restore asteroids
            foreach (var asteroidData in state.asteroids)
            {
                Vector3 position = new Vector3(asteroidData.posX, asteroidData.posY, 0);
                GameObject asteroidObject = Instantiate(asteroidPrefab, position, Quaternion.identity);
                Asteroid asteroid = asteroidObject.GetComponent<Asteroid>();
                
                if (asteroid != null)
                {
                    asteroid.SetSize(asteroidData.size);
                    activeAsteroids.Add(asteroid);
                    
                    // Assign the star prefab if not already assigned
                    if (asteroid.starPrefab == null && starPrefab != null)
                    {
                        asteroid.starPrefab = starPrefab;
                    }
                }
            }
            
            // Restore stars
            foreach (var starData in state.stars)
            {
                Vector3 position = new Vector3(starData.posX, starData.posY, 0);
                GameObject starObject = Instantiate(starPrefab, position, Quaternion.identity);
                Star star = starObject.GetComponent<Star>();
                
                if (star != null)
                {
                    activeStars.Add(star);
                }
            }
            
            Debug.Log($"Game loaded successfully. Loaded {state.asteroids.Count} asteroids and {state.stars.Count} stars.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load game: " + e.Message);
            return false;
        }
    }
    
    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }
    
    private void ClearAllGameObjects()
    {
        // Clear existing player
        GameObject existingPlayer = GameObject.FindWithTag("Player");
        if (existingPlayer != null)
        {
            Destroy(existingPlayer);
        }
        
        // Clear existing asteroids
        foreach (var asteroid in activeAsteroids)
        {
            if (asteroid != null)
            {
                Destroy(asteroid.gameObject);
            }
        }
        activeAsteroids.Clear();
        
        // Clear existing stars
        foreach (var star in activeStars)
        {
            if (star != null)
            {
                Destroy(star.gameObject);
            }
        }
        activeStars.Clear();
        
        // Find and destroy any other stars that might not be tracked in the activeStars list
        // Fix deprecated FindObjectsOfType method
        Star[] allRemainingStars = FindObjectsByType<Star>(FindObjectsSortMode.None);
        foreach (var star in allRemainingStars)
        {
            Destroy(star.gameObject);
        }
    }
    
    public void OnApplicationQuit()
    {
        // Save game before quitting
        if (autoSave)
        {
            SaveGame();
        }
    }

    // Add this method to let other objects register stars
    public void RegisterStar(Star star)
    {
        if (star != null && !activeStars.Contains(star))
        {
            activeStars.Add(star);
            Debug.Log("Star registered with GameController tracking system");
        }
    }

    // Toggle the pause state
    private void TogglePause()
    {
        IsPaused = !IsPaused;
        
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(IsPaused);
        }
        
        // Pause/unpause the game
        Time.timeScale = IsPaused ? 0f : 1f;
        
        Debug.Log(IsPaused ? "Game paused" : "Game resumed");
    }
    
    // Resume the game (for the resume button)    
    public void ShowOptions()
    {
        // Hide pause menu if it's active
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }
        
        // Show options panel
        if (optionsCanvas != null)
        {
            optionsCanvas.SetActive(true);
            
            // Initialize volume sliders with current values
            InitializeVolumeSliders();
            
            // Make sure slider listeners are set up
            SetupVolumeSliders();
            
            Debug.Log("Options panel opened");
        }
        else
        {
            Debug.LogError("Options canvas not assigned in the inspector");
        }
    }
      public void CloseOptions()
    {
        // Hide options panel
        if (optionsCanvas != null)
        {
            optionsCanvas.SetActive(false);
        }
        
        // Show pause menu again since we came from there
        if (pauseCanvas != null && IsPaused)
        {
            pauseCanvas.SetActive(true);
        }
        
        // Save volume settings to save file when closing options
        SaveVolumeSettingsInSaveFile();
        
        Debug.Log("Options panel closed and settings saved");
    }
    
    // Initialize slider values with current volume settings
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
    
    // Setup the onValueChanged listeners for volume sliders
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
      public void ResumeGame()
    {
        IsPaused = false;
        
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }
        
        // Resume time scale
        Time.timeScale = 1f;
        
        Debug.Log("Game resumed");
    }
    
    // Volume setting methods
    public void SetMasterVolume(float volume)
    {
        s_MasterVolume = volume;
        Debug.Log($"Master volume set to: {volume}");
        
        // Apply master volume to any active audio sources if needed
        // Use MenuController.SetVolumeSettingsFromSave to update the shared audio sources
        MenuController.SetVolumeSettingsFromSave(s_MasterVolume, s_MusicVolume, s_ShootingVolume, s_ExplosionVolume, s_StarCollectingVolume);
    }
    
    public void SetMusicVolume(float volume)
    {
        s_MusicVolume = volume;
        Debug.Log($"Music volume set to: {volume}");
        
        // Use MenuController to update music volume
        MenuController.SetVolumeSettingsFromSave(s_MasterVolume, s_MusicVolume, s_ShootingVolume, s_ExplosionVolume, s_StarCollectingVolume);
    }
    
    public void SetShootingVolume(float volume)
    {
        s_ShootingVolume = volume;
        Debug.Log($"Shooting volume set to: {volume}");
        
        // Use MenuController to keep everything in sync
        MenuController.SetVolumeSettingsFromSave(s_MasterVolume, s_MusicVolume, s_ShootingVolume, s_ExplosionVolume, s_StarCollectingVolume);
    }
    
    public void SetExplosionVolume(float volume)
    {
        s_ExplosionVolume = volume;
        Debug.Log($"Explosion volume set to: {volume}");
        
        // Use MenuController to keep everything in sync
        MenuController.SetVolumeSettingsFromSave(s_MasterVolume, s_MusicVolume, s_ShootingVolume, s_ExplosionVolume, s_StarCollectingVolume);
    }
    
    public void SetStarCollectingVolume(float volume)
    {
        s_StarCollectingVolume = volume;
        Debug.Log($"Star collecting volume set to: {volume}");
        
        // Use MenuController to keep everything in sync
        MenuController.SetVolumeSettingsFromSave(s_MasterVolume, s_MusicVolume, s_ShootingVolume, s_ExplosionVolume, s_StarCollectingVolume);
    }
    
    // Save current volume settings to the save file
    private void SaveVolumeSettingsInSaveFile()
    {
        string path = GetSavePath();
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
}
