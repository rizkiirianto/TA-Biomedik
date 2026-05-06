using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

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

    [Header("Conversation")]
    [SerializeField, TextArea(2, 4)] private string openingOperatorLine = "112, layanan darurat. Sebutkan lokasi dan apa yang terjadi.";
    [SerializeField, TextArea(2, 4)] private string completionFeedback = "Panggilan 112 selesai. Bantuan sedang menuju lokasi.";
    [SerializeField, Min(0.005f)] private float typingSpeed = 0.03f;
    [SerializeField, Min(0f)] private float operatorReplyDelaySeconds = 5f;
    [SerializeField, Min(0f)] private float finishDelaySeconds = 0.9f;

    [Header("LLM (Gemini)")]
    [SerializeField] private bool useGeminiOperator = false;
    [SerializeField] private string geminiApiKey;
    [SerializeField] private string geminiModel = "gemini-2.5-flash";
    [SerializeField, Range(0f, 1f)] private float geminiTemperature = 0.4f;
    [SerializeField, Min(16)] private int geminiMaxOutputTokens = 320;
    [SerializeField, Min(1)] private int geminiContextTurns = 6;
    [SerializeField, Min(1f)] private float geminiTimeoutSeconds = 12f;
    [SerializeField, Range(0, 5)] private int geminiMaxRetries = 2;
    [SerializeField, Min(0.2f)] private float geminiRetryBackoffSeconds = 1.2f;
    [SerializeField, Min(1f)] private float geminiRetryBackoffMultiplier = 2f;

    private sealed class MessageFacts
    {
        public bool HasLocation;
        public bool HasIncident;
        public bool HasVictimCount;
        public bool HasGreenVictim;
        public bool HasRedVictim;
        public bool HasBleeding;
        public bool HasSecondDegreeBurn;
        public bool HasThirdDegreeBurn;
        public bool HasAirwayBurn;
        public bool HasBrokenLeg;
        public bool HasAcknowledgement;
    }

    private readonly MessageFacts collectedFacts = new MessageFacts();
    private GameManager gameManager;
    private bool isInitialized;
    private bool isCompleting;
    private Coroutine finishRoutine;
    private Coroutine typingRoutine;
    private string queuedOperatorReply = string.Empty;
    private readonly StringBuilder historyBuilder = new StringBuilder();
    private bool operatorSkipRequested;
    private readonly List<ChatTurn> conversationTurns = new List<ChatTurn>();

    [Serializable]
    private sealed class ChatTurn
    {
        public string Speaker;
        public string Message;
    }

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
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        isCompleting = false;

        if (jiroNelpon != null)
        {
            jiroNelpon.SetActive(true);
        }

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
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (finishRoutine != null)
        {
            StopCoroutine(finishRoutine);
            finishRoutine = null;
        }

        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendButtonPressed);
        }

        if (historyPanelButton != null)
        {
            historyPanelButton.onClick.RemoveListener(OnHistoryButtonPressed);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextButtonPressed);
        }
    }

    private void Update()
    {
        if (!isInitialized || isCompleting || playerInputField == null)
        {
            return;
        }

        if (!playerInputField.isFocused)
        {
            return;
        }

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
        historyBuilder.Clear();
        SetHistoryText(string.Empty);
        ResetFacts();

        SetTranscriptLine(string.Empty);
        AppendOperatorLine(openingOperatorLine);
        TrackConversationTurn("Operator 112", openingOperatorLine);
        UpdatePrompt("Ketik jawabanmu lalu tekan Enter atau tombol kirim.");
        SetNextButtonActive(false);
        FocusInputField();
    }

    private void ResetFacts()
    {
        collectedFacts.HasLocation = false;
        collectedFacts.HasIncident = false;
        collectedFacts.HasVictimCount = false;
        collectedFacts.HasGreenVictim = false;
        collectedFacts.HasRedVictim = false;
        collectedFacts.HasBleeding = false;
        collectedFacts.HasSecondDegreeBurn = false;
        collectedFacts.HasThirdDegreeBurn = false;
        collectedFacts.HasAirwayBurn = false;
        collectedFacts.HasBrokenLeg = false;
        collectedFacts.HasAcknowledgement = false;
    }

    private void SendCurrentMessage()
    {
        if (isCompleting || playerInputField == null || isTypingMessage)
        {
            return;
        }

        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message))
        {
            FocusInputField();
            return;
        }

        playerInputField.text = string.Empty;

        MessageFacts messageFacts = AnalyzeMessage(message);
        MergeFacts(messageFacts);

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        if (useGeminiOperator)
        {
            typingRoutine = StartCoroutine(TypeConversationTurnWithGemini(message, messageFacts));
        }
        else
        {
            queuedOperatorReply = BuildOperatorReply(messageFacts);
            typingRoutine = StartCoroutine(TypeConversationTurn(message));
        }
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

        UpdatePrompt(GetNextPrompt());
        typingRoutine = null;
    }

    private IEnumerator TypeConversationTurnWithGemini(string playerMessage, MessageFacts messageFacts)
    {
        isTypingMessage = true;
        SetInputInteractable(false);
        SetJiroVisible(true);
        SetNextButtonActive(false);

        yield return StartCoroutine(TypeTranscriptLine("Jiro", playerMessage));
        AppendHistoryLine("Jiro", playerMessage);

        SetJiroVisible(false);
        UpdatePrompt("Menunggu balasan operator...");

        yield return StartCoroutine(FetchGeminiReply(playerMessage, messageFacts));

        if (!string.IsNullOrEmpty(queuedOperatorReply))
        {
            yield return StartCoroutine(TypeTranscriptLine("Operator 112", queuedOperatorReply));
            AppendHistoryLine("Operator 112", queuedOperatorReply);
        }

        queuedOperatorReply = string.Empty;
        isTypingMessage = false;
        SetInputInteractable(true);
        FocusInputField();

        if (IsConversationComplete())
        {
            StartCompletionSequence();
        }

        UpdatePrompt(GetNextPrompt());
        typingRoutine = null;
    }

    private IEnumerator WaitForOperatorDelayOrClick()
    {
        float remaining = Mathf.Max(0f, operatorReplyDelaySeconds);
        while (remaining > 0f)
        {
            if (operatorSkipRequested)
            {
                break;
            }

            remaining -= Time.deltaTime;
            yield return null;
        }
    }

    private void AppendPlayerLine(string message)
    {
        AppendTranscriptLine("Jiro", message);
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
        if (historyBuilder.Length > 0)
        {
            historyBuilder.AppendLine();
        }

        historyBuilder.Append(speaker);
        historyBuilder.Append(": ");
        historyBuilder.Append(message.Trim());

        SetHistoryText(historyBuilder.ToString());
        TrackConversationTurn(speaker, message);
        if (historyPanel != null && historyPanel.activeSelf)
        {
            ScrollHistoryToBottom();
        }
    }

    private void TrackConversationTurn(string speaker, string message)
    {
        if (string.IsNullOrWhiteSpace(speaker) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        conversationTurns.Add(new ChatTurn
        {
            Speaker = speaker.Trim(),
            Message = message.Trim()
        });

        int maxTurns = Mathf.Max(2, geminiContextTurns * 2);
        int excess = conversationTurns.Count - maxTurns;
        if (excess > 0)
        {
            conversationTurns.RemoveRange(0, excess);
        }
    }

    private void SetHistoryText(string value)
    {
        if (historyText != null)
        {
            historyText.text = value;
        }
    }

    public void SetHistoryPanelVisible(bool visible)
    {
        if (historyPanel == null)
        {
            return;
        }

        historyPanel.SetActive(visible);
        SetInputActive(!visible);
        SetInputInteractable(!visible && !isCompleting && !isTypingMessage);
        if (visible)
        {
            ScrollHistoryToBottom();
        }
        else
        {
            FocusInputField();
        }
    }

    private void ScrollHistoryToBottom()
    {
        if (historyScrollRect == null)
        {
            return;
        }

        StartCoroutine(ScrollHistoryToBottomNextFrame());
    }

    private IEnumerator ScrollHistoryToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (historyScrollRect != null)
        {
            historyScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void SetTranscriptLine(string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private IEnumerator FetchGeminiReply(string playerMessage, MessageFacts messageFacts)
    {
        if (string.IsNullOrWhiteSpace(geminiApiKey) || string.IsNullOrWhiteSpace(geminiModel))
        {
            queuedOperatorReply = BuildOperatorReply(messageFacts);
            yield break;
        }

        string prompt = BuildGeminiPrompt(playerMessage, messageFacts);
        GeminiRequest request = new GeminiRequest
        {
            contents = new[]
            {
                new GeminiContent
                {
                    role = "user",
                    parts = new[] { new GeminiPart { text = prompt } }
                }
            },
            generationConfig = new GeminiGenerationConfig
            {
                temperature = geminiTemperature,
                maxOutputTokens = geminiMaxOutputTokens
            }
        };

        string json = JsonUtility.ToJson(request);
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{geminiModel}:generateContent?key={geminiApiKey.Trim()}";

        int maxAttempts = Mathf.Max(0, geminiMaxRetries) + 1;
        float backoffSeconds = Mathf.Max(0.2f, geminiRetryBackoffSeconds);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.timeout = Mathf.CeilToInt(geminiTimeoutSeconds);

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string responseText = webRequest.downloadHandler.text;
                    if (!TryParseGeminiText(responseText, out string replyText))
                    {
                        Debug.LogWarning("Gemini response parse failed.");
                        queuedOperatorReply = BuildOperatorReply(messageFacts);
                        yield break;
                    }

                    string sanitized = SanitizeGeminiReply(replyText, messageFacts);
                    if (!IsLikelyCompleteSentence(sanitized))
                    {
                        string continuationPrompt = BuildContinuationPrompt(sanitized);
                        GeminiReplyHolder continuation = new GeminiReplyHolder();
                        yield return StartCoroutine(RequestGeminiOnce(continuationPrompt, GetContinuationMaxTokens(), continuation));
                        if (!string.IsNullOrWhiteSpace(continuation.Text))
                        {
                            sanitized = MergeContinuation(sanitized, continuation.Text);
                        }
                    }

                    queuedOperatorReply = sanitized;
                    yield break;
                }

                string errorBody = webRequest.downloadHandler != null
                    ? webRequest.downloadHandler.text
                    : string.Empty;
                bool shouldRetry = attempt < maxAttempts && ShouldRetryGemini(webRequest, errorBody);
                Debug.LogWarning($"Gemini request failed (attempt {attempt}/{maxAttempts}): {webRequest.error}. Body: {errorBody}");

                if (!shouldRetry)
                {
                    queuedOperatorReply = BuildOperatorReply(messageFacts);
                    yield break;
                }
            }

            yield return new WaitForSeconds(backoffSeconds);
            backoffSeconds *= Mathf.Max(1f, geminiRetryBackoffMultiplier);
        }
    }

    private bool ShouldRetryGemini(UnityWebRequest webRequest, string errorBody)
    {
        if (webRequest == null)
        {
            return false;
        }

        bool isRateLimited = webRequest.responseCode == 429 || webRequest.responseCode == 503;
        if (isRateLimited)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(errorBody))
        {
            return false;
        }

        string lowered = errorBody.ToLowerInvariant();
        return lowered.Contains("unavailable") || lowered.Contains("too many requests");
    }

    private sealed class GeminiReplyHolder
    {
        public string Text;
    }

    private IEnumerator RequestGeminiOnce(string prompt, int maxTokens, GeminiReplyHolder holder)
    {
        if (holder == null)
        {
            yield break;
        }

        holder.Text = string.Empty;

        GeminiRequest request = new GeminiRequest
        {
            contents = new[]
            {
                new GeminiContent
                {
                    role = "user",
                    parts = new[] { new GeminiPart { text = prompt } }
                }
            },
            generationConfig = new GeminiGenerationConfig
            {
                temperature = geminiTemperature,
                maxOutputTokens = maxTokens
            }
        };

        string json = JsonUtility.ToJson(request);
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{geminiModel}:generateContent?key={geminiApiKey.Trim()}";

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.timeout = Mathf.CeilToInt(geminiTimeoutSeconds);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                yield break;
            }

            string responseText = webRequest.downloadHandler.text;
            if (TryParseGeminiText(responseText, out string replyText))
            {
                holder.Text = replyText;
            }
        }
    }

    private static bool IsLikelyCompleteSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        string trimmed = text.TrimEnd();
        char last = trimmed[trimmed.Length - 1];
        return last == '.' || last == '?' || last == '!';
    }

    private static string MergeContinuation(string baseText, string continuation)
    {
        if (string.IsNullOrWhiteSpace(continuation))
        {
            return baseText;
        }

        string trimmedContinuation = continuation.Trim();
        int overlap = FindOverlap(baseText, trimmedContinuation);
        string suffix = overlap > 0 ? trimmedContinuation.Substring(overlap).TrimStart() : trimmedContinuation;
        string spacer = baseText.EndsWith(" ") || string.IsNullOrEmpty(suffix) ? string.Empty : " ";
        return (baseText + spacer + suffix).Trim();
    }

    private string BuildContinuationPrompt(string partialReply)
    {
        string seed = GetContinuationSeed(partialReply, 80);
        return $"Lanjutkan 1 kalimat terakhir secara langsung tanpa mengulang. Mulai tepat setelah teks ini: \"{seed}\"";
    }

    private int GetContinuationMaxTokens()
    {
        int half = Mathf.Max(24, geminiMaxOutputTokens / 3);
        return Mathf.Min(80, half);
    }

    private static int FindOverlap(string baseText, string continuation)
    {
        if (string.IsNullOrEmpty(baseText) || string.IsNullOrEmpty(continuation))
        {
            return 0;
        }

        int maxCheck = Mathf.Min(baseText.Length, continuation.Length);
        for (int len = maxCheck; len >= 6; len--)
        {
            string suffix = baseText.Substring(baseText.Length - len, len);
            if (continuation.StartsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return len;
            }
        }

        return 0;
    }

    private static string GetContinuationSeed(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string trimmed = text.Trim();
        if (trimmed.Length <= maxChars)
        {
            return trimmed;
        }

        return trimmed.Substring(trimmed.Length - maxChars, maxChars);
    }
    private string BuildGeminiPrompt(string playerMessage, MessageFacts messageFacts)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Peran: Operator 112 Indonesia.");
        builder.AppendLine("Gaya: formal, ringkas, 2-3 kalimat.");
        builder.AppendLine("Tujuan: kumpulkan info inti, tanya spesifik bila kurang.");
        builder.AppendLine("Butuh: lokasi; insiden; jumlah korban; kondisi hijau/merah + luka; konfirmasi 'siap'.");
        builder.AppendLine("Status: " + BuildFactsStatusSummary());
        builder.AppendLine("Riwayat singkat:");

        int start = Mathf.Max(0, conversationTurns.Count - (geminiContextTurns * 2));
        for (int i = start; i < conversationTurns.Count; i++)
        {
            ChatTurn turn = conversationTurns[i];
            builder.AppendLine($"{turn.Speaker}: {turn.Message}");
        }

        builder.AppendLine($"Jiro: {playerMessage}");
        builder.AppendLine("Balas sebagai Operator 112.");
        return builder.ToString();
    }

    private string BuildFactsStatusSummary()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(collectedFacts.HasLocation ? "lokasi=ok" : "lokasi=perlu");
        builder.Append(collectedFacts.HasIncident ? ", insiden=ok" : ", insiden=perlu");
        builder.Append(collectedFacts.HasVictimCount ? ", korban=ok" : ", korban=perlu");
        builder.Append(collectedFacts.HasGreenVictim ? ", hijau=ok" : ", hijau=perlu");
        builder.Append(collectedFacts.HasRedVictim ? ", merah=ok" : ", merah=perlu");
        builder.Append(collectedFacts.HasBleeding ? ", darah=ok" : ", darah=perlu");
        builder.Append(collectedFacts.HasSecondDegreeBurn ? ", bakar2=ok" : ", bakar2=perlu");
        builder.Append(collectedFacts.HasThirdDegreeBurn ? ", bakar3=ok" : ", bakar3=perlu");
        builder.Append(collectedFacts.HasAirwayBurn ? ", napas=ok" : ", napas=perlu");
        builder.Append(collectedFacts.HasBrokenLeg ? ", patah=ok" : ", patah=perlu");
        builder.Append(collectedFacts.HasAcknowledgement ? ", siap=ok" : ", siap=perlu");
        return builder.ToString();
    }

    private string SanitizeGeminiReply(string replyText, MessageFacts messageFacts)
    {
        if (string.IsNullOrWhiteSpace(replyText))
        {
            return BuildOperatorReply(messageFacts);
        }

        string normalized = replyText.Trim();
        if (normalized.Length > 400)
        {
            normalized = normalized.Substring(0, 400).Trim();
        }

        return normalized;
    }

    private bool TryParseGeminiText(string json, out string replyText)
    {
        replyText = string.Empty;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        GeminiResponse response;
        try
        {
            response = JsonUtility.FromJson<GeminiResponse>(json);
        }
        catch (Exception)
        {
            return false;
        }

        if (response?.candidates == null || response.candidates.Length == 0)
        {
            return false;
        }

        GeminiContent content = response.candidates[0].content;
        if (content == null || content.parts == null || content.parts.Length == 0)
        {
            return false;
        }

        replyText = content.parts[0].text ?? string.Empty;
        return !string.IsNullOrWhiteSpace(replyText);
    }

    [Serializable]
    private sealed class GeminiRequest
    {
        public GeminiContent[] contents;
        public GeminiGenerationConfig generationConfig;
    }

    [Serializable]
    private sealed class GeminiGenerationConfig
    {
        public float temperature = 0.4f;
        public int maxOutputTokens = 160;
    }

    [Serializable]
    private sealed class GeminiResponse
    {
        public GeminiCandidate[] candidates;
    }

    [Serializable]
    private sealed class GeminiCandidate
    {
        public GeminiContent content;
    }

    [Serializable]
    private sealed class GeminiContent
    {
        public string role;
        public GeminiPart[] parts;
    }

    [Serializable]
    private sealed class GeminiPart
    {
        public string text;
    }

    private void UpdatePrompt(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
        }
    }

    private void FocusInputField()
    {
        if (playerInputField == null || isCompleting || !playerInputField.interactable)
        {
            return;
        }

        playerInputField.ActivateInputField();
        playerInputField.Select();
    }

    private void SetInputInteractable(bool interactable)
    {
        if (playerInputField != null)
        {
            playerInputField.interactable = interactable;
        }

        if (sendButton != null)
        {
            sendButton.interactable = interactable;
        }
    }

    private void SetInputActive(bool active)
    {
        if (playerInputField != null)
        {
            playerInputField.gameObject.SetActive(active);
        }

        if (sendButton != null)
        {
            sendButton.gameObject.SetActive(active);
        }
    }

    private void SetJiroVisible(bool visible)
    {
        if (jiroNelpon != null)
        {
            jiroNelpon.SetActive(visible);
        }
    }

    private void SetNextButtonActive(bool active)
    {
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(active);
        }
    }

    private MessageFacts AnalyzeMessage(string rawMessage)
    {
        string message = Normalize(rawMessage);
        MessageFacts facts = new MessageFacts
        {
            HasLocation = ContainsAny(message, "kampus", "gedung", "lab", "laboratorium", "lantai", "ruang", "gedung kampus", "alamat"),
            HasIncident = ContainsAny(message, "ledakan", "meledak", "explosion", "kebakaran", "terbakar", "api", "asap"),
            HasVictimCount = ContainsAny(message, "3 korban", "3 orang", "tiga korban", "tiga orang", "jumlah korban", "ada 3", "ada tiga"),
            HasGreenVictim = ContainsAny(message, "hijau", "green", "mostly okay", "stabil", "masih sadar", "baik-baik saja"),
            HasRedVictim = ContainsAny(message, "merah", "red", "kritis", "gawat", "parah"),
            HasBleeding = ContainsAny(message, "darah", "berdarah", "perdarahan", "bleeding"),
            HasSecondDegreeBurn = ContainsAny(message, "derajat 2", "2nd degree", "second degree", "luka bakar 2", "burn 2"),
            HasThirdDegreeBurn = ContainsAny(message, "derajat 3", "3rd degree", "third degree", "luka bakar 3", "burn 3"),
            HasAirwayBurn = ContainsAny(message, "jalan napas", "airway", "napas", "terhirup asap"),
            HasBrokenLeg = ContainsAny(message, "patah", "broken leg", "kaki patah", "fraktur"),
            HasAcknowledgement = ContainsAny(message, "siap", "oke", "ok", "mengerti", "dimengerti", "ya")
        };

        return facts;
    }

    private void MergeFacts(MessageFacts messageFacts)
    {
        collectedFacts.HasLocation |= messageFacts.HasLocation;
        collectedFacts.HasIncident |= messageFacts.HasIncident;
        collectedFacts.HasVictimCount |= messageFacts.HasVictimCount;
        collectedFacts.HasGreenVictim |= messageFacts.HasGreenVictim;
        collectedFacts.HasRedVictim |= messageFacts.HasRedVictim;
        collectedFacts.HasBleeding |= messageFacts.HasBleeding;
        collectedFacts.HasSecondDegreeBurn |= messageFacts.HasSecondDegreeBurn;
        collectedFacts.HasThirdDegreeBurn |= messageFacts.HasThirdDegreeBurn;
        collectedFacts.HasAirwayBurn |= messageFacts.HasAirwayBurn;
        collectedFacts.HasBrokenLeg |= messageFacts.HasBrokenLeg;
        collectedFacts.HasAcknowledgement |= messageFacts.HasAcknowledgement;
    }

    private string BuildOperatorReply(MessageFacts messageFacts)
    {
        if (!collectedFacts.HasLocation)
        {
            return "Saya butuh lokasi tepatnya dulu. Sebutkan gedung, lantai, atau titik terdekat di kampus.";
        }

        if (!collectedFacts.HasIncident)
        {
            return "Lokasi sudah saya catat. Sekarang jelaskan kejadian utamanya: ada ledakan, kebakaran, atau asap tebal?";
        }

        if (!collectedFacts.HasVictimCount)
        {
            return "Baik, saya catat ada insiden darurat. Ada berapa korban di lokasi itu?";
        }

        if (!collectedFacts.HasGreenVictim || !collectedFacts.HasRedVictim)
        {
            return "Sebutkan pembagian kondisi korban. Siapa yang relatif aman atau hijau, dan siapa yang merah atau kritis?";
        }

        if (!collectedFacts.HasBleeding || !collectedFacts.HasSecondDegreeBurn || !collectedFacts.HasThirdDegreeBurn || !collectedFacts.HasAirwayBurn || !collectedFacts.HasBrokenLeg)
        {
            return "Sekarang jelaskan luka masing-masing korban secara singkat: perdarahan, luka bakar, gangguan napas, atau patah tulang.";
        }

        if (!collectedFacts.HasAcknowledgement)
        {
            return BuildDispatchReply() + " Balas 'siap' kalau kamu sudah mengerti instruksi terakhir.";
        }

        return completionFeedback;
    }

    private string BuildDispatchReply()
    {
        StringBuilder reply = new StringBuilder();
        reply.Append("Baik, bantuan sedang saya kirim sekarang.");

        if (collectedFacts.HasBleeding)
        {
            reply.Append(" Untuk korban dengan perdarahan, tekan luka dengan kain bersih jika aman.");
        }

        if (collectedFacts.HasSecondDegreeBurn)
        {
            reply.Append(" Untuk luka bakar derajat dua, jauhkan dari sumber panas dan dinginkan ringan bila aman.");
        }

        if (collectedFacts.HasThirdDegreeBurn || collectedFacts.HasAirwayBurn)
        {
            reply.Append(" Untuk luka bakar luas atau gangguan jalan napas, prioritaskan udara bersih dan pantau napas korban.");
        }

        if (collectedFacts.HasBrokenLeg)
        {
            reply.Append(" Jangan memaksa korban dengan patah tulang untuk berdiri atau berjalan.");
        }

        reply.Append(" Tetap di area aman dan tunggu petugas.");
        return reply.ToString();
    }

    private string GetNextPrompt()
    {
        if (!collectedFacts.HasLocation)
        {
            return "Mulai dengan lokasi kamu berada.";
        }

        if (!collectedFacts.HasIncident)
        {
            return "Jelaskan kejadian daruratnya.";
        }

        if (!collectedFacts.HasVictimCount)
        {
            return "Sebutkan jumlah korban.";
        }

        if (!collectedFacts.HasGreenVictim || !collectedFacts.HasRedVictim)
        {
            return "Tulis kondisi korban: hijau dan merah.";
        }

        if (!collectedFacts.HasBleeding || !collectedFacts.HasSecondDegreeBurn || !collectedFacts.HasThirdDegreeBurn || !collectedFacts.HasAirwayBurn || !collectedFacts.HasBrokenLeg)
        {
            return "Tambahkan detail cedera masing-masing korban.";
        }

        if (!collectedFacts.HasAcknowledgement)
        {
            return "Ketik 'siap' untuk mengakhiri panggilan.";
        }

        return string.Empty;
    }

    private bool IsConversationComplete()
    {
        return collectedFacts.HasLocation
            && collectedFacts.HasIncident
            && collectedFacts.HasVictimCount
            && collectedFacts.HasGreenVictim
            && collectedFacts.HasRedVictim
            && collectedFacts.HasBleeding
            && collectedFacts.HasSecondDegreeBurn
            && collectedFacts.HasThirdDegreeBurn
            && collectedFacts.HasAirwayBurn
            && collectedFacts.HasBrokenLeg
            && collectedFacts.HasAcknowledgement;
    }

    private void StartCompletionSequence()
    {
        if (isCompleting)
        {
            return;
        }

        isCompleting = true;

        SetInputInteractable(false);

        if (finishRoutine != null)
        {
            StopCoroutine(finishRoutine);
        }

        finishRoutine = StartCoroutine(FinishAfterDelay());
    }

    private IEnumerator FinishAfterDelay()
    {
        yield return new WaitForSeconds(finishDelaySeconds);

        if (gameManager != null)
        {
            gameManager.OnMiniGameComplete(completionFeedback);
        }

        finishRoutine = null;
    }

    private static string Normalize(string message)
    {
        return string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim().ToLowerInvariant();
    }

    private static bool ContainsAny(string message, params string[] keywords)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(keywords[i]) && message.Contains(keywords[i]))
            {
                return true;
            }
        }

        return false;
    }
}
