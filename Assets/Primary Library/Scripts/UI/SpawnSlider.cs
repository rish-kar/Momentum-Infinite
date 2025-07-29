using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Slider in UI to control the spawn rate to test the Evaluation mechanism.
/// </summary>
public class SpawnSlider : MonoBehaviour
{
    [Tooltip("Reference to Environment Spawn Manager in the main scene")]
    public EnvironmentObjectSpawnManager spawnManager;
    
    public Slider slider; // Range is from 1 to 100

    public TextMeshProUGUI valueText; // Text element attached to the slider to display the spawn percentage

    /// <summary>
    /// Called before the game starts and first frame is executed.
    /// </summary>
    void Start()
    {
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = spawnManager.spawnPercentage;  // References value from the EnvironmentObjectSpawnManager script
        
        // Triggering of event handler in case of a change
        slider.onValueChanged.AddListener(OnSliderChanged);
        UpdateSpawnLabel(slider.value);
    }

    /// <summary>
    /// Event handler for slider value changed.
    /// </summary>
    /// <param name="spawningNewValue"></param>
    void OnSliderChanged(float spawningNewValue)
    {
        spawnManager.spawnPercentage = spawningNewValue;     // Reverse assign this value to reflect the EnvironmentObjectSpawnManager script
        UpdateSpawnLabel(spawningNewValue);
    }

    /// <summary>
    /// Update the text label associated with the slider.
    /// </summary>
    /// <param name="spawningNewValue"></param>
    void UpdateSpawnLabel(float spawningNewValue)
    {
        if (valueText != null)
            valueText.text = $"{spawningNewValue:0}%";
    }
}