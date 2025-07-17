using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScore : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public TextMeshProUGUI score;
    
    [Header("Score Settings")]
    [SerializeField] private int scoreOffset = 45;
    [SerializeField] private bool debugScore = true;
    
    private PlayerMovement playerMovement;
    private bool isInitialized = false;
    
    private void Start()
    {
        // Delay initialization to ensure all objects are loaded
        Invoke(nameof(InitializeReferences), 0.1f);
    }
    
    private void InitializeReferences()
    {
        Debug.Log("PlayerScore: Starting initialization...", this);
        
        // Find player if not assigned
        if (player == null)
        {
            // Method 1: Try by tag
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerMovement = playerObj.GetComponent<PlayerMovement>();
                Debug.Log($"PlayerScore: Found player by tag: {playerObj.name} at position {player.position}", this);
            }
            else
            {
                // Method 2: Find by PlayerMovement component
                playerMovement = FindFirstObjectByType<PlayerMovement>();
                if (playerMovement != null)
                {
                    player = playerMovement.transform;
                    Debug.Log($"PlayerScore: Found player by component: {playerMovement.name} at position {player.position}", this);
                }
                else
                {
                    // Method 3: Search by name
                    GameObject foundPlayer = GameObject.Find("Player");
                    if (foundPlayer == null) foundPlayer = GameObject.Find("Crypto");
                    if (foundPlayer != null)
                    {
                        player = foundPlayer.transform;
                        playerMovement = foundPlayer.GetComponent<PlayerMovement>();
                        Debug.Log($"PlayerScore: Found player by name: {foundPlayer.name} at position {player.position}", this);
                    }
                }
            }
        }
        
        // Find score UI if not assigned
        if (score == null)
        {
            Debug.Log("PlayerScore: Searching for UI elements...", this);
            
            // Method 1: Try specific names
            string[] possibleNames = {"Score", "ScoreText", "UI_Score", "Text_Score", "PlayerScore"};
            foreach (string name in possibleNames)
            {
                GameObject scoreObj = GameObject.Find(name);
                if (scoreObj != null)
                {
                    TextMeshProUGUI tmp = scoreObj.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        score = tmp;
                        Debug.Log($"PlayerScore: Found score UI by name '{name}': {scoreObj.name}", this);
                        break;
                    }
                    
                    Text legacyText = scoreObj.GetComponent<Text>();
                    if (legacyText != null)
                    {
                        Debug.LogWarning($"Found Text component instead of TextMeshPro on {name}. Please use TextMeshPro for better performance.", this);
                    }
                }
            }
            
            // Method 2: Search in Canvas
            if (score == null)
            {
                Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                Debug.Log($"PlayerScore: Found {allCanvases.Length} canvases in scene", this);
                
                foreach (Canvas canvas in allCanvases)
                {
                    TextMeshProUGUI[] allTexts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true); // Include inactive
                    Debug.Log($"PlayerScore: Canvas '{canvas.name}' has {allTexts.Length} TextMeshPro components", this);
                    
                    foreach (var text in allTexts)
                    {
                        Debug.Log($"PlayerScore: Found TextMeshPro: '{text.name}' with text: '{text.text}'", this);
                        
                        // Look for likely score elements
                        if (text.name.ToLower().Contains("score") || 
                            text.text == "0" || text.text == "" || 
                            text.name.ToLower().Contains("text"))
                        {
                            score = text;
                            Debug.Log($"PlayerScore: Selected TextMeshPro: {text.name}", this);
                            break;
                        }
                    }
                    
                    if (score != null) break;
                }
            }
            
            // Method 3: Just take the first one found
            if (score == null)
            {
                TextMeshProUGUI[] allTMPs = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
                if (allTMPs.Length > 0)
                {
                    score = allTMPs[0];
                    Debug.Log($"PlayerScore: Using first TextMeshPro found: {score.name}", this);
                }
            }
        }
        
        // Final validation
        if (player == null)
        {
            Debug.LogError("PlayerScore: CRITICAL - Player not found! Ensure player GameObject exists with 'Player' tag or PlayerMovement component.", this);
            return;
        }
        
        if (score == null)
        {
            Debug.LogError("PlayerScore: CRITICAL - Score UI not found! Ensure there's a TextMeshPro component in the scene for displaying score.", this);
            return;
        }
        
        isInitialized = true;
        
        // Test the score calculation immediately
        int initialScore = Mathf.Max(0, (int)player.position.z + scoreOffset);
        score.text = initialScore.ToString();
        
        Debug.Log($"PlayerScore: SUCCESS! Player: {player.name} at {player.position}, Score UI: {score.name}, Initial Score: {initialScore}", this);
    }
    
    private void Update()
    {
        if (!isInitialized)
        {
            // Try to re-initialize every few seconds
            if (Time.frameCount % 180 == 0) // Every 3 seconds
            {
                InitializeReferences();
            }
            return;
        }
        
        UpdateScore();
    }
    
    private void UpdateScore()
    {
        // Validate references are still good
        if (player == null || score == null)
        {
            Debug.LogWarning("PlayerScore: References lost, attempting re-initialization...", this);
            isInitialized = false;
            return;
        }
        
        // Calculate and update score
        int playerScore = Mathf.Max(0, (int)player.position.z + scoreOffset);
        score.text = playerScore.ToString();
        
        if (debugScore && Time.frameCount % 60 == 0) // Log once per second
        {
            Debug.Log($"Score: {playerScore} (Player Z: {player.position.z:F2})", this);
        }
    }
}
