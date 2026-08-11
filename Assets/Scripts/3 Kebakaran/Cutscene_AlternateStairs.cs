using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Cutscene_AlternateStairs : MonoBehaviour, ICutscene
{
    [Header("Visual - Images")]
    [SerializeField] private GameObject Gambar4_TanggaTerbakar;
    [SerializeField] private GameObject Gambar5_BerlariKeSisi;
    [SerializeField] private GameObject Gambar6_TanggaSatunya;
    [SerializeField] private GameObject Gambar7_TanggaLanjut1;
    [SerializeField] private GameObject Gambar8_TanggaLanjut2;
    [SerializeField] private GameObject Gambar9_TanggaTerakhir;
    [SerializeField] private GameObject Gambar10_Lobby;
    [SerializeField] private GameObject Gambar11_Parkiran;

    [Header("UI")]
    [SerializeField] private GameObject panelDialog;
    [SerializeField] private TextMeshProUGUI textDialog;
    [SerializeField] private GameObject jiroBiasa;
    [SerializeField] private GameObject jiroTakut;
    [SerializeField] private GameObject redPanelAlarm;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip typewriterSound;
    [SerializeField] private AudioSource alarmAudioSource;
    [SerializeField] private AudioClip alarmLoopSound;

    [Header("Control")]
    [SerializeField] private Button nextButton;
    [SerializeField] private bool destroyOnFinish = true;
    [SerializeField] private float alarmBlinkInterval = 0.25f;

    [Header("Dialogue")]
    [TextArea(2,4)]
    [SerializeField] private string[] dialogueLines = new string[]
    {
        // Gambar 4 (stage 0) - narrator
        "Setelah memilih tangga darurat, ternyata tangga darurat terdekat terbakar dan ada debris yang menghalangi jalan.",
        // Gambar 5 (stage 1) - Jiro takut
        "Jiro: Aku harus cari jalan lain—ke sisi bangunan!",
        // Gambar 6 (stage 2) - narrator
        "Tangga darurat satunya terlihat aman. Jiro segera beralih ke tangga tersebut dan mulai turun.",
        // Gambar 7 (stage 3) - Jiro takut
        "Jiro: Turun cepat, jangan berhenti!",
        // Gambar 8 (stage 4) - narrator
        "Asap makin tebal di beberapa tingkat, namun tangga ini masih cukup aman untuk dilewati.",
        // Gambar 9 (stage 5) - narrator
        "Jiro sampai di ujung tangga darurat. Pintu keluar menuju lobby sudah dekat.",
        // Gambar 10 (stage 6) - Jiro biasa
        "Jiro: Aku sampai di lobby. Di sini tampak sepi — harus segera ke parkiran depan!",
        // Gambar 11 (stage 7) - Jiro biasa melihat gedung
        "Jiro: Lihat... gedung itu terbakar dan ada bekas ledakan. Semoga semua orang sudah keluar."
    };

    [TextArea(2,4)]
    [SerializeField] private string[] dialogueLinesEN = new string[]
    {
        "After choosing the emergency stairs, it turns out the nearest one is on fire and blocked by debris.",
        "Jiro: I need to find another way—to the side of the building!",
        "The other emergency stairs look safe. Jiro quickly switches to those stairs and starts descending.",
        "Jiro: Go down fast, don't stop!",
        "The smoke is getting thicker on some floors, but these stairs are still quite safe to pass.",
        "Jiro reaches the end of the emergency stairs. The exit to the lobby is near.",
        "Jiro: I've reached the lobby. It looks empty here — gotta head to the front parking lot quickly!",
        "Jiro: Look... the building is on fire and there are blast marks. I hope everyone got out safely."
    };

    private string[] DialogueLines => PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? dialogueLinesEN : dialogueLines;

    // parallel arrays for speaker handling: if true, line is narrator (no Jiro shown)
    [SerializeField] private bool[] isNarrator = new bool[] { true, false, true, false, true, true, false, false };
    // for Jiro lines, whether to use the 'takut' expression
    [SerializeField] private bool[] isJiroTakut = new bool[] { false, true, false, true, false, false, false, true };

    private GameManager gameManager;
    private Coroutine sequenceRoutine;
    private Coroutine alarmBlinkRoutine;
    private bool isTyping;
    private bool skipTypingRequested;
    private UnityAction nextListener;
    private bool nextClicked;
    private bool finished;
    private bool isAlarmBlinking;
    [SerializeField] private float typingSpeed = 0.035f;

    public void BeginCutscene(GameManager gm)
    {
        gameManager = gm;
        nextClicked = false;
        finished = false;
        isAlarmBlinking = false;

        // Pastikan gameObject aktif sebelum StartCoroutine
        gameObject.SetActive(true);

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        HookNextButton();

        if (panelDialog != null)
        {
            panelDialog.SetActive(true);
        }

        // Alarm harus menyala dari stage awal
        StartAlarmBlink();

        sequenceRoutine = StartCoroutine(PlaySequence());
    }

    private void HookNextButton()
    {
        if (nextButton == null && gameManager != null)
        {
            nextButton = gameManager.clickAdvanceButton;
        }

        if (nextButton == null)
        {
            Debug.LogWarning("Cutscene_AlternateStairs: nextButton belum di-assign.");
            return;
        }

        if (nextListener != null)
        {
            nextButton.onClick.RemoveListener(nextListener);
        }

        nextListener = OnNextClicked;
        nextButton.onClick.AddListener(nextListener);
        nextButton.gameObject.SetActive(true);
    }

    private IEnumerator PlaySequence()
    {
        int stages = DialogueLines.Length;
        for (int stage = 0; stage < stages; stage++)
        {
            ApplyStage(stage);

            yield return StartCoroutine(TypeLine(DialogueLines[stage]));
            yield return StartCoroutine(WaitForAdvance());
        }

        Complete();
    }

    private IEnumerator WaitForNextClick()
    {
        nextClicked = false;
        while (!nextClicked)
        {
            yield return null;
        }
    }

    private IEnumerator WaitForAdvance()
    {
        // Wait until typing finished and user advances
        while (isTyping)
        {
            yield return null;
        }

        // then wait for next click
        nextClicked = false;
        while (!nextClicked)
        {
            yield return null;
        }
    }

    private void ApplyStage(int stage)
    {
        bool s0 = stage == 0;
        bool s1 = stage == 1;
        bool s2 = stage == 2;
        bool s3 = stage == 3;
        bool s4 = stage == 4;
        bool s5 = stage == 5;
        bool s6 = stage == 6;
        bool s7 = stage == 7;

        if (Gambar4_TanggaTerbakar != null) Gambar4_TanggaTerbakar.SetActive(s0);
        if (Gambar5_BerlariKeSisi != null) Gambar5_BerlariKeSisi.SetActive(s1);
        if (Gambar6_TanggaSatunya != null) Gambar6_TanggaSatunya.SetActive(s2);
        if (Gambar7_TanggaLanjut1 != null) Gambar7_TanggaLanjut1.SetActive(s3);
        if (Gambar8_TanggaLanjut2 != null) Gambar8_TanggaLanjut2.SetActive(s4);
        if (Gambar9_TanggaTerakhir != null) Gambar9_TanggaTerakhir.SetActive(s5);
        if (Gambar10_Lobby != null) Gambar10_Lobby.SetActive(s6);
        if (Gambar11_Parkiran != null) Gambar11_Parkiran.SetActive(s7);

        // Speaker handling
        bool narrator = false;
        if (isNarrator != null && stage < isNarrator.Length) narrator = isNarrator[stage];

        bool jiroTakutState = false;
        if (isJiroTakut != null && stage < isJiroTakut.Length) jiroTakutState = isJiroTakut[stage];

        if (jiroBiasa != null) jiroBiasa.SetActive(!narrator && !jiroTakutState);
        if (jiroTakut != null) jiroTakut.SetActive(!narrator && jiroTakutState);

        // Red panel alarm should stop when gambar10 or gambar11 active (stage 6 or 7)
        if (stage >= 6)
        {
            StopAlarmBlink();
        }
        else
        {
            StartAlarmBlink();
        }
    }

    private void StartAlarmBlink()
    {
        if (redPanelAlarm == null || isAlarmBlinking)
        {
            return;
        }

        Image panelImage = redPanelAlarm.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color32(255, 0, 0, 50);
        }

        isAlarmBlinking = true;
        // start alarm audio loop if provided
        if (alarmAudioSource != null && alarmLoopSound != null)
        {
            alarmAudioSource.clip = alarmLoopSound;
            alarmAudioSource.loop = true;
            alarmAudioSource.Play();
        }

        alarmBlinkRoutine = StartCoroutine(BlinkAlarmRoutine());
    }

    private void StopAlarmBlink()
    {
        isAlarmBlinking = false;

        if (alarmBlinkRoutine != null)
        {
            StopCoroutine(alarmBlinkRoutine);
            alarmBlinkRoutine = null;
        }

        if (redPanelAlarm != null)
        {
            redPanelAlarm.SetActive(false);
        }

        if (alarmAudioSource != null && alarmAudioSource.isPlaying)
        {
            alarmAudioSource.Stop();
        }
    }

    private IEnumerator BlinkAlarmRoutine()
    {
        bool visible = false;
        while (isAlarmBlinking)
        {
            visible = !visible;
            if (redPanelAlarm != null) redPanelAlarm.SetActive(visible);
            yield return new WaitForSeconds(alarmBlinkInterval);
        }

        if (redPanelAlarm != null) redPanelAlarm.SetActive(false);
    }

    private IEnumerator TypeLine(string line)
    {
        if (textDialog == null)
        {
            yield break;
        }

        isTyping = true;
        skipTypingRequested = false;
        textDialog.text = string.Empty;

        foreach (char c in line)
        {
            if (skipTypingRequested)
            {
                textDialog.text = line;
                break;
            }

            textDialog.text += c;

            if (sfxAudioSource != null && typewriterSound != null && char.IsLetterOrDigit(c))
            {
                sfxAudioSource.PlayOneShot(typewriterSound);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        skipTypingRequested = false;
    }

    private void OnNextClicked()
    {
        if (finished) return;

        if (isTyping)
        {
            skipTypingRequested = true;
            return;
        }

        nextClicked = true;
    }

    public void Complete()
    {
        if (finished) return;
        finished = true;

        if (nextButton != null && nextListener != null)
        {
            nextButton.onClick.RemoveListener(nextListener);
        }

        if (panelDialog != null) panelDialog.SetActive(false);

        StopAlarmBlink();

        if (gameManager != null) gameManager.OnCutsceneComplete();

        if (destroyOnFinish) Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        StopAlarmBlink();

        if (nextButton != null && nextListener != null)
        {
            nextButton.onClick.RemoveListener(nextListener);
        }
    }
}
