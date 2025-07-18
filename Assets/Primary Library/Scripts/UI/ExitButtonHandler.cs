using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitButtonHandler : MonoBehaviour
{
    [SerializeField] private string reportSceneName = "Exit Report";

    void Awake()
    {
        var btn = GetComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(GoToReportScene);
    }

    void Update()                       // ESC key shortcut
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            GoToReportScene();
    }

    private void GoToReportScene()
    {
        // make sure time is running (in case game was paused)
        Time.timeScale = 1f;
        SceneManager.LoadScene(reportSceneName, LoadSceneMode.Single);
    }
}