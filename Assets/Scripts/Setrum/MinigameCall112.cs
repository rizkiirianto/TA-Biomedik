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
        }

        isCardPhaseActive = true;
        SetupCardListeners();
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
