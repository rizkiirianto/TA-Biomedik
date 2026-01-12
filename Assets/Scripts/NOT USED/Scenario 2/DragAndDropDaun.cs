using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDropDaun : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    public Vector3 originalPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //canvasGroup.alpha = 0.6f; // Membuat objek sedikit transparan saat di-drag
        canvasGroup.blocksRaycasts = false; // Memungkinkan event raycast menembus objek
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / transform.parent.localScale.x;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f; // Mengembalikan transparansi
        canvasGroup.blocksRaycasts = true; // Mengembalikan raycast
        
        // Cek apakah di-drop di atas DropArea
        bool droppedOnValidArea = false;
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            DropArea dropArea = eventData.pointerCurrentRaycast.gameObject.GetComponent<DropArea>();
            if (dropArea != null)
            {
                droppedOnValidArea = true;
            }
        }
        
        // Jika tidak di drop di tempat yang benar, kembalikan ke posisi awal
        if (!droppedOnValidArea)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }
}
