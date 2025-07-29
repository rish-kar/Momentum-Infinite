using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Plays the introduction video on the first introduction scene.
/// </summary>
public class IntroductionSceneVideoPlayer : MonoBehaviour
{
    public string nextSceneName = "Ground Level"; // Name of the main scene in which the game is set up
    private VideoPlayer _videoPlayer;

    // Specific to WebGL build for deployment on web browser
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void PlayIntroVideo();
#endif

    /// <summary>
    /// Start is called before the first frame update.
    /// </summary>
    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Calling the JS Plugin for HTML video playback
            PlayIntroVideo();
#else
        _videoPlayer = GetComponent<VideoPlayer>(); // Game object present in the inspector with Video Attached
        _videoPlayer.loopPointReached += OnVideoFinished;
        _videoPlayer.Play();
#endif
    }

    /// <summary>
    /// After the video finishes playing, load the next scene.
    /// </summary>
    /// <param name="videoPlayerGameObject">Video Player Game Object</param>
    void OnVideoFinished(VideoPlayer videoPlayerGameObject)
    {
        SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// Update is usually triggered once per frame.
    /// </summary>
    void Update()
    {
        // Skip Intro Video
        if (Input.GetKeyDown(KeyCode.Space))
            SceneManager.LoadScene(nextSceneName);
    }
}