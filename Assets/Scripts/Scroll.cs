using UnityEngine;

public class Scroll : MonoBehaviour
{
    public float speed = 1.0f; // Speed of the scrolling effect
    public Vector2 direction = Vector2.right; // Direction of scrolling (default: right)
    private Renderer rendererComponent;  // Reference to the renderer component
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the renderer component from this GameObject
        rendererComponent = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // Apply scrolling to the material's texture offset
        rendererComponent.material.mainTextureOffset += direction.normalized * speed * Time.deltaTime;
    }
}
