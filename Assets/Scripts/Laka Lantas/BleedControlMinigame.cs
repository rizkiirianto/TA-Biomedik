using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Diperlukan untuk mendeteksi event klik/tahan
using TMPro;

public class NosePressMiniGame : MonoBehaviour, IMiniGame
{
    [Header("Referensi UI")]
    public Slider progressBar;
    //public TextMeshProUGUI feedbackText; // Kita bisa pakai feedbackText dari GameManager

    [Header("Pengaturan Mini-game")]
    [SerializeField] private float timeToHold = 10f; // Berapa detik harus menahan

    public Image[] gambarHidungMimisan;
    public Image[] gambarTanganPenolong;
    private Image gambarHidungAktif;
    private Image gambarTanganAktif;

    public AudioSource jawabanBenar;
    public AudioSource jawabanSalah;
    // --- Variabel Internal ---
    private float currentHoldTime = 0f;
    private bool isHoldingCorrectArea = false;
    
    // Flag untuk mencegah audio diputar berulang
    private bool audioPlayed50 = false;
    private bool audioPlayed70 = false;
    private bool audioPlayed90 = false;
    
    // Referensi ke GameManager untuk memberitahu jika mini-game selesai
    private GameManager gameManager;


    // Fungsi ini akan dipanggil oleh GameManager untuk memulai mini-game
    public void BeginGame(GameManager gm)
    {
        this.gameManager = gm;

        // Reset state mini-game
        currentHoldTime = 0;
        progressBar.value = 0;
        isHoldingCorrectArea = false;
        
        // Reset flag audio
        audioPlayed50 = false;
        audioPlayed70 = false;
        audioPlayed90 = false;
        
        this.gameObject.SetActive(true); 
        SetHidungAktif(0);
        SetTanganAktif(0);
        
        Debug.Log("Minigame NosePress Dimulai via Interface!");
    }

    void Update()
    {
        if (gameManager == null) return;
        // Jika pemain sedang menahan di area yang benar
        if (isHoldingCorrectArea)
        {
            currentHoldTime += Time.deltaTime;
            progressBar.value = currentHoldTime / timeToHold;

            if (currentHoldTime >= 0.3 * timeToHold && !audioPlayed50)
            {
                SetHidungAktif(1);
                SetTanganAktif(1);
                jawabanBenar.Play();
                audioPlayed50 = true;
            }
            if (currentHoldTime >= 0.5 * timeToHold && !audioPlayed70)
            {
                SetHidungAktif(2);
                SetTanganAktif(2);
                jawabanBenar.Play();
                audioPlayed70 = true;
            }
            if (currentHoldTime >= 0.8 * timeToHold && !audioPlayed90)
            {
                SetHidungAktif(3);
                SetTanganAktif(3);
                jawabanBenar.Play();
                audioPlayed90 = true;
            }
            // Cek jika sudah berhasil
            if (currentHoldTime >= timeToHold)
            {
                OnSuccess();
                jawabanBenar.Play();
            }
        }
    }

    private void SetHidungAktif(int index)
    {
        // Pastikan indeks valid
        if (gambarHidungMimisan == null || index < 0 || index >= gambarHidungMimisan.Length)
        {
            return;
        }

        // Matikan semua kursor terlebih dahulu
        for (int i = 0; i < gambarHidungMimisan.Length; i++)
        {
            if (gambarHidungMimisan[i] != null)
            {
                gambarHidungMimisan[i].gameObject.SetActive(false);
            }
        }

        // Aktifkan kursor yang dipilih dan simpan sebagai kursor aktif
        gambarHidungAktif = gambarHidungMimisan[index];
        if (gambarHidungAktif != null)
        {
            gambarHidungAktif.gameObject.SetActive(true);
        }
    }

    private void SetTanganAktif(int index)
    {
        // Pastikan indeks valid
        if (gambarTanganPenolong == null || index < 0 || index >= gambarTanganPenolong.Length)
        {
            return;
        }

        // Matikan semua kursor terlebih dahulu
        for (int i = 0; i < gambarTanganPenolong.Length; i++)
        {
            if (gambarTanganPenolong[i] != null)
            {
                gambarTanganPenolong[i].gameObject.SetActive(false);
            }
        }

        // Aktifkan kursor yang dipilih dan simpan sebagai kursor aktif
        gambarTanganAktif = gambarTanganPenolong[index];
        if (gambarTanganAktif != null)
        {
            gambarTanganAktif.gameObject.SetActive(true);
        }
    }
    
    // --- Fungsi ini akan di-trigger oleh Event Trigger di UI ---

    public void OnCorrectZonePointerDown()
    {
        // Dipanggil saat mouse/jari mulai menekan di zona yang benar
        isHoldingCorrectArea = true;
    }

    public void OnPointerUp()
    {
        // Dipanggil saat mouse/jari dilepas (di mana saja)
        if (isHoldingCorrectArea && currentHoldTime < timeToHold)
        {
            // Jika dilepas terlalu cepat
            OnFailure("Coba lagi, kamu harus menahan tekanan di pangkal hidung ya!");
            jawabanSalah.Play();
        }
        isHoldingCorrectArea = false;
    }
    
    public void OnIncorrectZonePointerDown()
    {
        // Dipanggil saat menekan zona yang salah
        OnFailure("Bukan di situ! Coba tekan di bagian pangkal hidung.");
    }

    // --- Logika Sukses dan Gagal ---

    private void OnSuccess()
    {
        isHoldingCorrectArea = false; // Hentikan proses di Update()
        Debug.Log("Mini-game Berhasil!");
        
        // Non-aktifkan mini-game dan beritahu GameManager untuk lanjut
        this.gameObject.SetActive(false);
        gameManager.OnMiniGameComplete("Hebat! Tekanan di pangkal hidung bisa menghentikan pendarahan.");
    }

    private void OnFailure(string feedbackMessage)
    {
        SetHidungAktif(0);
        SetTanganAktif(0);
        isHoldingCorrectArea = false;
        Debug.Log("Mini-game Gagal!");

        // Tampilkan feedback kesalahan
        //StartCoroutine(gameManager.ShowFeedbackCoroutine(feedbackMessage, false));
        
        // Reset progress
        currentHoldTime = 0;
        progressBar.value = 0;
        
        // Reset flag audio ketika gagal
        audioPlayed50 = false;
        audioPlayed70 = false;
        audioPlayed90 = false;
    }
}
