using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneAmbulans : MonoBehaviour, ICutscene
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI narrationText;
    [SerializeField] private Button nextButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typewriterSound;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private bool destroyOnFinish = true;

    [TextArea(3, 8)]
    [SerializeField] private string narrationLine =
        "Dari kejauhan terdengar suara sirine mendekat dan tak lama kemudian ambulans pun tiba.\n\n" +
        "Petugas medis segera membawa korban ke dalam ambulans untuk mendapatkan penanganan lebih lanjut.\n" +
        "Berkat tindakan cepat dan tepat yang telah dilakukan, kondisi korban dapat ditangani sebelum menjadi lebih buruk.";

    private GameManager gameManager;
    private Coroutine routine;
    private bool finished;
    private bool isTyping;
    private bool skipTypingRequested;
    private bool waitingForNextClick;
    private bool nextClicked;

    public void BeginCutscene(GameManager gm)
    {
        gameManager = gm;
        finished = false;
        isTyping = false;
        skipTypingRequested = false;
        waitingForNextClick = false;
        nextClicked = false;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextButtonClicked);
            nextButton.onClick.AddListener(OnNextButtonClicked);
            nextButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("CutsceneAmbulans: nextButton belum di-assign.");
        }

        if (narrationText != null)
        {
            narrationText.text = string.Empty;
        }
        else
        {
            Debug.LogWarning("CutsceneAmbulans: narrationText belum di-assign.");
        }

        routine = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        string[] lines = GetNarrationLines();

        if (narrationText != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                yield return StartCoroutine(TypeText(lines[i]));

                waitingForNextClick = true;
                nextClicked = false;

                while (!nextClicked)
                {
                    yield return null;
                }

                waitingForNextClick = false;
            }
        }

        Complete();
    }

    private string[] GetNarrationLines()
    {
        if (string.IsNullOrWhiteSpace(narrationLine))
        {
            return new string[] { string.Empty };
        }

        string[] rawLines = narrationLine.Split('\n');
        System.Collections.Generic.List<string> cleaned = new System.Collections.Generic.List<string>();

        for (int i = 0; i < rawLines.Length; i++)
        {
            string line = rawLines[i].Trim();
            if (!string.IsNullOrEmpty(line))
            {
                cleaned.Add(line);
            }
        }

        if (cleaned.Count == 0)
        {
            cleaned.Add(narrationLine.Trim());
        }

        return cleaned.ToArray();
    }

    private IEnumerator TypeText(string line)
    {
        narrationText.text = string.Empty;
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
    }

    private void OnNextButtonClicked()
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

        if (waitingForNextClick)
        {
            nextClicked = true;
        }
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextButtonClicked);
        }
    }

    public void Complete()
    {
        if (finished) return;
        finished = true;

        if (gameManager != null)
        {
            gameManager.OnCutsceneComplete();
        }
        else
        {
            Debug.LogWarning("CutsceneAmbulans: GameManager is null, cannot signal completion.");
        }

        if (destroyOnFinish)
        {
            Destroy(gameObject);
        }
    }
}
