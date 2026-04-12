using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CutsceneCallTechnician : MonoBehaviour, ICutscene
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private Button advanceButton;
    [SerializeField] private GameObject dialogPanelRoot;

    [SerializeField] private GameObject gambarJiroBiasa;
    [SerializeField] private GameObject gambarJiroNelpon;
    [SerializeField] private GameObject gambarJiroKaget;
    [SerializeField] private GameObject gambarPakRaka;
    [SerializeField] private GameObject gambarRuangTamuMatiTengah;
    [SerializeField] private GameObject gambarTetanggaDatang;
    [SerializeField] private GameObject gambarPerbaikiLampu;
    [SerializeField] private GameObject gambarTersetrum;
    [SerializeField] private GameObject gambarLampuKonslet;

    [Header("Dialogue")]
    [TextArea(2, 4)]
    [SerializeField] private string[] dialogueLines = new string[]
    {
        "Jiro : Coba telepon Pak Raka, tetangga sebelah. Beliau biasanya ngerti listrik.",
        // gambarJiroBiasa on
        "*nada sambung telepon terdengar di tengah suara hujan deras*",
        // suara telpon
        // gambarJiroNelpon on, gambarJiroBiasa off
        "Pak Raka (telepon): Halo, Jiro? Tumben malam-malam gini telpon, ada apa?",
        "Jiro : Maaf ganggu, Pak. Lampu di ruang kerja saya flickering dari tadi.",
        "Pak Raka (telepon): Cuma satu lampu atau satu rumah?",
        "Jiro : Cuma satu pak lampu ruang tamu aja",
        "Pak Raka : Oh yaudah saya kesana aja buat ngecek",
        "Jiro : Siap pak, makasih banyak",
        // fade to black
        "10 menit kemudian",
        // gambarTetanggaDatang on
        // gambarJiroNelpon off, gambarPakRaka on
        "Pak Raka : Wah hujannya deras sekali",
        // gambarJiroBiasa on, gambarPakRaka off
        "Jiro : Mari pak masuk dulu saya ambilkan handuk",
        // gambarJiroNelpon off, gambarPakRaka on
        "Pak Raka : Aman Jiro, saya langsung cek aja lampunya yaa",
        // gambarJiroBiasa on, gambarPakRaka off
        "Jiro : Apa perlu saya matikan dulu listriknya pak?",
        // gambarJiroNelpon off, gambarPakRaka on
        "Pak Raka : Santai ajaa, paling cuma longgar kabelnya",
        // gambarTetanggaDatang off dan gambarPerbaikiLampu on
        // gambarJiroBiasa on, gambarPakRaka off
        "Jiro : Eh pak mending keringkan badan dulu ga sih pak daripada resiko kesetr-",
        //gambarLampuKonslet on, gambarRuangTamuMatiTengah off, panel off
        //sfx suara listrik
        //sfx orang teriak tersetrum
        //gambarRuangTamuMatiTengah on, gambarPerbaikiLampu on, gambarTersetrum on 
        //gambarJiroKaget on, gambarJiroBiasa on
        "Jiro : PAK RAKAA!!!"
    };

    [Header("Typewriter")]
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private bool hideDialogAfterFinish = true;
    [SerializeField] private bool destroyOnFinish = true;

    private GameManager gameManager;
    private Coroutine sequenceRoutine;
    private bool finished;
    private bool skipTypingRequested;
    private bool advanceRequested;
    private bool isTyping;
    private UnityAction advanceListener;

    public void BeginCutscene(GameManager gm)
    {
        gameManager = gm;
        finished = false;
        skipTypingRequested = false;
        advanceRequested = false;
        isTyping = false;

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        if (advanceButton != null && advanceListener != null)
        {
            advanceButton.onClick.RemoveListener(advanceListener);
            advanceListener = null;
        }

        if (dialogPanelRoot != null)
        {
            dialogPanelRoot.SetActive(true);
        }

        if (dialogText != null)
        {
            dialogText.text = string.Empty;
        }

        ResetVisualState();

        HookAdvanceButton();
        sequenceRoutine = StartCoroutine(PlayDialogueSequence());
    }

    private void HookAdvanceButton()
    {
        if (advanceButton == null)
        {
            if (gameManager != null && gameManager.clickAdvanceButton != null)
            {
                advanceButton = gameManager.clickAdvanceButton;
            }
            else
            {
                Debug.LogWarning("CutsceneLampuFlicker: advanceButton belum di-assign.");
                return;
            }
        }

        if (advanceListener != null)
        {
            advanceButton.onClick.RemoveListener(advanceListener);
        }

        advanceListener = OnAdvanceButtonClicked;
        advanceButton.onClick.AddListener(advanceListener);
    }

    private IEnumerator PlayDialogueSequence()
    {
        if (dialogText == null)
        {
            Debug.LogWarning("CutsceneLampuFlicker: dialogText belum di-assign.");
            Complete();
            yield break;
        }

        for (int i = 0; i < dialogueLines.Length; i++)
        {
            ApplyVisualState(i);

            yield return StartCoroutine(TypeLine(dialogueLines[i]));
            yield return StartCoroutine(WaitForAdvance());
        }

        FinishCutsceneView();
        Complete();
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        skipTypingRequested = false;
        dialogText.text = string.Empty;

        for (int i = 0; i < line.Length; i++)
        {
            if (skipTypingRequested)
            {
                dialogText.text = line;
                break;
            }

            dialogText.text += line[i];
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        skipTypingRequested = false;
    }

    private IEnumerator WaitForAdvance()
    {
        advanceRequested = false;

        while (!advanceRequested)
        {
            yield return null;
        }

        advanceRequested = false;
    }

    private void OnAdvanceButtonClicked()
    {
        if (isTyping)
        {
            skipTypingRequested = true;
            return;
        }

        advanceRequested = true;
    }

    private void FinishCutsceneView()
    {
        if (advanceButton != null && advanceListener != null)
        {
            advanceButton.onClick.RemoveListener(advanceListener);
        }

        if (hideDialogAfterFinish)
        {
            if (dialogPanelRoot != null)
            {
                dialogPanelRoot.SetActive(false);
            }
            else if (advanceButton != null)
            {
                advanceButton.gameObject.SetActive(false);
            }
        }
    }

    private void OnDisable()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        if (advanceButton != null && advanceListener != null)
        {
            advanceButton.onClick.RemoveListener(advanceListener);
        }
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

    private void ResetVisualState()
    {
        SetVisible(gambarJiroBiasa, true);
        SetVisible(gambarJiroNelpon, false);
        SetVisible(gambarJiroKaget, false);
        SetVisible(gambarPakRaka, false);
        SetVisible(gambarRuangTamuMatiTengah, true);
        SetVisible(gambarTetanggaDatang, false);
        SetVisible(gambarPerbaikiLampu, false);
        SetVisible(gambarTersetrum, false);
        SetVisible(gambarLampuKonslet, false);
    }

    private void ApplyVisualState(int lineIndex)
    {
        switch (lineIndex)
        {
            case 0:
                SetVisible(gambarJiroBiasa, true);
                SetVisible(gambarJiroNelpon, false);
                SetVisible(gambarJiroKaget, false);
                SetVisible(gambarPakRaka, false);
                SetVisible(gambarTetanggaDatang, false);
                SetVisible(gambarPerbaikiLampu, false);
                SetVisible(gambarTersetrum, false);
                SetVisible(gambarLampuKonslet, false);
                break;
            case 1:
                SetVisible(gambarJiroBiasa, false);
                SetVisible(gambarJiroNelpon, true);
                SetVisible(gambarJiroKaget, false);
                break;
            case 2:
            case 4:
                SetVisible(gambarJiroNelpon, false);
                SetVisible(gambarPakRaka, true);
                break;
            case 3:
            case 5:
                SetVisible(gambarJiroNelpon, true);
                SetVisible(gambarPakRaka, false);
                break;
            case 6:
                SetVisible(gambarJiroNelpon, false);
                SetVisible(gambarPakRaka, true);
                break;
            case 7:
                SetVisible(gambarJiroNelpon, true);
                SetVisible(gambarPakRaka, false);
                break;
            case 8:
                SetVisible(gambarJiroBiasa, false);
                SetVisible(gambarJiroNelpon, false);
                SetVisible(gambarPakRaka, false);
                SetVisible(gambarTetanggaDatang, true);
                break;
            case 9:
                SetVisible(gambarTetanggaDatang, true);
                SetVisible(gambarPakRaka, true);
                break;
            case 10:
                SetVisible(gambarJiroBiasa, true);
                SetVisible(gambarPakRaka, false);
                break;
            case 11:
                SetVisible(gambarJiroBiasa, false);
                SetVisible(gambarPakRaka, true);
                break;
            case 12:
                SetVisible(gambarJiroBiasa, true);
                SetVisible(gambarPakRaka, false);
                SetVisible(gambarPerbaikiLampu, true);
                SetVisible(gambarTetanggaDatang, false);
                break;
            case 13:
                SetVisible(gambarJiroBiasa, false);
                SetVisible(gambarPakRaka, true);
                break;
            case 14:
                SetVisible(gambarJiroBiasa, true);
                SetVisible(gambarPakRaka, false);
                SetVisible(gambarPerbaikiLampu, true);
                SetVisible(gambarTetanggaDatang, false);
                break;
            case 15:
                SetVisible(gambarJiroBiasa, false);
                SetVisible(gambarJiroKaget, false);
                SetVisible(gambarPakRaka, false);
                SetVisible(gambarPerbaikiLampu, false);
                SetVisible(gambarTetanggaDatang, false);
                SetVisible(gambarLampuKonslet, true);
                SetVisible(gambarTersetrum, false);
                SetVisible(gambarRuangTamuMatiTengah, false);
                break;
            case 16:
                SetVisible(gambarJiroKaget, true);
                SetVisible(gambarLampuKonslet, false);
                SetVisible(gambarTersetrum, true);
                SetVisible(gambarRuangTamuMatiTengah, true);
                break;
        }
    }

    private void SetVisible(GameObject target, bool visible)
    {
        if (target != null)
        {
            target.SetActive(visible);
        }
    }
}
