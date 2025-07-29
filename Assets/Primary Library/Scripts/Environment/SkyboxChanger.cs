using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Skybox Changer script will change the skybox according to the distance travelled by the player.
/// It will also play the video associated with the skybox which are defined in the inspector.
/// </summary>
public class SkyboxChanger : MonoBehaviour
{
    [Header("References to External Fields")]
    public Transform playerTransform;

    [SerializeField] VideoPlayer videoPlayer;

    [Header("Distances")] public int distanceInterval = 2000;
    public float durationOfTransition = 13.0f;

    [Header("Assets")] [SerializeField]
    List<Material> listOfSkyboxes = new(); // A list that will contain all the skyboxes

    [SerializeField]
    List<VideoClip> listOfVideoClips = new(); // A list that will hold all the videoclips to be used as skyboxes

    public int CurrentSkyboxIdx { get; private set; } // Getter and Setter

    readonly Stack<int> usedSkyboxes = new(); // List of used skyboxes to avoid repetition
    private int _nextSwapZAxis; // Next Z axis position where the skybox needs o be changed
    private bool _isTransitioning; // Currently checks if in transitioning state
    private Material _currentSkyboxInstance; // Current skybox material instance
    private Material _nextSkyboxInstance; // Next skybox material instance to be used during transition
    private Coroutine _transition; // Coroutine for handling the transition
    private float _defaultExposure = 1.0f;


    /// <summary>
    /// Awake is called at the beginning of the game.
    /// </summary>
    void Awake()
    {
        if (listOfSkyboxes.Count != listOfVideoClips.Count || listOfSkyboxes.Count == 0)
        {
            enabled = false;
            return;
        }

        int randomIndex = Random.Range(0, listOfSkyboxes.Count);
        InitialiseEnvironment(randomIndex);
        usedSkyboxes.Push(randomIndex); // Using a stacking mechanism to avoid repetition of skyboxes
        _nextSwapZAxis = distanceInterval;
    }

    /// <summary>
    /// Create a new instance and initialise the environment with the given index.
    /// </summary>
    /// <param name="index">Skybox Index</param>
    void InitialiseEnvironment(int index)
    {
        _currentSkyboxInstance = new Material(listOfSkyboxes[index]); // Create a material instance on the fly 
        RenderSettings.skybox = _currentSkyboxInstance; // Switch the render settings to the current skybox instance

        if (_currentSkyboxInstance.HasProperty("_Exposure"))
        {
            _defaultExposure = _currentSkyboxInstance.GetFloat("_Exposure"); // Fade Transition effect
        }

        videoPlayer.clip = listOfVideoClips[index];
        videoPlayer.Play(); // Plays the matching video clip for the skybox
        DynamicGI.UpdateEnvironment(); // Update the lighting environment
        CurrentSkyboxIdx = index; // Switch the index
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    void Update()
    {
        if (!playerTransform || _isTransitioning) return;

        // Pick new skybox when the plyaer reaches the Z axis position for the change
        if (playerTransform.position.z >= _nextSwapZAxis)
        {
            _nextSwapZAxis += distanceInterval;
            int nextIndex = PickNextIndex();

            if (_transition != null)
                StopCoroutine(_transition);

            _transition = StartCoroutine(TransitionRoutine(nextIndex));
        }
    }

    /// <summary>
    /// Function responsible for picking the next index of the skybox to be used in the game.
    /// </summary>
    /// <returns></returns>
    int PickNextIndex()
    {
        if (usedSkyboxes.Count == listOfSkyboxes.Count) usedSkyboxes.Clear();

        List<int> skyboxPool = new(); // Create a pool of available skyboxes
        for (int i = 0; i < listOfSkyboxes.Count; i++)
            if (!usedSkyboxes.Contains(i))
                skyboxPool.Add(i);

        int skyboxChoice = skyboxPool[Random.Range(0, skyboxPool.Count)]; // Select randomly from the pool
        usedSkyboxes.Push(skyboxChoice);
        return skyboxChoice;
    }

    /// <summary>
    /// The Transition Routine deals with handling the transition between skyboxes.
    /// </summary>
    /// <param name="nextIndex">Next Skybox Index</param>
    /// <returns>Wait null object</returns>
    IEnumerator TransitionRoutine(int nextIndex)
    {
        _isTransitioning = true; // Flag to prevent intrupt while transitioning

        // Create next material instance in advance for smoother transition
        _nextSkyboxInstance = new Material(listOfSkyboxes[nextIndex]);
        float nextSkyboxExposure = _defaultExposure;
        if (_nextSkyboxInstance.HasProperty("_Exposure"))
        {
            nextSkyboxExposure = _nextSkyboxInstance.GetFloat("_Exposure");
        }

        float fadeOutTransitionTime =
            durationOfTransition * 0.5f; // Sets fade out duration to half the total transition time
        float timer = 0f;
        while (timer < fadeOutTransitionTime)
        {
            timer += Time.deltaTime;
            float exposureTimer = Mathf.Clamp01(timer / fadeOutTransitionTime);

            // Reduces skybox exposure to black for the new skybox to load
            if (_currentSkyboxInstance.HasProperty("_Exposure"))
            {
                _currentSkyboxInstance.SetFloat("_Exposure", Mathf.Lerp(_defaultExposure, 0f, exposureTimer));
            }

            DynamicGI.UpdateEnvironment(); // Updates lighting system
            yield return null;
        }

        // Condition to ensure that the exposure is zero and the skybox is completely black before switching
        if (_currentSkyboxInstance.HasProperty("_Exposure"))
            _currentSkyboxInstance.SetFloat("_Exposure", 0f);

        // Render new material update
        RenderSettings.skybox = _nextSkyboxInstance;
        _currentSkyboxInstance = _nextSkyboxInstance;
        videoPlayer.clip = listOfVideoClips[nextIndex];
        videoPlayer.Play();
        usedSkyboxes.Push(nextIndex);
        CurrentSkyboxIdx = nextIndex;
        DynamicGI.UpdateEnvironment();

        // Condition to ensure that the exposure is zero and the skybox is completely black before switching
        if (_currentSkyboxInstance.HasProperty("_Exposure"))
            _currentSkyboxInstance.SetFloat("_Exposure", 0f);

        // Reset timer during second half of the transition
        float fadeInTime = durationOfTransition * 0.5f;
        timer = 0f;

        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float exposureTimer = Mathf.Clamp01(timer / fadeInTime);

            // Increase expsosure slowly
            if (_currentSkyboxInstance.HasProperty("_Exposure"))
            {
                _currentSkyboxInstance.SetFloat("_Exposure", Mathf.Lerp(0f, nextSkyboxExposure, exposureTimer));
            }

            DynamicGI.UpdateEnvironment();
            yield return null;
        }

        // Reach visible exposure of the new skybox
        if (_currentSkyboxInstance.HasProperty("_Exposure"))
            _currentSkyboxInstance.SetFloat("_Exposure", nextSkyboxExposure);

        DynamicGI.UpdateEnvironment();
        _isTransitioning = false;
    }

    /// <summary>
    /// On Destroy is called when the game object is disabled or destroyed.
    /// </summary>
    void OnDestroy()
    {
        if (_currentSkyboxInstance != null)
            Destroy(_currentSkyboxInstance);

        if (_nextSkyboxInstance != null)
            Destroy(_nextSkyboxInstance);
    }
}