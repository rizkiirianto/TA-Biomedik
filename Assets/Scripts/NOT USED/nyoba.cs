using UnityEngine;

public class SimpleHoverTest : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = Color.white; // Mulai dengan warna putih
        Debug.Log("Test Script Initialized on: " + gameObject.name);
    }

    // Fungsi ini akan mengubah warna jadi hijau saat mouse masuk
    void OnMouseEnter()
    {
        Debug.Log("SUCCESS! Mouse ENTERED the collider!");
        spriteRenderer.color = Color.green;
    }

    // Fungsi ini akan mengembalikan warna saat mouse keluar
    void OnMouseExit()
    {
        Debug.Log("SUCCESS! Mouse EXITED the collider!");
        spriteRenderer.color = Color.white;
    }
}