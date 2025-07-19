using UnityEngine;
using UnityEngine.UI;
using TMPro;

[AddComponentMenu("UI/Side‐Force Slider UI")]
public class PlayerSideSpeedSlider : MonoBehaviour
{
    [Tooltip("Your PlayerMovement component")]
    public PlayerMovement player;

    [Tooltip("The UI Slider that controls sideSpeed")]
    public Slider slider;

    [Tooltip("Optional: TMP text to show the current side-force value")]
    public TextMeshProUGUI valueText;

    // Tweak these to your desired range
    public float minSideForce = 0f;
    public float maxSideForce = 15f;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerMovement>();

        slider.minValue = minSideForce;
        slider.maxValue = maxSideForce;

        // Read the Inspector‐set default straight away:
        slider.value = player.SideSpeed;
        slider.onValueChanged.AddListener(OnSliderChanged);

        RefreshLabel(slider.value);
    }

    void OnSliderChanged(float v)
    {
        player.SideSpeed = v;
        RefreshLabel(v);
    }

    void RefreshLabel(float v)
    {
        if (valueText != null)
            valueText.text = $"{v:0}";
    }
}