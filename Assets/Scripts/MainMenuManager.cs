using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    public GameObject PanelScenario;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PanelScenario.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartButtonClicked()
    {
        PanelScenario.SetActive(true);
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
