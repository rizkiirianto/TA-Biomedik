using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Minigame_Triage : MonoBehaviour, IMiniGame
{
    [Header ("UI Utama")]
    [SerializeField] private TextMeshProUGUI textFeedback;
    [SerializeField] private TextMeshProUGUI textUtama;
    [Header("Patient")]
    [SerializeField] private GameObject Tono;
    [SerializeField] private GameObject Siti;
    [SerializeField] private GameObject Budi;
    [Header("Text Panel Patient")]
    [SerializeField] private GameObject textTono;
    [SerializeField] private GameObject textSiti;
    [SerializeField] private GameObject textBudi;
    [Header("Button Triage")]
    [SerializeField] private GameObject buttonSet;
    [SerializeField] private Button kategoriHijau;
    [SerializeField] private Button kategoriKuning;
    [SerializeField] private Button kategoriMerah;
    [SerializeField] private Button kategoriHitam;

    [Header("Kategori Triage")]
    [SerializeField] private Button kategoriTriageButton;
    [SerializeField] private TextMeshProUGUI textButtonKategori;
    [SerializeField] private GameObject panelKategoriTriage;
    [Header("Panel Budi Meninggal")]
    [SerializeField] private GameObject panelTonoMeninggal;

    private GameManager gameManager;
    private enum Patient { None, Tono, Siti, Budi }
    private enum TriaseCategory { Hijau, Kuning, Merah, Hitam }

    private Patient currentPatient = Patient.None;
    private HashSet<Patient> completedPatients = new HashSet<Patient>();
    private bool awaitingFinalAction = false;
    private bool awaitingBudiAssignment = false;
    private Button btnTono;
    private Button btnSiti;
    private Button btnBudi;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Keep minimal initialization here; real setup occurs in BeginGame/InitializeMinigame
    }
    public void BeginGame(GameManager gm)
    {
        gameManager = gm;
        InitializeMinigame();
    }

    private void InitializeMinigame()
    {
        // Set initial UI text
        if (textUtama != null)
        {
            textUtama.text = PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Determine victim priority based on triage categories" : "tentukan prioritas korban berdasarkan jenis jenis kategori triase";
            textUtama.gameObject.SetActive(true);
        }
        if (textFeedback != null) textFeedback.text = "";

        // Panel Kategori awal
        if (panelKategoriTriage != null) panelKategoriTriage.SetActive(false);
        if (textButtonKategori != null) textButtonKategori.text = PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Triage Category" : "Kategori Triase";
        if (buttonSet != null) buttonSet.SetActive(false);

        // Prepare patient buttons
        btnTono = Tono != null ? Tono.GetComponent<Button>() : null;
        btnSiti = Siti != null ? Siti.GetComponent<Button>() : null;
        btnBudi = Budi != null ? Budi.GetComponent<Button>() : null;

        if (btnTono != null)
        {
            btnTono.onClick.RemoveAllListeners();
            btnTono.onClick.AddListener(() => OnPatientClicked(Patient.Tono));
        }
        if (btnSiti != null)
        {
            btnSiti.onClick.RemoveAllListeners();
            btnSiti.onClick.AddListener(() => OnPatientClicked(Patient.Siti));
        }
        if (btnBudi != null)
        {
            btnBudi.onClick.RemoveAllListeners();
            btnBudi.onClick.AddListener(() => OnPatientClicked(Patient.Budi));
        }

        // Kategori triage panel button
        if (kategoriTriageButton != null)
        {
            kategoriTriageButton.onClick.RemoveAllListeners();
            kategoriTriageButton.onClick.AddListener(ToggleKategoriPanel);
            if (textButtonKategori != null) textButtonKategori.text = panelKategoriTriage != null && panelKategoriTriage.activeSelf ? (PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Close Panel" : "Tutup Panel") : (PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Triage Category" : "Kategori Triase");
        }

        // Category buttons
        if (kategoriHijau != null) { kategoriHijau.onClick.RemoveAllListeners(); kategoriHijau.onClick.AddListener(() => OnCategorySelected(TriaseCategory.Hijau)); }
        if (kategoriKuning != null) { kategoriKuning.onClick.RemoveAllListeners(); kategoriKuning.onClick.AddListener(() => OnCategorySelected(TriaseCategory.Kuning)); }
        if (kategoriMerah != null) { kategoriMerah.onClick.RemoveAllListeners(); kategoriMerah.onClick.AddListener(() => OnCategorySelected(TriaseCategory.Merah)); }
        if (kategoriHitam != null) { kategoriHitam.onClick.RemoveAllListeners(); kategoriHitam.onClick.AddListener(() => OnCategorySelected(TriaseCategory.Hitam)); }

        // Ensure patient texts are hidden
        if (textTono != null) textTono.SetActive(false);
        if (textSiti != null) textSiti.SetActive(false);
        if (textBudi != null) textBudi.SetActive(false);

        // Reset state
        currentPatient = Patient.None;
        completedPatients.Clear();
        awaitingFinalAction = false;
        awaitingBudiAssignment = false;
        if (panelTonoMeninggal != null) panelTonoMeninggal.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // noop
    }

    private void ToggleKategoriPanel()
    {
        if (panelKategoriTriage == null || textButtonKategori == null) return;
        bool newState = !panelKategoriTriage.activeSelf;
        panelKategoriTriage.SetActive(newState);
        textButtonKategori.text = newState ? (PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Close Panel" : "Tutup Panel") : (PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Triage Category" : "Kategori Triase");
    }

    private void OnPatientClicked(Patient p)
    {
        if (awaitingFinalAction)
        {
            if (awaitingBudiAssignment)
            {
                if (p == Patient.Siti)
                {
                    gameManager.OnMiniGameComplete(PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Budi is assigned to help Siti. Minigame completed." : "Budi ditugaskan menolong Siti. Minigame selesai.");
                }
                else if (p == Patient.Tono)
                {
                    StartCoroutine(ShowTonoMeninggalPanel());
                }
            }
            else
            {
                // Final action stage
                if (p == Patient.Budi)
                {
                    awaitingBudiAssignment = true;
                    if (textUtama != null) textUtama.text = PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Budi has no basic first aid skills. Who do we assign Budi to help?" : "Budi tidak memiliki basic pertolongan pertama. Kita tugaskan Budi untuk menolong siapa?";
                    if (btnBudi != null) btnBudi.interactable = false; // Disable Budi since choice is between Tono and Siti
                }
                else
                {
                    if (textFeedback != null) textFeedback.text = PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Jiro cannot help two people at the same time" : "Jiro tidak bisa menolong dua orang disaat bersamaan";
                }
            }
            return;
        }

        if (completedPatients.Contains(p))
        {
            if (textFeedback != null) textFeedback.text = PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Already categorized." : "Sudah dikategorikan.";
            return;
        }

        currentPatient = p;

        // Deactivate other patient gameobjects
        if (Tono != null) Tono.SetActive(p == Patient.Tono || !completedPatients.Contains(Patient.Tono));
        if (Siti != null) Siti.SetActive(p == Patient.Siti || !completedPatients.Contains(Patient.Siti));
        if (Budi != null) Budi.SetActive(p == Patient.Budi || !completedPatients.Contains(Patient.Budi));

        // Hide main instruction
        if (textUtama != null) textUtama.gameObject.SetActive(false);

        // Show only the selected patient's text panel
        if (textTono != null) textTono.SetActive(p == Patient.Tono);
        if (textSiti != null) textSiti.SetActive(p == Patient.Siti);
        if (textBudi != null) textBudi.SetActive(p == Patient.Budi);

        if (buttonSet != null) buttonSet.SetActive(true);
        if (textFeedback != null) textFeedback.text = "";
    }

    private void OnCategorySelected(TriaseCategory cat)
    {
        if (currentPatient == Patient.None) return;

        bool correct = false;
        switch (currentPatient)
        {
            case Patient.Tono: correct = (cat == TriaseCategory.Merah); break;
            case Patient.Siti: correct = (cat == TriaseCategory.Merah); break;
            case Patient.Budi: correct = (cat == TriaseCategory.Hijau); break;
        }

        if (correct)
        {
            // mark completed and reset UI to main state
            completedPatients.Add(currentPatient);

            if (textTono != null) textTono.SetActive(false);
            if (textSiti != null) textSiti.SetActive(false);
            if (textBudi != null) textBudi.SetActive(false);

            if (buttonSet != null) buttonSet.SetActive(false);

            // Reactivate all patient GameObjects but disable interaction for completed ones
            if (Tono != null) Tono.SetActive(true);
            if (Siti != null) Siti.SetActive(true);
            if (Budi != null) Budi.SetActive(true);

            if (btnTono != null) btnTono.interactable = !completedPatients.Contains(Patient.Tono);
            if (btnSiti != null) btnSiti.interactable = !completedPatients.Contains(Patient.Siti);
            if (btnBudi != null) btnBudi.interactable = !completedPatients.Contains(Patient.Budi);

            if (textUtama != null) textUtama.gameObject.SetActive(true);

            currentPatient = Patient.None;

            // Check completion
            if (completedPatients.Count >= 3)
            {
                if (textUtama != null) textUtama.text = PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "What should Jiro do now?" : "Apa yang harus Jiro lakukan sekarang?";
                awaitingFinalAction = true;

                
                if (btnTono != null) btnTono.interactable = true;
                if (btnSiti != null) btnSiti.interactable = true;
                if (btnBudi != null) btnBudi.interactable = true;
            }
        }
        else
        {
            // Wrong answer: feedback and hint
            string hint = PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Try checking the patient's vital and danger signs." : "Coba periksa tanda vital dan tanda bahaya pasien.";
            if (currentPatient == Patient.Budi) hint = PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Budi looks more stable than the others." : "Budi tampak lebih stabil dibanding yang lainnya.";

            if (textFeedback != null) textFeedback.text = (PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "Incorrect. " : "Salah. ") + hint;

            // Disable category buttons briefly so player cannot progress until they see the feedback
            SetCategoryButtonsInteractable(false);
            StartCoroutine(ReenableCategoryButtonsAfterDelay(1.5f));
        }
    }

    private void SetCategoryButtonsInteractable(bool state)
    {
        if (kategoriHijau != null) kategoriHijau.interactable = state;
        if (kategoriKuning != null) kategoriKuning.interactable = state;
        if (kategoriMerah != null) kategoriMerah.interactable = state;
        if (kategoriHitam != null) kategoriHitam.interactable = state;
    }

    private IEnumerator ReenableCategoryButtonsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Only re-enable if still in a patient selection state (not completed all)
        if (!awaitingFinalAction)
        {
            SetCategoryButtonsInteractable(true);
        }
    }

    private IEnumerator ShowTonoMeninggalPanel()
    {
        if (panelTonoMeninggal != null) panelTonoMeninggal.SetActive(true);
        if (btnSiti != null) btnSiti.interactable = false;
        if (btnBudi != null) btnBudi.interactable = false;

        yield return new WaitForSeconds(3f);

        if (panelTonoMeninggal != null) panelTonoMeninggal.SetActive(false);
        if (btnSiti != null) btnSiti.interactable = true;
        if (btnBudi != null) btnBudi.interactable = true;
    }
}
