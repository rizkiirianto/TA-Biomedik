using UnityEngine;
using UnityEngine.EventSystems;

public class DropZoneTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Buat slot untuk referensi ke skrip game utama kita
    public TargetedWipe mainWipeScript;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Saat kursor masuk ke area ini, panggil fungsi di skrip utama
        if (mainWipeScript != null)
        {
            mainWipeScript.SetCursorInDropZone(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Saat kursor keluar dari area ini, panggil fungsi di skrip utama
        if (mainWipeScript != null)
        {
            mainWipeScript.SetCursorInDropZone(false);
        }
    }

    
    
    
}