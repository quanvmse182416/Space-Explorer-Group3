using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    public string[] targetTags = new string[] { "Asteroid" };  // Target Asteroid tags by default
    public LayerMask targetLayers = 0;       // No layers targeted by default
    public float bulletSpeed = 10f;          // Speed of the bullet
    public float lifetime = 3f;              // How long the bullet exists before auto-destroying
    public List<GameObject> targetObjects = new List<GameObject>(); // Specific objects the bullet can interact with
    public bool hitAsteroidsByDefault = true; // Whether to hit asteroids by default (for backward compatibility)
    
    private Collider2D bulletCollider;
    private float collisionEnableDelay = 0.05f;  // Short delay before enabling collision
    private Camera mainCamera;               // Reference to main camera

    void Start()
    {
        // Cache main camera reference
        mainCamera = Camera.main;
        
        // Get the collider and temporarily disable it
        bulletCollider = GetComponent<Collider2D>();
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
            Invoke("EnableCollision", collisionEnableDelay);
        }
        
        // Destroy bullet after lifetime
        Destroy(gameObject, lifetime);
    }
    
    public void Initialize()
    {
        // Get the collider and temporarily disable it
        bulletCollider = GetComponent<Collider2D>();
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
            Invoke("EnableCollision", collisionEnableDelay);
        }
        
        // Cache main camera reference if not already set
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void EnableCollision()
    {
        if (bulletCollider != null)
            bulletCollider.enabled = true;
    }

    void Update()
    {
        // Move bullet forward
        transform.Translate(Vector3.up * bulletSpeed * Time.deltaTime);
        
        // Check if bullet is outside camera view and destroy if it is
        if (IsOutOfCameraView())
        {
            Destroy(gameObject);
        }
    }
    
    bool IsOutOfCameraView()
    {
        if (mainCamera == null)
            return false;
            
        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(transform.position);
        
        // Viewport coordinates are between 0 and 1 for items on screen
        // Using a small buffer (-0.1 to 1.1) to prevent premature destruction
        return viewportPosition.x < -0.1f || viewportPosition.x > 1.1f || 
               viewportPosition.y < -0.1f || viewportPosition.y > 1.1f;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignore collisions with other bullets
        if (collision.GetComponent<Bullet>() != null)
            return;
        
        // Check if this object is in our specific targets list
        if (targetObjects.Count > 0 && targetObjects.Contains(collision.gameObject))
        {
            HandleHit(collision.gameObject);
            return;
        }
        
        // Special case for asteroids if hitAsteroidsByDefault is true
        if (hitAsteroidsByDefault && collision.CompareTag("Asteroid"))
        {
            HandleHit(collision.gameObject);
            return;
        }
            
        // Only check layers if targetLayers is not empty (0)
        if (targetLayers != 0)
        {
            if (((1 << collision.gameObject.layer) & targetLayers) != 0)
            {
                HandleHit(collision.gameObject);
                return;
            }
        }
        
        // Only check tags if targetTags is not empty
        if (targetTags != null && targetTags.Length > 0)
        {
            foreach (string tag in targetTags)
            {
                if (collision.CompareTag(tag))
                {
                    HandleHit(collision.gameObject);
                    return;
                }
            }
        }
        
        // If we got here, this object isn't a valid target
    }
    
    private void HandleHit(GameObject target)
    {
        Debug.Log("Hit target: " + target.name);
        
        // Try to damage asteroid
        Asteroid asteroid = target.GetComponent<Asteroid>();
        if (asteroid != null)
        {
            // This is the line causing the error
            asteroid.TakeDamage(1);
        }
        
        // Destroy the bullet
        Destroy(gameObject);
    }
    
    public void AddTarget(GameObject target)
    {
        if (target != null && !targetObjects.Contains(target))
        {
            targetObjects.Add(target);
        }
    }
    
    public void ClearTargets()
    {
        targetObjects.Clear();
    }
}
