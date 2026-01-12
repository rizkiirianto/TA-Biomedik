using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MultilayerReveal : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    // 1. Tentukan semua gambar layer di Inspector, urutkan dari paling atas ke paling bawah.
    // Contoh: [0] = Mimisan Berat, [1] = Mimisan Sedang, [2] = Mimisan Ringan
    public Image[] layerImages; 

    [Header("Pengaturan Kuas")]
    public int brushSize = 70;
    
    // 2. Ambang batas (persentase) kapan harus pindah ke layer berikutnya.
    // 0.7f berarti 70% dari gambar harus terhapus sebelum pindah.
    [Range(0.1f, 1f)]
    public float revealThreshold = 0.7f;

    private int currentLayerIndex = 0;
    private Texture2D currentTexture;
    private RectTransform currentImageRectTransform;

    // Untuk menghitung persentase
    private int transparentPixels;
    private int totalPixels;

    void Start()
    {
        // Pastikan semua layer di bawah layer pertama tidak aktif saat mulai
        for (int i = 1; i < layerImages.Length; i++)
        {
            layerImages[i].gameObject.SetActive(false);
        }

        // Jika ada layer yang perlu dikerjakan, siapkan layer pertama
        if (layerImages.Length > 0)
        {
            SetupLayer(currentLayerIndex);
        }
    }

    // Fungsi untuk mempersiapkan sebuah layer agar bisa dihapus
    private void SetupLayer(int index)
    {
        // Aktifkan GameObject dari layer yang sekarang
        layerImages[index].gameObject.SetActive(true);
        currentImageRectTransform = layerImages[index].GetComponent<RectTransform>();

        // Buat duplikat texture dari layer saat ini agar yang asli tidak berubah
        Texture2D originalTexture = layerImages[index].sprite.texture;
        currentTexture = new Texture2D(originalTexture.width, originalTexture.height);
        currentTexture.SetPixels(originalTexture.GetPixels());
        currentTexture.Apply();

        // Pasang texture duplikat ke Image component
        layerImages[index].sprite = Sprite.Create(
            currentTexture,
            new Rect(0, 0, currentTexture.width, currentTexture.height),
            new Vector2(0.5f, 0.5f)
        );

        // Reset penghitung untuk layer baru
        totalPixels = currentTexture.width * currentTexture.height;
        transparentPixels = 0;
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
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(currentImageRectTransform, screenPosition, null, out localPoint))
        {
            // Normalisasi posisi (0-1)
            float px = Mathf.InverseLerp(currentImageRectTransform.rect.xMin, currentImageRectTransform.rect.xMax, localPoint.x);
            float py = Mathf.InverseLerp(currentImageRectTransform.rect.yMin, currentImageRectTransform.rect.yMax, localPoint.y);

            // Konversi ke koordinat pixel texture
            int pixelX = (int)(px * currentTexture.width);
            int pixelY = (int)(py * currentTexture.height);

            // Proses menghapus dengan kuas
            for (int x = -brushSize / 2; x < brushSize / 2; x++)
            {
                for (int y = -brushSize / 2; y < brushSize / 2; y++)
                {
                    // Opsional: Bentuk kuas lingkaran (hapus comment jika ingin dipakai)
                    if (new Vector2(x, y).magnitude < brushSize / 2)
                    {
                        int targetX = pixelX + x;
                        int targetY = pixelY + y;
                        
                        // Cek apakah pixel ini sudah transparan atau belum
                        // Ini penting agar tidak menghitung ulang pixel yang sama
                        if (currentTexture.GetPixel(targetX, targetY).a != 0)
                        {
                            currentTexture.SetPixel(targetX, targetY, Color.clear);
                            transparentPixels++;
                        }
                    }
                }
            }
            currentTexture.Apply();

            // 3. Setelah menghapus, cek apakah sudah waktunya pindah layer
            CheckForNextLayer();
        }
    }

    private void CheckForNextLayer()
    {
        float revealPercentage = (float)transparentPixels / totalPixels;

        // Jika persentase terhapus sudah melewati ambang batas
        if (revealPercentage >= revealThreshold)
        {
            // Sembunyikan layer saat ini
            layerImages[currentLayerIndex].gameObject.SetActive(false);
            
            // Pindah ke index layer berikutnya
            currentLayerIndex++;

            // Jika masih ada layer berikutnya, siapkan
            if (currentLayerIndex < layerImages.Length)
            {
                SetupLayer(currentLayerIndex);
            }
            else
            {
                // Semua layer sudah bersih, nonaktifkan script ini agar tidak memproses input lagi
                Debug.Log("Semua darah sudah dibersihkan!");
                this.enabled = false; 
            }
        }
    }
}