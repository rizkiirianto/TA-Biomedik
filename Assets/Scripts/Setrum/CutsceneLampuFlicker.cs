using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CutsceneLampuFlicker : MonoBehaviour, ICutscene
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private Button advanceButton;
    [SerializeField] private GameObject dialogPanelRoot;

    [Header("Room Flicker Objects")]
    [SerializeField] private GameObject ruangTamuMenyala;
    [SerializeField] private GameObject ruangTamuLampuMati;
    [SerializeField] private GameObject gambarJiroBiasa;
    [SerializeField] private GameObject gambarJiroNesu;

    [Header("Dialogue")]
    [TextArea(2, 4)]
    [SerializeField] private string[] dialogueLines = new string[]
    {
        "Ahh akhirnya sampai rumah juga, hampir aja terjebak hujan deras ini.",
        "*lampu ruang tamu flickering",
        "Aduhh kenapa lagi inii....baru juga sampai rumah udah ada masalah lagi"
    };

    [Header("Typewriter")]
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private float flickerMinInterval = 0.04f;
    [SerializeField] private float flickerMaxInterval = 0.18f;
    [SerializeField] private bool hideDialogAfterFinish = true;
    [SerializeField] private bool destroyOnFinish = true;

    private GameManager gameManager;
    private Coroutine sequenceRoutine;
    private Coroutine flickerRoutine;
    private bool finished;
    private bool skipTypingRequested;
    private bool advanceRequested;
    private bool isTyping;
    private bool isFlickering;
    private UnityAction advanceListener;

    public void BeginCutscene(GameManager gm)
    {
        gameManager = gm;

        if (dialogPanelRoot != null)
        {
            dialogPanelRoot.SetActive(true);
        }

        if (dialogText != null)
        {
            dialogText.text = string.Empty;
        }

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
            if (i == 1)
            {
                StartFlickerEffect();
                gambarJiroBiasa.SetActive(false);
                gambarJiroNesu.SetActive(true);
            }

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
        StopFlickerEffect();

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
        StopFlickerEffect();

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

    private void StartFlickerEffect()
    {
        if (isFlickering)
        {
            return;
        }

        if (ruangTamuMenyala == null && ruangTamuLampuMati == null)
        {
            Debug.LogWarning("CutsceneLampuFlicker: objek flicker ruang tamu belum di-assign.");
            return;
        }

        isFlickering = true;
        SetRoomState(true);
        flickerRoutine = StartCoroutine(FlickerLoop());
    }

    private void StopFlickerEffect()
    {
        isFlickering = false;

        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
            flickerRoutine = null;
        }

        SetRoomState(true);
    }

    private IEnumerator FlickerLoop()
    {
        while (isFlickering)
        {
            bool showOffRoom = Random.value > 0.5f;
            SetRoomState(!showOffRoom);

            float waitTime = Random.Range(flickerMinInterval, flickerMaxInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void SetRoomState(bool showLitRoom)
    {
        if (ruangTamuMenyala != null)
        {
            ruangTamuMenyala.SetActive(showLitRoom);
        }

        if (ruangTamuLampuMati != null)
        {
            ruangTamuLampuMati.SetActive(!showLitRoom);
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
}
