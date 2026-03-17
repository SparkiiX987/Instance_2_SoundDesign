using UnityEngine;

/// <summary>
/// Gere la perception de l'ennemi : ouie (sonar) + vue courte/longue.
/// S'abonne a SonarSoundEvent pour recevoir les cris du joueur.
/// </summary>
public class EnemyPerception : MonoBehaviour
{
    [SerializeField] private SO_EnemySettings settings;
    [Tooltip("Vide = trouve automatiquement le GameObject tague 'Player'.")]
    [SerializeField] private Transform playerTransform;

    // ── Etat perception ──────────────────────────────────────────────
    public bool     HeardSound      { get; private set; }
    public Vector3  LastSoundOrigin { get; private set; }
    public float    LastSoundVolume { get; private set; }
    public bool     CanSeePlayer    { get; private set; }

    // agressivite [0..1] calculee par EnemyBrain, lue ici pour scaling
    public float AggressionLevel    { get; set; }

    private float _soundMemoryTimer;
    private bool  _inChase;

    // ── Debug ─────────────────────────────────────────────────────────
    public bool showGizmos = true;

    // ---------------------------------------------------------------

    private void Awake()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) { playerTransform = player.transform; }
        }
    }

    private void OnEnable()
    {
        SonarSoundEvent.OnSonarEmitted += OnSonarHeard;
    }

    private void OnDisable()
    {
        SonarSoundEvent.OnSonarEmitted -= OnSonarHeard;
    }

    /// <summary>
    /// Appele par EnemyBrain pour indiquer si l'ennemi est en chase.
    /// En chase → utilise la vue longue.
    /// </summary>
    public void SetChaseMode(bool _inChase) { _inChase = _inChase; }

    private void Update()
    {
        UpdateVision();
        UpdateSoundMemory();
    }

    // ── Ouie ─────────────────────────────────────────────────────────

    private void OnSonarHeard(Vector3 _origin, float _normalizedVolume)
    {
        float hearingRadius = settings.hearingBaseRadius
                            * _normalizedVolume
                            * Mathf.Lerp(1f, settings.aggressionHearingMult, AggressionLevel);

        float dist = Vector3.Distance(transform.position, _origin);
        if (dist > hearingRadius) { return; }

        UnityEngine.Debug.Log($"[EnemyPerception] Son entendu ! dist={dist:F1} radius={hearingRadius:F1}");

        HeardSound      = true;
        LastSoundOrigin = _origin;
        LastSoundVolume = _normalizedVolume;
        _soundMemoryTimer = settings.memoryDuration;
    }

    private void UpdateSoundMemory()
    {
        if (!HeardSound) { return; }
        _soundMemoryTimer -= Time.deltaTime;
        if (_soundMemoryTimer <= 0f) { HeardSound = false; }
    }

    public void ClearSoundMemory()
    {
        HeardSound        = false;
        _soundMemoryTimer = 0f;
    }

    // ── Vue ──────────────────────────────────────────────────────────

    private void UpdateVision()
    {
        if (playerTransform == null) { CanSeePlayer = false; return; }

        float range = _inChase ? settings.longVisionRange  : settings.shortVisionRange;
        float angle = _inChase ? settings.longVisionAngle  : settings.shortVisionAngle;

        Vector3 toPlayer = playerTransform.position - transform.position;
        float   dist     = toPlayer.magnitude;

        if (dist > range) { CanSeePlayer = false; return; }

        float a = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (a > angle * 0.5f) { CanSeePlayer = false; return; }

        // Raycast pour verifier qu'il n'y a pas d'obstacle
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
            toPlayer.normalized, dist, settings.obstacleMask))
        {
            CanSeePlayer = false;
            return;
        }

        CanSeePlayer = true;
    }

    // ── Gizmos ───────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || settings == null) { return; }

        // Vue courte
        Gizmos.color = Color.yellow;
        DrawVisionCone(settings.shortVisionRange, settings.shortVisionAngle);

        // Vue longue
        Gizmos.color = Color.red;
        DrawVisionCone(settings.longVisionRange, settings.longVisionAngle);

        // Rayon d'ecoute
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawSphere(transform.position, settings.hearingBaseRadius);
    }

    private void DrawVisionCone(float _range, float _angle)
    {
        Vector3 fwd   = transform.forward;
        Vector3 left  = Quaternion.Euler(0, -_angle * 0.5f, 0) * fwd;
        Vector3 right = Quaternion.Euler(0,  _angle * 0.5f, 0) * fwd;
        Gizmos.DrawRay(transform.position, left  * _range);
        Gizmos.DrawRay(transform.position, right * _range);
        Gizmos.DrawRay(transform.position, fwd   * _range);
    }
}
