using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinigameCall112 : MonoBehaviour
{
    [SerializeField] private GameObject panelDialog;
    [SerializeField] private TextMeshProUGUI textDialog;
    [SerializeField] private GameObject panelCall112;
    [SerializeField] private GameObject gambarJiro;
    [SerializeField] private TextMeshProUGUI textLayarHP;
    [SerializeField] private List<Image> tanganImages = new List<Image>();
    [SerializeField] private List<Button> numberButtons = new List<Button>(); // Isi sesuai urutan: 0,1,2,3,4,5,6,7,8,9
    [SerializeField] private Button callButton;
    [SerializeField] private float dialogDuration = 2f;
    [SerializeField] private float pressFeedbackDuration = 0.18f;

    private const string StartDialogText = "Aku harus memanggil bantuan";
    private const string EmergencyNumber112 = "112";
    private const string OperatorReplyText = "112, layanan apa yang bisa kami bantu?";
    private const string WrongNumberText = "Nomor darurat salah";
    private const int IdleHandIndex = 0;
    private const int CallHandIndex = 1;
    private const int NumberHandStartIndex = 2; // Index 2..11 = tombol 0..9
    private const int MaxDialDigits = 12;
    private const float LayarXWhenCenter = 0f;
    private const float LayarXWhenShifted = 0f;

    private Coroutine handFeedbackRoutine;
    private Coroutine callFlowRoutine;
    private string dialedNumber = string.Empty;
    private bool isCalling;

    void Start()
    {
        SetupButtonListeners();
        StartCoroutine(BeginMinigameFlow());
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
        if (isCalling)
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
        if (isCalling)
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
