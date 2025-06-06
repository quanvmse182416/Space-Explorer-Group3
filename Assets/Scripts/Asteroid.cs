using UnityEngine;
using System.Collections;

public class Asteroid : MonoBehaviour
{
    public float size = 1.0f;              // Size multiplier (affects scale, health, and speed)
    public int baseHealth = 3;             // Base health before size scaling
    public float baseFallSpeed = 3.0f;     // Base fall speed before size scaling
    public float minRotationSpeed = 15f;   // Minimum rotation speed
    public float maxRotationSpeed = 60f;   // Maximum rotation speed
    public string playerTag = "Player";    // Tag of the player game object
    public float destroyBelowY = -10f;     // Y position below which the asteroid gets destroyed
    public GameObject explosionPrefab;     // Optional explosion effect when destroyed
    public float playerInvulnerableTime = 2.0f; // Time player is invulnerable after hit

    [Header("Drops")]
    public GameObject starPrefab;          // Star prefab to spawn when destroyed

    [Header("Rendering Settings")]
    public bool enableRotation = true;    // Whether to rotate the asteroid (now true by default)
    public int sortingOrder = 1;           // Sorting order for visibility control

    [Header("Audio")]
    [SerializeField] private AudioClip explosionSound; // Sound played when asteroid is destroyed
    

    // Define enum separately, without a header
    public enum MovementPattern { Straight, SineWave, ZigZag, Accelerating, Homing }
    
    [Header("Movement Settings")]
    [SerializeField] private MovementPattern movementType = MovementPattern.Straight;
    [SerializeField] private float horizontalAmplitude = 2.0f; // How far to move horizontally
    [SerializeField] private float horizontalFrequency = 1.0f; // How fast to move horizontally
    [SerializeField] private float acceleration = 0.2f;        // For accelerating pattern
    [SerializeField] private float homingStrength = 1.2f;      // For homing pattern
    [SerializeField] private float zigZagTimer = 1.0f;         // Time between direction changes

    // Private variables for tracking movement
    private Vector3 startPosition;
    private float movementTimer = 0f;
    private Vector2 currentDirection = Vector2.down;
    private float currentZigZagTimer = 0f;

    // Simple component to track invulnerability
    public class InvulnerabilityController : MonoBehaviour
    {
        // This is just a marker component
    }

    // Derived properties
    private int health;                    // Calculated health based on size
    private float fallSpeed;               // Calculated fall speed based on size
    private Vector3 originalScale;         // Original scale before size adjustments
    private Camera mainCamera;
    private float rotationSpeed;           // Actual rotation speed of this asteroid
    private SpriteRenderer spriteRenderer; // Reference to sprite renderer

    void Awake()
    {
        // Store the original scale immediately
        originalScale = transform.localScale;
        
        // If original scale is zero, set a default scale
        if (originalScale.magnitude < 0.01f)
        {
            originalScale = Vector3.one;
            Debug.Log("Asteroid had zero scale in Awake, setting default scale");
        }
    }

    void Start()
    {
        // Cache component references
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Fix rendering issues
        if (spriteRenderer == null)
        {
            Debug.LogError("Asteroid is missing SpriteRenderer component!");
        }
        else if (spriteRenderer.sprite == null)
        {
            Debug.LogError("Asteroid SpriteRenderer has no sprite assigned!");
        }
        else
        {
            // Ensure sprite is visible
            spriteRenderer.enabled = true;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f); // Full opacity
            Debug.Log($"Asteroid initialized with sprite: {spriteRenderer.sprite.name}, sorting order: {spriteRenderer.sortingOrder}");
        }

