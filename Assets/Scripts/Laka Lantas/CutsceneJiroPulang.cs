using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Cutscene1JiroPulang : MonoBehaviour, ICutscene
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
    [Tooltip("Jeda sebelum lanjut otomatis (jika pemain tidak menekan spasi)")]
    [SerializeField] private float delayBetweenLines = 2.0f;
    [Tooltip("Kalau true, object cutscene akan dihancurkan setelah selesai.")]
    [SerializeField] private bool destroyOnFinish = true;

    private GameManager gameManager;
    private Coroutine routine;
    private bool finished;

    // --- Variabel untuk fitur Skip ---
    private bool isTyping = false;
    private bool skipTyping = false;
    private bool skipDelay = false;

    public void BeginCutscene(GameManager gm)
    {
        this.gameManager = gm;
        jiroPortraitObj.SetActive(false);
        textNarasi.text = "";
        
        routine = StartCoroutine(PlayCutsceneSequence());
    }

    private void Update()
    {
        // Mengecek input Spasi setiap frame
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // Kondisi 1: Teks sedang mengetik, paksa selesai
                skipTyping = true;
            }
            else
            {
                // Kondisi 2: Teks sudah selesai, lewati waktu tunggu (delay)
                skipDelay = true;
            }
        }
    }

    private IEnumerator PlayCutsceneSequence()
    {
        // --- Urutan Cerita ---
        // Format: yield return StartCoroutine(PlayDialog("Teks", EkspresiSprite, SembunyikanPortrait?));
        
        yield return StartCoroutine(PlayDialog("Malam yang sunyi. Jiro berjalan pulang dengan langkah gontai setelah hari yang panjang.", null, true));

        yield return StartCoroutine(PlayDialog("Jiro: \"Hah... lelahnya. Aku cuma mau cepat rebahan di kasur...\"", jiroNormal));

        yield return StartCoroutine(PlayDialog("*CKIIIIITTT!!! BRAAAAKKKK!!!*", null, true));

        yield return StartCoroutine(PlayDialog("Jiro: \"Astaga! Suara apa itu?! Keras sekali dari arah perempatan!\"", jiroKaget));

        yield return StartCoroutine(PlayDialog("Jiro: \"Kecelakaan...? A-aku takut... Tapi jalanan ini sepi, tidak ada orang lain. Aku harus menolongnya!\"", jiroTakut));

        // --- Selesai ---
        Complete();
    }

    // Fungsi pembantu untuk memproses satu baris dialog
    private IEnumerator PlayDialog(string line, Sprite expression, bool hidePortrait = false)
    {
        // Mengatur tampilan portrait
        if (hidePortrait)
        {
            jiroPortraitObj.SetActive(false);
        }
        else if (expression != null)
        {
            jiroPortraitObj.SetActive(true);
            jiroPortraitImage.sprite = expression;
        }

        // Persiapan mengetik
        textNarasi.text = "";
        isTyping = true;
        skipTyping = false;

        // Proses Typewriter
        foreach (char letter in line.ToCharArray())
        {
            if (skipTyping)
            {
                // Jika ditekan spasi, langsung tampilkan semua teks dan hentikan loop
                textNarasi.text = line;
                break;
            }

            textNarasi.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        skipDelay = false; // Reset skipDelay sebelum masuk waktu tunggu

        // Proses Menunggu (Delay otomatis ATAU ditekan spasi)
        float timer = 0;
        while (timer < delayBetweenLines && !skipDelay)
        {
            timer += Time.deltaTime;
            yield return null; // Tunggu ke frame berikutnya
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

        if (destroyOnFinish)
        {
            Destroy(gameObject);
        }
    }
}