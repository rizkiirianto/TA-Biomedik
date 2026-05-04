using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneJiroTertabrak : MonoBehaviour, ICutscene
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject jiroLari;
    [SerializeField] private GameObject adaMobil;
    [SerializeField] private GameObject bloodSplatterObject;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip klaksonMobil;
    [SerializeField] private AudioClip bloodSplatterClip;

    [Header("Controls")]
    [SerializeField] private Button nextButton;
    [SerializeField] private float stageDurationSeconds = 5f;
    [SerializeField] private bool destroyOnFinish = true;

    private GameManager gameManager;
    private Coroutine sequenceRoutine;
    private bool finished;
    private bool nextClicked;

    public void BeginCutscene(GameManager gm)
    {
        gameManager = gm;
        finished = false;
        nextClicked = false;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextClicked);
            nextButton.onClick.AddListener(OnNextClicked);
            nextButton.gameObject.SetActive(true);
        }

        SetStageState(showJiroLari: true, showMobil: false, showBlood: false);

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        // Stage 1: Jiro berlari
        yield return WaitForNextOrTimeout(stageDurationSeconds);

        // Stage 2: Mobil muncul + klakson
        SetStageState(showJiroLari: false, showMobil: true, showBlood: false);
        PlayOneShotSafe(klaksonMobil);
        yield return WaitForNextOrTimeout(stageDurationSeconds);

        // Stage 3: Blood splatter + audio
        SetStageState(showJiroLari: false, showMobil: false, showBlood: true);
        PlayOneShotSafe(bloodSplatterClip);
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

    private void SetStageState(bool showJiroLari, bool showMobil, bool showBlood)
    {
        if (jiroLari != null)
        {
            jiroLari.SetActive(showJiroLari);
        }

        if (adaMobil != null)
        {
            adaMobil.SetActive(showMobil);
        }

        if (bloodSplatterObject != null)
        {
            bloodSplatterObject.SetActive(showBlood);
        }
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

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextClicked);
        }
    }
}
