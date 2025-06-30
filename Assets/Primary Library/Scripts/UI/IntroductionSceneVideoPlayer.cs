using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroductionSceneVideoPlayer : MonoBehaviour
{
    public string nextSceneName = "Ground Level"; 
    private VideoPlayer videoPlayer;

    void Start()
    {
        // Get the VideoPlayer component
        videoPlayer = GetComponent<VideoPlayer>();

        // Subscribe to the VideoPlayer's loopPointReached event
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // Load the next scene when the video finishes
        SceneManager.LoadScene(nextSceneName);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Replace with your desired key
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
