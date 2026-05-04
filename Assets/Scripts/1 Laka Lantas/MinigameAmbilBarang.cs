using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.Events;

public class MinigameAmbilBarang : MonoBehaviour, IMiniGame
{
    [Header("Game Settings")]
    [SerializeField] private int correctItemIndex = 4; // Jawaban benar default: saputangan (index 4)

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dialogText;   // Text untuk menampilkan monolog
    [SerializeField] private GameObject tasBagGameObject; // GameObj tas utama yang mati saat transisi
    [SerializeField] private GameObject itemTasAtas;      // Panel dengan item atas
    [SerializeField] private GameObject itemTasBawah;     // Panel dengan item bawah
    [SerializeField] private Button[] itemButtons;        // Semua 6 button item (atas dan bawah)
    [SerializeField] private CanvasGroup tasBagCanvasGroup; // Untuk fade transition
    [SerializeField] private CanvasGroup itemsCanvasGroup;  // Untuk fade transition items

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSfx;

    // Private Variables
    private GameManager gameManager;
    private bool itemSelected = false;
    private int wrongAttemptCount = 0;
    [SerializeField] private bool testingMode = false; // Set TRUE saat testing di scene terpisah

    private void Start()
    {
        EnsureCorrectItemIndex();

        // Untuk testing di scene terpisah tanpa GameManager
        if (testingMode && gameManager == null)
        {
            Debug.Log("MinigameAmbilBarang: Mode Testing - Starting game tanpa GameManager");
            StartCoroutine(GameSequenceTest());
        }
    }

    // 1. Implementasi fungsi wajib IMiniGame
    public void BeginGame(GameManager gm)
    {
        this.gameManager = gm;
        itemSelected = false;
        wrongAttemptCount = 0;

        EnsureCorrectItemIndex();

        Debug.Log("MinigameAmbilBarang: BeginGame dipanggil!");
        Debug.Log($"GameManager diterima: {(gm != null ? "Ya" : "Null")}");

        // Validasi referensi
        if (gm == null)
        {
            Debug.LogError("MinigameAmbilBarang: GameManager adalah NULL!");
            return;
        }

        if (dialogText == null)
        {
            Debug.LogError("MinigameAmbilBarang: Dialog Text belum di-assign!");
            return;
        }

        if (itemButtons == null || itemButtons.Length != 6)
        {
            Debug.LogError($"MinigameAmbilBarang: Item buttons tidak valid! Length: {(itemButtons?.Length ?? 0)}");
            return;
        }

        Debug.Log("MinigameAmbilBarang: Semua validasi sukses, mulai GameSequence!");
        // Mulai sequence game
        StartCoroutine(GameSequence());
    }

    private void EnsureCorrectItemIndex()
    {
        if (correctItemIndex < 0 || correctItemIndex >= 6)
        {
            Debug.LogWarning("MinigameAmbilBarang: Correct item index invalid. Fallback ke saputangan (index 4).");
            correctItemIndex = 4;
        }
    }

    // 2. Main Game Flow
    private IEnumerator GameSequence()
    {
        // STEP 1: Tampilkan monolog awal Jiro
        yield return StartCoroutine(ShowMonologue(
            "Jiro",
            "Aku perlu mencari sesuatu di tasku yang bisa membantu menghentikan pendarahan... Mari kita lihat apa yang ada di sini."
        ));

        // STEP 2: Tunggu pemain klik tombol lanjut
        yield return StartCoroutine(WaitForAdvanceClick());

        // STEP 3: Transisi dari tas ke item (fade out tas, fade in items)
        yield return StartCoroutine(TransitionFromBagToItems());

        // STEP 4: Tunggu pemain memilih item
        yield return StartCoroutine(WaitForItemSelection());

        // STEP 5: Handle hasil pilihan (sudah dilakukan di OnItemSelected)
        // Tunggu monolog selesai ditampilkan
        yield return new WaitForSeconds(1.5f);

        // STEP 6: Game selesai
        EndGame();
    }

    // 3. Monolog Display
    private IEnumerator ShowMonologue(string speakerName, string monologText)
    {
        if (dialogText == null)
        {
            Debug.LogError("MinigameAmbilBarang: dialogText tidak di-assign!");
            yield break;
        }

        // Format: "<b>Speaker</b>: Text"
        string fullText = $"<b>{speakerName}</b>: {monologText}";
        dialogText.text = fullText;
        Debug.Log($"Monolog ditampilkan: {fullText}");

        if (gameManager != null && gameManager.clickAdvancePanel != null)
        {
            gameManager.clickAdvancePanel.SetActive(true);
        }
        else if (testingMode)
        {
            // Testing mode - tampilkan text hint
            Debug.Log("Testing Mode: Klik mouse atau tekan SPACE untuk lanjut");
        }

        yield return null;
    }

