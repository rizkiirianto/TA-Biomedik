using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CutsceneKainDilepas : MonoBehaviour, ICutscene
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject masihPressure;
    [SerializeField] private GameObject kainDilepas;
    [SerializeField] private TextMeshProUGUI narrationText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typewriterSound;
    [SerializeField] private AudioClip bloodSplatterClip;

    [Header("Controls")]
    [SerializeField] private Button nextButton;
    [SerializeField] private float stageDurationSeconds = 5f;
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private bool destroyOnFinish = true;
    [TextArea(2, 5)]
    [SerializeField] private string narrationLine = "membuka kain dan pressure awal membuat perdarahan yang sempat berkurang menjadi seperti semula";

    private GameManager gameManager;
    private Coroutine sequenceRoutine;
    private Coroutine typeRoutine;
    private bool finished;
    private bool nextClicked;
    private bool isTyping;
    private bool skipTypingRequested;

    public void BeginCutscene(GameManager gm)
    {
        gameManager = gm;
        finished = false;
        nextClicked = false;
        isTyping = false;
        skipTypingRequested = false;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextClicked);
            nextButton.onClick.AddListener(OnNextClicked);
            nextButton.gameObject.SetActive(true);
        }

        SetStageState(showMasihPressure: true, showKainDilepas: false);
        if (narrationText != null)
        {
            narrationText.text = string.Empty;
        }

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        sequenceRoutine = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        // Stage 1: Masih melakukan pressure
        yield return WaitForNextOrTimeout(stageDurationSeconds);

        // Stage 2: Kain dilepas + darah keluar lagi + teks typewriter
        SetStageState(showMasihPressure: false, showKainDilepas: true);
        PlayOneShotSafe(bloodSplatterClip);
        if (narrationText != null)
        {
            if (typeRoutine != null)
            {
                StopCoroutine(typeRoutine);
            }
            typeRoutine = StartCoroutine(TypeNarration(narrationLine));

            // Tunggu sampai typing selesai dulu, baru izinkan lanjut berdasarkan input/timeout.
            while (isTyping)
            {
                yield return null;
            }
        }

        yield return WaitForNextOrTimeout(stageDurationSeconds);

        Complete();
    }

    private IEnumerator WaitForNextOrTimeout(float duration)
    {
        nextClicked = false;
        float timer = 0f;

        while (!nextClicked && timer < duration)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void SetStageState(bool showMasihPressure, bool showKainDilepas)
    {
        if (masihPressure != null)
        {
            masihPressure.SetActive(showMasihPressure);
        }

        if (kainDilepas != null)
        {
            kainDilepas.SetActive(showKainDilepas);
        }
    }

    private IEnumerator TypeNarration(string line)
    {
        narrationText.text = string.Empty;
        if (string.IsNullOrEmpty(line))
        {
            yield break;
        }

        isTyping = true;
        skipTypingRequested = false;

        foreach (char letter in line)
        {
            if (skipTypingRequested)
            {
                narrationText.text = line;
                break;
            }

            narrationText.text += letter;

            if (typewriterSound != null && audioSource != null && char.IsLetterOrDigit(letter))
            {
                audioSource.PlayOneShot(typewriterSound);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        skipTypingRequested = false;
        typeRoutine = null;
    }

    private void PlayOneShotSafe(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
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

        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextClicked);
        }
    }
}
