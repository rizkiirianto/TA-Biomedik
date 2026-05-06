using System.Collections;
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
    [SerializeField, TextArea(2, 4)] private string completionFeedback = "Panggilan 112 selesai. Bantuan sedang menuju lokasi.";
    [SerializeField, Min(0.005f)] private float typingSpeed = 0.03f;
    [SerializeField, Min(0f)] private float operatorReplyDelaySeconds = 5f;
    [SerializeField, Min(0f)] private float finishDelaySeconds = 0.9f;

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

        string operatorReply = BuildOperatorReply(messageFacts);
        queuedOperatorReply = operatorReply;

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
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
        if (historyPanel != null && historyPanel.activeSelf)
        {
            ScrollHistoryToBottom();
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