    // 4. Tunggu klik tombol lanjut
    private IEnumerator WaitForAdvanceClick()
    {
        bool clickedByButton = false;
        UnityAction tempAdvanceListener = null;

        // Jika ada tombol advance dari GameManager, pasang listener sementara milik minigame.
        if (gameManager != null && gameManager.clickAdvanceButton != null)
        {
            Button advanceBtn = gameManager.clickAdvanceButton;
            tempAdvanceListener = () => { clickedByButton = true; };
            advanceBtn.onClick.AddListener(tempAdvanceListener);
        }

        // Fallback input agar tidak stuck jika button tidak menerima klik di scene tertentu.
        while (!clickedByButton)
        {
            bool clickedByInput = Input.GetMouseButtonDown(0)
                                  || Input.GetKeyDown(KeyCode.Space)
                                  || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

            if (clickedByInput)
            {
                break;
            }

            yield return null;
        }

        // Lepas hanya listener yang kita tambah, jangan hapus listener lain milik GameManager.
        if (gameManager != null && gameManager.clickAdvanceButton != null && tempAdvanceListener != null)
        {
            gameManager.clickAdvanceButton.onClick.RemoveListener(tempAdvanceListener);
        }

        if (gameManager != null && gameManager.clickAdvancePanel != null)
        {
            gameManager.clickAdvancePanel.SetActive(false);
        }
    }

    // 5. Transisi visual dari tas ke items
    private IEnumerator TransitionFromBagToItems()
    {
        float fadeDuration = 0.3f; // Dipercepat sedikit agar lebih snappy
        
        Debug.Log("[Transisi] Memulai transisi yang lebih mulus...");

        // Setup Canvas Groups (Tetap sama seperti aslinya)
        if (tasBagCanvasGroup == null && tasBagGameObject != null)
        {
            tasBagCanvasGroup = tasBagGameObject.GetComponent<CanvasGroup>();
            if (tasBagCanvasGroup == null) tasBagCanvasGroup = tasBagGameObject.AddComponent<CanvasGroup>();
        }

        if (itemsCanvasGroup == null)
        {
            if (itemTasAtas != null) itemsCanvasGroup = itemTasAtas.GetComponent<CanvasGroup>();
            if (itemsCanvasGroup == null && itemTasBawah != null) itemsCanvasGroup = itemTasBawah.GetComponent<CanvasGroup>();

            if (itemsCanvasGroup == null)
            {
                Transform parent = itemTasAtas != null ? itemTasAtas.transform.parent : itemTasBawah.transform.parent;
                itemsCanvasGroup = parent.GetComponent<CanvasGroup>();
                if (itemsCanvasGroup == null) itemsCanvasGroup = parent.gameObject.AddComponent<CanvasGroup>();
            }
        }

        // PENTING: Set scale semua item ke 0 SEBELUM panel aktif agar tidak ada flicker/snap
        foreach (var btn in itemButtons)
        {
            if (btn != null) btn.GetComponent<RectTransform>().localScale = Vector3.zero;
        }

        // Aktifkan panel dan reset alpha
        if (itemTasAtas != null) itemTasAtas.SetActive(true);
        if (itemTasBawah != null) itemTasBawah.SetActive(true);
        if (itemsCanvasGroup != null) itemsCanvasGroup.alpha = 0f;

        // CROSSFADE: Fade out tas DAN Fade in panel items secara BERSAMAAN
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            if (tasBagCanvasGroup != null) tasBagCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            if (itemsCanvasGroup != null) itemsCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            
            yield return null;
        }

        // Pastikan state akhir rapi
        if (tasBagCanvasGroup != null) tasBagCanvasGroup.alpha = 0f;
        if (tasBagGameObject != null) tasBagGameObject.SetActive(false);
        if (itemsCanvasGroup != null) itemsCanvasGroup.alpha = 1f;

