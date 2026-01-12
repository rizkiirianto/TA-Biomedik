using UnityEngine;

public class DaunController : MonoBehaviour
{
    [Header("Daun Settings")]
    public bool isDaun = true;
    
    private MinigameBersihkanKotoran minigameManager;
    private bool isRemoved = false;
    
    void Start()
    {
        // Pastikan objek memiliki tag "Daun"
        if (isDaun && !CompareTag("Daun"))
        {
            tag = "Daun";
        }
        
        // Cari minigame manager
        minigameManager = FindFirstObjectByType<MinigameBersihkanKotoran>();
    }
    
    // Method ini akan dipanggil ketika daun dihancurkan
    void OnDestroy()
    {
        if (!isRemoved && minigameManager != null)
        {
            isRemoved = true;
            minigameManager.OnDaunRemoved();
        }
    }
    
    // Method untuk menandai bahwa daun telah dibuang
    public void MarkAsRemoved()
    {
        isRemoved = true;
    }
}