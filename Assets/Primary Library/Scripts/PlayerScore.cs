using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScore : MonoBehaviour
{
    public Transform player;
    public TextMeshProUGUI score;
    
    private void Update()
    {
        int playerScore = (int)player.position.z + 45;
        score.text = playerScore.ToString();
    }
}
