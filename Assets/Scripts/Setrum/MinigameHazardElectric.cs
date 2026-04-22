using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MinigameHazardElectric : MonoBehaviour, IMiniGame
{
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typewriterSound;
    [SerializeField] private AudioClip matikanSekringSound;
    [SerializeField] private Button advanceButton;
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private float introAutoAdvanceDelay = 1.2f;
    [SerializeField] private string[] dialogueLines = new string[]
    {
        "Apa yang harus kamu lakukan pertama kali?",
        "Gunakan mouse untuk berinteraksi",
    };
    [SerializeField] private Button hitboxTangga;
    [SerializeField] private Button hitboxTetangga;
    [SerializeField] private Button hitboxPintu;
    [SerializeField] private Button hitboxBreaker;
    [SerializeField] private Button hitboxJam;
    [SerializeField] private Button hitboxRuangSamping;
    [SerializeField] private Button hitboxLampu;
    [SerializeField] private GameObject panelBreaker;
    [SerializeField] private RectTransform zonaAtas;
    [SerializeField] private RectTransform zonaTengah;
    [SerializeField] private RectTransform zonaBawah;
    [SerializeField] private List<Image> breakerImages = new List<Image>(); // [0] = Atas, [1] = Tengah, [2] = Bawah
    [SerializeField] private float wrongAnswerDialogDuration = 4f;
    [TextArea]
    [SerializeField] private string wrongTanggaMessage = "Bukan itu. Coba perhatikan sumber listrik yang benar.";
    [TextArea]
    [SerializeField] private string wrongTetanggaMessage = "Bukan tetangga. Fokus pada alat di dalam rumah.";
    [TextArea]
    [SerializeField] private string wrongPintuMessage = "Pintu bukan jawaban yang tepat. Cari sumber listriknya.";
    [TextArea]
    [SerializeField] private string correctBreakerMessage = "Tepat sekali, matikan sumber listrik dengan breaker.";
    [TextArea]
    [SerializeField] private string wrongJamMessage = "Jam tidak ada hubungannya dengan bahaya listrik ini.";
    [TextArea]
    [SerializeField] private string wrongRuangSampingMessage = "Bukan ruang samping. Pilih bagian yang memutus arus listrik.";
    [TextArea]
    [SerializeField] private string wrongLampuMessage = "Lampu bukan sumber utama yang harus dimatikan.";
    
    private int currentDialogIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Coroutine autoAdvanceCoroutine;
    private Coroutine wrongAnswerCoroutine;
    private bool minigameStarted = false;
    private bool minigameCompleted = false;
    private bool interactionPhaseActive = false;
    private GameManager gameManager;
    
    // Breaker panel drag state
    private enum BreakerZone { Atas, Tengah, Bawah }
    private BreakerZone currentBreakerZone = BreakerZone.Atas;
    private bool isDraggingBreaker = false;

    void Start()
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
        if (minigameStarted)
        {
            return;
        }

        minigameStarted = true;

        // Setup dialog panel
        if (dialogPanel != null)
            dialogPanel.SetActive(true);

        // Setup button listener
        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveListener(AdvanceDialog);
            advanceButton.onClick.AddListener(AdvanceDialog);
        }

        SetupHitboxButtons();
        EnableHitboxButtons(false);
        interactionPhaseActive = false;

        // Mulai dialog pertama
        DisplayDialog(0);
    }

    private void SetupHitboxButtons()
    {
        RegisterHitboxButton(hitboxTangga, OnHitboxTanggaClicked);
        RegisterHitboxButton(hitboxTetangga, OnHitboxTetanggaClicked);
        RegisterHitboxButton(hitboxPintu, OnHitboxPintuClicked);
        RegisterHitboxButton(hitboxBreaker, OnHitboxBreakerClicked);
        RegisterHitboxButton(hitboxJam, OnHitboxJamClicked);
        RegisterHitboxButton(hitboxRuangSamping, OnHitboxRuangSampingClicked);
        RegisterHitboxButton(hitboxLampu, OnHitboxLampuClicked);
    }

    private void RegisterHitboxButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void DisplayDialog(int index)
    {
        if (index >= dialogueLines.Length)
        {
            EnterInteractionPhase();
            return;
        }

        interactionPhaseActive = false;
        EnableHitboxButtons(false);

        currentDialogIndex = index;

        // Stop typing coroutine sebelumnya jika ada
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        // Mulai typing effect
        typingCoroutine = StartCoroutine(TypeDialog(dialogueLines[index]));
    }

    private void EnterInteractionPhase()
    {
        HideDialogPanel();
        interactionPhaseActive = true;
        EnableHitboxButtons(true);

        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveListener(AdvanceDialog);
        }
    }

    private IEnumerator TypeDialog(string text)
    {
        isTyping = true;
        dialogText.text = "";

        foreach (char character in text)
        {
            dialogText.text += character;
            yield return new WaitForSeconds(typingSpeed);
            if (typewriterSound != null && audioSource != null && char.IsLetterOrDigit(character))
            {
                audioSource.PlayOneShot(typewriterSound);
            }
        }

        isTyping = false;
        typingCoroutine = null;
        ScheduleAutoAdvance();
    }

    private void ScheduleAutoAdvance()
    {
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
        }

        autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay());
    }

    private IEnumerator AutoAdvanceAfterDelay()
    {
        yield return new WaitForSeconds(introAutoAdvanceDelay);
        autoAdvanceCoroutine = null;

        if (!minigameCompleted)
        {
            DisplayDialog(currentDialogIndex + 1);
        }
    }

    private void AdvanceDialog()
    {
        if (isTyping)
        {
            // Jika masih typing, tampilkan teks lengkap sekaligus
            StopCoroutine(typingCoroutine);
            dialogText.text = dialogueLines[currentDialogIndex];
            isTyping = false;
            typingCoroutine = null;
            ScheduleAutoAdvance();
        }
        else
        {
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            // Lanjut ke dialog berikutnya
            DisplayDialog(currentDialogIndex + 1);
        }
    }

    private void HideDialogPanel()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
    }

    private void ShowDialogPanel(string message)
    {
        if (dialogText != null)
        {
            dialogText.text = message;
        }

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
        }
    }

    private void EnableHitboxButtons(bool state)
    {
        SetButtonInteractable(hitboxTangga, state);
        SetButtonInteractable(hitboxTetangga, state);
        SetButtonInteractable(hitboxPintu, state);
        SetButtonInteractable(hitboxBreaker, state);
        SetButtonInteractable(hitboxJam, state);
        SetButtonInteractable(hitboxRuangSamping, state);
        SetButtonInteractable(hitboxLampu, state);
    }

    private void SetButtonInteractable(Button button, bool state)
    {
        if (button != null)
        {
            button.interactable = state;
        }
    }

    private void HandleWrongChoice(string message)
    {
        if (minigameCompleted || isTyping || !interactionPhaseActive)
        {
            return;
        }

        if (wrongAnswerCoroutine != null)
        {
            StopCoroutine(wrongAnswerCoroutine);
        }

        ShowDialogPanel(message);
        wrongAnswerCoroutine = StartCoroutine(HideWrongChoicePanelAfterDelay());
    }

    private IEnumerator HideWrongChoicePanelAfterDelay()
    {
        yield return new WaitForSeconds(wrongAnswerDialogDuration);
        HideDialogPanel();
        wrongAnswerCoroutine = null;
    }

    private void OnHitboxTanggaClicked()
    {
        if (gameManager != null)
        {
            gameManager.PlayTemporaryCutsceneFromMinigame("JiroKesetrum");
            return;
        }

        HandleWrongChoice(wrongTanggaMessage);
    }

    private void OnHitboxTetanggaClicked()
    {
        if (gameManager != null)
        {
            gameManager.PlayTemporaryCutsceneFromMinigame("JiroKesetrum");
            return;
        }

        HandleWrongChoice(wrongTetanggaMessage);
    }

    private void OnHitboxPintuClicked()
    {
        HandleWrongChoice(wrongPintuMessage);
    }

    private void OnHitboxBreakerClicked()
    {
        if (minigameCompleted || isTyping || !interactionPhaseActive)
        {
            return;
        }

        // Disable semua hitbox selain breaker
        EnableHitboxButtons(false);
        
        // Aktifkan panel breaker
        SetupBreakerPanel();
    }

    private void SetupBreakerPanel()
    {
        if (panelBreaker == null)
        {
            Debug.LogError("Panel Breaker tidak di-assign!");
            return;
        }

        panelBreaker.SetActive(true);
        dialogPanel.SetActive(false);
        currentBreakerZone = BreakerZone.Atas;
        UpdateBreakerImageDisplay();

        // Setup mouse drag detection
        if (zonaAtas != null)
        {
            // Gunakan EventTrigger atau setup drag listener ke zona
            SetupZonaDragListener(zonaAtas, BreakerZone.Atas);
            SetupZonaDragListener(zonaTengah, BreakerZone.Tengah);
            SetupZonaDragListener(zonaBawah, BreakerZone.Bawah);
        }
    }

    private void SetupZonaDragListener(RectTransform zona, BreakerZone zone)
    {
        if (zona == null)
        {
            return;
        }

        // Tambah listener untuk detect mouse di zona ini
        // Ini akan di-trigger saat mouse berada di area zona
        // Kita cek di Update berdasarkan mouse position
    }

    private void UpdateBreakerImageDisplay()
    {
        // Disable semua image dulu
        for (int i = 0; i < breakerImages.Count; i++)
        {
            if (breakerImages[i] != null)
            {
                breakerImages[i].gameObject.SetActive(false);
            }
        }

        // Enable image sesuai zona saat ini
        if ((int)currentBreakerZone < breakerImages.Count && breakerImages[(int)currentBreakerZone] != null)
        {
            breakerImages[(int)currentBreakerZone].gameObject.SetActive(true);
        }
    }

    private BreakerZone GetZoneFromMousePosition()
    {
        Vector2 mousePos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelBreaker.GetComponent<RectTransform>(), 
            mousePos, 
            null, 
            out Vector2 localPos
        );

        // Cek zona berdasarkan Y position
        if (RectTransformUtility.RectangleContainsScreenPoint(zonaAtas, mousePos))
        {
            return BreakerZone.Atas;
        }
        else if (RectTransformUtility.RectangleContainsScreenPoint(zonaTengah, mousePos))
        {
            return BreakerZone.Tengah;
        }
        else if (RectTransformUtility.RectangleContainsScreenPoint(zonaBawah, mousePos))
        {
            return BreakerZone.Bawah;
        }

        return currentBreakerZone;
    }

    private void OnHitboxJamClicked()
    {
        HandleWrongChoice(wrongJamMessage);
    }

    private void OnHitboxRuangSampingClicked()
    {
        HandleWrongChoice(wrongRuangSampingMessage);
    }

    private void OnHitboxLampuClicked()
    {
        HandleWrongChoice(wrongLampuMessage);
    }

    private void CompleteMinigame()
    {
        if (!minigameCompleted)
        {
            minigameCompleted = true;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        // Hide panel breaker
        if (panelBreaker != null)
        {
            panelBreaker.SetActive(false);
        }

        // Hide dialog panel
        HideDialogPanel();

        if (wrongAnswerCoroutine != null)
        {
            StopCoroutine(wrongAnswerCoroutine);
            wrongAnswerCoroutine = null;
        }

        // Cleanup
        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveListener(AdvanceDialog);
        }

        EnableHitboxButtons(false);

        // Notify GameManager jika ada
        if (gameManager != null)
        {
            gameManager.OnMiniGameComplete("Listrik rumah sudah dimatikan");
        }

        // Sembunyikan dan hapus instance prefab minigame ini agar tidak tertinggal di scene
        gameObject.SetActive(false);
        Destroy(gameObject);
        // Atau bisa destroy object ini setelah selesai
        // Destroy(gameObject);
    }

    void Update()
    {
        // Handle breaker panel drag
        if (panelBreaker != null && panelBreaker.activeSelf && !minigameCompleted)
        {
            HandleBreakerDrag();
        }
    }

    private void HandleBreakerDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Cek apakah mouse di area panel breaker
            if (RectTransformUtility.RectangleContainsScreenPoint(panelBreaker.GetComponent<RectTransform>(), Input.mousePosition))
            {
                isDraggingBreaker = true;
                BreakerZone newZone = GetZoneFromMousePosition();
                if (newZone != currentBreakerZone)
                {
                    currentBreakerZone = newZone;
                    UpdateBreakerImageDisplay();
                }
            }
        }
        else if (Input.GetMouseButton(0) && isDraggingBreaker)
        {
            // Update zona saat drag
            BreakerZone newZone = GetZoneFromMousePosition();
            if (newZone != currentBreakerZone)
            {
                currentBreakerZone = newZone;
                UpdateBreakerImageDisplay();
            }
        }
        else if (Input.GetMouseButtonUp(0) && isDraggingBreaker)
        {
            isDraggingBreaker = false;

            // Check apakah dilepas di zona bawah
            if (currentBreakerZone == BreakerZone.Bawah)
            {
                audioSource.PlayOneShot(matikanSekringSound);
                CompleteBreakerPanel();
            }
        }
    }

    private void CompleteBreakerPanel()
    {
        minigameCompleted = true;
        ShowDialogPanel(correctBreakerMessage);
        //HideDialogPanel();
        EnableHitboxButtons(false);
        CompleteMinigame();
    }
}
