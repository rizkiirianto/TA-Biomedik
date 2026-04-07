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

    [Header("Audio Settings")]
    [Tooltip("Komponen AudioSource untuk memutar suara")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Suara ketikan mesin tik per huruf")]
    [SerializeField] private AudioClip typewriterSound;
    [Tooltip("Suara tabrakan mobil")]
    [SerializeField] private AudioClip crashSound;

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
        // Format: yield return StartCoroutine(PlayDialog("Teks", EkspresiSprite, SembunyikanPortrait?, SuaraSpesial?, CustomDelay?));
        
        yield return StartCoroutine(PlayDialog("Malam yang sunyi. Jiro berjalan pulang dengan langkah gontai setelah hari yang panjang.", null, true));

        yield return StartCoroutine(PlayDialog("Jiro: \"Hah... lelahnya. Aku cuma mau cepat rebahan di kasur...\"", jiroNormal));

        // Memasukkan crashSound sebagai parameter ke-4, dan angka 7f sebagai custom delay (parameter ke-5)
        yield return StartCoroutine(PlayDialog("*CKIIIIITTT!!! BRAAAAKKKK!!!*", null, true, crashSound, 7.0f));

        yield return StartCoroutine(PlayDialog("Jiro: \"Astaga! Suara apa itu?! Keras sekali dari arah perempatan!\"", jiroKaget));

        yield return StartCoroutine(PlayDialog("Jiro: \"Kecelakaan...? A-aku takut... Tapi jalanan ini sepi. Aku harus menolongnya!\"", jiroTakut));

        // --- Selesai ---
        Complete();
    }

    // Fungsi pembantu untuk memproses satu baris dialog
    // Ditambahkan parameter opsional customDelay (default -1f artinya pakai settingan standar)
    private IEnumerator PlayDialog(string line, Sprite expression, bool hidePortrait = false, AudioClip sfxClip = null, float customDelay = -1f)
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

        // Putar suara spesial jika ada (misal: suara tabrakan)
        if (sfxClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(sfxClip);
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

            // Putar suara ketikan untuk huruf dan angka (abaikan spasi agar lebih natural)
            if (typewriterSound != null && audioSource != null && char.IsLetterOrDigit(letter))
            {
                audioSource.PlayOneShot(typewriterSound);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        skipDelay = false; // Reset skipDelay sebelum masuk waktu tunggu

        // Menentukan berapa lama harus menunggu
        // Jika customDelay lebih dari atau sama dengan 0, pakai customDelay. Jika tidak, pakai delayBetweenLines
        float targetDelay = customDelay >= 0f ? customDelay : delayBetweenLines;

        // Proses Menunggu (Delay otomatis ATAU ditekan spasi)
        float timer = 0;
        while (timer < targetDelay && !skipDelay)
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