using UnityEngine;

public class MinigameBersihkanKotoran : MonoBehaviour
{
    [Header("Minigame Settings")]
    public GameObject[] daunObjects; // Array berisi semua objek daun yang perlu dibersihkan
    public string successMessage = "Bagus! Semua daun sudah dibersihkan!";
    
    private GameManager gameManager;
    private int totalDaun;
    private int daunTerbuang = 0;
    private bool gameCompleted = false;

    void Start()
    {
        // Jika daunObjects tidak diset manual, cari semua objek dengan tag "Daun"
        if (daunObjects.Length == 0)
        {
            GameObject[] foundDaun = GameObject.FindGameObjectsWithTag("Daun");
            daunObjects = foundDaun;
        }
        
        totalDaun = daunObjects.Length;
        Debug.Log($"Minigame Bersihkan Kotoran dimulai! Total daun: {totalDaun}");
    }

    public void StartMiniGame(GameManager manager)
    {
        gameManager = manager;
        Debug.Log("Minigame Bersihkan Kotoran dimulai!");
        
        // Reset counter jika minigame diulang
        daunTerbuang = 0;
        gameCompleted = false;
        
        // Pastikan semua daun aktif (jika ada yang ter-disable)
        foreach (GameObject daun in daunObjects)
        {
            if (daun != null)
            {
                daun.SetActive(true);
            }
        }
    }

    public void OnDaunRemoved()
    {
        if (gameCompleted) return;
        
        daunTerbuang++;
        Debug.Log($"Daun dibuang! Progress: {daunTerbuang}/{totalDaun}");
        
        // Cek apakah semua daun sudah dibuang
        if (daunTerbuang >= totalDaun)
        {
            CompleteMinigame();
        }
    }

    private void CompleteMinigame()
    {
        gameCompleted = true;
        Debug.Log("Minigame selesai! Semua daun sudah dibersihkan.");
        
        // Panggil GameManager untuk melanjutkan ke step berikutnya
        if (gameManager != null)
        {
            Debug.Log("Memanggil GameManager.OnMiniGameComplete");
            this.gameObject.SetActive(false);
            gameManager.OnMiniGameComplete(successMessage);
        }
        else
        {
            Debug.LogError("GameManager tidak ditemukan! Tidak bisa melanjutkan ke step berikutnya.");
        }
    }

    // Method untuk mengecek secara real-time berapa banyak daun yang tersisa
    void Update()
    {
        if (gameCompleted) return;
        
        // Hitung daun yang masih aktif
        int daunMasihAda = 0;
        foreach (GameObject daun in daunObjects)
        {
            if (daun != null && daun.activeInHierarchy)
            {
                daunMasihAda++;
            }
        }
        
        // Jika tidak ada daun yang tersisa, selesaikan minigame
        int daunTerhapus = totalDaun - daunMasihAda;
        if (daunTerhapus > daunTerbuang)
        {
            daunTerbuang = daunTerhapus;
            if (daunTerbuang >= totalDaun && !gameCompleted)
            {
                CompleteMinigame();
            }
        }
    }
}