        // Mulai memunculkan item satu per satu
        yield return StartCoroutine(StaggerShowItems());
    }

    // Helper: Tampilkan items satu per satu dengan scale animation
    private IEnumerator StaggerShowItems()
    {
        float staggerDelay = 0.08f;  // Dibuat lebih cepat agar terasa seperti "rantai" yang memuaskan
        float scaleDuration = 0.35f; // Durasi pas untuk efek bounce

        for (int i = 0; i < itemButtons.Length; i++)
        {
            StartCoroutine(ScaleItemIn(itemButtons[i], scaleDuration));
            yield return new WaitForSeconds(staggerDelay);
        }

        // Tunggu hingga animasi item terakhir selesai
        yield return new WaitForSeconds(scaleDuration);
    }

    // Helper: Scale satu item dari 0 ke 1 dengan Ease-Out-Back bounce
    private IEnumerator ScaleItemIn(Button itemButton, float duration)
    {
        if (itemButton == null) yield break;

        RectTransform rectTransform = itemButton.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // T (Time) dibatasi antara 0 dan 1
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Rumus Ease-Out-Back untuk bounce yang mulus dan natural (bukan linear)
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            
            rectTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        // Pastikan scale berakhir tepat di angka 1
        rectTransform.localScale = Vector3.one;
    }

    // 6. Tunggu pemain memilih salah satu item
    private IEnumerator WaitForItemSelection()
    {
        // Setup semua item button listeners
        for (int i = 0; i < itemButtons.Length; i++)
        {
            int itemIndex = i; // Local copy untuk closure
            itemButtons[i].onClick.AddListener(() => OnItemSelected(itemIndex));
        }

        // Tunggu hingga ada yang dipilih
        yield return new WaitUntil(() => itemSelected);

        // Lepas semua listener
        foreach (var btn in itemButtons)
        {
            btn.onClick.RemoveAllListeners();
        }
    }

    // 7. Handle pilihan item
    private void OnItemSelected(int itemIndex)
    {
        if (itemSelected) return; // Cegah double click

        itemSelected = true;

        // Disable semua button agar tidak bisa klik lagi
        foreach (var btn in itemButtons)
        {
            btn.interactable = false;
        }

        // Check apakah benar
        if (itemIndex == correctItemIndex)
        {
            StartCoroutine(ShowOutcome(true, itemIndex));
        }
        else
        {
            StartCoroutine(ShowOutcome(false, itemIndex));
        }
    }

    // 8. Tampilkan hasil (monolog benar atau salah)
    private IEnumerator ShowOutcome(bool isCorrect, int selectedItemIndex)
    {
        string outcomeText;
        if (isCorrect)
        {
            outcomeText = "Ya! Ini dia yang aku cari. Ini bisa membantu menghentikan pendarahan!";
        }
        else
        {
            wrongAttemptCount++;
            outcomeText = GetWrongItemMonologue(selectedItemIndex);
            
            // Jika salah, enable kembali buttons untuk coba lagi (opsional)
            itemSelected = false;
            foreach (var btn in itemButtons)
            {
                btn.interactable = true;
            }
        }

        yield return StartCoroutine(ShowMonologue("Jiro", outcomeText));
        yield return StartCoroutine(WaitForAdvanceClick());
    }

    private string GetWrongItemMonologue(int itemIndex)
    {
        switch (itemIndex)
        {
            case 0: // headphone
                return "Headphone? Ini tidak bisa dipakai buat menghentikan pendarahan.";
            case 1: // pencilcase
                return "Tempat pensil juga bukan yang aku butuhkan sekarang.";
            case 2: // buku
                return "Buku ini tidak membantu untuk pertolongan pertama.";
            case 3: // switch
                return "Sekarang bukan waktunya bermain main.";
            case 5: // botol
                return "Botol ini jelas bukan item utama untuk menghentikan pendarahan.";
            default:
                return "Hmm, sepertinya bukan ini. Aku harus pilih item yang lebih tepat.";
        }
    }

    // 8b. Game Sequence untuk Testing (tanpa GameManager)
    private IEnumerator GameSequenceTest()
    {
        wrongAttemptCount = 0;

        // STEP 1: Tampilkan monolog awal Jiro
        yield return StartCoroutine(ShowMonologue(
            "Jiro",
            "Aku perlu mencari sesuatu di tasku yang bisa membantu menghentikan pendarahan... Mari kita lihat apa yang ada di sini."
        ));

        // STEP 2: Tunggu pemain klik tombol lanjut
        yield return StartCoroutine(WaitForAdvanceClickTest());

        // STEP 3: Transisi dari tas ke item (fade out tas, fade in items)
        yield return StartCoroutine(TransitionFromBagToItems());

        // STEP 4: Tunggu pemain memilih item
        yield return StartCoroutine(WaitForItemSelection());

        // STEP 5: Handle hasil pilihan (sudah dilakukan di OnItemSelected)
        // Tunggu monolog selesai ditampilkan
        yield return new WaitForSeconds(1.5f);

        // STEP 6: Game selesai (tanpa panggil gameManager)
        Debug.Log("✅ MinigameAmbilBarang Testing: SELESAI!");
        this.gameObject.SetActive(false);
    }

    // Tunggu advance click untuk testing
    private IEnumerator WaitForAdvanceClickTest()
    {
        // Tunggu klik mouse atau keyboard
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space));
    }

    // 9. Akhiri game
    private void EndGame()
    {
        this.gameObject.SetActive(false);

        if (gameManager != null)
        {
            gameManager.OnMiniGameComplete("Berhasil menemukan item yang tepat!");
        }
    }
}
