using UnityEngine;
using UnityEngine.EventSystems;

// This script goes on the "Gauze" Image
public class DraggableGauze : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    public RectTransform targetArea;       // The Wound Area (Where to drop)
    public WoundPackingMiniGame manager;   // Reference to the main game script

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Canvas parentCanvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Add CanvasGroup automatically if missing (needed for raycast transparency)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Find the root canvas to handle scaling correctly
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. Remember where we started (to snap back later)
        originalPosition = rectTransform.anchoredPosition;
        
        // 2. Make slightly transparent and let raycasts pass through (so we can detect the wound behind it)
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 3. Move the object with the mouse/finger
        if (parentCanvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 4. Restore visibility
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 5. Check if we dropped it inside the Target Area
        if (IsOverTarget())
        {
            // Call the logic in your main script!
            manager.OnGauzeDropped(); 
        }

        // 6. Snap back to original position (Infinite gauze effect)
        rectTransform.anchoredPosition = originalPosition;
    }

    private bool IsOverTarget()
    {
        if (targetArea == null) return false;

        // Check if mouse position is inside the Target Rect
        Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(targetArea, Input.mousePosition, cam);
    }
}