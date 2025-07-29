using TMPro;
using UnityEngine;

/// <summary>
/// Score update for the player based on their position.
/// </summary>
public class PlayerScore : MonoBehaviour
{
    [Header("References to Objects in the Inspector")]
    public Transform player;

    public TextMeshProUGUI score; // UI element to display the score

    [Header("Score Settings")] [SerializeField]
    private int _scoreOffset = 45; // Offset is used as the initial player position to Z-Axis is -45 units

    [SerializeField] private bool _debugScore = true;

    private PlayerMovement _playerMovement; // PlayerMovement scripts to track the position of the player
    private bool _isInitialised = false;

    /// <summary>
    /// Function called before the first frame of the game is triggered.
    /// </summary>
    private void Start()
    {
        Invoke(nameof(InitialiseReferences), 0.1f); // A slight delay to ensure objects are loaded
    }

    /// <summary>
    /// Initialise multiple references to track the score.
    /// </summary>
    private void InitialiseReferences()
    {
        Debug.Log("PlayerScore: Starting initialisation...", this);

        // Null check player object and then assign it
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player"); // Find by Object Name
            if (playerObject != null)
            {
                player = playerObject.transform;
                _playerMovement = playerObject.GetComponent<PlayerMovement>();
            }
            else
            {
                _playerMovement = FindFirstObjectByType<PlayerMovement>(); // Find by Script Attached
                if (_playerMovement != null)
                {
                    player = _playerMovement.transform;
                }
            }
        }

        // Find score UI if not assigned
        if (score == null)
        {
            Debug.Log("PlayerScore: Searching for UI elements...", this);


            GameObject scoreObj = GameObject.Find("ScoreText");
            if (scoreObj != null)
            {
                TextMeshProUGUI tmp = scoreObj.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    score = tmp;
                    Debug.Log($"PlayerScore: Found score UI by name '{name}': {scoreObj.name}", this);
                }
            }
        }

        // Final validation check before proceeding
        if (player == null)
        {
            Debug.LogError(
                "PlayerScore: CRITICAL - Player not found. Score will not be updated as there is no tracking mechanism in place.",
                this);
            return;
        }

        if (score == null)
        {
            Debug.LogError(
                "PlayerScore: CRITICAL - Score UI not found. Score might not get displayed correctly.",
                this);
            return;
        }

        _isInitialised = true;

        // Testing the score calculation
        int initialScore = Mathf.Max(0, (int)player.position.z + _scoreOffset);
        score.text = initialScore.ToString();
    }

    /// <summary>
    /// Update called once per frame.
    /// </summary>
    private void Update()
    {
        if (!_isInitialised)
        {
            if (Time.frameCount % 180 ==
                0) // Every 3 seconds (assuming 60 frames/second count) re-initialise references if not initialised
            {
                InitialiseReferences();
            }

            return;
        }

        UpdateScore();
    }

    /// <summary>
    /// Update the score element in the UI based on the position of the player.
    /// </summary>
    private void UpdateScore()
    {
        // Null check references first before moving for upgrade
        if (player == null || score == null)
        {
            Debug.LogWarning("PlayerScore: References lost, attempting re-initialisation...", this);
            _isInitialised = false;
            return;
        }

        // Calculate and update score to reflect in the UI
        int playerScore = Mathf.Max(0, (int)player.position.z + _scoreOffset);
        score.text = playerScore.ToString();

        if (_debugScore && Time.frameCount % 60 == 0) // Log once per second only if debug is enabled
        {
            Debug.Log($"Score: {playerScore} (Player Z: {player.position.z:F2})", this);
        }
    }
}