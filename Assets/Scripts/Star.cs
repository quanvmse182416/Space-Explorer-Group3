using UnityEngine;

public class Star : MonoBehaviour
{
    public int pointValue = 10;           // How many points this star is worth
    public string playerTag = "Player";    // Tag of the player object
    public bool destroyOnCollect = true;   // Whether to destroy the star when collected
    public GameObject collectEffect;       // Optional collection effect prefab
    public AudioClip collectSound;         // Optional sound to play when collected
    [Range(0f, 1f)]
    public float collectSoundVolume = 0.7f; // Volume for collection sound
    public float fallSpeed = 2.0f;         // How fast the star falls down
    public float destroyBelowY = -10f;     // Y position below which the star gets destroyed

    private bool isCollected = false;
    private Camera mainCamera;

    void Start()
    {
        // Check for CircleCollider2D specifically
        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            // If we have any other type of collider, remove it
            Collider2D[] existingColliders = GetComponents<Collider2D>();
            foreach (Collider2D collider in existingColliders)
            {
                Destroy(collider);
            }

            // Add a CircleCollider2D (only log this in development builds)
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
            #if UNITY_EDITOR
            Debug.LogWarning("Star has no CircleCollider2D component. Adding one.");
            #endif
        }
        
        // Ensure is trigger is enabled
        circleCollider.isTrigger = true;

        // Cache main camera reference
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Make the star fall down
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        // Check if star is out of camera view
        if (IsOutOfCameraView())
        {
            Destroy(gameObject);
        }
    }

    bool IsOutOfCameraView()
    {
        // Check if below destroy threshold
        if (transform.position.y < destroyBelowY)
        {
            return true;
        }

        // Check if outside camera view, but ALLOW objects above camera
        if (mainCamera != null)
        {
            Vector3 viewportPosition = mainCamera.WorldToViewportPoint(transform.position);
            // Only check x-boundaries and bottom y-boundary
            // Objects ABOVE the camera (y > 1.1f) are allowed!
            return viewportPosition.x < -0.1f || viewportPosition.x > 1.1f || 
                   viewportPosition.y < -0.1f;
        }

        return false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if this is the player and star hasn't been collected yet
        if (!isCollected && collision.CompareTag(playerTag))
        {
            CollectStar();
        }
    }

    void CollectStar()
    {
        isCollected = true;

        // Add points to the score
        GameManager.AddScore(pointValue);

        // Play collection effect if one is assigned
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            
            // Make sure it has an animator component
            Animator effectAnimator = effect.GetComponent<Animator>();
            if (effectAnimator != null)
            {
                // Force the animation to play from the start
                effectAnimator.Play(0, 0, 0);
            }
            
            // Ensure the effect gets destroyed after playing
            if (!effect.GetComponent<StarEffectController>())
            {
                effect.AddComponent<StarEffectController>();
            }
        }

        // Play sound if one is assigned
        if (collectSound != null)
        {
            // Use MenuController.GetStarCollectingVolume() instead of hardcoded collectSoundVolume
            AudioSource.PlayClipAtPoint(collectSound, transform.position, MenuController.GetStarCollectingVolume());
            Debug.Log($"Playing star collection sound at volume: {MenuController.GetStarCollectingVolume()}");
        }

        // Hide the star immediately
        GetComponent<Renderer>().enabled = false;

        // Disable the collider
        GetComponent<Collider2D>().enabled = false;

        if (destroyOnCollect)
        {
            // Destroy after a small delay to allow sound to play
            Destroy(gameObject, collectSound != null ? collectSound.length : 0f);
        }
    }
}

// Simple static class to track score - you might want a more advanced system
public static class GameManager
{
    public static int Score { get; private set; }

    public static void AddScore(int points)
    {
        Score += points;
        Debug.Log("Score: " + Score);

        // If you have a UI Text component to display the score, update it here
        // For example: scoreText.text = "Score: " + Score;
    }

    public static void ResetScore()
    {
        Score = 0;
    }
}