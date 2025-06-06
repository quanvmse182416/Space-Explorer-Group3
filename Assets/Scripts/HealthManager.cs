using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthManager : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int currentLives;
    [SerializeField] private float invulnerabilityTime = 2.0f;

    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab; // Make sure this is set in the inspector!
    [Tooltip("Drag your player prefab here for respawning")]
    [SerializeField] private Transform respawnPoint;

    // Add this flag to track whether lives have been set via loading
    private bool livesInitialized = false;

    [Header("Heart Display")]
    [SerializeField] private float pixelsPerLife = 29f; // Each life equals 29 pixels

    [Header("References")]
    [SerializeField] private GameObject gameOverPanel;

    private GameObject currentPlayerInstance;
    private RectTransform heartRect;
    private Image heartImage;

    // Dictionary to store player stats between respawns
    private Dictionary<string, object> playerStats = new Dictionary<string, object>();

    // Use this component to track invulnerability instead of tags
    public class InvulnerabilityMarker : MonoBehaviour 
    {
        // Empty component just used as a marker
    }

    void Start()
    {
        // Initialize lives ONLY if they haven't been set by loading a save
        if (!livesInitialized)
        {
            currentLives = maxLives;
            Debug.Log("Initializing player with default lives: " + currentLives);
        }

        // Get the image and rect components
        heartImage = GetComponent<Image>();
        if (heartImage == null)
        {
            Debug.LogError("No Image component found on this GameObject!");
        }

        heartRect = GetComponent<RectTransform>();
        if (heartRect == null)
        {
            Debug.LogError("No RectTransform component found on this GameObject!");
        }

        UpdateHeartDisplay();

        // Find player if it exists
        currentPlayerInstance = GameObject.FindWithTag("Player");
        if (currentPlayerInstance == null)
        {
            Debug.LogWarning("Player not found in scene!");
        }

        // Hide game over panel if assigned
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    // Call this method when player takes damage from asteroids
    public void TakeDamage()
    {
        Debug.Log("Player taking damage. Lives before: " + currentLives);

        // Check if player is already invulnerable - now check for BOTH marker types
        if (currentPlayerInstance != null && 
            (currentPlayerInstance.GetComponent<InvulnerabilityMarker>() != null ||
             currentPlayerInstance.GetComponent<Asteroid.InvulnerabilityController>() != null))
        {
            Debug.Log("Player is invulnerable, ignoring damage");
            return;
        }

        currentLives = Mathf.Max(0, currentLives - 1);
        UpdateHeartDisplay();
        
        // Deduct 10 points from the score when losing a life
        GameManager.AddScore(-10);
        Debug.Log("Lost a life: -10 points penalty applied");

        // Save player stats before game over
        if (currentLives <= 1) // Will be 0 after the decrement
        {
            SavePlayerStats();
        }

        if (currentLives <= 0)
        {
            GameOver();
        }
        else
        {
            // Always use respawn logic whether player exists or not
            StartCoroutine(RespawnPlayer());
        }
    }

    // Save important stats from current player before death/respawn
    private void SavePlayerStats()
    {
        if (currentPlayerInstance != null)
        {
            Debug.Log("Saving player stats before respawn/game over");

            // Example - save position if needed
            playerStats["position"] = currentPlayerInstance.transform.position;

            // Example - save any MonoBehaviour component data
            // If player has a custom component like PlayerController, save its data
            var playerControllers = currentPlayerInstance.GetComponents<MonoBehaviour>();
            foreach (var component in playerControllers)
            {
                // Skip certain components we don't need to save - Collider2D removed from check
                if (component is InvulnerabilityMarker)
                    continue;

                // You can add custom saving logic for specific component types here
                // Example:
                // if (component is PlayerController controller)
                // {
                //    playerStats["score"] = controller.Score;
                //    playerStats["powerLevel"] = controller.PowerLevel;
                // }
            }
        }
    }

    // Apply saved stats to newly respawned player
    private void ApplyPlayerStats()
    {
        if (currentPlayerInstance != null && playerStats.Count > 0)
        {
            Debug.Log("Restoring player stats after respawn");

            // Example - restore components
            var playerControllers = currentPlayerInstance.GetComponents<MonoBehaviour>();
            foreach (var component in playerControllers)
            {
                // Skip certain components - Collider2D removed from check
                if (component is InvulnerabilityMarker)
                    continue;

                // You can add custom restoration logic for specific component types here
                // Example:
                // if (component is PlayerController controller)
                // {
                //     if (playerStats.TryGetValue("score", out var score))
                //         controller.Score = (int)score;
                //     if (playerStats.TryGetValue("powerLevel", out var powerLevel))
                //         controller.PowerLevel = (float)powerLevel;
                // }
            }
        }
    }

    private void UpdateHeartDisplay()
    {
        if (heartRect != null)
        {
            // Calculate new width based on current lives (29px per life)
            float newWidth = currentLives * pixelsPerLife;

            // Update the width of the heart image
            Vector2 sizeDelta = heartRect.sizeDelta;
            sizeDelta.x = newWidth;
            heartRect.sizeDelta = sizeDelta;

            Debug.Log($"Updated heart display width to {newWidth}px ({currentLives} lives)");

            // Hide image completely if no lives left
            if (heartImage != null)
            {
                heartImage.enabled = currentLives > 0;
            }
        }
    }

    private IEnumerator RespawnPlayer()
    {
        Debug.Log("RespawnPlayer coroutine started. Current player: " + (currentPlayerInstance != null ? "exists" : "null"));

        if (currentPlayerInstance != null)
        {
            // Store reference to the player's collider
            Collider2D playerCollider = currentPlayerInstance.GetComponent<Collider2D>();

            // Add invulnerability marker component
            InvulnerabilityMarker marker = currentPlayerInstance.AddComponent<InvulnerabilityMarker>();
            Debug.Log("Player now invulnerable for " + invulnerabilityTime + " seconds");

            // Explicitly disable player's collider during invulnerability
            if (playerCollider != null)
            {
                playerCollider.enabled = false;
                Debug.Log("Player collider disabled for invulnerability");
            }

            // Flash player sprite
            SpriteRenderer renderer = currentPlayerInstance.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                for (float t = 0; t < invulnerabilityTime; t += 0.1f)
                {
                    renderer.enabled = !renderer.enabled;
                    yield return new WaitForSeconds(0.1f);
                }
                renderer.enabled = true;
            }
            else
            {
                yield return new WaitForSeconds(invulnerabilityTime);
            }

            // Re-enable collisions explicitly
            if (currentPlayerInstance != null && playerCollider != null)
            {
                playerCollider.enabled = true;
                Debug.Log("Player collider re-enabled after invulnerability");
            }

            // Remove invulnerability marker
            if (currentPlayerInstance != null && marker != null)
            {
                Destroy(marker);
                Debug.Log("Player invulnerability ended");
            }
        }
        else if (playerPrefab != null)
        {
            // Respawn player
            yield return new WaitForSeconds(1.0f);
            Vector3 spawnPosition = respawnPoint != null ? 
                respawnPoint.position : new Vector3(0, -4, 0);

            // Save stats from current player if it exists
            SavePlayerStats();

            // Create new player instance
            currentPlayerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

            // Ensure player has the correct tag
            currentPlayerInstance.tag = "Player";

            // Make sure collider is enabled
            Collider2D playerCollider = currentPlayerInstance.GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                playerCollider.enabled = true;
            }
            else
            {
                Debug.LogWarning("Respawned player has no Collider2D component!");
            }

            // Remove any existing invulnerability markers (just to be safe)
            InvulnerabilityMarker[] markers = currentPlayerInstance.GetComponents<InvulnerabilityMarker>();
            foreach (InvulnerabilityMarker marker in markers)
            {
                Destroy(marker);
            }

            // Also check for and remove Asteroid's InvulnerabilityController
            Asteroid.InvulnerabilityController[] controllers = 
                currentPlayerInstance.GetComponents<Asteroid.InvulnerabilityController>();
            foreach (Asteroid.InvulnerabilityController controller in controllers)
            {
                Destroy(controller);
            }

            // Add an additional check for debugging
            Debug.Log("Checking for existing InvulnerabilityController components on respawned player");
            Asteroid.InvulnerabilityController[] asteroidControllers = 
                currentPlayerInstance.GetComponents<Asteroid.InvulnerabilityController>();
            if (asteroidControllers.Length > 0)
            {
                Debug.LogWarning($"Found {asteroidControllers.Length} asteroid invulnerability controllers - removing them");
                foreach (Asteroid.InvulnerabilityController controller in asteroidControllers)
                {
                    Destroy(controller);
                }
            }

            // Apply saved stats to the new player instance
            ApplyPlayerStats();
        }
        else
        {
            Debug.LogError("Cannot respawn player - playerPrefab is not assigned in HealthManager!");
        }
    }

    // Make GameOver public so it can be called from GameController
    public void GameOver()
    {
        Debug.Log("Game Over!");

        if (currentPlayerInstance != null)
        {
            Destroy(currentPlayerInstance);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);  
        }
        
        // Find and notify GameController to show retry button when game over
        // Updated to use non-deprecated method
        GameController controller = FindAnyObjectByType<GameController>();
        if (controller != null)
        {
            controller.ShowGameOverUI();
        }
    }

    public int Lives => currentLives;

    public void ResetHealth()
    {
        currentLives = maxLives;
        UpdateHeartDisplay();
    }

    // Method to check if a GameObject is currently invulnerable
    public static bool IsInvulnerable(GameObject obj)
    {
        return obj != null && obj.GetComponent<InvulnerabilityMarker>() != null;
    }

    // Add this to the HealthManager class to expose the playerPrefab
    public GameObject PlayerPrefab => playerPrefab;

    // Add this to the HealthManager class to expose the respawn point
    public Transform RespawnPoint => respawnPoint;

    // Modify this method to set the initialization flag and properly show the retry button
    public void SetLivesDirectly(int lives)
    {
        // Ensure lives are within valid range
        currentLives = Mathf.Clamp(lives, 0, maxLives);
        UpdateHeartDisplay();
        
        // Mark that lives have been initialized from save
        livesInitialized = true;
        Debug.Log("Lives set directly from save: " + currentLives);
        
        // If lives are 0, trigger game over
        if (currentLives <= 0)
        {
            // Find and notify GameController to show retry button
            // Updated to use non-deprecated method
            GameController controller = FindAnyObjectByType<GameController>();
            if (controller != null)
            {
                controller.ShowGameOverUI();  // This method activates the retry button
            }
            
            GameOver();
        }
    }

    // Add this method to the HealthManager class to update the player reference
    public void SetPlayerInstance(GameObject playerInstance)
    {
        currentPlayerInstance = playerInstance;
    }
}
