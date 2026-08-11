using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    public GameObject PanelScenario;
    public GameObject PanelScenarioEnglish;
    public Button startButton; // Optional: Drag the start button here in Inspector
    public Button firstScenarioButton; // Optional: Drag the first scenario button here

    [Header("Language Menus")]
    public GameObject EnglishMenu;
    public GameObject BahasaMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (PanelScenario != null) PanelScenario.SetActive(false);
        if (PanelScenarioEnglish != null) PanelScenarioEnglish.SetActive(false);
        UpdateLanguageMenuUI();
        
        // Auto-select Start Button for controller navigation
        if (startButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }
        else if (EventSystem.current != null)
        {
            // Fallback: try to find the first active button in the scene
            Button firstBtn = FindObjectOfType<Button>();
            if (firstBtn != null) EventSystem.current.SetSelectedGameObject(firstBtn.gameObject);
        }
    }

    public void SelectLanguageEnglish()
    {
        PlayerPrefs.SetString("SelectedLanguage", "EN");
        PlayerPrefs.Save();
        UpdateLanguageMenuUI();
    }

    public void SelectLanguageIndonesian()
    {
        PlayerPrefs.SetString("SelectedLanguage", "ID");
        PlayerPrefs.Save();
        UpdateLanguageMenuUI();
    }

    private void UpdateLanguageMenuUI()
    {
        string lang = PlayerPrefs.GetString("SelectedLanguage", "ID");
        if (EnglishMenu != null && BahasaMenu != null)
        {
            if (lang == "EN")
            {
                EnglishMenu.SetActive(true);
                BahasaMenu.SetActive(false);
            }
            else
            {
                EnglishMenu.SetActive(false);
                BahasaMenu.SetActive(true);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartButtonClicked()
    {
        string lang = PlayerPrefs.GetString("SelectedLanguage", "ID");
        GameObject activePanel = (lang == "EN" && PanelScenarioEnglish != null) ? PanelScenarioEnglish : PanelScenario;
        
        if (activePanel != null)
        {
            activePanel.SetActive(true);
            
            // Auto-select first scenario button for controller navigation
            if (firstScenarioButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstScenarioButton.gameObject);
            }
            else if (EventSystem.current != null)
            {
                Button firstScenarioBtn = activePanel.GetComponentInChildren<Button>();
                if (firstScenarioBtn != null) EventSystem.current.SetSelectedGameObject(firstScenarioBtn.gameObject);
            }
        }
    }

    public void Scenario1ButtonClicked()
    {
        PlayerPrefs.SetString("SelectedScenario", "Scenario1");
        PlayerPrefs.Save();
        SceneManager.LoadScene("Play");
    }

    public void Scenario2ButtonClicked()
    {
        PlayerPrefs.SetString("SelectedScenario", "Scenario2");
        PlayerPrefs.Save();
        SceneManager.LoadScene("Play");
    }
    public void Scenario3ButtonClicked()
    {
        PlayerPrefs.SetString("SelectedScenario", "Scenario3");
        PlayerPrefs.Save();
        SceneManager.LoadScene("Play");
    }

    public void ExitButtonClicked()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
