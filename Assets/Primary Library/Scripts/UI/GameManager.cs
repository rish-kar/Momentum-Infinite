using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attached to the Game Manager Object, script handles death logic to restart the game.
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] public float delayForRestart = 2f; // Delay for animation completion

    public GameObject deathScreen;

    /// <summary>
    /// Start is called before the first frame of the game is triggered.
    /// </summary>
    private void Start()
    {
        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "Death screen GameObject is not assigned in the inspector. Inconsistent ending might be observed.");
        }
    }

    /// <summary>
    /// Only trigger 'Game Over' animation when this function is called.
    /// </summary>
    public void GameEnds()
    {
        if (deathScreen) deathScreen.SetActive(true);
        Invoke("RestartGame", delayForRestart); // Lets the animation complete before triggering a restart
    }

    /// <summary>
    /// Loads the current scene to restart the game.
    /// </summary>
    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}