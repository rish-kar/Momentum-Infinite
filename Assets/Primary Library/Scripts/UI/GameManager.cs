using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] public float restartDelay = 2f;

    public GameObject deathScreen;


    private void Start()
    {
        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Death screen GameObject is not assigned in the inspector.");
        }
    }

    public void GameEnds()
    {
        Debug.Log("Game Over");
        if (deathScreen) deathScreen.SetActive(true);
        Invoke("RestartGame", restartDelay);
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}