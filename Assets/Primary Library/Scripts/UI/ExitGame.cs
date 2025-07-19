using UnityEngine;

public class ExitGame : MonoBehaviour
{
    /// <summary>
    /// Call this on your Exit button's OnClick.
    /// </summary>
    public void Quit()
    {
        // If running in a built player:
        Application.Quit();

        // If in the Editor, stop Play mode:
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}