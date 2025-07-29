using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Update the side speed of the player using the UI Slider using Mouse input during runtime.
/// </summary>
[AddComponentMenu("UI/Side Force Slider UI")]
public class PlayerSideSpeedSlider : MonoBehaviour
{
    public PlayerMovement player; // Reference to the PlayerMovement script to control side speed

    public Slider slider; // UI component displayed during runtime

    public TextMeshProUGUI valueText; // Text component attached with the slider

    // Range of side force, keeping a range enough to give time for the player to see the movement
    public float minSideForce = 0f;
    public float maxSideForce = 15f;

    /// <summary>
    /// Start is called before first frame executes.
    /// </summary>
    void Start()
    {
        // Null check, find using script component if needed
        if (player == null) player = FindObjectOfType<PlayerMovement>();

        slider.minValue = minSideForce;
        slider.maxValue = maxSideForce;

        // Read values from the PlayerMovement script
        slider.value = player.SideSpeed;
        slider.onValueChanged.AddListener(OnSliderChanged);

        RefreshLabel(slider.value);
    }

    /// <summary>
    /// Slider changing event handler.
    /// </summary>
    /// <param name="sideSpeedValue">Value of Side Force</param>
    void OnSliderChanged(float sideSpeedValue)
    {
        player.SideSpeed = sideSpeedValue;
        RefreshLabel(sideSpeedValue);
    }

    /// <summary>
    /// Refresh the text label to display the current side force value applied.
    /// </summary>
    /// <param name="sideSpeedValue">Value of Side Force</param>
    void RefreshLabel(float sideSpeedValue)
    {
        if (valueText != null)
            valueText.text = $"{sideSpeedValue:0}";
    }
}