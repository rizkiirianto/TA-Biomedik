using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinigameHeadtiltChinlift : MonoBehaviour, IMiniGame
{
    [Header("Visuals")]
    [SerializeField] private GameObject tonoCloseUp;
    [SerializeField] private GameObject tonoCloseHeadTilt;
    [SerializeField] private GameObject tonoCloseUpChinLift;

    [Header("Interactables")]
    [SerializeField] private Button AreaHeadTilt;
    [SerializeField] private Button AreaChinLift;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI text;

    private GameManager gameManager;

    public void BeginGame(GameManager gm)
    {
        gameManager = gm;
        InitializeMinigame();
    }

    private void InitializeMinigame()
    {
        // 1. aktif pertama ketika awal minigame
        tonoCloseUp.SetActive(true);
        tonoCloseHeadTilt.SetActive(false);
        tonoCloseUpChinLift.SetActive(false);

        // 1. pemain harus klik AreaHeadTilt 
        AreaHeadTilt.gameObject.SetActive(true);
        AreaChinLift.gameObject.SetActive(false);

        // 1. Kalimat panduan untuk head tilt
        if (text != null) text.text = "Tekan dahi korban dan dorong ke belakang perlahan (Head Tilt).";

        AreaHeadTilt.onClick.RemoveAllListeners();
        AreaHeadTilt.onClick.AddListener(OnHeadTiltClicked);

        AreaChinLift.onClick.RemoveAllListeners();
        AreaChinLift.onClick.AddListener(OnChinLiftClicked);
    }

    private void OnHeadTiltClicked()
    {
        // 2. Gameobject ini aktif, tonoCloseUp inaktif
        tonoCloseUp.SetActive(false);
        tonoCloseHeadTilt.SetActive(true);
        AreaHeadTilt.gameObject.SetActive(false);

        // 2. Kalimat panduan untuk chin lift
        if (text != null) text.text = "Sekarang angkat dagu korban ke atas untuk membuka jalan napas (Chin Lift).";

        // 2. Pemain harus klik areaChinLift 
        AreaChinLift.gameObject.SetActive(true);
    }

    private void OnChinLiftClicked()
    {
        // 3. Gameobject ini aktif, tonoCloseHeadTilt inaktif
        tonoCloseHeadTilt.SetActive(false);
        tonoCloseUpChinLift.SetActive(true);
        AreaChinLift.gameObject.SetActive(false);

        StartCoroutine(EndingSequence());
    }

    private IEnumerator EndingSequence()
    {
        string[] endingTexts = new string[]
        {
            // 3. Jiro berhasil melakukan prosedur HeadTiltChinLift
            "Jiro berhasil melakukan prosedur Head-Tilt Chin-Lift.",
            // 4. Jiro melihat dada Tono yang tidak lagi bergerak
            "Jiro mengamati dada Tono yang sudah tidak lagi bergerak naik turun.",
            // 5. Jiro menempelkan telinganya untuk mendengarkan suara nafas Tono
            "Jiro menempelkan telinganya ke dekat wajah Tono untuk mendengarkan suara napas.",
            // 6. Jiro tidak bisa mendengar dan merasakan nafas Tono
            "Hening. Jiro tidak bisa mendengar maupun merasakan hembusan napas Tono.",
            "Jiro sadar jalan napas bagian dalam Tono sudah tertutup pembengkakan akibat inhalasi asap panas.",

            // Ketika masuk sequence kalimat ini yang aktif adalah gambar tonoCloseUp
            // 7. Pada intinya Jiro tidak bisa menolong Tono lebih lanjut
            "Tono berhenti bernapas dan Jiro tidak memiliki peralatan medis untuk menolongnya lebih lanjut.",
            // 8. Tunjukkan keputusasaan dan kesedihan Jiro karena tidak bisa menolong Tono
            "Rasa putus asa dan kesedihan yang mendalam menyelimuti Jiro. Ia hanya bisa terdiam menatap Tono.",
            "Jiro melepas tangan dari Tono, melabelinya sebagai Kategori Hitam, lalu berlari untuk membantu Budi menyelamatkan Siti."
        };

        for (int i = 0; i < endingTexts.Length; i++)
        {
            if (i == 5)
            {
                // Kembali mengaktifkan tonoCloseUp saat masuk ke kalimat "Tono berhenti bernapas..."
                if (tonoCloseUpChinLift != null) tonoCloseUpChinLift.SetActive(false);
                if (tonoCloseUp != null) tonoCloseUp.SetActive(true);
            }

            if (text != null) text.text = endingTexts[i];
            // Memberikan waktu untuk pemain membaca (4 detik), atau bisa diskip lebih cepat dengan klik kiri
            yield return StartCoroutine(WaitForKeyPressOrTime(4f));
        }

        if (gameManager != null)
        {
            gameManager.OnMiniGameComplete("Kamu telah berusaha semampumu, namun Tono tidak dapat diselamatkan.");
        }
    }

    private IEnumerator WaitForKeyPressOrTime(float time)
    {
        float timer = 0;
        while (timer < time)
        {
            // Klik kiri untuk mempercepat teks
            if (Input.GetMouseButtonDown(0))
            {
                yield return null; // tunggu 1 frame agar klik tidak langsung memicu aksi lain
                break;
            }
            timer += Time.deltaTime;
            yield return null;
        }
    }
}
