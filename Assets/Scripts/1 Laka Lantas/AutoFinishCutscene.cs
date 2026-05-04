using System.Collections;
using UnityEngine;

public class AutoFinishCutscene : MonoBehaviour, ICutscene
{
    [Tooltip("Durasi cutscene dalam detik sebelum otomatis selesai.")]
    [SerializeField] private float duration = 5f;
    [Tooltip("Kalau true, object cutscene akan dihancurkan setelah selesai.")]
    [SerializeField] private bool destroyOnFinish = true;
    private GameManager gameManager;
    private Coroutine routine;
    private bool finished;

    public void BeginCutscene(GameManager gameManager)
    {
        this.gameManager = gameManager;

        // Pastikan animasi default berjalan (biasanya sudah otomatis).
        // Kalau prefab punya Animator yang disable, ini akan menyalakannya.
        var animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = true;

        // Kalau prefab punya ParticleSystem dan tidak autoplay, ini akan menyalakannya.
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (!ps.isPlaying) ps.Play(true);
        }

        routine = StartCoroutine(AutoCompleteAfterDelay());
    }

    private IEnumerator AutoCompleteAfterDelay()
    {
        yield return new WaitForSeconds(duration);
        Complete();
    }

    private void OnDisable()
    {
        // Kalau object dimatikan/dihancurkan sebelum selesai, hentikan coroutine biar aman.
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
            Debug.LogWarning("AutoFinishCutscene: GameManager is null, cannot signal completion.");
        }

        if (destroyOnFinish)
        {
            Destroy(gameObject);
        }
    }
}