using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaManager : MonoBehaviour
{
    private UnityWebRequest activeRequest;
    [Header("Ollama Settings")]
    [SerializeField] private string ollamaUrl = "http://localhost:11434/api/generate";
    [SerializeField] private string modelName;
    //[SerializeField] private string testingInput;
    
    [TextArea(3, 5)]
    [SerializeField] private string systemPrompt;

    // Struktur Data Request ke Ollama
    [Serializable]
    private class OllamaRequest
    {
        public string model;
        public string system; // Untuk mengatur "perilaku" AI
        public string prompt; // Pesan dari user/game
        public bool stream;
    }

    // Struktur Data Response dari Ollama
    [Serializable]
    private class OllamaResponse
    {
        public string response;
        public bool done;
    }

    /// <summary>
    /// Memanggil Ollama dan mengembalikan hasilnya melalui callback.
    /// </summary>
    public void GenerateResponse(string userPrompt, Action<string> onComplete)
    {
        StartCoroutine(SendRequestRoutine(userPrompt, onComplete));
    }

    private IEnumerator SendRequestRoutine(string userPrompt, Action<string> onComplete)
    {
        // 1. Siapkan payload JSON
        OllamaRequest requestData = new OllamaRequest
        {
            model = modelName,
            system = systemPrompt,
            prompt = userPrompt,
            stream = false // Set false agar balasan dikirim utuh, tidak per kata
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        // 2. Setup UnityWebRequest
        activeRequest = new UnityWebRequest(ollamaUrl, "POST");
        activeRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        activeRequest.downloadHandler = new DownloadHandlerBuffer();
        activeRequest.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("[OllamaManager] Mengirim prompt ke AI...");

        yield return activeRequest.SendWebRequest();

        if (activeRequest.result == UnityWebRequest.Result.ConnectionError || activeRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"[OllamaManager] Error: {activeRequest.error}");
            onComplete?.Invoke("Maaf, terjadi gangguan pada jaringan komunikasi darurat.");
        }
        else
        {
            string jsonResponse = activeRequest.downloadHandler.text;
            OllamaResponse responseData = JsonUtility.FromJson<OllamaResponse>(jsonResponse);
            Debug.Log($"[OllamaManager] Balasan diterima: {responseData.response}");
            onComplete?.Invoke(responseData.response.Trim());
        }

        // Bersihkan memori secara manual setelah selesai
        activeRequest.Dispose();
        activeRequest = null;
    }

    // buat testing
    /*
    private void Start()
    {
        // Hapus atau comment kode ini setelah pengujian berhasil
        GenerateResponse(testingInput, (jawaban) => 
        {
            Debug.LogWarning("HASIL TEST OLLAMA: " + jawaban);
        });
    }
    */

    private void OnDestroy()
    {
        // Jika Unity di-stop saat request masih jalan, batalkan paksa!
        if (activeRequest != null && !activeRequest.isDone)
        {
            activeRequest.Abort();
            activeRequest.Dispose();
            Debug.Log("[OllamaManager] Request dibatalkan secara aman karena game berhenti.");
        }
    }
}