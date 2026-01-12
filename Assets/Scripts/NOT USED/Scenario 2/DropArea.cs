using UnityEngine;
using UnityEngine.EventSystems;

public class DropArea : MonoBehaviour, IDropHandler
{
    private MinigameBersihkanKotoran minigameManager;

    void Start()
    {
        // Cari MinigameBersihkanKotoran di scene
        minigameManager = FindFirstObjectByType<MinigameBersihkanKotoran>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("OnDrop");

        // Memeriksa apakah objek yang di-drop adalah objek yang benar
        if (eventData.pointerDrag != null)
        {
            // Pastikan objek yang di-drag memiliki tag "Daun"
            // Jangan lupa tambahkan tag "Daun" pada objek Daun_Image di inspector
            if (eventData.pointerDrag.CompareTag("Daun"))
            {
                // Tandai daun sebagai sudah dibuang (untuk mencegah double counting)
                DaunController daunController = eventData.pointerDrag.GetComponent<DaunController>();
                if (daunController != null)
                {
                    daunController.MarkAsRemoved();
                }

                // Menghilangkan objek daun
                Destroy(eventData.pointerDrag);
                Debug.Log("Daun dibuang ke tempat sampah!");

                // Beritahu minigame manager bahwa ada daun yang dibuang
                if (minigameManager != null)
                {
                    minigameManager.OnDaunRemoved();
                }
            }
        }
    }
}