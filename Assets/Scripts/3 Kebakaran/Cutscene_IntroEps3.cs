using System.Collections;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Cutscene_IntroEps3 : MonoBehaviour, ICutscene
{
    [Header("Visual")]
    [SerializeField] private GameObject Gambar1MejaLab;
    [SerializeField] private GameObject Gambar2DalamLab;
    [SerializeField] private GameObject Gambar3LuarLab;
    [SerializeField] private GameObject panelDialog;
    [SerializeField] private TextMeshProUGUI textDialog;
    [SerializeField] private GameObject jiroBiasa;
    [SerializeField] private GameObject jiroTakut;
    [SerializeField] private GameObject redPanelAlarm;
    [SerializeField] private float alarmBlinkInterval = 0.25f;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip typewriterSound;
    [SerializeField] private AudioSource alarmAudioSource;
    [SerializeField] private AudioClip alarmLoopSound;
    [SerializeField] private AudioClip explosionSound;

    [Header("Control")]
    [SerializeField] private Button nextButton;
    [SerializeField] private bool destroyOnFinish = true;

    [SerializeField] private float typingSpeed = 0.035f;

    [Header("Dialog")]
    [TextArea(2, 5)]
    [SerializeField] private string[] dialogLines = new string[]
    {
        "Di suatu hari Jiro sedang berada di gedung kampusnya untuk mengerjakan tugasnya.",
        "Jiro sedang berada di lab jurusannya saat tiba-tiba terdengar suara ledakan dan alarm kebakaran mulai berbunyi.",
        "Jiro: Hah?! Itu suara ledakan?! Alarm kebakaran juga nyala... aku harus tetap tenang!",
        "Jiro: Aku harus segera keluar dari sini!"
    };

    private GameManager gameManager;
    private Coroutine sequenceRoutine;
    private Coroutine alarmBlinkRoutine;
    private bool isTyping;
    private bool skipTypingRequested;
    private UnityAction nextListener;
    private bool nextClicked;
    private bool finished;
    private bool isAlarmBlinking;

    public void BeginCutscene(GameManager gm)
    {
        gameManager = gm;
        nextClicked = false;
        finished = false;
        isAlarmBlinking = false;
        isTyping = false;
        skipTypingRequested = false;

        // Pastikan gameObject aktif sebelum StartCoroutine
        gameObject.SetActive(true);

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        StopAlarmBlink();

        // Mute ambiance audio for stage 0 (set volume to 0)
        if (gameManager != null && gameManager.ambianceAudioSource != null)
        {
            gameManager.ambianceAudioSource.volume = 0f;
        }

        HookNextButton();

        if (panelDialog != null)
        {
            panelDialog.SetActive(true);
        }

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
        for (int stage = 0; stage < dialogLines.Length; stage++)
        {
            ApplyStage(stage);

            yield return StartCoroutine(TypeLine(dialogLines[stage]));
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
        while (isTyping)
        {
            yield return null;
        }

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

        if (Gambar1MejaLab != null)
        {
            Gambar1MejaLab.SetActive(s0);
        }

        if (Gambar2DalamLab != null)
        {
            Gambar2DalamLab.SetActive(s1 || s2);
        }

        if (Gambar3LuarLab != null)
        {
            Gambar3LuarLab.SetActive(s3);
        }

        if (jiroBiasa != null)
        {
            jiroBiasa.SetActive(s3);
        }

        if (jiroTakut != null)
        {
            jiroTakut.SetActive(s2);
        }

        if (s1 || s2 || s3)
        {
            StartAlarmBlink();
        }
        else
        {
            StopAlarmBlink();
        }

        // Play explosion sound once at stage 1
        if (s1 && sfxAudioSource != null && explosionSound != null)
        {
            sfxAudioSource.PlayOneShot(explosionSound);
        }

        // Handle ambiance audio: mute at stage 0, unmute at stage 1+
        if (gameManager != null && gameManager.ambianceAudioSource != null)
        {
            if (s0)
            {
                // Mute ambiance at stage 0
                gameManager.ambianceAudioSource.volume = 0f;
                gameManager.ambianceAudioSource2.volume = 0f;
            }
            else
            {
                // Restore ambiance volume at stage 1 onwards (default 1.0)
                gameManager.ambianceAudioSource.volume = 1f;
                gameManager.ambianceAudioSource2.volume = 1f;
            }
        }
    }

    private void StartAlarmBlink()
    {
        if (redPanelAlarm == null || isAlarmBlinking)
        {
            return;
        }

        // Paksa warna overlay merah transparan sesuai request: 255,0,0 dengan alpha 50.
        Image panelImage = redPanelAlarm.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color32(255, 0, 0, 50);
        }

        isAlarmBlinking = true;

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
            redPanelAlarm.SetActive(visible);
            yield return new WaitForSeconds(alarmBlinkInterval);
        }

        if (redPanelAlarm != null)
        {
            redPanelAlarm.SetActive(false);
        }
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
        if (finished)
        {
            return;
        }

        if (isTyping)
        {
            skipTypingRequested = true;
            return;
        }

        nextClicked = true;
    }

    public void Complete()
    {
        if (finished)
        {
            return;
        }

        finished = true;

        if (nextButton != null && nextListener != null)
        {
            nextButton.onClick.RemoveListener(nextListener);
        }

        if (panelDialog != null)
        {
            panelDialog.SetActive(false);
        }

        StopAlarmBlink();

        if (gameManager != null)
        {
            gameManager.OnCutsceneComplete();
        }

        if (destroyOnFinish)
        {
            Destroy(gameObject);
        }
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
