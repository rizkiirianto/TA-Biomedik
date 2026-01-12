using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;

[System.Serializable]
public class MiniGameRegistry
{
    public string id;
    public GameObject prefab;
}

public class GameManager : MonoBehaviour
{
    [Header("Referensi UI Utama")]
    public GameObject quizUIParent;
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;
    public Transform canvasTransform;
    public TextMeshProUGUI narrativeText;
    public Image narrativeImage;
    public Image backgroundImage;
    public TextMeshProUGUI scoreText;
    public GameObject playerModel;
    [Tooltip("Panel Button transparan untuk melanjutkan dialog")]
    public Button clickAdvanceButton;

    [Header("Registrasi Prefab Minigame")]
    public List<MiniGameRegistry> miniGameRegistry;

    // Variabel Logika Game
    private QuizData quizData;
    private int currentStepIndex = 0;
    private GameObject activeMiniGameInstance;
    private bool isWaitingForAdvance;
    private bool isQuizFinished = false;
    private int totalScore = 0; // --- TAMBAHAN: Menyimpan total skor pemain
    private int currentQuestionAttempts = 0; // --- TAMBAHAN: Melacak percobaan di kuis saat ini
    private float minigameStartTime = 0f; // --- TAMBAHAN: Mencatat waktu mulai minigame

    void Start()
    {
        feedbackPanel.SetActive(false);
        clickAdvanceButton.gameObject.SetActive(false);
        clickAdvanceButton.onClick.AddListener(OnAdvanceClicked);
        LoadQuizData();
        ShowStep(currentStepIndex);
        totalScore = 0;
        UpdateScoreText();
        LoadQuizData();
        Application.targetFrameRate = 60;
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Skor: " + totalScore;
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
    }

    private void ShowStep(int stepIndex)
    {
        if (activeMiniGameInstance != null) Destroy(activeMiniGameInstance);

        if (quizData == null || stepIndex >= quizData.steps.Count)
        {
            EndQuiz();
            return;
        }

        Step currentStep = quizData.steps[stepIndex];

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
    }
    private void ShowDialog(Step step)
    {
        playerModel.SetActive(false);
        quizUIParent.SetActive(true);
        feedbackPanel.SetActive(false);

        LoadNarrativeImage(step.narrativeImage);
        
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

        // Matikan semua tombol opsi karena ini cuma dialog
        foreach (var btn in optionButtons)
        {
            btn.gameObject.SetActive(false);
        }

        // Langsung siapkan tombol advance agar pemain bisa klik layar untuk lanjut
        PrepareToAdvance(); 
    }
    private void ShowQuiz(Step quizStep)
    {
        playerModel.SetActive(false);
        quizUIParent.SetActive(true);
        feedbackPanel.SetActive(false);
        questionText.text = quizStep.instruction;

        LoadBackgroundImage(quizStep.backgroundImage);

        currentQuestionAttempts = 0;

        // --- PERBAIKAN UTAMA ADA DI SINI ---
        // Selalu pastikan tombol pilihan aktif setiap kali step kuis baru ditampilkan.
        SetOptionButtonsInteractable(true);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < quizStep.options.Count)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = quizStep.options[i].text;
                int optionIndex = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(optionIndex));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void StartMiniGame(Step miniGameStep)
    {
        playerModel.SetActive(false);
        quizUIParent.SetActive(false);
        
        // 1. Cari prefab berdasarkan ID di Registry (tetap perlu ini untuk spawn)
        MiniGameRegistry gameToStart = miniGameRegistry.FirstOrDefault(mg => mg.id == miniGameStep.minigameID);

        if (gameToStart != null && gameToStart.prefab != null)
        {
            minigameStartTime = Time.time;
            // Spawn Prefab
            activeMiniGameInstance = Instantiate(gameToStart.prefab, canvasTransform);

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
        AudioSource buttonAudio = optionButtons[optionIndex].GetComponent<AudioSource>();
        if (buttonAudio != null)
        {
            buttonAudio.Play();
        }
        
        currentQuestionAttempts++;
        Option selectedOption = quizData.steps[currentStepIndex].options[optionIndex];
        feedbackText.text = selectedOption.feedback;
        
        narrativeText.text = selectedOption.narrative;

        // Load gambar naratif jika ada
        LoadNarrativeImage(selectedOption.narrativeImage);

        if (selectedOption.isCorrect)
        {
            // Jangan tampilkan feedback panel untuk jawaban benar
            feedbackPanel.SetActive(false);
            
            int scoreGained = 0;
            if (currentQuestionAttempts == 1)
            {
                scoreGained = 100;
            }
            else if (currentQuestionAttempts == 2)
            {
                scoreGained = 50;
            }
            else
            {
                scoreGained = 25;
            }
            totalScore += scoreGained;
            UpdateScoreText();
            Debug.Log($"Jawaban Benar, Dapat skor {scoreGained}. Total skor : {totalScore}");
            PrepareToAdvance();
        }
        else
        {
            // Tampilkan feedback panel hanya untuk jawaban salah
            feedbackPanel.SetActive(true);
            // Otomatis sembunyikan feedback panel setelah 2 detik
            StartCoroutine(HideFeedbackPanelAfterDelay(2f));
        }
    }

    public void OnMiniGameComplete(string successFeedback)
    {
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
        
        // Kembalikan player model dan UI
        playerModel.SetActive(true);
        quizUIParent.SetActive(true); // Tampilkan kembali UI untuk feedback/instruksi
        narrativeText.text = quizData.steps[currentStepIndex].instruction; // Tampilkan instruksi minigame sebagai narasi
        foreach (var btn in optionButtons) btn.gameObject.SetActive(false); // Pastikan tombol pilihan tersembunyi

        // Load gambar naratif untuk minigame jika ada
        LoadNarrativeImage(quizData.steps[currentStepIndex].narrativeImage);
        LoadBackgroundImage(quizData.steps[currentStepIndex].backgroundImage);

        feedbackText.text = successFeedback;
        feedbackPanel.SetActive(true);
        
        Debug.Log("Memanggil PrepareToAdvance untuk lanjut ke step berikutnya");
        PrepareToAdvance();
    }

    private void PrepareToAdvance()
    {
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

    private void GoToNextStep()
    {
        currentStepIndex++;
        Debug.Log($"GoToNextStep dipanggil - pindah ke step index: {currentStepIndex}");
        ShowStep(currentStepIndex);
    }

    private void SetOptionButtonsInteractable(bool state)
    {
        foreach (Button btn in optionButtons)
        {
            btn.interactable = state;
        }
    }

    private void EndQuiz()
    {
        isQuizFinished = true; // Set penanda bahwa skenario selesai
        quizUIParent.SetActive(true);
        clickAdvanceButton.gameObject.SetActive(true); // Tampilkan tombol untuk kembali ke menu
        isWaitingForAdvance = false; // Pastikan ini false agar tidak menjalankan GoToNextStep

        // Tampilkan pesan selesai
        narrativeText.text = "Kamu berhasil! Skenario selesai.";
        questionText.text = "Selamat!";
        feedbackPanel.SetActive(false);
        foreach (Button btn in optionButtons)
        {
            btn.gameObject.SetActive(false);
        }
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
}