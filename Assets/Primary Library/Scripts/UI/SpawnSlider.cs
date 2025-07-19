using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds a UI Slider to the EnvironmentObjectSpawnManager.spawnPercentage.
/// </summary>
public class SpawnSlider : MonoBehaviour
{
    [Tooltip("Reference to the Environment Object Spawn Manager in the scene.")]
    public EnvironmentObjectSpawnManager spawnManager;

    [Tooltip("UI Slider that controls spawnPercentage (0–100).")]
    public Slider slider;

    [Tooltip("Optional: Text element to show current slider value.")]
    public TextMeshProUGUI valueText;

    void Start()
    {
        // Initialize slider range and start value
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = spawnManager.spawnPercentage;  // :contentReference[oaicite:0]{index=0}

        // Listen for changes
        slider.onValueChanged.AddListener(OnSliderChanged);
        UpdateLabel(slider.value);
    }

    void OnSliderChanged(float newValue)
    {
        // Apply to your spawner
        spawnManager.spawnPercentage = newValue;      // :contentReference[oaicite:1]{index=1}
        UpdateLabel(newValue);
    }

    void UpdateLabel(float v)
    {
        if (valueText != null)
            valueText.text = $"{v:0}%";
    }
}