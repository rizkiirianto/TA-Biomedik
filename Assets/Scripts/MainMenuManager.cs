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
    public Button startButton; // Optional: Drag the start button here in Inspector
    public Button firstScenarioButton; // Optional: Drag the first scenario button here

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PanelScenario.SetActive(false);
        
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

    // Update is called once per frame
    void Update()
    {

    }

    public void StartButtonClicked()
    {
        PanelScenario.SetActive(true);
        
        // Auto-select first scenario button for controller navigation
        if (firstScenarioButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstScenarioButton.gameObject);
        }
        else if (EventSystem.current != null)
        {
            Button firstScenarioBtn = PanelScenario.GetComponentInChildren<Button>();
            if (firstScenarioBtn != null) EventSystem.current.SetSelectedGameObject(firstScenarioBtn.gameObject);
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
