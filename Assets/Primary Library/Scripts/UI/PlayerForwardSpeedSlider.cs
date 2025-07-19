using UnityEngine;
using UnityEngine.UI;
using TMPro;

[AddComponentMenu("UI/Player Speed Slider UI")]
public class PlayerForwardSpeedSlider : MonoBehaviour
{
    [Tooltip("The PlayerMovement component to drive.")]
    public PlayerMovement player;

    [Tooltip("The UI Slider that will set runSpeed.")]
    public Slider slider;

    [Tooltip("Optional: TextMeshProUGUI to display current speed.")]
    public TextMeshProUGUI speedText;

    void Start()
    {
        // make sure we have references
        if (player == null)
            player = FindObjectOfType<PlayerMovement>();

        // init slider range & value
        slider.minValue = 0f;
        slider.maxValue = 1000f;                // adjust to your desired max
        slider.value    = player.RunSpeed;
        slider.onValueChanged.AddListener(OnSliderChanged);
        RefreshLabel(slider.value);
    }

    void OnSliderChanged(float v)
    {
        // push into your movement script
        player.RunSpeed = v;                // :contentReference[oaicite:0]{index=0}
        RefreshLabel(v);
    }

    void RefreshLabel(float v)
    {
        if (speedText != null)
            speedText.text = $"{v:0}";
    }
}