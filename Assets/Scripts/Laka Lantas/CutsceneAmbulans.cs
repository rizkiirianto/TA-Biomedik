using System.Collections;
using TMPro;
using UnityEngine;

public class CutsceneAmbulans : MonoBehaviour, ICutscene
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI narrationText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typewriterSound;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private float delayBetweenLines = 1.25f;
    [SerializeField] private float autoCompleteDelay = 2.5f;
    [SerializeField] private bool destroyOnFinish = true;

    [TextArea(3, 8)]
    [SerializeField] private string narrationLine =
        "Dari kejauhan terdengar suara sirine mendekat dan tak lama kemudian ambulans pun tiba.\n\n" +
        "Petugas medis segera membawa korban ke dalam ambulans untuk mendapatkan penanganan lebih lanjut.\n" +
        "Berkat tindakan cepat dan tepat yang telah dilakukan, kondisi korban dapat ditangani sebelum menjadi lebih buruk.";

    private GameManager gameManager;
    private Coroutine routine;
    private bool finished;

    public void BeginCutscene(GameManager gm)
    {
        gameManager = gm;

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
        if (narrationText != null)
        {
            string[] lines = GetNarrationLines();

            for (int i = 0; i < lines.Length; i++)
            {
                yield return StartCoroutine(TypeText(lines[i]));

                if (i < lines.Length - 1)
                {
                    yield return new WaitForSeconds(delayBetweenLines);
                    narrationText.text = string.Empty;
                }
            }
        }

        yield return new WaitForSeconds(autoCompleteDelay);
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

        foreach (char letter in line)
        {
            narrationText.text += letter;

            if (typewriterSound != null && audioSource != null && char.IsLetterOrDigit(letter))
            {
                audioSource.PlayOneShot(typewriterSound);
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
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
