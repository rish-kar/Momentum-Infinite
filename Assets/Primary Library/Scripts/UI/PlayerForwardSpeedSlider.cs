using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Slider in UI to control the forward force attached to the player.
/// </summary>
[AddComponentMenu("UI/Player Speed Slider UI")]
public class PlayerForwardSpeedSlider : MonoBehaviour
{
    public PlayerMovement player;

    [Tooltip("UI Slider to control the forward speed of the player.")]
    public Slider slider;

    [Tooltip("Text to display current speed.")]
    public TextMeshProUGUI speedText;

    /// <summary>
    /// Start is called before the first frame of the game begins.
    /// </summary>
    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerMovement>();

        // Range of speed
        slider.minValue = 0f;
        slider.maxValue = 1000f;

        // Current run speed
        slider.value = player.RunSpeed;
        slider.onValueChanged.AddListener(OnSliderChanged);
        RefreshLabel(slider.value);
    }

    /// <summary>
    /// Triggered when the slider is changed using mouse during the game run.
    /// </summary>
    /// <param name="speedValue">Value of Player's Current Speed</param>
    void OnSliderChanged(float speedValue)
    {
        player.RunSpeed = speedValue; // Assigning value to the player movement script
        RefreshLabel(speedValue);
    }

    /// <summary>
    /// Refreshes the text label which shows the current speed.
    /// </summary>
    /// <param name="speedValue">Value of Player's Current Speed</param>
    void RefreshLabel(float speedValue)
    {
        if (speedText != null)
            speedText.text = $"{speedValue:0}";
    }
}