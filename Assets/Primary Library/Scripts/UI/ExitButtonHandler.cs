using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script primarily works as an event handler to switch the scene from
/// Ground Scene (Game Scene) to Exit Report Scene (Crash Report).
/// </summary>
public class ExitButtonHandler : MonoBehaviour
{
    [SerializeField] private string _reportSceneName = "Exit Report";

    /// <summary>
    /// Triggered when instance is loaded.
    /// </summary>
    void Awake()
    {
        var exitButton = GetComponent<UnityEngine.UI.Button>();
        exitButton.onClick.AddListener(GoToReportScene);
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    void Update()
    {
        // Can also be triggered using 'Escape' key
        if (Input.GetKeyDown(KeyCode.Escape))
            GoToReportScene();
    }

    /// <summary>
    /// Switch scene function.
    /// </summary>
    private void GoToReportScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_reportSceneName, LoadSceneMode.Single);
    }
}