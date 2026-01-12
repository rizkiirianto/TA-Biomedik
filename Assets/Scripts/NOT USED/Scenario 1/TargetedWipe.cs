/// <summary>
/// Script untuk Minigame Wipe/Erase dengan Multi-Layer.
/// Minigame ini diinisialisasi oleh dirinya sendiri melalui fungsi Start()
/// dan menerima referensi GameManager melalui StartMiniGame() untuk melapor kembali.
///
/// CARA KERJA:
/// 1. Skrip aktif dan menjalankan setup awal di fungsi Start().
/// 2. Ia mengelola serangkaian lapisan gambar yang bisa dihapus.
/// 3. Kalkulasi posisi sentuhan didasarkan pada RectTransform dari gambar yang aktif saat itu.
/// 4. Saat semua lapisan selesai, ia akan memanggil GameManager untuk melanjutkan alur permainan.
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

// =========================================================================================
// KELAS DATA WIPE LAYER
// =========================================================================================
[System.Serializable]
public class WipeLayer
{
    [Tooltip("Komponen Image UI untuk lapisan ini.")]
    public Image image;
    [Tooltip("Texture hitam-putih sebagai peta area yang bisa dihapus.")]
    public Texture2D mask;
    [Tooltip("(Opsional) Panel UI yang akan muncul saat lapisan ini aktif.")]
    public GameObject feedbackPanel;
}

