using UnityEngine;


/// <summary>
/// Exit Game script used to quit the application attached to the Exit Game button.
/// </summary>
public class ExitGame : MonoBehaviour
{
    /// <summary>
    /// Exit application function.
    /// </summary>
    public void Quit()
    {
        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}