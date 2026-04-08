using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
using System.Text;

[System.Serializable]
public class MiniGameRegistry
{
    public string id;
    public GameObject prefab;
}
[System.Serializable]
public class CutsceneRegistry
{
    public string id;
    public GameObject prefab;
}
[System.Serializable]
public class AmbanceAudioRegistry
{
    public string scenarioId;
    public AudioClip ambanceClip;
}

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    private class EpisodeKeyPointRecap
    {
        public string keyPoint;
        public string selectedAnswer;
        public int attempts;
    }

    [Header("Referensi UI Utama")]
    public GameObject quizUIParent;
    public Button skipButtonDevMode;
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
    public Button[] optionButtons2;
    public Sprite[] gambarPortraitKarakter;
    public Image gambarPortraitQuiz;
    public Image gambarPortraitDialog;
    public GameObject optionParents;
    public GameObject optionParents2;
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;
    public Transform canvasTransform;
    public TextMeshProUGUI narrativeText;
    public Image narrativeImage;
    public Image backgroundImage;
    public TextMeshProUGUI scoreText;
    [Tooltip("Panel Button transparan untuk melanjutkan dialog")]
    public Button clickAdvanceButton;
    public GameObject clickAdvancePanel;
    public RectTransform optionParentRect;

    [Header("Episode Recap UI")]
    public GameObject recapPanel;
    public TextMeshProUGUI recapKeyPointsText;

    [Header("Quiz Timer")]
    public TextMeshProUGUI quizTimerText;
    public Image quizTimerFillImage;
    [Min(0f)] public float defaultQuizTimeSeconds = 15f;
    [Min(0f)] public float defaultLowTimeCueSeconds = 5f;
    public Color timerOverlayColor = new Color(1f, 0f, 0f, 1f);
    [Range(0f, 1f)] public float timerOverlayBaseAlpha = 0f;
    [Range(0f, 1f)] public float timerOverlayPulseMaxAlpha = 0.2f;
    [Range(1f, 20f)] public float warningPulseSpeed = 8f;

    [Header("Quiz Timer Audio")]
    public AudioSource clockAudioSource;
    public AudioSource heartbeatAudioSource;
    [Min(0.1f)] public float clockTickIntervalSeconds = 1f;
    [Min(0.1f)] public float heartbeatTickIntervalSeconds = 1f;

    public Camera mainSceneCamera;

    [Header("Registrasi Prefab Minigame")]
    public List<MiniGameRegistry> miniGameRegistry;
    public List<CutsceneRegistry> cutsceneRegistry;

    [Header("Ambiance Audio untuk Setiap Episode")]
    public AudioSource ambianceAudioSource;
    public List<AmbanceAudioRegistry> ambianceAudioRegistry;

    [Header("Dialog SFX")]
    public AudioSource dialogSfxAudioSource;

    // Variabel Logika Game
    private QuizData quizData;
    private int currentStepIndex = 0;
    private GameObject activeMiniGameInstance;
    private GameObject activeCutsceneInstance;
    private bool isWaitingForAdvance;
    private bool isQuizFinished = false;
    private int totalScore = 0; // --- TAMBAHAN: Menyimpan total skor pemain
    private int currentQuestionAttempts = 0; // --- TAMBAHAN: Melacak percobaan di kuis saat ini
    private float minigameStartTime = 0f; // --- TAMBAHAN: Mencatat waktu mulai minigame
    private Coroutine revealOptionsCoroutine;
    private GameObject activeOptionParent;
    private Button[] activeOptionButtons = Array.Empty<Button>();
    private Coroutine quizTimerCoroutine;
    private float currentQuizTimeRemaining;
    private float currentQuizTimeLimit;
    private float currentLowTimeCueThreshold;
    private float nextClockTickAtUnscaledTime;
    private float nextHeartbeatTickAtUnscaledTime;
    private bool isQuizTimerRunning;
    private Color questionTextBaseColor = Color.white;
    private readonly Dictionary<string, AudioClip> dialogSfxCache = new Dictionary<string, AudioClip>();
    private readonly List<EpisodeKeyPointRecap> episodeRecaps = new List<EpisodeKeyPointRecap>();
    private int totalQuizAttempts = 0;
    [Header("Quiz Scoring")]
    [Min(0)] public int pointsFirstAttempt = 100;
    [Min(0)] public int pointsSecondAttempt = 70;
    [Min(0)] public int pointsThirdOrMoreAttempt = 40;
    private int finalNormalizedScore = 0;

    void Start()
    {
        feedbackPanel.SetActive(false);
        if (recapPanel != null)
        {
            recapPanel.SetActive(false);
        }
        clickAdvanceButton.gameObject.SetActive(false);
        clickAdvanceButton.onClick.AddListener(OnAdvanceClicked);
        if (questionText != null)
        {
            questionTextBaseColor = questionText.color;
        }
        LoadQuizData();
        ShowStep(currentStepIndex);
        totalScore = 0;
        ResetEpisodeRecapData();
        UpdateScoreText();
        LoadQuizData();
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        // Dev mode: tekan K untuk skip satu step
        if (Input.GetKeyDown(KeyCode.K))
        {
            OnSkipButtonClicked();
        }
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Skor Quiz: " + totalScore;
        }
    }

    private void LoadBackgroundImage (string imageName)
    {
        if (backgroundImage != null && !string.IsNullOrEmpty(imageName))
        {
            // Load sprite dari folder Resources
            Sprite newSprite = Resources.Load<Sprite>(imageName.Replace(".png", "").Replace(".jpg", ""));
            if (newSprite != null)
            {
                backgroundImage.sprite = newSprite;
                backgroundImage.gameObject.SetActive(true);
                Debug.Log($"Berhasil memuat gambar naratif: {imageName}");
            }
            else
            {
                Debug.LogWarning($"Gambar naratif tidak ditemukan: {imageName}");
                backgroundImage.gameObject.SetActive(false);
            }
        }
        else
        {
            // Sembunyikan gambar jika tidak ada nama file yang diberikan
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(false);
            }
        }
    }
    private void LoadNarrativeImage(string imageName)
    {
        if (narrativeImage != null && !string.IsNullOrEmpty(imageName))
        {
            // Load sprite dari folder Resources
            Sprite newSprite = Resources.Load<Sprite>(imageName.Replace(".png", "").Replace(".jpg", ""));
            if (newSprite != null)
            {
                narrativeImage.sprite = newSprite;
                narrativeImage.gameObject.SetActive(true);
                Debug.Log($"Berhasil memuat gambar naratif: {imageName}");
            }
            else
            {
                Debug.LogWarning($"Gambar naratif tidak ditemukan: {imageName}");
                narrativeImage.gameObject.SetActive(false);
            }
        }
        else
        {
            // Sembunyikan gambar jika tidak ada nama file yang diberikan
            if (narrativeImage != null)
            {
                narrativeImage.gameObject.SetActive(false);
            }
        }
    }

    private void LoadQuizData()
    {
        ResetEpisodeRecapData();

        string scenarioName = PlayerPrefs.GetString("SelectedScenario", "Scenario1");
        TextAsset jsonFile = Resources.Load<TextAsset>(scenarioName);
        if (jsonFile != null)
        {
            quizData = JsonUtility.FromJson<QuizData>(jsonFile.text);
        }
        else
        {
            Debug.LogError($"Gagal memuat file '{scenarioName}.json' dari folder Resources!");
        }
        
        PlayAmbianceForScenario(scenarioName);
    }

    private void PlayAmbianceForScenario(string scenarioName)
    {
        if (ambianceAudioSource == null)
        {
            Debug.LogWarning("Ambiance AudioSource tidak diset di GameManager!");
            return;
        }

        AmbanceAudioRegistry ambianceEntry = ambianceAudioRegistry
            .FirstOrDefault(audio => audio.scenarioId == scenarioName);

        if (ambianceEntry != null && ambianceEntry.ambanceClip != null)
        {
            ambianceAudioSource.clip = ambianceEntry.ambanceClip;
            ambianceAudioSource.loop = true;
            ambianceAudioSource.Play();
            Debug.Log($"Memainkan ambiance untuk skenario: {scenarioName}");
        }
        else
        {
            Debug.LogWarning($"Ambiance audio untuk skenario '{scenarioName}' tidak ditemukan di registry!");
            if (ambianceAudioSource.isPlaying)
            {
                ambianceAudioSource.Stop();
            }
        }
    }

    private void ShowStep(int stepIndex)
    {
        StopQuizTimer(resetVisuals: true);

        if (activeMiniGameInstance != null) Destroy(activeMiniGameInstance);
        if (activeCutsceneInstance != null) Destroy(activeCutsceneInstance);

        if (quizData == null || stepIndex >= quizData.steps.Count)
        {
            EndQuiz();
            return;
        }

        Step currentStep = quizData.steps[stepIndex];
        PlayDialogSfx(currentStep.dialogSfx);

        if (currentStep.stepType == "quiz")
        {
            ShowQuiz(currentStep);
        }
        else if (currentStep.stepType == "minigame")
        {
            StartMiniGame(currentStep);
        }
        else if (currentStep.stepType == "dialog") // LOGIKA BARU
        {
            ShowDialog(currentStep);
        }
        else if (currentStep.stepType == "cutscene")
        {
            ShowCutscene(currentStep);
        }
    }
    
    private void ShowDialog(Step step)
    {
        StopQuizTimer(resetVisuals: true);
        quizUIParent.SetActive(true);
        feedbackPanel.SetActive(false);
        clickAdvancePanel.SetActive(true);
        SetActiveOptionParent(null);
        HideAllOptionButtons();

        LoadNarrativeImage(step.narrativeImage);
        LoadPortraitDialogue(step.gambarPortrait);
        
        // Tampilkan teks dialog
        // Jika ada speakerName, formatnya: "Nama: Teks"
        if (!string.IsNullOrEmpty(step.speakerName))
        {
            narrativeText.text = $"<b>{step.speakerName}</b>: {step.instruction}";
             // Gunakan narrativeText (area narasi) atau questionText tergantung preferensi UI Anda
             // Disini saya asumsikan narrativeText lebih cocok untuk cerita panjang
        }
        else
        {
            narrativeText.text = step.instruction;
        }

        // Sembunyikan pertanyaan kuis agar tidak bingung
        questionText.text = ""; 

        // Langsung siapkan tombol advance agar pemain bisa klik layar untuk lanjut
        PrepareToAdvance(); 
    }

    private void PlayDialogSfx(string clipName)
    {
        if (dialogSfxAudioSource == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(clipName))
        {
            if (dialogSfxAudioSource.isPlaying)
            {
                dialogSfxAudioSource.Stop();
            }
            return;
        }

        string normalizedClipName = clipName.Trim();
        if (!dialogSfxCache.TryGetValue(normalizedClipName, out AudioClip clip))
        {
            string resourcePath = normalizedClipName.Replace(".mp3", "").Replace(".wav", "").Replace(".ogg", "");
            clip = Resources.Load<AudioClip>(resourcePath);
            dialogSfxCache[normalizedClipName] = clip;
        }

        if (clip == null)
        {
            Debug.LogWarning($"Dialog SFX tidak ditemukan di Resources: {normalizedClipName}");
            return;
        }

        if (dialogSfxAudioSource.isPlaying)
        {
            dialogSfxAudioSource.Stop();
        }

        dialogSfxAudioSource.clip = clip;
        dialogSfxAudioSource.Play();
    }
    private void LoadPortraitImage(int index)
    {
        if (gambarPortraitQuiz == null) return;
        if (index >= 0 && gambarPortraitKarakter != null && index < gambarPortraitKarakter.Length)
        {
            gambarPortraitQuiz.sprite = gambarPortraitKarakter[index];
            gambarPortraitQuiz.gameObject.SetActive(true);
        }
        else
        {
            gambarPortraitQuiz.gameObject.SetActive(false);
        }
    }
    private void LoadPortraitDialogue(int index)
    {
        if (gambarPortraitDialog == null) return;
        if (index >= 0 && gambarPortraitKarakter != null && index < gambarPortraitKarakter.Length)
        {
            gambarPortraitDialog.sprite = gambarPortraitKarakter[index];
            gambarPortraitDialog.gameObject.SetActive(true);
        }
        else
        {
            gambarPortraitDialog.gameObject.SetActive(false);
        }
    }

    private void ShowQuiz(Step quizStep)
    {
        StopQuizTimer(resetVisuals: true);
        quizUIParent.SetActive(true);
        feedbackPanel.SetActive(false);

        activeOptionParent = ResolveOptionParent(quizStep);
        SetActiveOptionParent(activeOptionParent);
        activeOptionButtons = ResolveOptionButtons(activeOptionParent);

        questionText.text = quizStep.instruction;

        LoadBackgroundImage(quizStep.backgroundImage);
        LoadPortraitDialogue(-1);
        LoadPortraitImage(quizStep.gambarPortrait);

        RectTransform targetOptionParentRect = activeOptionParent != null
            ? activeOptionParent.GetComponent<RectTransform>()
            : optionParentRect;

        if (targetOptionParentRect != null)
        {
            Vector2 currentPos = targetOptionParentRect.anchoredPosition;
            currentPos.x = quizStep.optionParentPosX;
            if (!float.IsNaN(quizStep.optionParentPosY))
            {
                currentPos.y = quizStep.optionParentPosY;
            }
            targetOptionParentRect.anchoredPosition = currentPos;
        }

        currentQuestionAttempts = 0;

        // Selalu pastikan tombol pilihan aktif setiap kali step kuis baru ditampilkan.
        SetOptionButtonsInteractable(true);

        // Setup semua tombol, tapi sembunyikan dulu — akan muncul satu per satu
        int activeCount = 0;
        for (int i = 0; i < activeOptionButtons.Length; i++)
        {
            if (i < quizStep.options.Count)
            {
                activeOptionButtons[i].gameObject.SetActive(false);
                TextMeshProUGUI buttonLabel = activeOptionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonLabel != null)
                {
                    buttonLabel.text = quizStep.options[i].text;
                }
                int optionIndex = i;
                activeOptionButtons[i].onClick.RemoveAllListeners();
                activeOptionButtons[i].onClick.AddListener(() => OnOptionSelected(optionIndex));
                activeCount++;
            }
            else
            {
                activeOptionButtons[i].gameObject.SetActive(false);
            }
        }

        // Mulai efek muncul satu per satu
        if (revealOptionsCoroutine != null) StopCoroutine(revealOptionsCoroutine);
        revealOptionsCoroutine = StartCoroutine(RevealOptionsOneByOne(activeCount));

        ConfigureTimerForQuizStep(quizStep);
    }

    private IEnumerator RevealOptionsOneByOne(int count)
    {
        for (int i = 0; i < count && i < activeOptionButtons.Length; i++)
        {
            // Specify UnityEngine here
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.4f, 0.9f));
            activeOptionButtons[i].gameObject.SetActive(true);
        }

        BeginQuizTimerIfNeeded();
        revealOptionsCoroutine = null;
    }

    private void StartMiniGame(Step miniGameStep)
    {
        StopQuizTimer(resetVisuals: true);
        quizUIParent.SetActive(false);
        mainSceneCamera.gameObject.SetActive(false);
        
        // 1. Cari prefab berdasarkan ID di Registry (tetap perlu ini untuk spawn)
        MiniGameRegistry gameToStart = miniGameRegistry.FirstOrDefault(mg => mg.id == miniGameStep.minigameID);

        if (gameToStart != null && gameToStart.prefab != null)
        {
            minigameStartTime = Time.time;

            // Cek apakah prefab memiliki komponen RectTransform (berarti ini adalah elemen UI)
            if (gameToStart.prefab.GetComponent<RectTransform>() != null)
            {
                Debug.Log($"Spawning UI Minigame: {miniGameStep.minigameID}");
                
                // 1. Pastikan Main Camera menyala untuk merender Canvas
                mainSceneCamera.gameObject.SetActive(true); 
                
                // 2. Spawn Prefab di DALAM Canvas
                activeMiniGameInstance = Instantiate(gameToStart.prefab, canvasTransform);
                
                // 3. Pastikan posisinya di tengah layar
                RectTransform rt = activeMiniGameInstance.GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.zero; 
                rt.localScale = Vector3.one;
            }
            else
            {
                Debug.Log($"Spawning World Minigame: {miniGameStep.minigameID}");
                
                // 1. Matikan Main Camera karena minigame world bawa kamera sendiri
                mainSceneCamera.gameObject.SetActive(false); 
                
                // 2. Spawn Prefab di LUAR Canvas (di root)
                activeMiniGameInstance = Instantiate(gameToStart.prefab, Vector3.zero, Quaternion.identity);
            }

            // --- MAGIC HAPPENS HERE ---
            // Kita tidak peduli nama script-nya apa, kita cuma cari 'IMiniGame'
            IMiniGame gameScript = activeMiniGameInstance.GetComponent<IMiniGame>();

            if (gameScript != null)
            {
                gameScript.BeginGame(this); // Satu perintah untuk semua jenis game!
            }
            else
            {
                Debug.LogError($"Prefab {miniGameStep.minigameID} tidak punya script yang implement IMiniGame!");
            }
        }
        else
        {
            Debug.LogError($"Prefab minigame ID '{miniGameStep.minigameID}' tidak ditemukan di Registry!");
        }
    }

    public void OnOptionSelected(int optionIndex)
    {
        // Mainkan suara klik button dari AudioSource pada button yang diklik
        Button[] buttonsForCurrentQuiz = GetCurrentOptionButtons();
        AudioSource buttonAudio = null;
        if (optionIndex >= 0 && optionIndex < buttonsForCurrentQuiz.Length)
        {
            buttonAudio = buttonsForCurrentQuiz[optionIndex].GetComponent<AudioSource>();
        }
        if (buttonAudio != null)
        {
            buttonAudio.Play();
        }
        
        RegisterQuizAttempt();
        Step currentStep = quizData.steps[currentStepIndex];
        Option selectedOption = currentStep.options[optionIndex];
        feedbackText.text = selectedOption.feedback;
        
        narrativeText.text = selectedOption.narrative;

        // Load gambar naratif jika ada
        LoadNarrativeImage(selectedOption.narrativeImage);

        // Jika opsi menyediakan portrait, perbarui portrait yang ditampilkan.
        if (selectedOption.gambarPortrait >= 0)
        {
            LoadPortraitDialogue(selectedOption.gambarPortrait);
            LoadPortraitImage(selectedOption.gambarPortrait);
        }
        else
        {
            // Jangan mewarisi portrait opsi sebelumnya jika opsi ini tidak punya portrait.
            LoadPortraitDialogue(-1);
            LoadPortraitImage(currentStep.gambarPortrait);
        }

        if (selectedOption.isCorrect)
        {
            StopQuizTimer(resetVisuals: true);

            // Jangan tampilkan feedback panel untuk jawaban benar
            feedbackPanel.SetActive(false);
            
            int scoreGained = GetQuizScoreForAttempt(currentQuestionAttempts);
            totalScore += scoreGained;
            UpdateScoreText();
            RegisterKeyPointRecap(currentStep, selectedOption, currentQuestionAttempts);
            Debug.Log($"Jawaban Benar, Dapat skor {scoreGained}. Total skor : {totalScore}");
            PrepareToAdvance();
        }
        else
        {
            // Tampilkan feedback panel hanya untuk jawaban salah
            feedbackPanel.SetActive(true);
            if (activeOptionParent != null)
            {
                activeOptionParent.SetActive(false);
            }
            // Otomatis sembunyikan feedback panel setelah 2 detik
            StartCoroutine(HideFeedbackPanelAfterDelay(2f));
            StartCoroutine(ShowOptionParentPanelAfterDelay(2f, activeOptionParent));
        }
    }

    public void OnMiniGameComplete(string successFeedback)
    {
        StopQuizTimer(resetVisuals: true);
        mainSceneCamera.gameObject.SetActive(true);
        /*
        float completionTime = Time.time - minigameStartTime;
        Step currentStep = quizData.steps[currentStepIndex];
        int scoreGained = 0;
        if (completionTime <= currentStep.goldTime)
        {
            scoreGained = 250; // Skor Emas
            Debug.Log($"Minigame Selesai (Emas)! Waktu: {completionTime:F2}s. Dapat {scoreGained} poin.");
        }
        else if (completionTime <= currentStep.silverTime)
        {
            scoreGained = 150; // Skor Perak
            Debug.Log($"Minigame Selesai (Perak)! Waktu: {completionTime:F2}s. Dapat {scoreGained} poin.");
        }
        else
        {
             scoreGained = 50; // Skor Perunggu
             Debug.Log($"Minigame Selesai (Perunggu)! Waktu: {completionTime:F2}s. Dapat {scoreGained} poin.");
        }
        totalScore += scoreGained;
        UpdateScoreText();
        */
        // Kembalikan player model dan UI
        quizUIParent.SetActive(true); 
        clickAdvancePanel.SetActive(false); 
        narrativeText.text = quizData.steps[currentStepIndex].instruction; // Tampilkan instruksi minigame sebagai narasi
        HideAllOptionButtons(); // Pastikan tombol pilihan tersembunyi

        // Load gambar naratif untuk minigame jika ada
        LoadNarrativeImage(quizData.steps[currentStepIndex].narrativeImage);
        LoadBackgroundImage(quizData.steps[currentStepIndex].backgroundImage);

        feedbackText.text = successFeedback;
        feedbackPanel.SetActive(true);
        
        Debug.Log("Memanggil PrepareToAdvance untuk lanjut ke step berikutnya");
        PrepareToAdvance();
    }

    private void ShowCutscene(Step cutsceneStep)
    {   
        StopQuizTimer(resetVisuals: true);
        quizUIParent.SetActive(false);
        feedbackPanel.SetActive(false);
        clickAdvancePanel.SetActive(false);
        clickAdvanceButton.gameObject.SetActive(false);
        isWaitingForAdvance = false;
        LoadPortraitImage(-1); // Sembunyikan portrait di cutscene
        LoadPortraitDialogue(-1);

        // Cari prefab cutscene berdasarkan ID
        CutsceneRegistry cutsceneToStart = cutsceneRegistry
            .FirstOrDefault(cs => cs.id == cutsceneStep.cutsceneID);

        if (cutsceneToStart == null || cutsceneToStart.prefab == null)
        {
            Debug.LogError($"Prefab cutscene ID '{cutsceneStep.cutsceneID}' tidak ditemukan di Cutscene Registry!");
            PrepareToAdvance();
            return;
        }

        // Spawn: logika sama seperti minigame (UI vs world)
        if (cutsceneToStart.prefab.GetComponent<RectTransform>() != null)
        {
            Debug.Log($"Spawning UI Cutscene: {cutsceneStep.cutsceneID}");

            // Pastikan camera ON untuk render Canvas
            mainSceneCamera.gameObject.SetActive(true);

            activeCutsceneInstance = Instantiate(cutsceneToStart.prefab, canvasTransform);

            RectTransform rt = activeCutsceneInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
        else
        {
            Debug.Log($"Spawning World Cutscene: {cutsceneStep.cutsceneID}");

            // Kalau cutscene world bawa kamera sendiri, matikan main camera (opsional).
            // Kalau cutscene world TIDAK bawa kamera sendiri, biarkan main camera tetap aktif.
            // Untuk konsisten dengan minigame kamu:
            mainSceneCamera.gameObject.SetActive(false);

            activeCutsceneInstance = Instantiate(cutsceneToStart.prefab, Vector3.zero, Quaternion.identity);
        }

        // Cari komponen ICutscene dan jalankan
        ICutscene cutsceneScript = activeCutsceneInstance.GetComponent<ICutscene>();
        if (cutsceneScript != null)
        {
            cutsceneScript.BeginCutscene(this);
        }
        else
        {
            Debug.LogError($"Prefab Cutscene {cutsceneStep.cutsceneID} tidak punya script yang implement ICutscene!");
            PrepareToAdvance();
        }
    }

    public void OnCutsceneComplete(string optionalFeedback = "")
    {
        // Kembalikan main camera (kalau tadi dimatikan)
        mainSceneCamera.gameObject.SetActive(true);

        // (opsional) tampilkan feedback singkat
        if (!string.IsNullOrEmpty(optionalFeedback))
        {
            quizUIParent.SetActive(true);
            feedbackText.text = optionalFeedback;
            feedbackPanel.SetActive(true);
        }
        else
        {
            feedbackPanel.SetActive(false);
        }

        GoToNextStep();
    }

    private void PrepareToAdvance()
    {
        StopQuizTimer(resetVisuals: true);
        isWaitingForAdvance = true;
        SetOptionButtonsInteractable(false);
        clickAdvanceButton.gameObject.SetActive(true);
        Debug.Log("PrepareToAdvance dipanggil - tombol advance aktif, menunggu klik pemain");
    }

    private void OnAdvanceClicked()
    {
        Debug.Log($"OnAdvanceClicked dipanggil - isQuizFinished: {isQuizFinished}, isWaitingForAdvance: {isWaitingForAdvance}");
        
        // Jika skenario sudah selesai, kembali ke menu
        if (isQuizFinished)
        {
            Debug.Log("Kembali ke menu utama");
            ReturnToScenarioMenu();
        }
        // Jika tidak, lanjut ke step berikutnya
        else if (isWaitingForAdvance)
        {
            Debug.Log("Melanjutkan ke step berikutnya");
            isWaitingForAdvance = false;
            clickAdvanceButton.gameObject.SetActive(false);
            GoToNextStep();
        }
    }

    // Dev mode: method untuk skip satu step
    private void OnSkipButtonClicked()
    {
        Debug.Log($"[DEV MODE] Skip button diklik - meloncati step {currentStepIndex}");
        
        // Pastikan mainSceneCamera hidup kembali (untuk cutscene yang meng-disable camera)
        mainSceneCamera.gameObject.SetActive(true);
        
        isWaitingForAdvance = false;
        clickAdvanceButton.gameObject.SetActive(false);
        GoToNextStep();
    }

    private void GoToNextStep()
    {
        currentStepIndex++;
        Debug.Log($"GoToNextStep dipanggil - pindah ke step index: {currentStepIndex}");
        ShowStep(currentStepIndex);
    }

    private void SetOptionButtonsInteractable(bool state)
    {
        foreach (Button btn in GetCurrentOptionButtons())
        {
            btn.interactable = state;
        }
    }

    private void EndQuiz()
    {
        StopQuizTimer(resetVisuals: true);
        finalNormalizedScore = CalculateFinalNormalizedScore();
        isQuizFinished = true; // Set penanda bahwa skenario selesai
        quizUIParent.SetActive(true);
        if (recapPanel != null)
        {
            recapPanel.SetActive(true);
        }
        clickAdvanceButton.gameObject.SetActive(true); // Tampilkan tombol untuk kembali ke menu
        isWaitingForAdvance = false; // Pastikan ini false agar tidak menjalankan GoToNextStep

        // Tampilkan recap episode
        narrativeText.text = $"Kamu berhasil! Skenario selesai. Skor akhir kamu: {finalNormalizedScore}/100. Berikut recap keputusanmu.";
        questionText.text = "Recap Episode";
        feedbackPanel.SetActive(false);
        HideAllOptionButtons();
        if (scoreText != null)
        {
            scoreText.text = $"Skor: {finalNormalizedScore}/100";
        }
        BuildAndShowEpisodeRecap();
    }

    // Ganti fungsi ReturnToScenarioMenu di GameManager.cs
    private void ReturnToScenarioMenu()
    {
        // Memuat scene dengan nama "MainMenu". Pastikan nama ini sama persis
        // dengan nama file scene Anda di Unity Project.
        SceneManager.LoadScene("MainMenu");
    }

    // Coroutine untuk menyembunyikan feedback panel setelah delay
    private IEnumerator HideFeedbackPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        feedbackPanel.SetActive(false);
    }

    private IEnumerator ShowOptionParentPanelAfterDelay(float delay, GameObject optionParentToShow)
    {
        yield return new WaitForSeconds(delay);
        if (optionParentToShow != null)
        {
            optionParentToShow.SetActive(true);
        }
    }

    private void ConfigureTimerForQuizStep(Step quizStep)
    {
        currentQuizTimeLimit = quizStep.quizTimeSeconds > 0f ? quizStep.quizTimeSeconds : defaultQuizTimeSeconds;
        currentLowTimeCueThreshold = quizStep.lowTimeCueSeconds > 0f
            ? quizStep.lowTimeCueSeconds
            : defaultLowTimeCueSeconds;

        if (currentLowTimeCueThreshold > currentQuizTimeLimit)
        {
            currentLowTimeCueThreshold = currentQuizTimeLimit;
        }

        currentQuizTimeRemaining = currentQuizTimeLimit;
        UpdateTimerVisuals();
    }

    private void BeginQuizTimerIfNeeded()
    {
        if (currentQuizTimeLimit <= 0f)
        {
            StopTimerAudio();
            HideTimerVisuals();
            return;
        }

        StopQuizTimer(resetVisuals: false);
        isQuizTimerRunning = true;
        ResetTimerAudioTickSchedule();
        quizTimerCoroutine = StartCoroutine(RunQuizTimer());
    }

    private IEnumerator RunQuizTimer()
    {
        while (isQuizTimerRunning && currentQuizTimeRemaining > 0f)
        {
            currentQuizTimeRemaining -= Time.deltaTime;
            if (currentQuizTimeRemaining < 0f)
            {
                currentQuizTimeRemaining = 0f;
            }

            UpdateTimerVisuals();
            yield return null;
        }

        quizTimerCoroutine = null;
        if (!isQuizTimerRunning)
        {
            yield break;
        }

        isQuizTimerRunning = false;
        HandleQuizTimeExpired();
    }

    private void HandleQuizTimeExpired()
    {
        StopTimerAudio();

        if (quizData == null || currentStepIndex < 0 || currentStepIndex >= quizData.steps.Count)
        {
            return;
        }

        Step currentStep = quizData.steps[currentStepIndex];
        if (currentStep.stepType != "quiz")
        {
            return;
        }

        RegisterQuizAttempt();
        SetOptionButtonsInteractable(false);

        string timeoutMessage = string.IsNullOrWhiteSpace(currentStep.timeoutFeedback)
            ? "Waktu habis! Coba lagi."
            : currentStep.timeoutFeedback;

        feedbackText.text = timeoutMessage;
        feedbackPanel.SetActive(true);

        if (activeOptionParent != null)
        {
            activeOptionParent.SetActive(false);
        }

        StartCoroutine(HideFeedbackPanelAfterDelay(2f));
        StartCoroutine(ShowOptionParentPanelAfterDelay(2f, activeOptionParent));
        StartCoroutine(RestartQuizTimerAfterDelay(2f));
    }

    private IEnumerator RestartQuizTimerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (quizData == null || currentStepIndex < 0 || currentStepIndex >= quizData.steps.Count)
        {
            yield break;
        }

        Step currentStep = quizData.steps[currentStepIndex];
        if (currentStep.stepType != "quiz")
        {
            yield break;
        }

        SetOptionButtonsInteractable(true);
        ConfigureTimerForQuizStep(currentStep);
        BeginQuizTimerIfNeeded();
    }

    private void StopQuizTimer(bool resetVisuals)
    {
        isQuizTimerRunning = false;
        StopTimerAudio();

        if (quizTimerCoroutine != null)
        {
            StopCoroutine(quizTimerCoroutine);
            quizTimerCoroutine = null;
        }

        if (resetVisuals)
        {
            ResetTimerVisualState();
        }
    }

    private void UpdateTimerVisuals()
    {
        if (currentQuizTimeLimit <= 0f)
        {
            StopTimerAudio();
            HideTimerVisuals();
            return;
        }

        float normalized = currentQuizTimeLimit > 0f
            ? Mathf.Clamp01(currentQuizTimeRemaining / currentQuizTimeLimit)
            : 0f;

        bool isWarning = currentLowTimeCueThreshold > 0f && currentQuizTimeRemaining <= currentLowTimeCueThreshold;
        float pulse = isWarning ? (Mathf.Sin(Time.unscaledTime * warningPulseSpeed) + 1f) * 0.5f : 0f;

        UpdateTimerAudio(isWarning);

        if (quizTimerText != null)
        {
            quizTimerText.gameObject.SetActive(true);
            quizTimerText.text = $"{Mathf.CeilToInt(currentQuizTimeRemaining)}";
        }

        if (quizTimerFillImage != null)
        {
            quizTimerFillImage.gameObject.SetActive(true);
            if (quizTimerFillImage.type == Image.Type.Filled)
            {
                quizTimerFillImage.fillAmount = normalized;
            }

            float overlayAlpha = isWarning
                ? Mathf.Lerp(0f, timerOverlayPulseMaxAlpha, pulse)
                : timerOverlayBaseAlpha;

            Color overlayColor = timerOverlayColor;
            overlayColor.a = overlayAlpha;
            quizTimerFillImage.color = overlayColor;
        }
    }

    private void HideTimerVisuals()
    {
        if (quizTimerText != null)
        {
            quizTimerText.gameObject.SetActive(false);
        }

        if (quizTimerFillImage != null)
        {
            Color overlayColor = timerOverlayColor;
            overlayColor.a = 0f;
            quizTimerFillImage.color = overlayColor;
            quizTimerFillImage.gameObject.SetActive(false);
        }
    }

    private void ResetTimerVisualState()
    {
        currentQuizTimeLimit = 0f;
        currentQuizTimeRemaining = 0f;
        currentLowTimeCueThreshold = 0f;
        HideTimerVisuals();
    }

    private void ResetTimerAudioTickSchedule()
    {
        float now = Time.unscaledTime;
        nextClockTickAtUnscaledTime = now;
        nextHeartbeatTickAtUnscaledTime = now;
    }

    private void UpdateTimerAudio(bool isWarning)
    {
        if (!isQuizTimerRunning)
        {
            return;
        }

        float now = Time.unscaledTime;

        if (clockAudioSource != null && clockAudioSource.clip != null && now >= nextClockTickAtUnscaledTime)
        {
            clockAudioSource.PlayOneShot(clockAudioSource.clip);
            nextClockTickAtUnscaledTime = now + clockTickIntervalSeconds;
        }

        if (!isWarning)
        {
            nextHeartbeatTickAtUnscaledTime = now;
            return;
        }

        if (heartbeatAudioSource != null && heartbeatAudioSource.clip != null && now >= nextHeartbeatTickAtUnscaledTime)
        {
            heartbeatAudioSource.PlayOneShot(heartbeatAudioSource.clip);
            nextHeartbeatTickAtUnscaledTime = now + heartbeatTickIntervalSeconds;
        }
    }

    private void StopTimerAudio()
    {
        if (clockAudioSource != null)
        {
            clockAudioSource.Stop();
        }

        if (heartbeatAudioSource != null)
        {
            heartbeatAudioSource.Stop();
        }
    }

    private GameObject ResolveOptionParent(Step step)
    {
        string target = (step.optionParentTarget ?? string.Empty).Trim().ToLowerInvariant();

        if (target == "optionparents2")
        {
            return optionParents2 != null ? optionParents2 : optionParents;
        }

        return optionParents;
    }

    private void SetActiveOptionParent(GameObject target)
    {
        if (optionParents != null)
        {
            optionParents.SetActive(target == optionParents);
        }

        if (optionParents2 != null)
        {
            optionParents2.SetActive(target == optionParents2);
        }
    }

    private Button[] ResolveOptionButtons(GameObject targetParent)
    {
        if (targetParent == optionParents2 && optionButtons2 != null && optionButtons2.Length > 0)
        {
            return optionButtons2;
        }

        if (targetParent == optionParents && optionButtons != null && optionButtons.Length > 0)
        {
            return optionButtons;
        }

        return DiscoverOptionButtons(targetParent);
    }

    private Button[] DiscoverOptionButtons(GameObject parent)
    {
        if (parent == null)
        {
            return Array.Empty<Button>();
        }

        return parent
            .GetComponentsInChildren<Button>(true)
            .OrderBy(btn => btn.transform.GetSiblingIndex())
            .ToArray();
    }

    private Button[] GetCurrentOptionButtons()
    {
        if (activeOptionButtons != null && activeOptionButtons.Length > 0)
        {
            return activeOptionButtons;
        }

        return optionButtons ?? Array.Empty<Button>();
    }

    private void HideAllOptionButtons()
    {
        foreach (Button btn in GetAllConfiguredOptionButtons())
        {
            if (btn != null)
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerable<Button> GetAllConfiguredOptionButtons()
    {
        HashSet<Button> uniqueButtons = new HashSet<Button>();

        if (optionButtons != null)
        {
            foreach (Button btn in optionButtons)
            {
                if (btn != null) uniqueButtons.Add(btn);
            }
        }

        if (optionButtons2 != null)
        {
            foreach (Button btn in optionButtons2)
            {
                if (btn != null) uniqueButtons.Add(btn);
            }
        }

        foreach (Button btn in DiscoverOptionButtons(optionParents))
        {
            if (btn != null) uniqueButtons.Add(btn);
        }

        foreach (Button btn in DiscoverOptionButtons(optionParents2))
        {
            if (btn != null) uniqueButtons.Add(btn);
        }

        return uniqueButtons;
    }

    private void RegisterQuizAttempt()
    {
        currentQuestionAttempts++;
        totalQuizAttempts++;
    }

    private void RegisterKeyPointRecap(Step step, Option selectedOption, int attempts)
    {
        if (step == null || selectedOption == null)
        {
            return;
        }

        EpisodeKeyPointRecap recap = new EpisodeKeyPointRecap
        {
            keyPoint = string.IsNullOrWhiteSpace(step.instruction) ? "(Tanpa deskripsi pertanyaan)" : step.instruction.Trim(),
            selectedAnswer = string.IsNullOrWhiteSpace(selectedOption.text) ? "(Tanpa jawaban)" : selectedOption.text.Trim(),
            attempts = Mathf.Max(1, attempts)
        };

        episodeRecaps.Add(recap);
    }

    private void BuildAndShowEpisodeRecap()
    {
        if (recapKeyPointsText == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Skor akhir (normalisasi): {finalNormalizedScore}/100");
        builder.AppendLine($"Skor quiz mentah: {totalScore}");
        builder.AppendLine($"Total percobaan menjawab: {totalQuizAttempts}");
        builder.AppendLine();

        if (episodeRecaps.Count == 0)
        {
            builder.AppendLine("Belum ada key point yang tercatat.");
            recapKeyPointsText.text = builder.ToString();
            return;
        }

        for (int i = 0; i < episodeRecaps.Count; i++)
        {
            EpisodeKeyPointRecap recap = episodeRecaps[i];
            builder.AppendLine($"{i + 1}. {recap.keyPoint}");
            builder.AppendLine($"   Jawaban benar: {recap.selectedAnswer}");
            builder.AppendLine($"   Percobaan sampai benar: {recap.attempts}");
            builder.AppendLine();
        }

        recapKeyPointsText.text = builder.ToString().TrimEnd();
    }

    private void ResetEpisodeRecapData()
    {
        episodeRecaps.Clear();
        totalQuizAttempts = 0;
        currentQuestionAttempts = 0;
        finalNormalizedScore = 0;

        if (recapKeyPointsText != null)
        {
            recapKeyPointsText.text = string.Empty;
        }

        if (recapPanel != null)
        {
            recapPanel.SetActive(false);
        }
    }

    private int GetQuizScoreForAttempt(int attempts)
    {
        if (attempts <= 1)
        {
            return pointsFirstAttempt;
        }

        if (attempts == 2)
        {
            return pointsSecondAttempt;
        }

        return pointsThirdOrMoreAttempt;
    }

    private int CalculateFinalNormalizedScore()
    {
        int totalQuizQuestions = CountQuizSteps();
        if (totalQuizQuestions <= 0 || pointsFirstAttempt <= 0)
        {
            return 0;
        }

        int maxPossibleRawScore = totalQuizQuestions * pointsFirstAttempt;
        if (maxPossibleRawScore <= 0)
        {
            return 0;
        }

        float normalized = ((float)totalScore / maxPossibleRawScore) * 100f;
        return Mathf.Clamp(Mathf.RoundToInt(normalized), 0, 100);
    }

    private int CountQuizSteps()
    {
        if (quizData == null || quizData.steps == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < quizData.steps.Count; i++)
        {
            Step step = quizData.steps[i];
            if (step != null && step.stepType == "quiz")
            {
                count++;
            }
        }

        return count;
    }
}