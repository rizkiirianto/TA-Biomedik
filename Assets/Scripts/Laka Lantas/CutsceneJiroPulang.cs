using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CutsceneJiroPulang : MonoBehaviour, ICutscene
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI textNarasi;
    [SerializeField] private GameObject jiroPortraitObj;
    [SerializeField] private Image jiroPortraitImage;

    [Header("Jiro Expressions")]
    [SerializeField] private Sprite jiroNormal;
    [SerializeField] private Sprite jiroKaget;
    [SerializeField] private Sprite jiroTakut;

    [Header("Settings")]
    [Tooltip("Kecepatan teks muncul per huruf")]
    [SerializeField] private float typingSpeed = 0.04f;
    [Tooltip("Jeda sebelum lanjut ke kalimat berikutnya")]
    [SerializeField] private float delayBetweenLines = 1.5f;
    [Tooltip("Kalau true, object cutscene akan dihancurkan setelah selesai.")]
    [SerializeField] private bool destroyOnFinish = true;

    private GameManager gameManager;
    private Coroutine routine;
    private bool finished;

    // Fungsi ini dipanggil oleh sistemmu saat cutscene di-instantiate
    public void BeginCutscene(GameManager gm)
    {
        this.gameManager = gm;

        // Reset UI di awal
        jiroPortraitObj.SetActive(false);
        textNarasi.text = "";

        // Mulai sequence ceritanya
        routine = StartCoroutine(PlayCutsceneSequence());
    }

    private IEnumerator PlayCutsceneSequence()
    {
        // --- 1. Narasi Awal ---
        yield return StartCoroutine(TypeText("Malam yang sunyi. Jiro berjalan pulang dengan langkah gontai setelah hari yang panjang."));
        yield return new WaitForSeconds(delayBetweenLines);

        // --- 2. Jiro Monolog (Ekspresi Normal) ---
        jiroPortraitObj.SetActive(true);
        jiroPortraitImage.sprite = jiroNormal;
        yield return StartCoroutine(TypeText("Jiro: \"Hah... lelahnya. Aku cuma mau cepat rebahan di kasur...\""));
        yield return new WaitForSeconds(delayBetweenLines);

        // --- 3. Decitan dan Tabrakan ---
        jiroPortraitObj.SetActive(false); // Sembunyikan portrait biar dramatis
        yield return StartCoroutine(TypeText("*CKIIIIITTT!!! BRAAAAKKKK!!!*"));
        yield return new WaitForSeconds(delayBetweenLines);

        // --- 4. Jiro Kaget (Ekspresi Kaget) ---
        jiroPortraitObj.SetActive(true);
        jiroPortraitImage.sprite = jiroKaget;
        yield return StartCoroutine(TypeText("Jiro: \"Astaga! Suara apa itu?! Keras sekali dari arah perempatan!\""));
        yield return new WaitForSeconds(delayBetweenLines);

        // --- 5. Jiro Takut & Harus Menolong (Ekspresi Takut) ---
        jiroPortraitImage.sprite = jiroTakut;
        yield return StartCoroutine(TypeText("Jiro: \"Kecelakaan...? A-aku takut melihat darah... Tapi jalanan ini sepi, tidak ada orang lain. Aku harus menolongnya!\""));
        yield return new WaitForSeconds(delayBetweenLines + 1f);

        // --- 6. Cutscene Selesai ---
        Complete();
    }

    private IEnumerator TypeText(string line)
    {
        textNarasi.text = "";
        foreach (char letter in line.ToCharArray())
        {
            textNarasi.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private void OnDisable()
    {
        // Hentikan coroutine kalau object tiba-tiba dimatikan biar aman
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
            Debug.LogWarning("Cutscene1JiroPulang: GameManager is null, cannot signal completion.");
        }

        if (destroyOnFinish)
        {
            Destroy(gameObject);
        }
    }
}