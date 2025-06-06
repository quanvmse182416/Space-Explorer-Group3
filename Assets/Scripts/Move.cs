using UnityEngine;
using UnityEngine.InputSystem;

public class Move : MonoBehaviour
{
    public float speed = 5f;  // Movement speed, adjustable in Inspector
    public bool enableMouseFacing = true;  // Toggle to enable/disable mouse facing
    
    [Tooltip("Angle offset in degrees to adjust which part of the sprite faces the mouse")]
    public float facingAngleOffset = -90f;  // Adjust this value if the right side should face the mouse

    private Camera mainCamera;

    void Start()
    {
        // Cache camera reference
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found! Player mouse facing won't work correctly.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Don't allow movement or rotation if the game is paused
        if (GameController.IsPaused)
            return;
        
        // Handle movement (independent of facing)
        HandleMovement();
        
        // Handle facing direction (towards mouse)
        if (enableMouseFacing)
        {
            FaceTowardsMouse();
        }
    }
    
    void HandleMovement()
    {
        // Get input from WASD keys and arrow keys
        Vector2 movement = Vector2.zero;
        
        // Check for vertical movement (W/S or Up/Down arrows)
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) {
            movement.y += 1;     // Up
        }
        
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) {
            movement.y -= 1;     // Down
        }
        
        // Check for horizontal movement (A/D or Left/Right arrows)
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) {
            movement.x -= 1;     // Left
        }
        
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) {
            movement.x += 1;     // Right
        }
        
        // Normalize to prevent diagonal movement being faster
        if (movement.magnitude > 1f)
            movement.Normalize();
        
        // Move the object in world space (independent of rotation)
        transform.position += (Vector3)(movement * speed * Time.deltaTime);
    }
    
    void FaceTowardsMouse()
    {
        if (mainCamera == null)
            return;
            
        // Get mouse position in world coordinates
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        
        // Calculate direction from player to mouse
        Vector2 direction = mouseWorldPosition - (Vector2)transform.position;
        
        // Calculate angle and set rotation
        if (direction != Vector2.zero)
        {
            // Calculate angle and add offset so the right side faces the target
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle + facingAngleOffset, Vector3.forward);
        }
    }
    
    public void ToggleMouseFacing(bool enabled)
    {
        enableMouseFacing = enabled;
    }
}
