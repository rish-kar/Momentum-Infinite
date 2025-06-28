using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public static class VideoPreloader
{
    /// <summary>Prepare a new clip and invoke callback once it is ready.</summary>
    public static IEnumerator SwapWhenReady(VideoPlayer vp, VideoClip next, System.Action onReady)
    {
        vp.Stop();
        vp.clip = next;
        vp.Prepare();                       // async load + decode first frame
        while (!vp.isPrepared)              // wait *one* frame per poll
            yield return null;

        onReady?.Invoke();                  // e.g. start fade coroutine
        vp.Play();
    }
}