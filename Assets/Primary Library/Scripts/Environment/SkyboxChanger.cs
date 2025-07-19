using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

public class SkyboxChanger : MonoBehaviour
{
    /* ── Inspector ───────────────────────────────────────────── */
    [Header("References")] public Transform player;
    [SerializeField] VideoPlayer videoPlayer;

    [Header("Distances")] public int distanceInterval = 2000;
    public float transitionDuration = 13.0f;

    [Header("Assets")] 
    [SerializeField] List<Material> skyboxes = new();
    [SerializeField] List<VideoClip> videoClips = new();
    public int CurrentSkyboxIdx { get; private set; }

    /* ── Private State ───────────────────────────────────────── */
    readonly Stack<int> used = new();
    private int nextSwapZ;
    private bool isTransitioning;
    private Material currentSkyboxInstance;
    private Material nextSkyboxInstance;
    private Coroutine transitionRoutine;
    private float defaultExposure = 1.0f;

    void Awake()
    {
        if (skyboxes.Count != videoClips.Count || skyboxes.Count == 0)
        {
            enabled = false;
            return;
        }

        int i0 = Random.Range(0, skyboxes.Count);
        InitializeEnvironment(i0);
        used.Push(i0);
        nextSwapZ = distanceInterval;
    }

    void InitializeEnvironment(int index)
    {
        // Create new material instance (critical fix)
        currentSkyboxInstance = new Material(skyboxes[index]);
        RenderSettings.skybox = currentSkyboxInstance;
        
        // Store default exposure
        if (currentSkyboxInstance.HasProperty("_Exposure"))
        {
            defaultExposure = currentSkyboxInstance.GetFloat("_Exposure");
        }
        
        videoPlayer.clip = videoClips[index];
        videoPlayer.Play();
        DynamicGI.UpdateEnvironment();
        CurrentSkyboxIdx = index;
    }

    void Update()
    {
        if (!player || isTransitioning) return;

        if (player.position.z >= nextSwapZ)
        {
            nextSwapZ += distanceInterval;
            int nextIndex = PickNextIndex();
            
            if (transitionRoutine != null) 
                StopCoroutine(transitionRoutine);
                
            transitionRoutine = StartCoroutine(TransitionRoutine(nextIndex));
        }
    }

    int PickNextIndex()
    {
        if (used.Count == skyboxes.Count) used.Clear();

        List<int> pool = new();
        for (int i = 0; i < skyboxes.Count; i++)
            if (!used.Contains(i))
                pool.Add(i);

        int choice = pool[Random.Range(0, pool.Count)];
        used.Push(choice);
        return choice;
    }

    IEnumerator TransitionRoutine(int nextIndex)
    {
        isTransitioning = true;
        
        // Create next material instance in advance
        nextSkyboxInstance = new Material(skyboxes[nextIndex]);
        float nextExposure = defaultExposure;
        if (nextSkyboxInstance.HasProperty("_Exposure"))
        {
            nextExposure = nextSkyboxInstance.GetFloat("_Exposure");
        }
        
        // Phase 1: Fade out current environment
        float fadeOutTime = transitionDuration * 0.5f;
        float timer = 0f;
        
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeOutTime);
            
            // Fade skybox exposure
            if (currentSkyboxInstance.HasProperty("_Exposure"))
            {
                currentSkyboxInstance.SetFloat("_Exposure", Mathf.Lerp(defaultExposure, 0f, t));
            }
            
            DynamicGI.UpdateEnvironment();
            yield return null;
        }
        
        // Ensure fully black
        if (currentSkyboxInstance.HasProperty("_Exposure")) 
            currentSkyboxInstance.SetFloat("_Exposure", 0f);
        
        // Phase 2: Swap environment
        RenderSettings.skybox = nextSkyboxInstance;
        currentSkyboxInstance = nextSkyboxInstance;
        videoPlayer.clip = videoClips[nextIndex];
        videoPlayer.Play();
        used.Push(nextIndex);
        CurrentSkyboxIdx = nextIndex;
        DynamicGI.UpdateEnvironment();
        
        // Set initial black state
        if (currentSkyboxInstance.HasProperty("_Exposure"))
            currentSkyboxInstance.SetFloat("_Exposure", 0f);
        
        // Phase 3: Fade in new environment
        float fadeInTime = transitionDuration * 0.5f;
        timer = 0f;
        
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeInTime);
            
            // Fade skybox exposure
            if (currentSkyboxInstance.HasProperty("_Exposure"))
            {
                currentSkyboxInstance.SetFloat("_Exposure", Mathf.Lerp(0f, nextExposure, t));
            }
            
            DynamicGI.UpdateEnvironment();
            yield return null;
        }
        
        // Ensure fully visible
        if (currentSkyboxInstance.HasProperty("_Exposure")) 
            currentSkyboxInstance.SetFloat("_Exposure", nextExposure);
        
        DynamicGI.UpdateEnvironment();
        isTransitioning = false;
    }

    void OnDestroy()
    {
        // Clean up material instances
        if (currentSkyboxInstance != null) 
            Destroy(currentSkyboxInstance);
            
        if (nextSkyboxInstance != null) 
            Destroy(nextSkyboxInstance);
    }
}