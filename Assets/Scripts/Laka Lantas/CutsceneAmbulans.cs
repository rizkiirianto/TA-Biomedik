using System.Collections;
using TMPro;
using UnityEngine;

public class CutsceneAmbulans : MonoBehaviour, ICutscene
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI narrationText;

    [Header("Settings")]
    [SerializeField] private float autoCompleteDelay = 2.5f;
    [SerializeField] private bool destroyOnFinish = true;

    private GameManager gameManager;
    private Coroutine routine;
    private bool finished;

    public void BeginCutscene(GameManager gm)
    {
        gameManager = gm;

        if (narrationText != null)
        {
            narrationText.text = "Dari kejauhan terdengar suara sirine mendekat dan tak lama kemudian ambulans pun tiba.";
        }
        else
        {
            Debug.LogWarning("CutsceneAmbulans: narrationText belum di-assign.");
        }

        routine = StartCoroutine(AutoCompleteAfterDelay());
    }

    private IEnumerator AutoCompleteAfterDelay()
    {
        yield return new WaitForSeconds(autoCompleteDelay);
        Complete();
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
