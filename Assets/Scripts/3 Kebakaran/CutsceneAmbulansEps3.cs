using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneAmbulansEps3 : MonoBehaviour, ICutscene
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI narrationText;
    [SerializeField] private Button nextButton;
     [SerializeField] private GameObject jiroNangisSendiri;
    [SerializeField] private GameObject jiroNangis;
    

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typewriterSound;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private bool destroyOnFinish = true;

    [TextArea(10, 20)]
    [SerializeField] private string narrationLine =
        "Tak lama kemudian, suara sirine ambulans menggema di seluruh kawasan ITS, memecah kepanikan yang sejak tadi menyelimuti lokasi kejadian.\n\n" +
        "Namun sayangnya sudah tidak ada yang bisa dilakukan untuk Tono.\n\n" +
        "Luka bakar yang ia alami terlalu parah. Asap panas dan cedera pada saluran pernapasannya membuat tubuhnya tak lagi mampu bertahan. Di siang yang terasa begitu panjang itu, Tono mengembuskan napas terakhirnya.\n\n" +
        "Siti, Jiro, dan Budi segera dilarikan ke rumah sakit untuk mendapatkan penanganan lebih lanjut.\n\n" +
        "Di sudut lorong IGD, Jiro hanya bisa terduduk lemas sambil menangis, tangannya gemetar dan dipenuhi rasa bersalah.\n\n" +
        "“Aku gagal nyelamatin dia…”\n\n" +
        "Seorang paramedis perlahan menepuk bahunya.\n\n" +
        "“Tidak,” ucapnya pelan. “Kamu sudah melakukan semua yang kamu bisa.”\n\n" +
        "Jiro terdiam, menahan tangisnya.\n\n" +
        "“Keputusanmu untuk tetap kembali menolong mereka… itu tindakan yang berani. Kalau kamu tidak bertindak tadi, mungkin kami juga tidak akan bisa menyelamatkan Siti.”\n\n" +
        "Paramedis itu menatap Jiro sejenak sebelum melanjutkan,\n\n" +
        "“Kamu mungkin tidak bisa menyelamatkan semua orang… tapi hari ini, kamu tetap berhasil menyelamatkan seseorang.”\n\n" +
        "Tangis Jiro pecah kembali, kali ini bukan hanya karena kehilangan, tetapi juga karena akhirnya ia menyadari bahwa keberanian terkadang bukan tentang memenangkan semuanya… melainkan tetap memilih menolong, bahkan di tengah rasa takut.";

    [TextArea(10, 20)]
    [SerializeField] private string narrationLineEN =
        "Shortly after, the sound of ambulance sirens echoed throughout the ITS area, breaking the panic that had enveloped the scene.\n\n" +
        "But unfortunately, there was nothing more that could be done for Tono.\n\n" +
        "The burns he suffered were too severe. The hot smoke and injuries to his respiratory tract meant his body could no longer hold on. On that long day, Tono took his last breath.\n\n" +
        "Siti, Jiro, and Budi were immediately rushed to the hospital for further treatment.\n\n" +
        "In the corner of the ER hallway, Jiro could only sit limply, crying, his hands trembling and filled with guilt.\n\n" +
        "“I failed to save him…”\n\n" +
        "A paramedic slowly patted his shoulder.\n\n" +
        "“No,” he said softly. “You did everything you could.”\n\n" +
        "Jiro was silent, holding back his tears.\n\n" +
        "“Your decision to go back and help them… that was a brave act. If you hadn't acted then, we might not have been able to save Siti either.”\n\n" +
        "The paramedic looked at Jiro for a moment before continuing,\n\n" +
        "“You might not be able to save everyone… but today, you still managed to save someone.”\n\n" +
        "Jiro broke down crying again, this time not just from loss, but also because he finally realized that bravery is sometimes not about winning everything… but rather still choosing to help, even in the midst of fear.";

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

        if (jiroNangisSendiri != null) 
        {
            jiroNangisSendiri.SetActive(false);
        }
        if (jiroNangis != null)
        {
            jiroNangis.SetActive(false);
        }

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
                if (jiroNangisSendiri != null && (lines[i].Contains("Di sudut lorong IGD") || lines[i].Contains("In the corner of the ER hallway")))
                {
                    jiroNangisSendiri.SetActive(true);
                }
                if (jiroNangis != null && (lines[i].Contains("Seorang paramedis") || lines[i].Contains("A paramedic")))
                {
                    jiroNangis.SetActive(true);
                }

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
        string currentNarration = PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? narrationLineEN : narrationLine;
        
        if (string.IsNullOrWhiteSpace(currentNarration))
        {
            return new string[] { string.Empty };
        }

        string[] rawLines = currentNarration.Split('\n');
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
            cleaned.Add(currentNarration.Trim());
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
