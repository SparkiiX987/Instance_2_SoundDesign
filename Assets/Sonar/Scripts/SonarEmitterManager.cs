using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton qui gere jusqu'a 8 emetteurs ennemis
/// et pousse leurs donnees au shader chaque frame.
/// </summary>
public class SonarEmitterManager : MonoBehaviour
{
    private static SonarEmitterManager _instance;
    private static readonly List<S_ToySonarEmitter> _emitters = new();

    private const int MAX = 8;

    // Shader property IDs — tableaux de 8
    private static readonly int[] ID_Origin   = new int[MAX];
    private static readonly int[] ID_Radius   = new int[MAX];
    private static readonly int[] ID_Active   = new int[MAX];
    private static readonly int[] ID_Color    = new int[MAX];
    private static readonly int[] ID_FireTime = new int[MAX];
    private static readonly int[] ID_MaxRad   = new int[MAX];
    private static readonly int[] ID_FadeDur  = new int[MAX];

    private static void EnsureInstance()
    {
        if (_instance != null) return;
        var go = new GameObject("SonarEmitterManager");
        _instance = go.AddComponent<SonarEmitterManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < MAX; i++)
        {
            ID_Origin[i]   = Shader.PropertyToID($"_EnemyOrigin{i}");
            ID_Radius[i]   = Shader.PropertyToID($"_EnemyRadius{i}");
            ID_Active[i]   = Shader.PropertyToID($"_EnemyActive{i}");
            ID_Color[i]    = Shader.PropertyToID($"_EnemyColor{i}");
            ID_FireTime[i] = Shader.PropertyToID($"_EnemyFireTime{i}");
            ID_MaxRad[i]   = Shader.PropertyToID($"_EnemyMaxRad{i}");
            ID_FadeDur[i]  = Shader.PropertyToID($"_EnemyFadeDur{i}");
        }

        // Initialiser tous les emetteurs a inactif
        for (int i = 0; i < MAX; i++)
        {
            Shader.SetGlobalFloat(ID_Active[i],   0f);
            Shader.SetGlobalFloat(ID_FireTime[i], 0f);
            Shader.SetGlobalFloat(ID_MaxRad[i],   0f);
            Shader.SetGlobalFloat(ID_FadeDur[i],  0f);
        }
    }

    public static void Register(S_ToySonarEmitter e)
    {
        EnsureInstance();
        if (_emitters.Contains(e)) return;
        if (_emitters.Count >= MAX)
        {
            Debug.LogWarning("SonarEmitterManager: max 8 emetteurs atteint !");
            return;
        }
        e.emitterIndex = _emitters.Count;
        _emitters.Add(e);
    }

    public static void Unregister(S_ToySonarEmitter e)
    {
        _emitters.Remove(e);
        // Desactiver le slot
        int i = e.emitterIndex;
        if (i < MAX)
        {
            Shader.SetGlobalFloat(ID_Active[i],   0f);
            Shader.SetGlobalFloat(ID_Radius[i],   0f);
            Shader.SetGlobalFloat(ID_FireTime[i], 0f);
        }
    }

    public static void PushEmitter(S_ToySonarEmitter e, float radius, Color color)
    {
        int i = e.emitterIndex;
        if (i >= MAX) return;
        Shader.SetGlobalVector(ID_Origin[i], e.transform.position);
        Shader.SetGlobalFloat(ID_Radius[i],  radius);
        Shader.SetGlobalFloat(ID_Active[i],  radius > 0f ? 1f : 0f);
        Shader.SetGlobalColor(ID_Color[i],   color);
    }

    public static void PushFireTime(S_ToySonarEmitter e, float fireTime, float maxRad, float fadeDur)
    {
        int i = e.emitterIndex;
        if (i >= MAX) return;
        Shader.SetGlobalFloat(ID_FireTime[i], fireTime);
        Shader.SetGlobalFloat(ID_MaxRad[i],   maxRad);
        Shader.SetGlobalFloat(ID_FadeDur[i],  fadeDur);
    }
}
