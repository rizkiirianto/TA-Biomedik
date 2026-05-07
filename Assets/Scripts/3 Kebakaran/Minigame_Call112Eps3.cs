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

    [Header("Patient Panel")]
    [SerializeField] private GameObject patientPanel;
    [SerializeField] private Button patientPanelButton;

    [Header("Conversation")]
    [SerializeField, TextArea(2, 4)] private string openingOperatorLine = "112, layanan darurat. Sebutkan lokasi dan apa yang terjadi.";
    [SerializeField, TextArea(2, 4)] private string completionFeedback = "Panggilan 112 selesai. Bantuan sedang menuju lokasi. Segera lakukan pertolongan pertama pada korban kritis.";
    [SerializeField, Min(0.005f)] private float typingSpeed = 0.03f;
    [SerializeField, Min(0f)] private float operatorReplyDelaySeconds = 5f;
    [SerializeField, Min(0f)] private float finishDelaySeconds = 0.9f;

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
        lastOperatorReply = string.Empty;
        lastRuleResponseIndex.Clear();
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

        queuedOperatorReply = HandleMessageForState(message);

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

    public void SetPatientPanelVisible(bool visible)
    {
        if (patientPanel == null) return;

        patientPanel.SetActive(visible);
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
            return PickVariant("state1_both", new List<string>
            {
                "Bisa tolong sebutkan lokasi Anda dan kondisi daruratnya.",
                "Di mana lokasi Anda dan apa kondisi daruratnya?",
                "Mohon jelaskan lokasi dan kondisi darurat yang terjadi."
            });
        }

        if (!hasLocation)
        {
            return PickVariant("state1_location", new List<string>
            {
                "Bisakah tolong beri tau lokasi Anda di mana? ",
                "Mohon sebutkan lokasi yang detail",
                "Dimana Anda berada saat ini?"
            });
        }

        if (!hasEmergency)
        {
            if (!hasLedakan && hasKebakaran)
            {
                return PickVariant("state1_need_ledakan", new List<string>
                {
                    "Apakah Anda tahu asal api itu dari mana? Apakah ada suara ledakan yang Anda dengar?",
                    "Apakah ada suara atau petunjuk asal api itu?",
                    "Bisa jelaskan apakah ada suara keras sebelum api muncul?"
                });
            }

            if (!hasKebakaran && hasLedakan)
            {
                return PickVariant("state1_need_kebakaran", new List<string>
                {
                    "Apakah Anda bisa melihat api atau asap dari ledakan itu? Bagaimana dampaknya?",
                    "Apakah ada api atau asap setelah ledakan? Seberapa besar dampaknya?",
                    "Bisa jelaskan apakah terlihat api dan asap setelah ledakan?"
                });
            }

            return PickVariant("state1_emergency", new List<string>
            {
                "Apa kondisi darurat yang terjadi disana?",
                "Mohon jelaskan kondisi darurat yang terjadi!",
                "Apa yang terjadi di lokasi saat ini?"
            });
        }

        currentState = QuestionState.Safety;
        return PickVariant("state1_ok", new List<string>
        {
            "Baik, lokasi dan kondisi darurat dicatat. Apakah posisi Anda aman?",
            "Saya catat lokasi dan kondisi darurat. Apakah Anda sudah di tempat aman?",
            "Lokasi dan kondisi darurat dicatat. Posisi Anda aman?"
        });
    }

    private string HandleSafety(string message)
    {
        if (!HasSafetyInfo(message))
        {
            return PickVariant("state2_need", new List<string>
            {
                "Apakah posisi Anda aman saat ini?",
                "Mohon pastikan posisi Anda aman. Sudah aman?",
                "Anda sudah berada di tempat aman?"
            });
        }

        currentState = QuestionState.VictimCount;
        return PickVariant("state2_ok", new List<string>
        {
            "Baik. Apakah ada korban lain di lokasi?",
            "Baik. Ada berapa orang lain yang terdampak di lokasi selain anda?",
            "Posisi aman dicatat. Apakah ada korban lain?"
        });
    }

    private string HandleVictimCount(string message)
    {
        if (!HasVictimCountInfo(message))
        {
            return PickVariant("state3_need", new List<string>
            {
                "Pastikan jumlah korban di lokasi benar sesuai yang anda lihat",
                "Mohon bantu sebutkan jumlah korban yang terdampak.",
                "Coba pastikan ada berapa jumlah korban!"
            });
        }

        currentState = QuestionState.Victim1;
        return PickVariant("state3_ok", new List<string>
        {
            "Baik, ada tiga korban. Bisakah anda jelaskan kondisi korban pertama?",
            "Tiga korban dicatat. Bagaimana kondisi korban pertama?",
            "Baik, tiga korban. Tolong jelaskan kondisi korban pertama!"
        });
    }

    private string HandleVictimDetails(string message)
    {
        List<VictimType> detected = DetectVictims(message);
        List<VictimType> newVictims = new List<VictimType>();
        for (int i = 0; i < detected.Count; i++)
        {
            if (!identifiedVictims.Contains(detected[i]))
            {
                newVictims.Add(detected[i]);
            }
        }

        if (newVictims.Count == 0)
        {
            return AskForNextVictim();
        }

        string advice = BuildVictimAdvice(newVictims, 2);
        for (int i = 0; i < newVictims.Count; i++)
        {
            identifiedVictims.Add(newVictims[i]);
        }

        UpdateVictimStateAfterProgress();

        if (currentState == QuestionState.Closing)
        {
            return advice + " " + BuildClosingResponse();
        }

        return advice + " " + AskForNextVictim();
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
        VictimType next = GetNextMissingVictim();
        switch (next)
        {
            case VictimType.Budi:
                return PickVariant("ask_budi", new List<string>
                {
                    "Bagaimana kondisi Budi (korban stabil/asap ringan)?",
                    "Jelaskan kondisi Budi yang stabil.",
                    "Mohon jelaskan kondisi korban stabil (Budi)."
                });
            case VictimType.Siti:
                return PickVariant("ask_siti", new List<string>
                {
                    "Bagaimana kondisi Siti (perdarahan dan luka bakar)?",
                    "Jelaskan kondisi Siti yang mengalami perdarahan.",
                    "Mohon jelaskan kondisi korban kedua (Siti)."
                });
            case VictimType.Tono:
                return PickVariant("ask_tono", new List<string>
                {
                    "Bagaimana kondisi Tono (luka bakar berat/patah)?",
                    "Jelaskan kondisi Tono yang kritis.",
                    "Mohon jelaskan kondisi korban terakhir (Tono)."
                });
            default:
                return "Mohon jelaskan kondisi korban berikutnya.";
        }
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
        bool mentionsKorban = message.Contains("korban") || message.Contains("orang");

        if (message.Contains("budi") || (mentionsKorban && ContainsAny(message, "hijau", "green", "stabil", "aman", "hanya", "asap ringan", "terhirup asap sedikit", "hirup asap sedikit", "mostly fine")))
        {
            victims.Add(VictimType.Budi);
        }

        if (message.Contains("siti") || (mentionsKorban && ContainsAny(message, "perdarahan", "pendarahan", "berdarah", "lengan", "tangan", "derajat 2", "tingkat dua", "luka bakar 2", "second degree")))
        {
            victims.Add(VictimType.Siti);
        }

        if (message.Contains("tono") || (mentionsKorban && ContainsAny(message, "tingkat tiga", "derajat 3", "luka bakar hebat", "luka bakar luas", "sekujur tubuh", "patah", "kaki patah", "pingsan", "tidak sadar", "airway", "jalan napas")))
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
                return PickVariant("adv_budi", new List<string>
                {
                    "Pindahkan korban stabil ke udara bersih jika aman.",
                    "Pastikan korban stabil berada di area berudara bersih.",
                    "Jauhkan korban stabil dari asap dan pantau kondisinya."
                });
            case VictimType.Siti:
                return PickVariant("adv_siti", new List<string>
                {
                    "Tekan luka dengan kain bersih dan beri tekanan stabil jika aman. Dinginkan luka bakar bila aman.",
                    "Hentikan perdarahan dengan menekan luka, dan dinginkan luka bakar secara ringan.",
                    "Tekan luka dan tahan tekanan, lalu dinginkan luka bakar bila memungkinkan."
                });
            case VictimType.Tono:
                return PickVariant("adv_tono", new List<string>
                {
                    "Prioritaskan napas korban. Pindahkan ke udara bersih jika aman dan pantau napas.",
                    "Jauhkan dari panas dan fokus pada pernapasan korban.",
                    "Pastikan udara bersih dan pantau napas korban."
                });
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
        bool hasKorbanWord = ContainsAny(message, "korban", "orang");
        bool hasThree = ContainsAny(message, "3", "tiga");
        return hasKorbanWord && hasThree;
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
        return PickVariant("closing", new List<string>
        {
            "Baik, bantuan segera tiba. Jaga keselamatan Anda.",
            "Diterima. Tim dalam perjalanan. Tetap di area aman.",
            "Instruksi dipahami. Bantuan menuju lokasi."
        }) + " " + BuildDispatchReply();
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

    private string PickVariant(string key, List<string> options)
    {
        if (options == null || options.Count == 0)
        {
            return string.Empty;
        }

        int lastIndex = -1;
        if (lastRuleResponseIndex.TryGetValue(key, out int storedIndex))
        {
            lastIndex = storedIndex;
        }

        List<int> candidates = new List<int>();
        for (int i = 0; i < options.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(options[i]))
            {
                continue;
            }

            if (i != lastIndex && !string.Equals(options[i].Trim(), lastOperatorReply, StringComparison.Ordinal))
            {
                candidates.Add(i);
            }
        }

        int chosenIndex = candidates.Count > 0
            ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
            : UnityEngine.Random.Range(0, options.Count);

        lastRuleResponseIndex[key] = chosenIndex;
        return options[chosenIndex].Trim();
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