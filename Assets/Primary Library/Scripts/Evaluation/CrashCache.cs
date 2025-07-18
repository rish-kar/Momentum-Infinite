using System.Collections.Generic;
using UnityEngine;


public class CrashCache : MonoBehaviour
{
    private static CrashCache _instance;
    public static List<BlockageDetailDTO> reports = new();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}