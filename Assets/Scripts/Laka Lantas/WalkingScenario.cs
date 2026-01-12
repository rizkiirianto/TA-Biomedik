using UnityEngine;

// 1. Tambahkan ", IMiniGame" agar script ini dikenali oleh GameManager universal
public class WalkingScenario : MonoBehaviour, IMiniGame
{
    [Header("References")]
    // Hapus [SerializeField] karena GameManager akan diberikan otomatis via kode
    private GameManager gameManager; 
    
    [SerializeField] public Collider2D targetZone; 
    [SerializeField] public Collider2D playerCollider; 

    [Header("Settings")]
    [SerializeField] private float requiredStayTime = 3f;

    [Header("Debug Info")]
    public float stayTimer = 0f;
    private bool completed = false;
    private bool isGameActive = false; // Tambahan: Supaya timer tidak jalan sebelum game resmi dimulai

    // 2. Implementasi fungsi wajib dari Interface IMiniGame
    public void BeginGame(GameManager gm)
    {
        // GameManager memberikan dirinya sendiri ke script ini
        this.gameManager = gm;
        
        // Mulai logika game
        isGameActive = true; 
        Debug.Log("Minigame Walking Dimulai via Interface!");
        
        // (Opsional) Validasi targetZone/playerCollider bisa pindah ke sini atau tetap di Start
    }

    void Start()
    {
        // Validasi agar tidak error
        if (targetZone == null || playerCollider == null)
        {
            Debug.LogError("Target Zone (Square) atau Player belum dimasukkan ke inspector WalkingScenario!");
            enabled = false;
        }
    }

    void Update()
    {
        // Cek isGameActive agar logika tidak jalan sebelum GameManager siap
        if (completed || !isGameActive) return;

        if (targetZone.IsTouching(playerCollider))
        {
            HandleStay();
        }
        else
        {
            HandleExit();
        }
    }

    private void HandleStay()
    {
        stayTimer += Time.deltaTime;

        if (stayTimer >= requiredStayTime)
        {
            CompleteScenario();
        }
    }

    private void HandleExit()
    {
        if (stayTimer > 0)
        {
            stayTimer = 0f;
            Debug.Log("Keluar zona, timer reset");
        }
    }

    private void CompleteScenario()
    {
        completed = true;
        isGameActive = false;
        Debug.Log("Scenario Selesai!");
        
        // Objek dimatikan setelah lapor ke GameManager
        this.gameObject.SetActive(false);
        
        if (gameManager != null)
        {
            // Panggil fungsi universal di GameManager
            gameManager.OnMiniGameComplete("Player reach target");
        }
    }
}