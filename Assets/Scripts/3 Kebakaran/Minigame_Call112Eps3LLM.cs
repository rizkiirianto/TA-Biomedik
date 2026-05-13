using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Minigame_Call112Eps3LLM : MonoBehaviour, IMiniGame
{
    [Header("Chat UI")]
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TMP_InputField playerInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject jiroNelpon;
    [SerializeField] private Button nextButton;

    [Header("History Panel")]
    [SerializeField] private GameObject historyPanel;
    [SerializeField] private ScrollRect historyScrollRect;
    [SerializeField] private TextMeshProUGUI historyText;
    [SerializeField] private Button historyPanelButton;

    [Header("Patient Panel")]
    [SerializeField] private GameObject patientPanel;
    [SerializeField] private Button patientPanelButton;

    [Header("Conversation")]
    
    [SerializeField, TextArea(2, 4)] private string openingOperatorLine = "112, layanan darurat. Sebutkan lokasi dan apa yang terjadi.";
    [SerializeField, TextArea(2, 4)] private string completionFeedback = "Panggilan 112 selesai. Bantuan sedang menuju lokasi. Segera lakukan pertolongan pertama pada korban kritis.";
    [SerializeField, Min(0.005f)] private float typingSpeed = 0.03f;
    [SerializeField, Min(0f)] private float operatorReplyDelaySeconds = 5f;
    [SerializeField, Min(0f)] private float finishDelaySeconds = 0.9f;

    [Header("AI Manager")]
    [SerializeField] private OllamaManager ollamaManager;

    private enum QuestionState
    {
        EmergencyLocation,
        Safety,
        VictimCount,
        Victim1,
        Victim2,
        Victim3,
        Closing
    }

    private enum VictimType
    {
        Budi,
        Siti,
        Tono
    }

    private QuestionState currentState = QuestionState.EmergencyLocation;
    private readonly HashSet<VictimType> identifiedVictims = new HashSet<VictimType>();

    // remember partial info across multiple user messages
    private bool recordedLocationInfo = false;
    private bool recordedHasLedakan = false;
    private bool recordedHasKebakaran = false;

    private GameManager gameManager;
    private bool isInitialized;
    private bool isCompleting;
    private Coroutine finishRoutine;
    private Coroutine typingRoutine;
    private string queuedOperatorReply = string.Empty;
    private readonly StringBuilder historyBuilder = new StringBuilder();
    private bool operatorSkipRequested;
    
    private void Start()
    {
        InitializeMinigame();
    }

    public void BeginGame(GameManager gm)
    {
        gameManager = gm;
        InitializeMinigame();
    }

    private void InitializeMinigame()
    {
        if (isInitialized) return;

        isInitialized = true;
        isCompleting = false;

        if (jiroNelpon != null) jiroNelpon.SetActive(true);

        if (playerInputField != null)
        {
            playerInputField.text = string.Empty;
            playerInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            playerInputField.characterLimit = 0;
        }

        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendButtonPressed);
            sendButton.onClick.AddListener(OnSendButtonPressed);
        }

        if (historyPanelButton != null)
        {
            historyPanelButton.onClick.RemoveListener(OnHistoryButtonPressed);
            historyPanelButton.onClick.AddListener(OnHistoryButtonPressed);
        }

        if (patientPanelButton!= null) 
        {
            patientPanelButton.onClick.RemoveListener(OnPatientButtonPressed);
            patientPanelButton.onClick.AddListener(OnPatientButtonPressed);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextButtonPressed);
            nextButton.onClick.AddListener(OnNextButtonPressed);
        }

        SetNextButtonActive(false);
        ResetConversation();
    }

    private void OnDestroy()
    {
        // Simpan log sisa jika script dihancurkan (misal pindah scene atau stop play)
        SaveHistoryLog();
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        if (finishRoutine != null) StopCoroutine(finishRoutine);
        if (sendButton != null) sendButton.onClick.RemoveListener(OnSendButtonPressed);
        if (historyPanelButton != null) historyPanelButton.onClick.RemoveListener(OnHistoryButtonPressed);
        if (patientPanelButton != null) patientPanelButton.onClick.RemoveListener(OnPatientButtonPressed);
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextButtonPressed);
    }

    private void Update()
    {
        if (!isInitialized || isCompleting || playerInputField == null) return;
        if (!playerInputField.isFocused) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendCurrentMessage();
        }
    }

    private void OnSendButtonPressed()
    {
        SendCurrentMessage();
    }

    private void OnHistoryButtonPressed()
    {
        bool isVisible = historyPanel != null && historyPanel.activeSelf;
        SetHistoryPanelVisible(!isVisible);
    }

    private void OnPatientButtonPressed()
    {
        bool isVisible = patientPanel != null && patientPanel.activeSelf;
        SetPatientPanelVisible(!isVisible);
    }

    private void OnNextButtonPressed()
    {
        operatorSkipRequested = true;
    }

    private void ResetConversation()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        queuedOperatorReply = string.Empty;
        
        historyBuilder.Clear();
        identifiedVictims.Clear();
        recordedLocationInfo = false;
        recordedHasLedakan = false;
        recordedHasKebakaran = false;
        currentState = QuestionState.EmergencyLocation;
        SetHistoryText(string.Empty);

        SetTranscriptLine(string.Empty);
        AppendOperatorLine(openingOperatorLine);
        UpdatePrompt("Ketik jawabanmu lalu tekan Enter atau tombol kirim.");
        SetNextButtonActive(false);
        FocusInputField();
    }

    private void SendCurrentMessage()
    {
        if (isCompleting || playerInputField == null || isTypingMessage) return;

        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message))
        {
            FocusInputField();
            return;
        }

        playerInputField.text = string.Empty;

        if (typingRoutine != null) StopCoroutine(typingRoutine);

        // 1. Dapatkan "Tujuan Balasan" dari Rule-Based / State Machine (Misal: "Bisa tolong sebutkan lokasi Anda?")
        string intendedOperatorReply = HandleMessageForState(message);

        // 2. Mulai proses hybrid dengan AI
        typingRoutine = StartCoroutine(ProcessMessageHybrid(message, intendedOperatorReply));
    }

    private bool isTypingMessage;

    private IEnumerator ProcessMessageHybrid(string playerMessage, string intendedReply)
    {
        isTypingMessage = true;
        SetInputInteractable(false);
        SetJiroVisible(true);
        SetNextButtonActive(true);

        // 1. Ketik pesan Jiro
        yield return StartCoroutine(TypeTranscriptLine("Jiro", playerMessage));
        AppendHistoryLine("Jiro", playerMessage);

        operatorSkipRequested = false;
        yield return StartCoroutine(WaitForOperatorDelayOrClick());

        SetJiroVisible(false);
        SetNextButtonActive(false);

        // 2. Tampilkan indikator mengetik
        SetTranscriptLine("Operator 112: sedang mengetik...");

        // 3. Bangun Prompt untuk AI (Hindari penggunaan struktur label seperti "Pesan:")
        string llmPrompt = $"Pelapor baru saja berkata: \"{playerMessage}\"\nSebagai operator, sampaikan pesan ini kepadanya dengan natural: \"{intendedReply}\"\n\nTuliskan LANGSUNG kalimat balasanmu. Jangan gunakan tanda kutip dan jangan sebutkan identitasmu di awal kalimat, JANGAN menyebutkan nama korban atau kondisi medis yang TIDAK ada dalam Instruksi Operator, Jika pesan berisi tindakan medis, gunakan kalimat perintah yang sopan (contoh: 'Tolong pindahkan...', 'Berikan tekanan...'), JANGAN gunakan kata ganti 'kami' seolah-olah kamu yang melakukannya di lokasi.";
        
        bool isLLMDone = false;
        string finalReply = intendedReply; // Gunakan teks rule-based sebagai fallback jika AI gagal

        if (ollamaManager != null)
        {
            ollamaManager.GenerateResponse(llmPrompt, (response) =>
            {
                if (!string.IsNullOrEmpty(response) && !response.Contains("gangguan"))
                {
                    // --- SAFETY NET: Bersihkan teks bocor dan halusinasi ---
                    string cleanResponse = response
                        .Replace("Pesan Pelapor:", "")
                        .Replace("Pesan pelapor:", "")
                        .Replace("Instruksi Operator:", "")
                        .Replace("Instruksi operator:", "")
                        .Replace("Tujuan Balasan Operator:", "")
                        .Replace("Operator 112:", "")
                        .Replace("Operator:", "")
                        .Replace("\"", "") // Hapus tanda kutip jika AI menambahkannya
                        .Replace("*", "") // Hapus format markdown asterisk jika ada
                        .Trim(); 
                    
                    // Pastikan teks tidak kosong setelah dibersihkan
                    if (!string.IsNullOrWhiteSpace(cleanResponse))
                    {
                        finalReply = cleanResponse;
                        Debug.Log("[LLM] Jawaban berhasil di-generate dari Ollama.");
                    }
                    else
                    {
                        Debug.Log("[LLM Fallback] Jawaban Ollama kosong setelah dibersihkan, menggunakan intendedReply.");
                    }
                }
                else
                {
                    Debug.Log("[LLM Fallback] Jawaban dari Ollama kosong atau error (gangguan), menggunakan intendedReply.");
                }
                isLLMDone = true;
            });

            while (!isLLMDone) yield return null;
        }
        else
        {
            Debug.Log("[LLM Fallback] OllamaManager tidak terpasang, menggunakan intendedReply.");
            // Jika lupa masukin OllamaManager ke Inspector
            isLLMDone = true;
        }

        // 4. Ketik hasil akhir dari AI
        yield return StartCoroutine(TypeTranscriptLine("Operator 112", finalReply));
        AppendHistoryLine("Operator 112", finalReply);
        

        isTypingMessage = false;
        SetInputInteractable(true);
        FocusInputField();

        // 5. Cek kelanjutan game
        if (IsConversationComplete())
        {
            StartCompletionSequence();
        }
        else
        {
            UpdatePrompt(GetNextPrompt());
        }

        typingRoutine = null;
    }

    private IEnumerator WaitForOperatorDelayOrClick()
    {
        float remaining = Mathf.Max(0f, operatorReplyDelaySeconds);
        while (remaining > 0f)
        {
            if (operatorSkipRequested) break;
            remaining -= Time.deltaTime;
            yield return null;
        }
    }

    private void AppendOperatorLine(string message)
    {
        AppendTranscriptLine("Operator 112", message);
    }

    private IEnumerator TypeTranscriptLine(string speaker, string message)
    {
        string linePrefix = speaker + ": ";
        SetTranscriptLine(linePrefix);

        float safeTypingSpeed = Mathf.Max(0.005f, typingSpeed);
        StringBuilder lineBuilder = new StringBuilder(linePrefix.Length + message.Length);
        lineBuilder.Append(linePrefix);
        for (int i = 0; i < message.Length; i++)
        {
            lineBuilder.Append(message[i]);
            SetTranscriptLine(lineBuilder.ToString());
            yield return new WaitForSeconds(safeTypingSpeed);
        }
    }

    private void AppendTranscriptLine(string speaker, string message)
    {
        SetTranscriptLine(speaker + ": " + message.Trim());
    }

    private void AppendHistoryLine(string speaker, string message)
    {
        if (historyBuilder.Length > 0) historyBuilder.AppendLine();

        historyBuilder.Append(speaker);
        historyBuilder.Append(": ");
        historyBuilder.Append(message.Trim());

        SetHistoryText(historyBuilder.ToString());
        if (historyPanel != null && historyPanel.activeSelf)
        {
            ScrollHistoryToBottom();
        }
    }

    private void SetHistoryText(string value)
    {
        if (historyText != null) historyText.text = value;
    }

    public void SetHistoryPanelVisible(bool visible)
    {
        if (historyPanel == null) return;

        historyPanel.SetActive(visible);
        SetInputActive(!visible);
        SetInputInteractable(!visible && !isCompleting && !isTypingMessage);
        if (visible) ScrollHistoryToBottom();
        else FocusInputField();
    }

    public void SetPatientPanelVisible(bool visible)
    {
        if (patientPanel == null) return;

        patientPanel.SetActive(visible);
        
        if (patientPanelButton != null)
        {
            RectTransform btnRect = patientPanelButton.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                Vector2 pos = btnRect.anchoredPosition;
                pos.x = visible ? 45f : 235f;
                btnRect.anchoredPosition = pos;
            }
        }
    }

    private void ScrollHistoryToBottom()
    {
        if (historyScrollRect == null) return;
        StartCoroutine(ScrollHistoryToBottomNextFrame());
    }

    private IEnumerator ScrollHistoryToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (historyScrollRect != null) historyScrollRect.verticalNormalizedPosition = 0f;
    }

    private void SetTranscriptLine(string value)
    {
        if (text != null) text.text = value;
    }

    private string HandleMessageForState(string rawMessage)
    {
        string message = Normalize(rawMessage);

        switch (currentState)
        {
            case QuestionState.EmergencyLocation:
                return HandleEmergencyLocation(message);
            case QuestionState.Safety:
                return HandleSafety(message);
            case QuestionState.VictimCount:
                return HandleVictimCount(message);
            case QuestionState.Victim1:
            case QuestionState.Victim2:
            case QuestionState.Victim3:
                return HandleVictimDetails(message);
            case QuestionState.Closing:
                return BuildClosingResponse();
            default:
                return "Mohon jelaskan kondisi Anda saat ini.";
        }
    }

    private string HandleEmergencyLocation(string message)
    {
        // update stored info so user can answer location and emergency across multiple messages
        if (HasLocationInfo(message)) recordedLocationInfo = true;
        if (HasLedakanInfo(message)) recordedHasLedakan = true;
        if (HasKebakaranInfo(message)) recordedHasKebakaran = true;

        bool hasLocation = recordedLocationInfo || HasLocationInfo(message);
        bool hasLedakan = recordedHasLedakan || HasLedakanInfo(message);
        bool hasKebakaran = recordedHasKebakaran || HasKebakaranInfo(message);
        bool hasEmergency = hasLedakan && hasKebakaran;

        if (!hasLocation && !hasEmergency)
        {
            return "Bisa tolong sebutkan lokasi Anda dan kondisi daruratnya.";
        }

        if (!hasLocation)
        {
            return "Bisakah tolong beri tahu lokasi Anda di mana?";
        }

        if (!hasEmergency)
        {
            if (!hasLedakan && hasKebakaran)
            {
                return "Apakah Anda tahu asal api itu dari mana? Apakah ada suara ledakan yang Anda dengar?";
            }

            if (!hasKebakaran && hasLedakan)
            {
                return "Apakah Anda bisa melihat api atau asap dari ledakan itu? Bagaimana dampaknya?";
            }

            return "Apa kondisi darurat yang terjadi di sana?";
        }

        currentState = QuestionState.Safety;
        return "Baik, lokasi dan kondisi darurat dicatat. Apakah posisi Anda aman?";
    }

    private string HandleSafety(string message)
    {
        if (!HasSafetyInfo(message))
        {
            return "Apakah posisi Anda aman saat ini?";
        }

        currentState = QuestionState.VictimCount;
        return "Baik. Apakah ada korban lain di lokasi?";
    }

    private string HandleVictimCount(string message)
    {
        if (!HasVictimCountInfo(message))
        {
            return "Pastikan jumlah korban di lokasi benar sesuai yang anda lihat.";
        }

        currentState = QuestionState.Victim1;
        return "Baik, ada tiga korban. Bisakah anda jelaskan kondisi korban pertama?";
    }

    private string HandleVictimDetails(string message)
    {
        List<VictimType> detectedInThisTurn = DetectVictims(message);
        List<VictimType> newlyIdentified = new List<VictimType>();

        foreach (var victim in detectedInThisTurn)
        {
            if (!identifiedVictims.Contains(victim))
            {
                newlyIdentified.Add(victim);
                identifiedVictims.Add(victim);
            }
        }

        // Jika pemain ngomong tapi tidak ada keyword korban yang terdeteksi
        if (newlyIdentified.Count == 0)
        {
            return "Bisa tolong deskripsikan lebih detail luka atau kondisi korban tersebut? Apakah ada perdarahan atau luka bakar?";
        }

        // Berikan saran medis HANYA untuk korban yang baru saja disebutkan
        string medicalAdvice = BuildVictimAdvice(newlyIdentified, 2);
        
        UpdateVictimStateAfterProgress();

        if (currentState == QuestionState.Closing)
        {
            return medicalAdvice + " " + BuildClosingResponse();
        }

        // Gabungkan saran medis dengan pertanyaan untuk korban selanjutnya
        return medicalAdvice + " " + AskForNextVictim();
    }

    private void UpdateVictimStateAfterProgress()
    {
        int count = identifiedVictims.Count;
        if (count >= 3)
        {
            currentState = QuestionState.Closing;
            return;
        }

        if (currentState == QuestionState.Victim1)
        {
            currentState = count >= 2 ? QuestionState.Victim3 : QuestionState.Victim2;
        }
        else if (currentState == QuestionState.Victim2)
        {
            currentState = QuestionState.Victim3;
        }
        else if (currentState == QuestionState.Victim3 && count < 3)
        {
            currentState = QuestionState.Victim2;
        }
    }

    private string AskForNextVictim()
    {
        int count = identifiedVictims.Count;

        // Jika belum ada korban yang disebutkan sama sekali
        if (count == 0)
        {
            return "Bisa tolong jelaskan bagaimana kondisi korban yang Anda lihat di sana?";
        }
        
        // Jika sudah ada 1 atau 2 korban, tanyakan korban berikutnya secara umum
        if (count < 3)
        {
            return "Baik, data korban dicatat. Bagaimana dengan kondisi korban lainnya yang ada di lokasi?";
        }

        return "Apakah ada informasi tambahan mengenai para korban?";
    }

    private VictimType GetNextMissingVictim()
    {
        if (!identifiedVictims.Contains(VictimType.Budi)) return VictimType.Budi;
        if (!identifiedVictims.Contains(VictimType.Siti)) return VictimType.Siti;
        return VictimType.Tono;
    }

    private List<VictimType> DetectVictims(string message)
    {
        List<VictimType> victims = new List<VictimType>();

        // Deteksi tanpa harus mewajibkan kata "korban"
        if (message.Contains("budi") || ContainsAny(message, "hijau", "green", "stabil", "aman", "hanya", "asap ringan", "terhirup asap sedikit", "hirup asap sedikit", "mostly fine"))
        {
            victims.Add(VictimType.Budi);
        }

        if (message.Contains("siti") || ContainsAny(message, "perdarahan", "pendarahan", "berdarah", "lengan", "tangan", "derajat 2", "tingkat dua", "luka bakar 2", "second degree"))
        {
            victims.Add(VictimType.Siti);
        }

        if (message.Contains("tono") || ContainsAny(message, "tingkat tiga", "tingkat 3", "derajat 3", "derajat tiga", "luka bakar parah", "terbakar", "luka bakar hebat", "luka bakar luas", "sekujur tubuh", "patah", "kaki patah", "pingsan", "tidak sadar", "airway", "jalan napas"))
        {
            victims.Add(VictimType.Tono);
        }

        return victims;
    }

    private string BuildVictimAdvice(List<VictimType> victims, int maxCount)
    {
        List<string> replies = new List<string>();
        for (int i = 0; i < victims.Count && replies.Count < maxCount; i++)
        {
            replies.Add(GetAdviceForVictim(victims[i]));
        }

        return string.Join(" ", replies);
    }

    private string GetAdviceForVictim(VictimType victim)
    {
        switch (victim)
        {
            case VictimType.Budi:
                return "Pindahkan korban stabil ke udara bersih jika aman.";
            case VictimType.Siti:
                return "Tekan luka dengan kain bersih dan beri tekanan stabil jika aman.";
            case VictimType.Tono:
                return "Prioritaskan napas korban: pindahkan ke udara bersih jika aman dan pantau napas.";
            default:
                return "Pastikan korban dalam kondisi aman.";
        }
    }

    private bool HasLocationInfo(string message)
    {
        return ContainsAny(message,
            "tower 2", "tower2", "menara 2", "menara2",
            "tw2", "tw 2", "tw2 its", "tw2, its", "its tw2"
        );
    }

    private bool HasEmergencyInfo(string message)
    {
        return HasLedakanInfo(message) && HasKebakaranInfo(message);
    }

    private bool HasLedakanInfo(string message)
    {
        return ContainsAny(message, "ledakan", "meledak", "suara keras");
    }

    private bool HasKebakaranInfo(string message)
    {
        return ContainsAny(message, "kebakaran", "terbakar", "api", "asap");
    }

    private bool HasSafetyInfo(string message)
    {
        return ContainsAny(message, "aman", "selamat", "parkiran", "parkir", "lantai 1", "di luar", "turun", "evakuasi", "lobby");
    }

    private bool HasVictimCountInfo(string message)
    {
        //bool hasKorbanWord = ContainsAny(message, "korban", "orang");
        bool hasThree = ContainsAny(message, "3", "tiga");
        return hasThree;
    }

    private static bool ContainsAny(string message, params string[] keywords)
    {
        if (string.IsNullOrEmpty(message) || keywords == null || keywords.Length == 0)
        {
            return false;
        }

        string normalizedMessage = Normalize(message);
        if (string.IsNullOrEmpty(normalizedMessage))
        {
            return false;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            string normalizedKeyword = Normalize(keyword);
            if (string.IsNullOrEmpty(normalizedKeyword))
            {
                continue;
            }

            if (normalizedMessage.Contains(normalizedKeyword))
            {
                return true;
            }
        }

        return false;
    }

    private string BuildClosingResponse()
    {
        return "Baik, bantuan segera tiba. Jaga keselamatan Anda." + " " + BuildDispatchReply();
    }

    private string BuildDispatchReply()
    {
        return "Ambulans dan pemadam sudah diberangkatkan. Tetap di jalur aman dan jangan matikan telepon jika tidak terpaksa.";
    }

    private string GetNextPrompt()
    {
        switch (currentState)
        {
            case QuestionState.EmergencyLocation:
                return "Ada kondisi darurat apa dan di mana?";
            case QuestionState.Safety:
                return "Apakah posisi Anda aman?";
            case QuestionState.VictimCount:
                return "Apakah ada korban lain di sana?";
            case QuestionState.Victim1:
            case QuestionState.Victim2:
            case QuestionState.Victim3:
                return AskForNextVictim();
            case QuestionState.Closing:
                return "Ketik 'siap' jika mengerti instruksi operator.";
            default:
                return string.Empty;
        }
    }

    private bool IsConversationComplete()
    {
        return currentState == QuestionState.Closing;
    }

    

    private void StartCompletionSequence()
    {
        if (isCompleting) return;

        isCompleting = true;
        SetInputInteractable(false);
        // --- Panggil fungsi simpan log di sini ---
        SaveHistoryLog();

        if (finishRoutine != null) StopCoroutine(finishRoutine);
        finishRoutine = StartCoroutine(FinishAfterDelay());
    }

    private IEnumerator FinishAfterDelay()
    {
        yield return new WaitForSeconds(finishDelaySeconds);
        if (gameManager != null) gameManager.OnMiniGameComplete(completionFeedback);
        finishRoutine = null;
    }

    private static string Normalize(string message)
    {
        return string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim().ToLowerInvariant();
    }

    private void UpdatePrompt(string message)
    {
        if (promptText != null) promptText.text = message;
    }

    private void FocusInputField()
    {
        if (playerInputField == null || isCompleting || !playerInputField.interactable) return;
        playerInputField.ActivateInputField();
        playerInputField.Select();
    }

    private void SetInputInteractable(bool interactable)
    {
        if (playerInputField != null) playerInputField.interactable = interactable;
        if (sendButton != null) sendButton.interactable = interactable;
    }

    private void SetInputActive(bool active)
    {
        if (playerInputField != null) playerInputField.gameObject.SetActive(active);
        if (sendButton != null) sendButton.gameObject.SetActive(active);
    }

    private void SetJiroVisible(bool visible)
    {
        if (jiroNelpon != null) jiroNelpon.SetActive(visible);
    }

    private void SetNextButtonActive(bool active)
    {
        if (nextButton != null) nextButton.gameObject.SetActive(active);
    }

    private void SaveHistoryLog()
    {
        // Jangan simpan kalau log-nya kosong
        if (historyBuilder.Length == 0) return;

        // Bikin nama file unik pakai format Waktu_Tanggal (Contoh: Call112Log_20260509_204530.txt)
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Call112Log_{timestamp}.txt";

        // Lokasi aman bawaan Unity
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            // Tulis isi historyBuilder ke dalam file teks
            File.WriteAllText(filePath, historyBuilder.ToString());
            Debug.LogWarning($"[LOG SAVED] Transkrip berhasil disimpan di: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LOG ERROR] Gagal menyimpan transkrip: {e.Message}");
        }
    }
}