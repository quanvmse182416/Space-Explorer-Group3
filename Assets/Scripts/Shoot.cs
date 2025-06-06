using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour
{
    public GameObject bulletPrefab;        // Assign your bullet prefab in inspector
    public float fireRate = 0.2f;          // Time between shots
    public LayerMask bulletLayer;          // Layer for bullets (create a "Bullets" layer in Unity)
    public float bulletLifetime = 2f;      // How long bullets exist before auto-destroying
    public float bulletSpeed = 10f;        // How fast bullets travel (optional override)
    public bool useCustomBulletSpeed = false; // Whether to override bullet's default speed
    public int bulletsPerShot = 1;         // Number of bullets to fire in a single shot
    public float spacingBetweenBullets = 0.5f; // Spacing between bullets in units
    
    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;  // Sound effect when firing bullets
    [Range(0f, 1f)]
    
    private float nextFireTime = 0f;       // Time when player can shoot again
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        
        // Check if we have a bullet prefab assigned
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet prefab not assigned to Shoot script!");
        }
    }

    void Update()
    {
        // Don't process input if the game is paused
        if (GameController.IsPaused)
            return;
        
        // Check for left mouse button or space key input
        if (((Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame) || 
            (Keyboard.current.spaceKey.isPressed || Keyboard.current.spaceKey.wasPressedThisFrame)) 
            && Time.time >= nextFireTime)
        {
            FireBullet();
            nextFireTime = Time.time + fireRate;
        }
    }
    
    void FireBullet()
    {
        // Play shooting sound effect
        if (shootSound != null)
        {
            // Use MenuController.GetShootingVolume() instead of hardcoded shootVolume
            AudioSource.PlayClipAtPoint(shootSound, transform.position, MenuController.GetShootingVolume());
        }
        
        // Get mouse position in world coordinates
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 worldMousePosition = mainCamera.ScreenToWorldPoint(mousePosition);
        
        // Calculate direction from player to mouse
        Vector2 shootDirection = worldMousePosition - (Vector2)transform.position;
        shootDirection.Normalize();
        
        // Calculate perpendicular vector for bullet spacing
        Vector2 perpendicularDirection = new Vector2(-shootDirection.y, shootDirection.x);
        
        // Calculate total width of bullet formation
        float totalWidth = spacingBetweenBullets * (bulletsPerShot - 1);
        
        for (int i = 0; i < bulletsPerShot; i++)
        {
            // Calculate the position offset for this bullet
            float offset;
            if (bulletsPerShot > 1)
            {
                // Center the bullets around the shooting line
                offset = -totalWidth / 2f + (totalWidth / (bulletsPerShot - 1)) * i;
            }
            else
            {
                // Single bullet has no offset
                offset = 0f;
            }
            
            // Calculate position with both forward distance and perpendicular spacing
            Vector2 bulletOffset = (shootDirection * 1.0f) + (perpendicularDirection * offset);
            Vector2 spawnPosition = (Vector2)transform.position + bulletOffset;
            
            // Create bullet at the calculated position
            GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
            
            // Set the bullet layer
            if (bulletLayer != 0)
                bullet.layer = (int)Mathf.Log(bulletLayer.value, 2);
            
            // Get or add Rigidbody2D to bullet
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = bullet.AddComponent<Rigidbody2D>();
            
            // Configure rigidbody for no physical interactions
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            
            // Get or add Bullet component
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript == null)
                bulletScript = bullet.AddComponent<Bullet>();
                
            // Only configure speed and lifetime
            if (useCustomBulletSpeed)
                bulletScript.bulletSpeed = bulletSpeed;
                
            bulletScript.lifetime = bulletLifetime;
            
            // Make sure the collider is a trigger
            Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
            if (bulletCollider != null)
                bulletCollider.isTrigger = true;
            
            // Make sure the bullet doesn't collide with the player
            if (bulletCollider != null && GetComponent<Collider2D>() != null)
                Physics2D.IgnoreCollision(bulletCollider, GetComponent<Collider2D>());
                
            // All bullets go in the same direction (no angle spread)
            bullet.transform.up = shootDirection;
            
            // Initialize the bullet
            bulletScript.Initialize();
        }
    }
}
