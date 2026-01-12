using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Reveal : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public int brushSize = 50;
    private Image topImage;
    private Texture2D topTexture;
    private RectTransform imageRectTransform;

    void Start()
    {
        topImage = GetComponent<Image>();
        imageRectTransform = GetComponent<RectTransform>();
        // Buat temporary texture untuk dimainkan
        Texture2D originalTexture = topImage.sprite.texture;
        Texture2D tempTexture = new Texture2D(originalTexture.width, originalTexture.height);
        tempTexture.SetPixels(originalTexture.GetPixels());
        tempTexture.Apply();

        // Texture duplikat itu tadi dipasang
        topImage.sprite = Sprite.Create(
            tempTexture,
            new Rect(0, 0, tempTexture.width, tempTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        topTexture = tempTexture;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Erase(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Erase(eventData.position);
    }

    private void Erase(Vector2 screenPosition)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(imageRectTransform, screenPosition, null, out localPoint))
        {
            // normalized
            float px = Mathf.InverseLerp(imageRectTransform.rect.xMin, imageRectTransform.rect.xMax, localPoint.x);
            float py = Mathf.InverseLerp(imageRectTransform.rect.yMin, imageRectTransform.rect.yMax, localPoint.y);

            // convert jadi pixel
            int pixelX = (int)(px * topTexture.width);
            int pixelY = (int)(py * topTexture.height);

            // eksekusi brush
            for (int x = -brushSize / 2; x < brushSize / 2; x++)
            {
                for (int y = -brushSize / 2; y < brushSize / 2; y++)
                {
                    // Optional: Brush bentuknya lingkaran
                    //if (new Vector2(x, y).magnitude < brushSize / 2)
                    //{
                        topTexture.SetPixel(pixelX + x, pixelY + y, Color.clear);
                    //}
                }
            }
            topTexture.Apply();
        }
    }
}
