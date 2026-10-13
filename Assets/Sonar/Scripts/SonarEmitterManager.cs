using UnityEngine;

/// <summary>
/// Distribue les emetteurs sonar sur un nombre fixe de slots de globals
/// shader. Un slot = un jeu de variables _EnemyXxxN lues par StifledEdge.
/// </summary>
public class SonarEmitterManager : MonoBehaviour
{
    private static SonarEmitterManager _instance;

    public const int MAX = 20;

    /// <summary>Occupant de chaque slot. null = libre.</summary>
    private static readonly S_ToySonarEmitter[] _slots = new S_ToySonarEmitter[MAX];

    private static readonly int[] ID_Origin   = new int[MAX];
    private static readonly int[] ID_Radius   = new int[MAX];
    private static readonly int[] ID_Active   = new int[MAX];
    private static readonly int[] ID_Color    = new int[MAX];
    private static readonly int[] ID_FireTime = new int[MAX];
    private static readonly int[] ID_MaxRad   = new int[MAX];
    private static readonly int[] ID_FadeDur  = new int[MAX];

    // Les IDs sont resolus au chargement du type, pas dans Awake : Register
    // peut etre appele avant qu'une instance existe.
    static SonarEmitterManager()
    {
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
    }

    private static void EnsureInstance()
    {
        if (_instance != null) { return; }
        GameObject go = new GameObject("SonarEmitterManager");
        _instance = go.AddComponent<SonarEmitterManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        // DontDestroyOnLoad exige un objet racine. Pose en scene sous un
        // parent, il se contentait de logger un avertissement et de ne rien
        // faire, donc le manager mourait au changement de scene.
        if (transform.parent != null) { transform.SetParent(null, true); }
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < MAX; i++) { ClearSlot(i); }
    }

    // ── Slots ────────────────────────────────────────────────────────

    private static void ClearSlot(int i)
    {
        Shader.SetGlobalFloat(ID_Active[i],   0f);
        Shader.SetGlobalFloat(ID_Radius[i],   0f);
        Shader.SetGlobalFloat(ID_FireTime[i], 0f);
        Shader.SetGlobalFloat(ID_MaxRad[i],   0f);
        Shader.SetGlobalFloat(ID_FadeDur[i],  0f);
    }

    public static void Register(S_ToySonarEmitter e)
    {
        EnsureInstance();

        for (int i = 0; i < MAX; i++)
        {
            if (_slots[i] == e) { e.emitterIndex = i; return; }
        }

        for (int i = 0; i < MAX; i++)
        {
            // La comparaison a null attrape aussi les emetteurs detruits
            // sans passer par Unregister, typiquement lors d'un changement
            // de scene : leur slot redevient libre.
            if (_slots[i] == null)
            {
                _slots[i]      = e;
                e.emitterIndex = i;
                ClearSlot(i);
                return;
            }
        }

        // Sans index negatif, un emetteur refuse gardait 0 et continuait a
        // ecrire dans le slot 0 par-dessus son occupant legitime.
        e.emitterIndex = -1;
        Debug.LogWarning(
            $"[SonarEmitterManager] {MAX} emetteurs au maximum : '{e.name}' ne sera pas rendu.", e);
    }

    public static void Unregister(S_ToySonarEmitter e)
    {
        int i = e.emitterIndex;
        e.emitterIndex = -1;

        if (i < 0 || i >= MAX)  { return; }
        if (_slots[i] != e)     { return; }

        _slots[i] = null;
        ClearSlot(i);
    }

    // ── Publication vers le shader ───────────────────────────────────

    public static void PushEmitter(S_ToySonarEmitter e, float radius, Color color)
    {
        int i = e.emitterIndex;
        if (i < 0 || i >= MAX) { return; }

        Shader.SetGlobalVector(ID_Origin[i], e.transform.position);
        Shader.SetGlobalFloat(ID_Radius[i],  radius);
        Shader.SetGlobalFloat(ID_Active[i],  radius > 0f ? 1f : 0f);
        Shader.SetGlobalColor(ID_Color[i],   color);
    }

    public static void PushFireTime(S_ToySonarEmitter e, float fireTime, float maxRad, float fadeDur)
    {
        int i = e.emitterIndex;
        if (i < 0 || i >= MAX) { return; }

        Shader.SetGlobalFloat(ID_FireTime[i], fireTime);
        Shader.SetGlobalFloat(ID_MaxRad[i],   maxRad);
        Shader.SetGlobalFloat(ID_FadeDur[i],  fadeDur);
    }
}
