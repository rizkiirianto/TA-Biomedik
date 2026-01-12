using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;

public class WoundPackingMiniGame : MonoBehaviour, IMiniGame
{
    [Header("Game Settings")]
    [Tooltip("Berapa kali harus klik untuk menyumbat luka sampai penuh")]
    public int targetPacks = 5; 
    [Tooltip("Berapa kali harus klik untuk membalut sampai selesai")]
    public int targetWraps = 3; 

    [Header("UI Fase 1: Sumbat Luka (Packing)")]
    public GameObject panelFasePacking;    // Panel utama fase 1
    public Button tombolKassa;             // Tombol gambar kassa (yang diklik pemain)
    public Transform tempatSpawnKassa;     // Objek kosong di tengah luka untuk wadah gumpalan
    public GameObject prefabGumpalanKassa; // Gambar gumpalan kassa kecil (sprite)

    [Header("UI Fase 2: Balut Luka (Bandaging)")]
    public GameObject panelFaseBandaging;  // Panel utama fase 2
    public GameObject gambarPaha;
    public Button tombolPerban;            // Tombol gambar perban (yang diklik pemain)
    public GameObject[] tahapanPerban;     // Array gambar perban (tahap 1, 2, 3)

    [Header("Audio (Opsional)")]
    public AudioSource audioSfx;           // Sumber suara
    public AudioClip suaraKain;            // Suara kassa/perban

    // Private Variables
    private GameManager gameManager;
    private int clickCount = 0;
    private bool isPhaseTwo = false;

    // 1. Fungsi Wajib IMiniGame
    public void BeginGame(GameManager gm)
    {
        this.gameManager = gm;
        ResetGame();
        Debug.Log("Minigame Sumbat & Balut Dimulai!");
    }

    private void ResetGame()
    {
        clickCount = 0;
        isPhaseTwo = false;

        // Reset UI
        panelFasePacking.SetActive(true);
        panelFaseBandaging.SetActive(false);

        // Hapus gumpalan kassa sisa game sebelumnya
        foreach (Transform child in tempatSpawnKassa)
        {
            Destroy(child.gameObject);
        }

        // Sembunyikan semua tahapan perban
        foreach (var img in tahapanPerban)
        {
            img.SetActive(false);
        }
    }

    // 2. Fungsi untuk Tombol Kassa (Fase 1)
    public void OnGauzeDropped()
    {
        if (isPhaseTwo) return;

        // Mainkan suara jika ada
        if (audioSfx && suaraKain) audioSfx.PlayOneShot(suaraKain);

        // Munculkan visual gumpalan kassa di posisi acak sekitar luka
        GameObject gumpalan = Instantiate(prefabGumpalanKassa, tempatSpawnKassa);
        
        // Acak sedikit posisi dan rotasi biar terlihat alami menumpuk
        RectTransform rt = gumpalan.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(Random.Range(-30, 30), Random.Range(-30, 30));
        rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

        clickCount++;

        // Cek apakah sudah cukup penuh?
        if (clickCount >= targetPacks)
        {
            Invoke("MulaiFaseDua", 0.5f); // Jeda sedikit sebelum ganti fase
        }
    }

    private void MulaiFaseDua()
    {
        isPhaseTwo = true;
        clickCount = 0; // Reset hitungan untuk fase 2
        
        panelFasePacking.SetActive(false);
        panelFaseBandaging.SetActive(true);
        
        Debug.Log("Masuk Fase 2: Pembalutan");
    }

    // 3. Fungsi untuk Tombol Perban (Fase 2)
    public void OnKlikPerban()
    {
        if (!isPhaseTwo) return;

        // Safety check: Ensure we don't go out of bounds of the array
        if (clickCount < tahapanPerban.Length)
        {
            // Play Audio
            if (audioSfx && suaraKain) audioSfx.PlayOneShot(suaraKain);

            // --- STEP 1: HIDE THE PREVIOUS IMAGE ---
            if (clickCount == 0)
            {
                // If this is the very first click, hide the base leg (Gambar Paha)
                if (gambarPaha != null) gambarPaha.SetActive(false);
            }
            else
            {
                // If this is the 2nd or 3rd click, hide the PREVIOUS bandage
                // (clickCount - 1 is safe here because clickCount > 0)
                tahapanPerban[clickCount - 1].SetActive(false);
            }

            // --- STEP 2: SHOW THE CURRENT IMAGE ---
            tahapanPerban[clickCount].SetActive(true);

            // --- STEP 3: INCREMENT & CHECK WIN ---
            clickCount++;

            // Check if we reached the target
            if (clickCount >= targetWraps)
            {
                Invoke("SelesaiGame", 0.5f);
            }
        }
    }

    private void SelesaiGame()
    {
        this.gameObject.SetActive(false);
        if (gameManager != null)
        {
            gameManager.OnMiniGameComplete("Luka berhasil disumbat dan dibalut!");
        }
    }
}