// =========================================================================================
// KELAS UTAMA TARGETED WIPE
// =========================================================================================
public class TargetedWipe : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    #region Variabel Publik
    //--------------------------------------------------------------------------------------
    [Header("Pengaturan Lapisan")]
    [Tooltip("Daftar semua lapisan yang akan ditampilkan secara berurutan.")]
    public WipeLayer[] layers;

    [Header("Pengaturan Kuas")]
    [Tooltip("Ukuran diameter kuas penghapus dalam piksel.")]
    public int brushSize = 70;

    [Header("Pengaturan Kursor Kustom")]
    [Tooltip("Canvas utama yang menampung UI minigame ini.")]
    private Canvas parentCanvas;
    [Tooltip("Aktifkan untuk menggunakan UI Image sebagai kursor.")]
    public bool useCustomCursor;
    [Tooltip("Drag komponen UI Image yang akan dijadikan kursor ke sini.")]
    public Image[] customCursorImages;
    //public Image customCursorTisuBersih;
    //public Image customCursorTisuAgakKotor;
    //public Image customCursorTisuKotor;

    [Header("Pengaturan Progres")]
    [Tooltip("Persentase area mask yang harus terhapus untuk beralih lapisan (0.9 = 90%).")]
    [Range(0.1f, 1f)]
    public float wipeThreshold = 0.9f;

    [Header("Minigame Pilih Item di Awal")]
    public GameObject panelPilihItem;
    public TextMeshProUGUI pilihanItemFeedbackText;
    public GameObject panelBukaWadahTisu;
    public AudioSource jawabanBenar;
    public AudioSource jawabanSalah;
    //--------------------------------------------------------------------------------------
    #endregion

    #region Variabel Privat
    //--------------------------------------------------------------------------------------
    private GameManager gameManager;
    private int currentLayerIndex = 0;
    private Texture2D currentActiveTexture;
    private Texture2D currentActiveMask;
    private RectTransform currentImageRectTransform; // Acuan RectTransform dari GAMBAR yang aktif
    private int wipedPixelsInMaskArea;
    private int totalWipeablePixels;
    private bool isWipeActive = true;
    private Image activeCustomCursor;
    private bool isCursorInsideDropZone = false;
    //--------------------------------------------------------------------------------------
    #endregion

    #region Inisialisasi
    //--------------------------------------------------------------------------------------
    /// <summary>
    /// Dipanggil oleh GameManager untuk memberikan referensi dirinya.
    /// </summary>
    public void StartMiniGame(GameManager gm)
    {
        gameManager = gm;
        this.gameObject.SetActive(true);
    }

    /// <summary>
    /// Start dipanggil sebelum frame pertama. Digunakan untuk setup awal minigame.
    /// </summary>
    void Start()
    {
        useCustomCursor = true;
        panelPilihItem.SetActive(true);
        // Pastikan semua lapisan di bawah lapisan pertama nonaktif.
        for (int i = 1; i < layers.Length; i++)
        {
            layers[i].image.gameObject.SetActive(false);
        }

        // Siapkan lapisan pertama.
        if (layers.Length > 0)
        {
            SetupLayer(currentLayerIndex);
        }

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("Error: TargetedWipe tidak dapat menemukan Canvas di parent-nya! Pastikan prefab ini berada di dalam sebuah Canvas.");
        }
    }

    public void ActivateCustomCursor()
    {
        useCustomCursor = true;

        // Cek untuk memastikan inisialisasi hanya berjalan sekali
        if (activeCustomCursor == null && customCursorImages != null && customCursorImages.Length > 0)
        {
            Debug.Log("Kursor kustom diaktifkan melalui fungsi, melakukan inisialisasi...");
            Cursor.visible = false;
            SetActiveCursor(0);
        }
    }

    public void SetCursorInDropZone(bool isInside)
    {
        isCursorInsideDropZone = isInside;
        Debug.Log("Kursor sekarang di dalam Drop Zone: " + isInside);
    }

    /// <summary>
    /// Mengatur kursor mana yang aktif berdasarkan indeks di array.
    /// </summary>
    private void SetActiveCursor(int index)
    {
        // Pastikan indeks valid
        if (customCursorImages == null || index < 0 || index >= customCursorImages.Length)
        {
            return;
        }

        // Matikan semua kursor terlebih dahulu
        for (int i = 0; i < customCursorImages.Length; i++)
        {
            if (customCursorImages[i] != null)
            {
                customCursorImages[i].gameObject.SetActive(false);
            }
        }

        // Aktifkan kursor yang dipilih dan simpan sebagai kursor aktif
        activeCustomCursor = customCursorImages[index];
        if (activeCustomCursor != null)
        {
            activeCustomCursor.gameObject.SetActive(true);
        }
    }
    //--------------------------------------------------------------------------------------
    #endregion
    #region Update Loop
    //--------------------------------------------------------------------------------------
    /// <summary>
    /// Update dipanggil setiap frame.
    /// </summary>
    void Update()
    {
        // Jika menggunakan kursor kustom dan ada kursor yang aktif
        if (useCustomCursor && activeCustomCursor != null)
        {
            if (Cursor.visible) Cursor.visible = false;

            // Hanya tampilkan kursor jika proses wipe aktif
            activeCustomCursor.gameObject.SetActive(isWipeActive);

            // Gerakkan kursor yang sedang aktif, apa pun itu
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                Input.mousePosition,
                parentCanvas.worldCamera,
                out localPoint
            );
            activeCustomCursor.rectTransform.localPosition = localPoint;
        }
        else
        {
            // Logika untuk memastikan kursor sistem kembali normal
            if (!Cursor.visible) Cursor.visible = true;
        }
    }
    //--------------------------------------------------------------------------------------
    #endregion
    #region Logika Utama
    //--------------------------------------------------------------------------------------
    /// <summary>
    /// Menyiapkan lapisan berdasarkan indeks yang diberikan.
    /// </summary>
    private void SetupLayer(int index)
    {
        currentLayerIndex = index;
        WipeLayer currentLayer = layers[index];
        currentLayer.image.gameObject.SetActive(true);

        // PENTING: Mengambil RectTransform dari GAMBAR yang aktif, bukan dari manager.
        currentImageRectTransform = currentLayer.image.GetComponent<RectTransform>();
        currentActiveMask = currentLayer.mask;

        // Tampilkan panel feedback jika ada.
        if (currentLayer.feedbackPanel != null)
        {
            StartCoroutine(ShowFeedbackPanel(currentLayer.feedbackPanel, 2f));
            jawabanBenar.Play();
        }

        // Buat salinan tekstur agar bisa dimodifikasi.
        Texture2D originalTexture = currentLayer.image.sprite.texture;
        currentActiveTexture = new Texture2D(originalTexture.width, originalTexture.height);
        currentActiveTexture.SetPixels(originalTexture.GetPixels());
        currentActiveTexture.Apply();

        // Terapkan salinan tekstur ke sprite.
        currentLayer.image.sprite = Sprite.Create(
            currentActiveTexture,
            new Rect(0, 0, currentActiveTexture.width, currentActiveTexture.height),
            new Vector2(0.5f, 0.5f)
        );

        CalculateTotalWipeablePixels();
    }

    /// <summary>
    /// Fungsi utama untuk menghapus piksel berdasarkan posisi sentuhan.
    /// </summary>
    private void Erase(Vector2 screenPosition)
    {
        if (!isWipeActive || currentImageRectTransform == null) return;

        Vector2 localPoint;
        var canvas = currentImageRectTransform.GetComponentInParent<Canvas>();
        // PENTING: Kalkulasi posisi didasarkan pada RectTransform GAMBAR yang aktif.
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(currentImageRectTransform, screenPosition, canvas.worldCamera, out localPoint))
        {
            float px = Mathf.InverseLerp(currentImageRectTransform.rect.xMin, currentImageRectTransform.rect.xMax, localPoint.x);
            float py = Mathf.InverseLerp(currentImageRectTransform.rect.yMin, currentImageRectTransform.rect.yMax, localPoint.y);
            int pixelX = (int)(px * currentActiveTexture.width);
            int pixelY = (int)(py * currentActiveTexture.height);

            for (int x = -brushSize / 2; x < brushSize / 2; x++)
            {
                for (int y = -brushSize / 2; y < brushSize / 2; y++)
                {
                    if (new Vector2(x, y).magnitude < brushSize / 2)
                    {
                        int targetX = pixelX + x;
                        int targetY = pixelY + y;
                        if (targetX < 0 || targetX >= currentActiveTexture.width || targetY < 0 || targetY >= currentActiveTexture.height) continue;
                        if (currentActiveMask.GetPixel(targetX, targetY).r > 0.5f)
                        {
                            if (currentActiveTexture.GetPixel(targetX, targetY).a != 0)
                            {
                                currentActiveTexture.SetPixel(targetX, targetY, Color.clear);
                                wipedPixelsInMaskArea++;
                            }
                        }
                    }
                }
            }
            currentActiveTexture.Apply();
            CheckForNextLayer();
        }
    }

    /// <summary>
    /// Memeriksa progres dan memutuskan apakah akan beralih lapisan atau menyelesaikan minigame.
    /// </summary>
    private void CheckForNextLayer()
    {
        if (totalWipeablePixels == 0) return;
        float wipePercentage = (float)wipedPixelsInMaskArea / totalWipeablePixels;
        if (wipePercentage > 0.5f)
        {
            SetActiveCursor(1);
            Debug.Log("Tisue agak kotor muncul");
        }

        if (wipePercentage >= wipeThreshold)
        {
            SetActiveCursor(2);
            Debug.Log("Tisue kotor muncul");
        }
    }

    private void WipeNextLayer()
    {
        SetActiveCursor(0);
        layers[currentLayerIndex].image.gameObject.SetActive(false);
        currentLayerIndex++;

        if (currentLayerIndex < layers.Length)
        {
            SetupLayer(currentLayerIndex);
        }
        else
        {
            if (gameManager != null)
            {
                gameManager.OnMiniGameComplete("Bagus! Darah sudah dibersihkan.");
            }
            this.gameObject.SetActive(false);
        }
    }

    public void OnRightItemClicked()
    {
        panelPilihItem.SetActive(false);
        jawabanBenar.Play();
        panelBukaWadahTisu.SetActive(true);
    }

    public void OnWrongItemClicked()
    {
        pilihanItemFeedbackText.text = "Wah kurang tepat, coba lagii";
        jawabanSalah.Play();
    }
    //--------------------------------------------------------------------------------------
    #endregion

    #region Fungsi Event & Pembantu
    //--------------------------------------------------------------------------------------
    public void OnPointerDown(PointerEventData eventData) { Erase(eventData.position); }
    public void OnDrag(PointerEventData eventData) { Erase(eventData.position); }

    private IEnumerator ShowFeedbackPanel(GameObject panel, float delay) { isWipeActive = false; panel.SetActive(true); yield return new WaitForSeconds(delay); panel.SetActive(false); isWipeActive = true; }

    private void CalculateTotalWipeablePixels()
    {
        wipedPixelsInMaskArea = 0;
        totalWipeablePixels = 0;
        Color[] maskPixels = currentActiveMask.GetPixels();
        foreach (Color pixel in maskPixels)
        {
            if (pixel.r > 0.5f) { totalWipeablePixels++; }
        }
    }
    /// <summary>
    /// Dipanggil saat objek ini dinonaktifkan.
    /// </summary>
    void OnDisable()
    {
        // Penting: Pastikan kursor sistem kembali normal saat minigame selesai atau nonaktif.
        Cursor.visible = true;
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        // Pertama, hitung persentase wipe saat ini
        if (totalWipeablePixels == 0) return;
        float wipePercentage = (float)wipedPixelsInMaskArea / totalWipeablePixels;

        // Cek TIGA kondisi:
        // 1. Apakah kursor ada di dalam drop zone?
        // 2. Apakah progres wipe sudah mencapai ambang batas (tisu sudah kotor)?
        // 3. Apakah kursor kustom sedang aktif?
        if (isCursorInsideDropZone && wipePercentage >= wipeThreshold && useCustomCursor)
        {
            Debug.Log("Tisu kotor dibuang! Pindah ke layer berikutnya.");
            WipeNextLayer(); // Jalankan fungsi yang Anda mau
        }
    }
    //--------------------------------------------------------------------------------------
    #endregion
}