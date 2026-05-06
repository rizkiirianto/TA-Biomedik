using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Minigame_Call112Eps3 : MonoBehaviour, IMiniGame
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

    [Header("Conversation")]
    [SerializeField, TextArea(2, 4)] private string openingOperatorLine = "112, layanan darurat. Sebutkan lokasi dan apa yang terjadi.";
    [SerializeField, TextArea(2, 4)] private string completionFeedback = "Panggilan 112 selesai. Bantuan sedang menuju lokasi. Segera lakukan pertolongan pertama pada korban kritis.";
    [SerializeField, Min(0.005f)] private float typingSpeed = 0.03f;
    [SerializeField, Min(0f)] private float operatorReplyDelaySeconds = 5f;
    [SerializeField, Min(0f)] private float finishDelaySeconds = 0.9f;

    [Header("Keyword Rules")]
    private readonly List<KeywordRule> keywordRules = new List<KeywordRule>();

    // Target kategori yang harus dikumpulkan pemain untuk menyelesaikan minigame
    private readonly List<string> requiredCategories = new List<string> 
    { 
        "Lokasi", 
        "Kejadian", 
        "JumlahKorban", 
        "KondisiKorban",
        "Konfirmasi"
    };
    private HashSet<string> achievedCategories = new HashSet<string>();

    private GameManager gameManager;
    private bool isInitialized;
    private bool isCompleting;
    private Coroutine finishRoutine;
    private Coroutine typingRoutine;
    private string queuedOperatorReply = string.Empty;
    private readonly StringBuilder historyBuilder = new StringBuilder();
    private bool operatorSkipRequested;
    
    [Serializable]
    private sealed class KeywordRule
    {
        public string id;
        public bool enabled = true;
        public bool requireAllKeywords;
        public string progressCategory; // Kategori progres yang akan dicentang jika rule ini terpanggil
        public List<string> keywords = new List<string>();
        public List<string> excludeKeywords = new List<string>();
        [TextArea(2, 4)] public List<string> responses = new List<string>();
    }

    private string lastOperatorReply = string.Empty;
    private readonly Dictionary<string, int> lastRuleResponseIndex = new Dictionary<string, int>();

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
        SeedKeywordRules();

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
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        if (finishRoutine != null) StopCoroutine(finishRoutine);
        if (sendButton != null) sendButton.onClick.RemoveListener(OnSendButtonPressed);
        if (historyPanelButton != null) historyPanelButton.onClick.RemoveListener(OnHistoryButtonPressed);
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
        lastOperatorReply = string.Empty;
        lastRuleResponseIndex.Clear();
        historyBuilder.Clear();
        achievedCategories.Clear();
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

        // Cek apakah pesan pemain mengenai Keyword Rule
        string keywordReply = GetKeywordRuleReply(message);
        
        if (!string.IsNullOrEmpty(keywordReply))
        {
            queuedOperatorReply = keywordReply;
        }
        else
        {
            // Jika pemain mengetik asal/tidak kena keyword, berikan fallback hint
            queuedOperatorReply = GetFallbackHint();
        }

        // Jika semua kategori sudah tercapai, timpa balasan dengan instruksi dispatch
        if (IsConversationComplete() && !string.IsNullOrEmpty(keywordReply))
        {
            queuedOperatorReply = keywordReply + " " + BuildDispatchReply();
        }

        typingRoutine = StartCoroutine(TypeConversationTurn(message));
    }

    private bool isTypingMessage;

    private IEnumerator TypeConversationTurn(string playerMessage)
    {
        isTypingMessage = true;
        SetInputInteractable(false);
        SetJiroVisible(true);
        SetNextButtonActive(true);

        yield return StartCoroutine(TypeTranscriptLine("Jiro", playerMessage));
        AppendHistoryLine("Jiro", playerMessage);

        if (!string.IsNullOrEmpty(queuedOperatorReply))
        {
            operatorSkipRequested = false;
            yield return StartCoroutine(WaitForOperatorDelayOrClick());
            SetJiroVisible(false);
            SetNextButtonActive(false);
            yield return StartCoroutine(TypeTranscriptLine("Operator 112", queuedOperatorReply));
            AppendHistoryLine("Operator 112", queuedOperatorReply);
            lastOperatorReply = queuedOperatorReply;
        }
        else
        {
            SetJiroVisible(false);
            SetNextButtonActive(false);
        }

        queuedOperatorReply = string.Empty;
        isTypingMessage = false;
        SetInputInteractable(true);
        FocusInputField();

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

    private string GetKeywordRuleReply(string rawMessage)
    {
        if (keywordRules == null || keywordRules.Count == 0) return string.Empty;

        string message = Normalize(rawMessage);
        for (int i = 0; i < keywordRules.Count; i++)
        {
            KeywordRule rule = keywordRules[i];
            if (rule == null || !rule.enabled || rule.responses == null || rule.responses.Count == 0) continue;

            if (IsRuleMatch(message, rule))
            {
                // Jika match dan punya kategori progres, tambahkan ke Hashset
                if (!string.IsNullOrEmpty(rule.progressCategory))
                {
                    achievedCategories.Add(rule.progressCategory);
                }
                return PickRuleResponse(rule);
            }
        }

        return string.Empty;
    }

    private string GetFallbackHint()
    {
        if (!achievedCategories.Contains("Lokasi"))
            return "Maaf, saya tidak menangkap lokasi Anda. Berada di gedung atau area mana?";
        if (!achievedCategories.Contains("Kejadian"))
            return "Bisa diperjelas apa insiden utamanya? Apakah ada kebakaran atau hal lain?";
        if (!achievedCategories.Contains("JumlahKorban"))
            return "Berapa perkiraan jumlah korban di lokasi saat ini?";
        if (!achievedCategories.Contains("KondisiKorban"))
            return "Mohon jelaskan secara spesifik luka korban (perdarahan, patah tulang, atau luka bakar).";
        if (!achievedCategories.Contains("Konfirmasi"))
            return "Bantuan segera dikirim. Balas 'siap' jika Anda sudah mengerti instruksi untuk bertahan.";

        return "Mohon sampaikan detail lebih lanjut agar kami bisa merespons dengan tepat.";
    }

    private void SeedKeywordRules()
    {
        if (keywordRules.Count > 0)
        {
            return;
        }

        // ==========================================
        // 1. RULES FEEDBACK & NARASI (Tanpa Kategori Progres)
        // ==========================================
        keywordRules.Add(new KeywordRule
        {
            id = "bohong",
            keywords = new List<string> { "bohong", "becanda", "prank" },
            responses = new List<string>
            {
                "Ini jalur darurat. Mohon berikan informasi yang benar agar bantuan bisa dikirim.",
                "Ini layanan darurat. Mohon sampaikan kejadian sebenarnya agar petugas bisa membantu.",
                "Mohon tidak bercanda. Kami butuh informasi yang benar untuk kirim bantuan."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "panik",
            keywords = new List<string> { "panik", "takut", "tolong" },
            responses = new List<string>
            {
                "Tenang. Tarik napas, saya akan bantu. Sebutkan lokasi tepatnya.",
                "Saya dengarkan. Tetap tenang dan sebutkan lokasi secara jelas.",
                "Tenang dulu. Sebutkan lokasi agar bantuan bisa dikirim."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "awal_lantai3",
            keywords = new List<string> { "lantai 3", "lt 3" },
            responses = new List<string>
            {
                "Baik, Anda awalnya di lantai 3. Sekarang Anda berada di mana?",
                "Catat posisi awal di lantai 3. Lokasi Anda sekarang di mana?",
                "Baik, awal di lantai 3. Mohon sebutkan lokasi Anda saat ini."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "tangga_runtuh",
            keywords = new List<string> { "tangga", "runtuh", "ambruk" },
            // Catatan: Jika requireAllKeywords = true, player HARUS mengetik kata tangga AND runtuh AND ambruk sekaligus.
            // Jika maksudmu "tangga" DAN ("runtuh" ATAU "ambruk"), lebih baik hapus "ambruk" dari list ini agar lebih mudah terpanggil.
            requireAllKeywords = true, 
            responses = new List<string>
            {
                "Baik. Tangga darurat runtuh dicatat. Anda sudah di area aman?",
                "Saya catat tangga runtuh. Mohon pastikan Anda di lokasi aman.",
                "Baik, tangga darurat runtuh. Apakah masih ada akses evakuasi lain?"
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "lari_tangga_lain",
            keywords = new List<string> { "lari", "tangga", "satunya", "tangga lain", "tangga kedua" },
            responses = new List<string>
            {
                "Baik. Anda menuju tangga lain. Pastikan tetap aman saat evakuasi.",
                "Baik, pindah ke tangga darurat lain dicatat. Pastikan jalur aman.",
                "Saya catat Anda ke tangga lain. Hati-hati dan utamakan keselamatan."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "turun_aman",
            keywords = new List<string> { "turun", "aman", "selamat", "berhasil turun" },
            responses = new List<string>
            {
                "Baik, Anda sudah turun dengan aman. Tetap di area aman dan sebutkan lokasi saat ini.",
                "Baik, Anda selamat turun. Mohon tetap aman dan sampaikan lokasi Anda.",
                "Catat Anda berhasil turun aman. Sekarang Anda berada di mana?"
            }
        });


        // ==========================================
        // 2. RULES LOKASI
        // ==========================================
        keywordRules.Add(new KeywordRule
        {
            id = "lokasi_tower2",
            progressCategory = "Lokasi",
            keywords = new List<string> { "tower 2", "tower2", "menara 2", "menara2" },
            responses = new List<string>
            {
                "Baik, Tower 2 ITS, bantuan sudah saya kirimkan menuju kesana",
                "Tower 2 dicatat. Apa yang terjadi?",
                "Baik, Tower 2. Apakah anda bisa ceritakan apa yang terjadi?."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "lokasi_kurang_spesifik",
            // Tidak ada progressCategory karena belum spesifik
            keywords = new List<string> { "kampus", "gedung", "institut", "fakultas" },
            excludeKeywords = new List<string> { "tower 2", "tower2", "menara 2", "menara2" },
            responses = new List<string> 
            { 
                "Di kampus sebelah mana tepatnya? Tolong sebutkan nama gedung atau towernya.",
                "Area kampus terlalu luas. Sebutkan spesifik Anda berada di tower mana.",
                "Bisa lebih spesifik? Gedung atau tower apa yang Anda maksud?"
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "parkiran_bawah",
            progressCategory = "Lokasi",
            keywords = new List<string> { "parkiran", "parkir", "basement", "bawah gedung" },
            responses = new List<string>
            {
                "Baik, Anda di parkiran bawah. Tetap di area aman dan tunggu bantuan.",
                "Lokasi pemanggil di parkiran dicatat. Mohon tetap aman.",
                "Baik, parkiran bawah. Apakah korban bersama Anda saat ini?"
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "lihat_korban_parkiran",
            progressCategory = "Lokasi",
            keywords = new List<string> { "lihat", "melihat", "korban", "parkiran", "parkir" },
            requireAllKeywords = false, // Kuubah jadi false agar tidak perlu mengetik ke-5 kata sekaligus
            responses = new List<string>
            {
                "Baik, Anda melihat korban di parkiran. Jelaskan kondisi masing-masing korban.",
                "Catat korban di parkiran. Berapa korban dan bagaimana kondisinya?",
                "Baik. Korban di parkiran dicatat. Mohon jelaskan kondisi mereka."
            }
        });


        // ==========================================
        // 3. RULES KEJADIAN
        // ==========================================
        keywordRules.Add(new KeywordRule
        {
            id = "lantai2_ledakan",
            progressCategory = "Kejadian",
            keywords = new List<string> { "lantai 2", "lt 2", "ledakan", "meledak" },
            requireAllKeywords = false, // Sama seperti di atas, disarankan false agar lebih fleksibel
            responses = new List<string>
            {
                "Baik, ledakan di lantai 2 dicatat. Di bagian mana lantai 2?",
                "Baik, lantai 2 dan ledakan dicatat. Ada api atau asap tebal?",
                "Saya catat ledakan di lantai 2. Sebutkan titik terdekat."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "kebakaran",
            progressCategory = "Kejadian",
            keywords = new List<string> { "kebakaran", "terbakar", "api", "asap" },
            responses = new List<string>
            {
                "Baik. Ada kebakaran. Berapa korban di lokasi?",
                "Baik, ada kebakaran. Sebutkan jumlah korban.",
                "Saya catat kebakaran. Ada berapa korban?"
            }
        });


        // ==========================================
        // 4. RULES JUMLAH KORBAN
        // ==========================================
        keywordRules.Add(new KeywordRule
        {
            id = "jumlah_korban_3",
            progressCategory = "JumlahKorban",
            keywords = new List<string> { "3 korban", "tiga korban", "3 orang", "tiga orang" },
            responses = new List<string>
            {
                "Baik, ada tiga korban. Jelaskan kondisi masing-masing korban.",
                "Tiga korban dicatat. Mohon jelaskan kondisi tiap korban.",
                "Baik, tiga korban. Siapa yang stabil dan siapa yang kritis?"
            }
        });


        // ==========================================
        // 5. RULES KONDISI KORBAN
        // ==========================================
        keywordRules.Add(new KeywordRule
        {
            id = "korban1_asap_ringan",
            progressCategory = "KondisiKorban",
            keywords = new List<string> { "terhirup asap sedikit", "hirup asap sedikit", "asap ringan", "mostly fine", "masih sadar", "stabil" },
            responses = new List<string>
            {
                "Baik. Pindahkan korban stabil ke udara bersih jika aman.",
                "Korban stabil dicatat. Pastikan berada di area berudara bersih.",
                "Baik. Jauhkan korban stabil dari asap dan pantau kondisinya."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "korban2_perdarahan_kaca",
            progressCategory = "KondisiKorban",
            keywords = new List<string> { "pecahan kaca", "kaki berdarah", "perdarahan", "berdarah" },
            responses = new List<string>
            {
                "Tekan luka dengan kain bersih dan beri tekanan stabil jika aman.",
                "Hentikan perdarahan dengan menekan luka memakai kain bersih.",
                "Tekan luka dan tahan tekanan bila aman."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "korban2_luka_bakar2",
            progressCategory = "KondisiKorban",
            keywords = new List<string> { "luka bakar 2", "derajat 2", "second degree" },
            responses = new List<string>
            {
                "Untuk luka bakar derajat dua, dinginkan ringan bila aman dan jangan diolesi.",
                "Luka bakar derajat dua dicatat. Dinginkan ringan dan jangan beri salep.",
                "Baik. Dinginkan luka bakar derajat dua bila aman."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "korban3_luka_bakar_berat",
            progressCategory = "KondisiKorban",
            keywords = new List<string> { "luka bakar hebat", "luka bakar luas", "terbakar parah", "derajat 3", "third degree" },
            responses = new List<string>
            {
                "Prioritaskan keselamatan. Jauhkan dari sumber panas dan pantau napas.",
                "Baik. Jauhkan dari panas dan fokus pada pernapasan korban.",
                "Catat luka bakar berat. Pastikan udara bersih dan pantau napas."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "korban3_airway",
            progressCategory = "KondisiKorban",
            keywords = new List<string> { "airway", "jalan napas", "terbakar di napas", "terhirup asap", "sesak" },
            responses = new List<string>
            {
                "Segera pindahkan ke udara bersih jika aman dan pantau napasnya.",
                "Baik. Pastikan korban di udara bersih dan awasi napas.",
                "Catat gangguan napas. Pindahkan ke area bersih bila aman."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "korban3_pingsan",
            progressCategory = "KondisiKorban",
            keywords = new List<string> { "pingsan", "tidak sadar", "hilang kesadaran" },
            responses = new List<string>
            {
                "Cek napas dan respons korban. Jika tidak bernapas, butuh bantuan segera.",
                "Baik. Periksa napas korban. Jika tidak bernapas, informasikan segera.",
                "Catat tidak sadar. Pantau napas dan respons korban."
            }
        });

        keywordRules.Add(new KeywordRule
        {
            id = "korban3_patah_kaki",
            progressCategory = "KondisiKorban",
            keywords = new List<string> { "patah", "fraktur", "kaki patah" },
            responses = new List<string>
            {
                "Jangan memindahkan korban kecuali sangat darurat. Stabilkan posisi jika bisa.",
                "Baik. Hindari memindahkan korban. Stabilkan posisi bila aman.",
                "Catat patah tulang. Jangan paksa korban bergerak."
            }
        });


        // ==========================================
        // 6. RULE KONFIRMASI (Untuk menamatkan game)
        // ==========================================
        keywordRules.Add(new KeywordRule
        {
            id = "konfirmasi_mengerti",
            progressCategory = "Konfirmasi",
            keywords = new List<string> { "siap", "oke", "ok", "mengerti", "dimengerti", "ya" },
            responses = new List<string> 
            { 
                "Instruksi dipahami. Tetap di jalur aman.",
                "Baik, bantuan segera tiba. Jaga keselamatan Anda.",
                "Diterima. Lakukan pertolongan pertama semampu Anda hingga tim medis tiba." 
            }
        });
    }

    private static bool IsRuleMatch(string message, KeywordRule rule)
    {
        // 1. Cek Exclude Keywords (Pencegat)
        if (rule.excludeKeywords != null && rule.excludeKeywords.Count > 0)
        {
            for (int i = 0; i < rule.excludeKeywords.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(rule.excludeKeywords[i]) && message.Contains(rule.excludeKeywords[i].Trim().ToLowerInvariant()))
                {
                    return false; // Batal terpicu karena mengandung kata yang dikecualikan
                }
            }
        }

        // 2. Cek Keywords Utama
        if (rule.keywords == null || rule.keywords.Count == 0) return false;

        if (rule.requireAllKeywords)
        {
            for (int i = 0; i < rule.keywords.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(rule.keywords[i]) && !message.Contains(rule.keywords[i].Trim().ToLowerInvariant()))
                    return false;
            }
            return true;
        }

        for (int i = 0; i < rule.keywords.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(rule.keywords[i]) && message.Contains(rule.keywords[i].Trim().ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private string PickRuleResponse(KeywordRule rule)
    {
        List<int> candidates = new List<int>();
        for (int i = 0; i < rule.responses.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(rule.responses[i])) continue;
            if (!string.Equals(rule.responses[i].Trim(), lastOperatorReply, StringComparison.Ordinal))
            {
                candidates.Add(i);
            }
        }

        int chosenIndex = candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : UnityEngine.Random.Range(0, rule.responses.Count);
        string selected = rule.responses[chosenIndex] ?? string.Empty;
        lastRuleResponseIndex[rule.id ?? string.Empty] = chosenIndex;
        return selected.Trim();
    }

    private string BuildDispatchReply()
    {
        return "Ambulans dan pemadam sudah diberangkatkan. Tetap di jalur aman dan jangan matikan telepon jika tidak terpaksa.";
    }

    private string GetNextPrompt()
    {
        if (!achievedCategories.Contains("Lokasi")) return "Sebutkan lokasi kejadian.";
        if (!achievedCategories.Contains("Kejadian")) return "Jelaskan apa yang baru saja terjadi.";
        if (!achievedCategories.Contains("JumlahKorban")) return "Sebutkan jumlah korban.";
        if (!achievedCategories.Contains("KondisiKorban")) return "Jelaskan kondisi cedera pada para korban.";
        if (!achievedCategories.Contains("Konfirmasi")) return "Ketik 'siap' jika mengerti instruksi operator.";
        
        return string.Empty;
    }

    private bool IsConversationComplete()
    {
        foreach (string req in requiredCategories)
        {
            if (!achievedCategories.Contains(req)) return false;
        }
        return true;
    }

    private void StartCompletionSequence()
    {
        if (isCompleting) return;

        isCompleting = true;
        SetInputInteractable(false);
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
}