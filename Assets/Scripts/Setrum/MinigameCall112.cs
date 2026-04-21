using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MinigameCall112 : MonoBehaviour, IMiniGame
{
    [SerializeField] private GameObject panelDialog;
    [SerializeField] private TextMeshProUGUI textDialog;
    [SerializeField] private GameObject panelCall112;
    [SerializeField] private GameObject gambarJiro;
    [SerializeField] private GameObject panelDeckKartu;
    [SerializeField] private TextMeshProUGUI textLayarHP;
    [SerializeField] private List<Image> tanganImages = new List<Image>();
    [SerializeField] private List<Button> numberButtons = new List<Button>(); // Isi sesuai urutan: 0,1,2,3,4,5,6,7,8,9
    [SerializeField] private Button callButton;
    [SerializeField] private float dialogDuration = 2f;
    [SerializeField] private float pressFeedbackDuration = 0.18f;
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private GameObject nextStepRoot;
    [SerializeField] private bool hideDialogAtFinish = true;
    [SerializeField] private bool disableThisObjectAtFinish = true;
    [SerializeField] private UnityEvent onMinigameFinished;
    [SerializeField] private string minigameSuccessFeedback = "Panggilan 112 selesai.";
    [SerializeField] private List<string> correctCardIds = new List<string> { "Lokasi", "Lingkungan", "Vital" };
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tombolHPSound;
    [SerializeField] private AudioClip dialSound;
    [SerializeField] private AudioClip shuffleCardSound;
    [SerializeField] private AudioClip typewriterSound;
    [SerializeField] private AudioClip mainkanKartuSound;
    [SerializeField] private AudioClip hangupSound;

    private const string StartDialogText = "Aku harus memanggil bantuan";
    private const string EmergencyNumber112 = "112";
    private const string OperatorReplyText = "112, layanan apa yang bisa kami bantu?";
    private const string WrongNumberText = "Nomor darurat salah";
    private const string WaitingText = ". . . . .";
    private const string CallDisconnectedText = "--telepon terputus";
    private const string BatteryOutText = "SIAL KENAPA SEKARANG HABISNYA BATERAIKU!!!!";
    private const int RequiredCardSelections = 3;
    private const int IdleHandIndex = 0;
    private const int CallHandIndex = 1;
    private const int NumberHandStartIndex = 2; // Index 2..11 = tombol 0..9
    private const int MaxDialDigits = 12;
    private const float LayarXWhenCenter = 0f;
    private const float LayarXWhenShifted = 0f;
    private const float DeckEnterStartX = 2000f;
    private const float DeckEnterOvershootX = -100f;
    private const float DeckEnterFinalX = 0f;
    private const float DeckEnterFirstDuration = 0.5f;
    private const float DeckEnterSecondDuration = 0.28f;
    private const float RepeatedSfxGap = 0.5f;

    private Coroutine handFeedbackRoutine;
    private Coroutine callFlowRoutine;
    private Coroutine typingRoutine;
    private string dialedNumber = string.Empty;
    private bool isCalling;
    private bool isCardPhaseActive;
    private int selectedCardCount;
    private int correctSelectedCardCount;
    private bool isTypingDialog;
    private bool isWaitingForDialogContinue;
    private bool skipTypingRequested;
    private bool continueDialogRequested;
    private bool blockClickUntilRelease;
    private bool isMinigameFinished;
    private bool minigameInitialized;
    private GameManager gameManager;

    private bool IsCallingTextActive()
    {
        return textLayarHP != null && string.Equals(textLayarHP.text, "Calling", System.StringComparison.Ordinal);
    }

    private void StartDialLoopIfCallingText()
    {
        if (audioSource == null || dialSound == null)
        {
            return;
        }

        if (!IsCallingTextActive())
        {
            return;
        }

        if (audioSource.isPlaying && audioSource.clip == dialSound && audioSource.loop)
        {
            return;
        }

        audioSource.clip = dialSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void StopDialLoop()
    {
        if (audioSource == null)
        {
            return;
        }

        if (audioSource.clip == dialSound)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
        }
    }

    void Start()
    {
        InitializeMinigame();
    }

    public void BeginGame(GameManager gm)
    {
        gameManager = gm;
        InitializeMinigame();
    }

    private void InitializeMinigame()
    {
        if (minigameInitialized)
        {
            return;
        }

        minigameInitialized = true;
        SetupButtonListeners();
        StartCoroutine(BeginMinigameFlow());
    }

    private void Update()
    {
        if (blockClickUntilRelease)
        {
            if (!Input.GetMouseButton(0))
            {
                blockClickUntilRelease = false;
            }

            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (isTypingDialog)
        {
            skipTypingRequested = true;
            return;
        }

        if (isWaitingForDialogContinue)
        {
            continueDialogRequested = true;
        }
    }

    private IEnumerator BeginMinigameFlow()
    {
        SetOnlyHandActive(IdleHandIndex);
        isCalling = false;
        dialedNumber = string.Empty;
        textLayarHP.text = dialedNumber;

        if (panelCall112 != null)
        {
            panelCall112.SetActive(true);
        }

        if (panelDialog != null)
        {
            panelDialog.SetActive(true);
        }

        if (textDialog != null)
        {
            textDialog.text = StartDialogText;
        }

        yield return new WaitForSeconds(dialogDuration);

        if (panelDialog != null)
        {
            panelDialog.SetActive(false);
        }

        if (panelCall112 != null)
        {
            panelCall112.SetActive(true);
        }
        if (panelDeckKartu != null) 
        {
            panelDeckKartu.SetActive(false);
        }
        
    }

    private void SetupButtonListeners()
    {
        for (int i = 0; i < numberButtons.Count; i++)
        {
            if (numberButtons[i] == null)
            {
                continue;
            }

            int number = i;
            numberButtons[i].onClick.RemoveAllListeners();
            numberButtons[i].onClick.AddListener(() => OnNumberPressed(number));
        }

        if (callButton != null)
        {
            callButton.onClick.RemoveAllListeners();
            callButton.onClick.AddListener(OnCallPressed);
        }
    }

    private void OnNumberPressed(int number)
    {
        if (isCalling || isMinigameFinished)
        {
            return;
        }

        if (dialedNumber.Length < MaxDialDigits)
        {
            dialedNumber += number.ToString();
        }

        if (textLayarHP != null)
        {
            textLayarHP.text = dialedNumber;
        }

        int handIndex = NumberHandStartIndex + number;
        audioSource.PlayOneShot(tombolHPSound);
        PlayHandPressFeedback(handIndex);
    }

    private void OnCallPressed()
    {
        if (isCalling || isMinigameFinished)
        {
            return;
        }

        if (textLayarHP != null)
        {
            textLayarHP.text = "Calling";
            StartDialLoopIfCallingText();
        }

        PlayHandPressFeedback(CallHandIndex);

        if (dialedNumber == EmergencyNumber112)
        {
            if (callFlowRoutine != null)
            {
                StopCoroutine(callFlowRoutine);
            }

            callFlowRoutine = StartCoroutine(HandleSuccessful112Call());
            return;
        }

        if (callFlowRoutine != null)
        {
            StopCoroutine(callFlowRoutine);
        }

        callFlowRoutine = StartCoroutine(HandleWrongNumberCall());
    }

    private IEnumerator HandleSuccessful112Call()
    {
        isCalling = true;
        yield return new WaitForSeconds(dialogDuration);
        StopDialLoop();

        if (panelCall112 != null)
        {
            panelCall112.SetActive(false);
        }

        if (panelDialog != null)
        {
            panelDialog.SetActive(true);
        }

        if (textDialog != null)
        {
            textDialog.text = OperatorReplyText;
        }
        callFlowRoutine = null;

        yield return new WaitForSeconds(1.5f);
        StartCoroutine(CardSelectionPhase());
    }

    private IEnumerator CardSelectionPhase()
    {
        selectedCardCount = 0;
        correctSelectedCardCount = 0;

        if (textDialog != null)
        {
            textDialog.text = WaitingText;
        }

        if (gambarJiro != null)
        {
            gambarJiro.SetActive(true);
        }

        yield return new WaitForSeconds(1f);

        if (panelDialog != null)
        {
            panelDialog.SetActive(true);
        }

        if (panelDeckKartu != null)
        {
            panelDeckKartu.SetActive(true);

            if (audioSource != null && shuffleCardSound != null)
            {
                audioSource.PlayOneShot(shuffleCardSound);
            }

            yield return StartCoroutine(PlayDeckEntranceAnimation());
        }

        isCardPhaseActive = true;
        SetupCardListeners();
    }

    private IEnumerator PlayDeckEntranceAnimation()
    {
        if (panelDeckKartu == null)
        {
            yield break;
        }

        RectTransform deckRect = panelDeckKartu.GetComponent<RectTransform>();
        if (deckRect == null)
        {
            yield break;
        }

        Vector2 anchoredPos = deckRect.anchoredPosition;
        deckRect.anchoredPosition = new Vector2(DeckEnterStartX, anchoredPos.y);

        yield return StartCoroutine(AnimateRectTransformX(deckRect, DeckEnterOvershootX, DeckEnterFirstDuration));
        yield return StartCoroutine(AnimateRectTransformX(deckRect, DeckEnterFinalX, DeckEnterSecondDuration));
    }

    private IEnumerator AnimateRectTransformX(RectTransform targetRect, float targetX, float duration)
    {
        if (targetRect == null)
        {
            yield break;
        }

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        float startX = targetRect.anchoredPosition.x;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            Vector2 pos = targetRect.anchoredPosition;
            pos.x = Mathf.Lerp(startX, targetX, easedT);
            targetRect.anchoredPosition = pos;
            yield return null;
        }

        Vector2 finalPos = targetRect.anchoredPosition;
        finalPos.x = targetX;
        targetRect.anchoredPosition = finalPos;
    }

    private void SetupCardListeners()
    {
        CallCard[] cards = panelDeckKartu.GetComponentsInChildren<CallCard>();
        foreach (CallCard card in cards)
        {
            card.SetupCard(() => OnCardSelected(card));
        }
    }

    private void OnCardSelected(CallCard selectedCard)
    {
        if (!isCardPhaseActive)
        {
            return;
        }

        isCardPhaseActive = false;

        if (audioSource != null && mainkanKartuSound != null)
        {
            audioSource.PlayOneShot(mainkanKartuSound);
        }

        selectedCard.RemoveCard();
        selectedCardCount++;

        if (IsCorrectCard(selectedCard.GetCardId()))
        {
            correctSelectedCardCount++;
        }

        blockClickUntilRelease = true;

        string cardDialog = selectedCard.GetCardDialog();
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(HandleCardDialogFlow(cardDialog));
    }

    private IEnumerator HandleCardDialogFlow(string cardDialog)
    {
        if (panelDialog != null)
        {
            panelDialog.SetActive(true);
        }

        yield return StartCoroutine(TypeDialogText(cardDialog));
        yield return StartCoroutine(WaitForDialogContinue());

        if (selectedCardCount >= RequiredCardSelections)
        {
            panelDeckKartu.SetActive(false);
            gambarJiro.SetActive(false);
            yield return StartCoroutine(TypeDialogText(CallDisconnectedText));
            yield return StartCoroutine(PlayOneShotRepeated(hangupSound, 3));
            yield return StartCoroutine(WaitForDialogContinue());
            yield return StartCoroutine(TypeDialogText(BatteryOutText));
            yield return StartCoroutine(WaitForDialogContinue());

            FinishMinigameAndProceed();

            typingRoutine = null;
            yield break;
        }

        isCardPhaseActive = true;
        typingRoutine = null;
    }

    private IEnumerator PlayOneShotRepeated(AudioClip clip, int repeatCount)
    {
        if (audioSource == null || clip == null || repeatCount <= 0)
        {
            yield break;
        }

        for (int i = 0; i < repeatCount; i++)
        {
            audioSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length);

            if (i < repeatCount - 1)
            {
                yield return new WaitForSeconds(RepeatedSfxGap);
            }
        }
    }

    private IEnumerator TypeDialogText(string fullText)
    {
        if (textDialog == null)
        {
            yield break;
        }

        textDialog.text = string.Empty;
        skipTypingRequested = false;
        isTypingDialog = true;

        float safeTypingSpeed = Mathf.Max(0.005f, typingSpeed);
        for (int i = 0; i < fullText.Length; i++)
        {
            if (skipTypingRequested)
            {
                textDialog.text = fullText;
                break;
            }

            textDialog.text += fullText[i];

            if (audioSource != null && typewriterSound != null && !char.IsWhiteSpace(fullText[i]))
            {
                audioSource.PlayOneShot(typewriterSound);
            }

            yield return new WaitForSeconds(safeTypingSpeed);
        }

        isTypingDialog = false;
        skipTypingRequested = false;
    }

    private IEnumerator WaitForDialogContinue()
    {
        continueDialogRequested = false;
        isWaitingForDialogContinue = true;

        while (!continueDialogRequested)
        {
            yield return null;
        }

        isWaitingForDialogContinue = false;
        continueDialogRequested = false;
    }

    private void FinishMinigameAndProceed()
    {
        if (isMinigameFinished)
        {
            return;
        }

        isMinigameFinished = true;
        isCardPhaseActive = false;
        isCalling = false;
        StopDialLoop();

        if (panelCall112 != null)
        {
            panelCall112.SetActive(false);
        }

        if (panelDeckKartu != null)
        {
            panelDeckKartu.SetActive(false);
        }

        if (gambarJiro != null)
        {
            gambarJiro.SetActive(false);
        }

        if (hideDialogAtFinish && panelDialog != null)
        {
            panelDialog.SetActive(false);
        }

        onMinigameFinished?.Invoke();

        if (gameManager != null)
        {
            gameManager.RegisterMinigameCall112Result(correctSelectedCardCount, RequiredCardSelections);
            gameManager.OnMiniGameComplete(minigameSuccessFeedback);
        }
        else if (nextStepRoot != null)
        {
            nextStepRoot.SetActive(true);
        }

        if (disableThisObjectAtFinish)
        {
            gameObject.SetActive(false);
        }
    }

    private bool IsCorrectCard(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return false;
        }

        for (int i = 0; i < correctCardIds.Count; i++)
        {
            if (string.Equals(correctCardIds[i], cardId, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator HandleWrongNumberCall()
    {
        isCalling = true;
        yield return new WaitForSeconds(dialogDuration);
        StopDialLoop();

        if (panelCall112 != null)
        {
            panelCall112.SetActive(false);
        }

        if (panelDialog != null)
        {
            panelDialog.SetActive(true);
        }

        if (textDialog != null)
        {
            textDialog.text = WrongNumberText;
        }

        // Tampilkan pesan salah sebentar, lalu kembalikan ke mode input nomor.
        yield return new WaitForSeconds(dialogDuration);

        dialedNumber = string.Empty;

        if (textLayarHP != null)
        {
            textLayarHP.text = dialedNumber;
        }

        if (panelDialog != null)
        {
            panelDialog.SetActive(false);
        }

        if (panelCall112 != null)
        {
            panelCall112.SetActive(true);
        }

        SetOnlyHandActive(IdleHandIndex);
        isCalling = false;

        callFlowRoutine = null;
    }

    private void PlayHandPressFeedback(int pressedHandIndex)
    {
        if (handFeedbackRoutine != null)
        {
            StopCoroutine(handFeedbackRoutine);
        }

        handFeedbackRoutine = StartCoroutine(HandPressFeedbackRoutine(pressedHandIndex));
    }

    private IEnumerator HandPressFeedbackRoutine(int pressedHandIndex)
    {
        SetOnlyHandActive(pressedHandIndex);
        yield return new WaitForSeconds(pressFeedbackDuration);
        SetOnlyHandActive(IdleHandIndex);
        handFeedbackRoutine = null;
    }

    private void SetOnlyHandActive(int activeIndex)
    {
        for (int i = 0; i < tanganImages.Count; i++)
        {
            if (tanganImages[i] == null)
            {
                continue;
            }

            tanganImages[i].gameObject.SetActive(i == activeIndex);
        }

        if (textLayarHP != null)
        {
            RectTransform layarRect = textLayarHP.rectTransform;
            Vector3 posisi = layarRect.localPosition;
            bool useCenterX = activeIndex == 0 || activeIndex == 3 || activeIndex == 4;
            posisi.x = useCenterX ? LayarXWhenCenter : LayarXWhenShifted;
            layarRect.localPosition = posisi;
        }
    }

    private void OnDestroy()
    {
        StopDialLoop();

        if (callFlowRoutine != null)
        {
            StopCoroutine(callFlowRoutine);
        }

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        for (int i = 0; i < numberButtons.Count; i++)
        {
            if (numberButtons[i] != null)
            {
                numberButtons[i].onClick.RemoveAllListeners();
            }
        }

        if (callButton != null)
        {
            callButton.onClick.RemoveAllListeners();
        }
    }
}