        // Ensure z-position is correct for 2D visibility
        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, 0);

        // Initialize with size
        ApplySize();

        // ALWAYS enable rotation regardless of inspector setting
        // This overrides the enableRotation setting in case it was disabled
        enableRotation = true;

        // Apply a random rotation speed (more pronounced)
        rotationSpeed = Random.Range(minRotationSpeed * 1.2f, maxRotationSpeed * 1.2f);
        
        // Random rotation direction (clockwise or counter-clockwise)
        rotationSpeed *= (Random.value > 0.5f) ? 1 : -1;
        
        // Scale rotation speed based on asteroid size - smaller asteroids spin faster
        float sizeRatio = (3.0f - size) / 2.5f;  // Inverse relationship (smaller = faster)
        rotationSpeed *= Mathf.Lerp(0.7f, 1.7f, sizeRatio);
        
        Debug.Log($"Asteroid initialized with rotation speed: {rotationSpeed}");

        // Ensure the asteroid has a collider with "Is Trigger" enabled
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning("Asteroid has no Collider2D component. Adding one.");
            gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
        }
        else
        {
            collider.isTrigger = true;
        }

        // Initialize for movement pattern
        startPosition = transform.position;
        currentZigZagTimer = zigZagTimer;
        
        // Randomly select a movement pattern if desired
        if (Random.value > 0.3f) // 70% chance of a special movement pattern
        {
            movementType = (MovementPattern)Random.Range(0, System.Enum.GetValues(typeof(MovementPattern)).Length);
        }
    }

    // Apply size affects to scale, health, and speed
    private void ApplySize()
    {
        // Ensure size is within reasonable bounds
        size = Mathf.Clamp(size, 0.5f, 3.0f);

        // Check if originalScale is valid
        if (originalScale.magnitude < 0.01f)
        {
            originalScale = Vector3.one;
            Debug.Log("Asteroid had zero originalScale in ApplySize, using default");
        }

        // Apply size to scale with safeguards
        Vector3 newScale = originalScale * size;
        
        // Additional safeguard against zero scale
        if (newScale.magnitude < 0.01f)
        {
            newScale = Vector3.one * size;
            Debug.LogWarning($"Calculated scale was too small, using default. Size: {size}");
        }
        
        transform.localScale = newScale;
        Debug.Log($"Applied asteroid scale: {newScale}, originalScale: {originalScale}, size: {size}");

        // Scale health with size (larger = more health)
        health = Mathf.RoundToInt(baseHealth * size);

        // Scale fall speed inversely with size (larger = slower)
        fallSpeed = baseFallSpeed / size;
    }

    // Public method to change size after initialization
    public void SetSize(float newSize)
    {
        size = newSize;
        
        // Double check that originalScale is valid before applying size
        if (originalScale.magnitude < 0.01f)
        {
            originalScale = Vector3.one;
            Debug.LogWarning("originalScale was invalid in SetSize, using default Vector3.one");
        }
        
        ApplySize();
    }

    void Update()
    {
        // Force apply rotation regardless of enableRotation setting
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        
        // Update movement timers
        movementTimer += Time.deltaTime;
        
        // Apply movement based on pattern
        Vector3 movement = Vector3.zero;
        
        switch (movementType)
        {
            case MovementPattern.Straight:
                // Simple vertical movement (existing behavior)
                movement = Vector3.down * fallSpeed * Time.deltaTime;
                break;
                
            case MovementPattern.SineWave:
                // Sine wave movement pattern - smooth side to side motion
                float horizontalOffset = Mathf.Sin(movementTimer * horizontalFrequency) * horizontalAmplitude * Time.deltaTime;
                movement = new Vector3(horizontalOffset, -fallSpeed * Time.deltaTime, 0);
                break;
                
            case MovementPattern.ZigZag:
                // Zig-zag pattern with abrupt direction changes
                currentZigZagTimer -= Time.deltaTime;
                if (currentZigZagTimer <= 0)
                {
                    // Change direction
                    float randomAngle = Random.Range(-45f, 45f);
                    currentDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.down;
                    currentZigZagTimer = zigZagTimer * Random.Range(0.8f, 1.2f); // Slight randomness in timing
                }
                movement = currentDirection * fallSpeed * Time.deltaTime;
                break;
                
            case MovementPattern.Accelerating:
                // Gradually increase speed
                float currentSpeed = fallSpeed * (1 + movementTimer * acceleration);
                movement = Vector3.down * currentSpeed * Time.deltaTime;
                break;
                
            case MovementPattern.Homing:
                // Try to move toward the player
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    // Calculate direction to player
                    Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
                    
                    // Enforce a minimum downward velocity component
                    Vector2 constrainedDirection = directionToPlayer;
                    
                    // Limit upward movement - ensure asteroid keeps falling
                    if (constrainedDirection.y > -0.2f)  // If moving upward or barely downward
                    {
                        constrainedDirection.y = -0.2f;  // Force a minimum downward component
                        constrainedDirection = constrainedDirection.normalized; // Re-normalize
                    }
                    
                    // Blend between downward and toward player (with constraints)
                    Vector2 blendedDirection = Vector2.Lerp(Vector2.down, constrainedDirection, homingStrength * 0.4f);
                    
                    // Guarantee minimum downward movement
                    if (blendedDirection.y > -0.3f)
                    {
                        blendedDirection.y = -0.3f;
                        blendedDirection = blendedDirection.normalized;
                    }
                    
                    movement = blendedDirection * fallSpeed * Time.deltaTime;
                }
                else
                {
                    // No player found, just fall down
                    movement = Vector3.down * fallSpeed * Time.deltaTime;
                }
                break;
        }
        
        // Apply the movement
        transform.position += movement;

        // Check if the asteroid is out of camera view
        if (IsOutOfCameraView())
        {
            Debug.Log($"Asteroid {gameObject.name} is out of camera view, destroying");
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
        // Check if this is the player
        if (collision.CompareTag(playerTag))
        {
            // Check if player is currently invulnerable (using either component to track)
            if (collision.gameObject.GetComponent<InvulnerabilityController>() != null ||
                collision.gameObject.GetComponent<HealthManager.InvulnerabilityMarker>() != null) 
            {
                Debug.Log("Player is invulnerable, ignoring asteroid collision");
                return;
            }
            
            // Look for HealthManager to damage the player - using new non-deprecated method
            HealthManager healthManager = FindAnyObjectByType<HealthManager>();
            if (healthManager != null)
            {
                healthManager.TakeDamage();
                
                // Let HealthManager handle invulnerability - don't start our own coroutine
                // REMOVE: StartCoroutine(MakePlayerInvulnerable(collision.gameObject, playerInvulnerableTime));
            }
            
            // Create a hit effect (optional)
            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, collision.transform.position, Quaternion.identity);
                
                // Add ExplosionController to ensure the explosion is destroyed after playing
                if (!explosion.GetComponent<ExplosionController>())
                {
                    explosion.AddComponent<ExplosionController>();
                }
            }

            // Destroy the asteroid on player contact
            DestroyAsteroid();
        }
        // Add this check for bullets
        else if (collision.GetComponent<Bullet>() != null)
        {
            Debug.Log("Asteroid hit by bullet: " + collision.gameObject.name);
            TakeDamage(1);

            // Destroy the bullet
            Destroy(collision.gameObject);
        }
    }

    // THIS METHOD IS NOW REDUNDANT - HealthManager handles invulnerability
    /* 
    IEnumerator MakePlayerInvulnerable(GameObject player, float duration)
    {
        // Method code commented out to avoid conflicts with HealthManager
    }
    */

    // Method for bullets to call when they hit the asteroid
    public void TakeDamage(int damage = 1)
    {
        health -= damage;

        if (health <= 0)
        {
            DestroyAsteroid();
        }
    }

    public void DestroyAsteroid()
    {
        // Play explosion sound
        PlayExplosionSound();
        
        // Show explosion effect if one is assigned
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);

            // Scale the explosion with the asteroid size
            explosion.transform.localScale *= size;
            
            // Ensure the explosion gets destroyed after playing
            if (!explosion.GetComponent<ExplosionController>())
            {
                explosion.AddComponent<ExplosionController>();
            }
        }
        
        // Spawn a star at the current position
        SpawnStar();

        // Destroy the asteroid
        Destroy(gameObject);
    }

    // Add this method to play a sound that will continue even after the asteroid is destroyed
    private void PlayExplosionSound()
    {
        if (explosionSound != null)
        {
            // Use MenuController.GetExplosionVolume() instead of hardcoded explosionVolume
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, MenuController.GetExplosionVolume());
            Debug.Log($"Playing explosion sound at volume: {MenuController.GetExplosionVolume()}");
        }
    }

    // Spawn a star at the asteroid's position
    private void SpawnStar()
    {
        // Try to find the star prefab if not assigned
        if (starPrefab == null)
        {
            // First check if it's in Resources folder
            starPrefab = Resources.Load<GameObject>("Prefabs/Star");

            // If not in Resources, try to find it in the scene or project
            if (starPrefab == null)
            {
                // Look for an existing star in the scene to use as template
                Star existingStar = FindAnyObjectByType<Star>(FindObjectsInactive.Include);
                if (existingStar != null)
                {
                    starPrefab = existingStar.gameObject;
                }
                else
                {
                    Debug.LogWarning("Star Prefab not found in Resources or Scene. Please assign it in the Inspector.");
                    return;
                }
            }
        }

        // Now that we have a starPrefab (either assigned or found), instantiate it
        if (starPrefab != null)
        {
            // Instantiate the star at the exact position of the asteroid
            GameObject starObject = Instantiate(starPrefab, transform.position, Quaternion.identity);
            
            // Find the GameController and add this star to its tracking list
            GameController gameController = FindAnyObjectByType<GameController>();
            if (gameController != null && starObject != null)
            {
                Star star = starObject.GetComponent<Star>();
                if (star != null)
                {
                    gameController.RegisterStar(star);
                }
            }
        }
        else
        {
            Debug.LogWarning("Star Prefab not assigned on asteroid. Cannot spawn star on destruction.");
        }
    }

    // Helper properties for external access
    public float Size { get { return size; } }
    public int Health { get { return health; } }
    public float FallSpeed { get { return fallSpeed; } }

    // Add this method to the Asteroid class
    public void SetBonusHealth(int bonusHealth)
    {
        // Add bonus health directly
        health += bonusHealth;
        
        // Visual indication that this asteroid is reinforced
        if (spriteRenderer != null)
        {
            // Color coding based on strength:
            if (bonusHealth <= 3)
            {
                // Slight orange tint for slightly reinforced asteroids
                spriteRenderer.color = new Color(1.0f, 0.8f, 0.6f);
            }
            else if (bonusHealth <= 8)
            {
                // Red tint for moderately reinforced asteroids
                spriteRenderer.color = new Color(1.0f, 0.5f, 0.5f);
            }
            else
            {
                // Purple tint for heavily reinforced asteroids
                spriteRenderer.color = new Color(0.8f, 0.4f, 1.0f);
            }
        }
    }
}