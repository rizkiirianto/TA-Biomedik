using UnityEngine;

public class ScrollTexture : MonoBehaviour
{
    public float scrollSpeed = -2f; // Negative to flow downwards
    private SpriteRenderer spriteRenderer;
    private Material mat;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Create an instance of the material so we don't accidentally animate everything in the game
        mat = spriteRenderer.material; 
    }

    void Update()
    {
        // Calculate how far to push the texture this frame
        float offset = Time.time * scrollSpeed;
        
        // Apply the offset to the Y axis of the main texture
        mat.mainTextureOffset = new Vector2(0, offset);
    }
}