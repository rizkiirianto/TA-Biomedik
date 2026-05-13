using System.Collections.Generic;
using UnityEngine;

public class TreatmentManager : MonoBehaviour
{
    [Header("GameObjects")]
    public GameObject patientArmIncomplete;
    public GameObject patientArmComplete;
    public GameObject patientArmBasahAtas;
    public GameObject patientArmBasahBawah;
    
    [Header("Controllers")]
    public WaterPourController waterController; 

    [Header("Settings")]
    public float timeToClean = 3f;

    [Header("Target Spots (Assign in Inspector)")]
    public Collider2D[] topSpots;
    public Collider2D[] bottomSpots;

    private Dictionary<Collider2D, float> targetTimers = new Dictionary<Collider2D, float>();
    
    private int topCompleted = 0;
    private int bottomCompleted = 0;
    public bool isComplete = false;

    void Start()
    {
        // Masukkan semua spot ke dalam dictionary targetTimers
        if (topSpots != null)
        {
            foreach (Collider2D col in topSpots)
            {
                if (col != null && !targetTimers.ContainsKey(col)) targetTimers.Add(col, 0f);
            }
        }
        
        if (bottomSpots != null)
        {
            foreach (Collider2D col in bottomSpots)
            {
                if (col != null && !targetTimers.ContainsKey(col)) targetTimers.Add(col, 0f);
            }
        }

        // Jika array tidak diisi di Inspector, otomatis ambil semua collider anak sebagai fallback
        if (targetTimers.Count == 0)
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col in colliders)
            {
                if (!targetTimers.ContainsKey(col)) targetTimers.Add(col, 0f);
            }
        }

        if (patientArmComplete != null) patientArmComplete.SetActive(false);
        if (patientArmBasahAtas != null) patientArmBasahAtas.SetActive(false);
        if (patientArmBasahBawah != null) patientArmBasahBawah.SetActive(false);
    }

    public void CheckHit(Collider2D hitCollider)
    {
        if (targetTimers.ContainsKey(hitCollider))
        {
            targetTimers[hitCollider] += Time.deltaTime;

            if (targetTimers[hitCollider] >= timeToClean)
            {
                targetTimers.Remove(hitCollider);
                hitCollider.gameObject.SetActive(false);

                // Cek bagian mana yang baru saja selesai
                if (IsTopSpot(hitCollider))
                {
                    topCompleted++;
                    if (topSpots != null && topCompleted >= topSpots.Length)
                    {
                        if (patientArmBasahAtas != null) patientArmBasahAtas.SetActive(true);
                        if (patientArmIncomplete != null) patientArmIncomplete.SetActive(false);
                    }
                }
                else if (IsBottomSpot(hitCollider))
                {
                    bottomCompleted++;
                    if (bottomSpots != null && bottomCompleted >= bottomSpots.Length)
                    {
                        if (patientArmBasahBawah != null) patientArmBasahBawah.SetActive(true);
                        if (patientArmIncomplete != null) patientArmIncomplete.SetActive(false);
                    }
                }

                if (targetTimers.Count == 0)
                {
                    CompleteTreatment();
                }
            }
        }
    }

    private bool IsTopSpot(Collider2D col)
    {
        if (topSpots == null) return false;
        foreach (var t in topSpots)
        {
            if (t == col) return true;
        }
        return false;
    }

    private bool IsBottomSpot(Collider2D col)
    {
        if (bottomSpots == null) return false;
        foreach (var t in bottomSpots)
        {
            if (t == col) return true;
        }
        return false;
    }

    void CompleteTreatment()
    {
        if (patientArmIncomplete != null) patientArmIncomplete.SetActive(false);
        if (patientArmComplete != null) patientArmComplete.SetActive(true);
        
        // Sembunyikan gambar parsial karena sudah diganti dengan lengan komplit
        if (patientArmBasahAtas != null) patientArmBasahAtas.SetActive(false);
        if (patientArmBasahBawah != null) patientArmBasahBawah.SetActive(false);
        
        // Matikan kucuran air
        if (waterController != null)
        {
            waterController.DisablePouring();
        }
        
        isComplete = true;
        Debug.Log("Semua spot telah dibersihkan! Minigame selesai.");
    }
}