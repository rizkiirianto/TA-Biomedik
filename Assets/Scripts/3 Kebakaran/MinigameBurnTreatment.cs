using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinigameBurnTreatment : MonoBehaviour, IMiniGame
{
    [Header("UI & Buttons")]
    public Button WatchButton;
    public Button ElbowBandButton;
    public Button nextButton;
    
    [Header("Arm Visuals")]
    public GameObject patienArmAccessories;
    public GameObject patienArmNoWatch;
    public GameObject patienArmNoBand;
    
    [Header("Text Panel")]
    public GameObject panelText;
    public TextMeshProUGUI text;
    
    [Header("Water Pouring Segment")]
    public GameObject waterPouringSegment;
    public TreatmentManager treatmentManager;

    private GameManager gameManager;
    private bool isTyping = false;
    private string fullText = "Segera lepaskan jam tangan dan perhiasan ketat dari lengan korban. Luka bakar tingkat 2 akan memicu pembengkakan (edema) dengan cepat. Jika tidak segera dilepas, aksesoris dapat berubah menjadi torniket yang mencekik aliran darah.";
    private Coroutine typingCoroutine;
    
    private bool watchRemoved = false;
    private bool bandRemoved = false;
    private bool treatmentPhaseActive = false;
    private bool isFinished = false;

    public void BeginGame(GameManager gm)
    {
        gameManager = gm;
        
        watchRemoved = false;
        bandRemoved = false;
        treatmentPhaseActive = false;
        isFinished = false;

        UpdateArmVisuals();
        waterPouringSegment.SetActive(false);
        
        if (WatchButton != null) WatchButton.gameObject.SetActive(false);
        if (ElbowBandButton != null) ElbowBandButton.gameObject.SetActive(false);

        if (panelText != null) panelText.SetActive(true);
        if (nextButton != null) nextButton.gameObject.SetActive(true);

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnNextButtonClicked);

        WatchButton.onClick.RemoveAllListeners();
        WatchButton.onClick.AddListener(OnWatchClicked);

        ElbowBandButton.onClick.RemoveAllListeners();
        ElbowBandButton.onClick.AddListener(OnElbowBandClicked);

        typingCoroutine = StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        if (text != null) text.text = "";
        foreach (char c in fullText.ToCharArray())
        {
            if (text != null) text.text += c;
            yield return new WaitForSeconds(0.03f);
        }
        isTyping = false;
    }

    private void OnNextButtonClicked()
    {
        if (isTyping)
        {
            // Skip typing and show full text
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            if (text != null) text.text = fullText;
            isTyping = false;
        }
        else
        {
            // Hide text panel and next button, start accessory removal phase
            if (panelText != null) panelText.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);
            
            if (WatchButton != null) WatchButton.gameObject.SetActive(true);
            if (ElbowBandButton != null) ElbowBandButton.gameObject.SetActive(true);
            
            if (WatchButton != null) WatchButton.interactable = true;
            if (ElbowBandButton != null) ElbowBandButton.interactable = true;
        }
    }

    private void OnWatchClicked()
    {
        watchRemoved = true;
        WatchButton.gameObject.SetActive(false);
        UpdateArmVisuals();
        CheckAccessoriesRemoved();
    }

    private void OnElbowBandClicked()
    {
        bandRemoved = true;
        ElbowBandButton.gameObject.SetActive(false);
        UpdateArmVisuals();
        CheckAccessoriesRemoved();
    }

    private void UpdateArmVisuals()
    {
        if (patienArmAccessories != null) patienArmAccessories.SetActive(!watchRemoved && !bandRemoved);
        if (patienArmNoWatch != null) patienArmNoWatch.SetActive(watchRemoved && !bandRemoved);
        if (patienArmNoBand != null) patienArmNoBand.SetActive(!watchRemoved && bandRemoved);
    }

    private void CheckAccessoriesRemoved()
    {
        if (watchRemoved && bandRemoved)
        {
            if (patienArmNoWatch != null) patienArmNoWatch.SetActive(false);
            if (patienArmNoBand != null) patienArmNoBand.SetActive(false);
            
            if (waterPouringSegment != null) waterPouringSegment.SetActive(true);
            treatmentPhaseActive = true;
        }
    }

    void Update()
    {
        if (treatmentPhaseActive && !isFinished && treatmentManager != null)
        {
            if (treatmentManager.isComplete)
            {
                isFinished = true;
                StartCoroutine(FinishMinigame());
            }
        }

        // FALLBACK: Jika tombol aksesoris adalah objek 2D dengan Collider2D (bukan UI Canvas)
        if (!treatmentPhaseActive && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                if (WatchButton != null && WatchButton.gameObject.activeSelf && WatchButton.interactable && hit.collider.gameObject == WatchButton.gameObject)
                {
                    OnWatchClicked();
                }
                else if (ElbowBandButton != null && ElbowBandButton.gameObject.activeSelf && ElbowBandButton.interactable && hit.collider.gameObject == ElbowBandButton.gameObject)
                {
                    OnElbowBandClicked();
                }
            }
        }
    }

    private IEnumerator FinishMinigame()
    {
        yield return new WaitForSeconds(1.5f);
        if (gameManager != null)
        {
            gameManager.OnMiniGameComplete(PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Accessories have been removed and the burn area successfully cooled." : "Aksesoris telah dilepas dan area luka bakar berhasil didinginkan.");
        }
    }
}
