using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Acts as a cache storage for holding details related to blockage.
/// Enhances performance and acts as a temporary storage location.
/// </summary>
public class CrashCache : MonoBehaviour
{
    // Keeping values static as they will not be destoryed during the lifecycle of the events
    private static CrashCache _crashCacheObject;
    public static List<BlockageDetailDTO> reports = new();

    /// <summary>
    /// Called when the game beings.
    /// </summary>
    void Awake()
    {
        if (_crashCacheObject == null)
        {
            _crashCacheObject = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}