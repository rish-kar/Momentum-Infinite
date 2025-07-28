using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Runtime.InteropServices; // ← add this

public class IntroductionSceneVideoPlayer : MonoBehaviour
{
    public string nextSceneName = "Ground Level";
    private VideoPlayer videoPlayer;

    // Only include this declaration in a WebGL build
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void PlayIntroVideo();
#endif

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Call your JS plugin to show an HTML5 <video>
            PlayIntroVideo();
#else
        // Normal Unity VideoPlayer path
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Play();
#endif
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        SceneManager.LoadScene(nextSceneName);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            SceneManager.LoadScene(nextSceneName);
    }
}