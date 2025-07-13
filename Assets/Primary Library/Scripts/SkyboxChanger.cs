using UnityEngine;
using UnityEngine.Video;                 // <- gives VideoPlayer & VideoClip
using System.Collections;
using System.Collections.Generic;

public class SkyboxChanger : MonoBehaviour
{
    /* ── Inspector ───────────────────────────────────────────── */
    [Header("References")]
    public Transform    player;          // your runner / camera
    [SerializeField]    VideoPlayer      videoPlayer;   // background VP

    [Header("Distances")]
    public int   distanceInterval   = 2000;
    public float transitionDuration = 2.5f;

    [Header("Assets (drag-in lists, same order!)")]
    [SerializeField] private List<Material>  skyboxes   = new(); // 6 mats
    [SerializeField] private List<VideoClip> videoClips = new(); // 6 clips
    public int CurrentSkyboxIdx { get; private set; }   // ← NEW


    /* ── private state ───────────────────────────────────────── */
    readonly Stack<int> used = new();    // store index so skybox = clip
    int       nextSwapZ;
    Coroutine fadeRoutine;

    /* ────────────────────────────────────────────────────────── */
    void Awake()
    {
        // quick sanity: same count?
        if (skyboxes.Count != videoClips.Count || skyboxes.Count == 0)
        {
            // Debug.LogError("Skyboxes & VideoClips lists must both contain the same non-zero number of elements!");
            enabled = false;
            return;
        }

        // pick initial pair
        int i0 = Random.Range(0, skyboxes.Count);
        RenderSettings.skybox = skyboxes[i0];
        videoPlayer.clip      = videoClips[i0];
        videoPlayer.Play();
        DynamicGI.UpdateEnvironment();
        used.Push(i0);

        CurrentSkyboxIdx = i0;
        nextSwapZ = distanceInterval;
    }

    void Update()
    {
        if (!player) return;

        if (player.position.z >= nextSwapZ)
        {
            nextSwapZ += distanceInterval;
            int ix = PickNextIndex();
            CurrentSkyboxIdx = ix; // ← update current index

            // prepare clip first, then fade skybox when ready
            StartCoroutine(VideoPreloader.SwapWhenReady(
                videoPlayer,
                videoClips[ix],
                () =>
                {
                    if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                    fadeRoutine = StartCoroutine(FadeSkybox(skyboxes[ix]));
                }));
        }
    }

    /* ---------- helpers ---------- */
    int PickNextIndex()
    {
        if (used.Count == skyboxes.Count) used.Clear();

        List<int> pool = new();
        for (int i = 0; i < skyboxes.Count; i++)
            if (!used.Contains(i)) pool.Add(i);

        int choice = pool[Random.Range(0, pool.Count)];
        used.Push(choice);
        return choice;
    }

    IEnumerator FadeSkybox(Material next)
    {
        Material current = RenderSettings.skybox;
        bool canFade = current && next &&
                       current.HasProperty("_Exposure") &&
                       next.HasProperty("_Exposure");

        if (!canFade)
        {
            RenderSettings.skybox = next;
            DynamicGI.UpdateEnvironment();
            yield break;
        }

        float half = transitionDuration * 0.5f;
        float curExp = current.GetFloat("_Exposure");
        float nextExp = next.GetFloat("_Exposure");

        // fade out
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            current.SetFloat("_Exposure", curExp * (1f - t / half));
            yield return null;
        }
        current.SetFloat("_Exposure", 0f);

        // swap & fade in
        RenderSettings.skybox = next;
        DynamicGI.UpdateEnvironment();
        next.SetFloat("_Exposure", 0f);

        for (float t = 0; t < half; t += Time.deltaTime)
        {
            next.SetFloat("_Exposure", Mathf.Lerp(0f, nextExp, t / half));
            yield return null;
        }
        next.SetFloat("_Exposure", nextExp);
    }
}
