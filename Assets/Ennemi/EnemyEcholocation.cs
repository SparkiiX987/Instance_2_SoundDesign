using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Echolocalisation de l'ennemi en mode Chase.
/// - Emet periodiquement une onde sonar depuis sa position
/// - Joue un enregistrement aleatoire de la voix du joueur via FMOD
/// - L'onde revele l'environnement autour de l'ennemi
/// - Aide l'ennemi a naviguer dans l'obscurite
/// Appele par EnemyBrain.SetState(Chase).
/// </summary>
[RequireComponent(typeof(EnemyVoiceCapture))]
public class EnemyEcholocation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private SO_EnemySettings settings;

    [Header("Echolocalisation")]
    [Tooltip("Intervalle entre deux echos en secondes.")]
    [SerializeField] private float echoInterval    = 2.5f;
    [Tooltip("Portee de l'onde de l'ennemi.")]
    [SerializeField] private float echoRange        = 15f;
    [Tooltip("Volume de lecture des enregistrements [0..1].")]
    [Range(0f, 1f)]
    [SerializeField] private float playbackVolume   = 0.8f;
    [Tooltip("Variation aleatoire de pitch pour chaque replay.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float pitchVariation   = 0.15f;

    // ── Shader IDs ───────────────────────────────────────────────────
    // L'ennemi utilise des globals DIFFERENTS du joueur
    // pour ne pas ecraser le sonar du joueur
    private static readonly int ID_EnemyWaveOrigin  = Shader.PropertyToID("_EnemyWaveOrigin");
    private static readonly int ID_EnemyWaveRadius  = Shader.PropertyToID("_EnemyWaveRadius");
    private static readonly int ID_EnemyWaveActive  = Shader.PropertyToID("_EnemyWaveActive");

    // ── Etat ─────────────────────────────────────────────────────────
    private EnemyVoiceCapture _voiceCapture;
    private NavMeshAgent      _agent;
    private bool              _active;
    private float             _echoTimer;
    private float             _currentRadius;
    private float             _waveSpeed = 10f;

    // FMOD channel pour la lecture
    private FMOD.Channel _playbackChannel;
    private bool         _playing;

    // ---------------------------------------------------------------

    private void Awake()
    {
        _voiceCapture = GetComponent<EnemyVoiceCapture>();
        _agent        = GetComponent<NavMeshAgent>();
    }

    private void OnDisable()
    {
        StopEcholocation();
    }

    // ── API publique — appelee par EnemyBrain ────────────────────────

    public void StartEcholocation(float _interval = -1f)
    {
        if (_interval > 0f) { echoInterval = _interval; }
        _active    = true;
        _echoTimer = 0f;
    }

    public void StopEcholocation()
    {
        _active        = false;
        _currentRadius = 0f;
        Shader.SetGlobalFloat(ID_EnemyWaveActive, 0f);
        StopPlayback();
    }

    // ── Update ───────────────────────────────────────────────────────

    private void Update()
    {
        if (!_active) { return; }

        // Propage l'onde courante
        if (_currentRadius > 0f)
        {
            _currentRadius += _waveSpeed * Time.deltaTime;
            Shader.SetGlobalVector(ID_EnemyWaveOrigin, transform.position);
            Shader.SetGlobalFloat( ID_EnemyWaveRadius, _currentRadius);

            if (_currentRadius >= echoRange)
            {
                _currentRadius = 0f;
                Shader.SetGlobalFloat(ID_EnemyWaveActive, 0f);
            }
        }

        // Chrono avant le prochain echo
        _echoTimer -= Time.deltaTime;
        if (_echoTimer <= 0f)
        {
            EmitEcho();
            _echoTimer = echoInterval;
        }
    }

    // ── Event ────────────────────────────────────────────────────────
    /// <summary>
    /// Declenche quand l'onde touche le joueur.
    /// EnemyBrain s'abonne pour passer en Chase.
    /// </summary>
    public event System.Action<Vector3> OnPlayerDetectedByEcho;

    // ── Echo ─────────────────────────────────────────────────────────

    private void EmitEcho()
    {
        _currentRadius = 0.1f;
        Shader.SetGlobalVector(ID_EnemyWaveOrigin, transform.position);
        Shader.SetGlobalFloat( ID_EnemyWaveRadius, _currentRadius);
        Shader.SetGlobalFloat( ID_EnemyWaveActive, 1f);

        if (_voiceCapture.HasSamples)
        {
            PlaySample(_voiceCapture.GetRandomSample());
        }

        // Detecte le joueur quand l'onde l'atteint
        StartCoroutine(DetectPlayerWithWave());
    }

    private System.Collections.IEnumerator DetectPlayerWithWave()
    {
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null) { yield break; }

        float dist = Vector3.Distance(transform.position, playerGO.transform.position);
        if (dist > echoRange) { yield break; }

        // Attend que l'onde arrive au joueur
        yield return new UnityEngine.WaitForSeconds(dist / _waveSpeed);

        // Verifie qu'aucun mur ne bloque
        Vector3 dir = (playerGO.transform.position - transform.position).normalized;
        bool blocked = Physics.Raycast(
            transform.position + Vector3.up * 0.5f,
            dir, dist,
            settings != null ? settings.obstacleMask : 0);

        if (blocked) { yield break; }

        UnityEngine.Debug.Log($"[EnemyEcholocation] Joueur detecte par echo a {dist:F1}m !");
        OnPlayerDetectedByEcho?.Invoke(playerGO.transform.position);
    }

    // ── Playback FMOD ────────────────────────────────────────────────

    private void PlaySample(FMOD.Sound _sound)
    {
        if (!_sound.hasHandle()) { return; }

        StopPlayback();

        FMOD.System core = FMODUnity.RuntimeManager.CoreSystem;

        // Joue le Sound en 3D depuis la position de l'ennemi
        FMOD.RESULT r = core.playSound(
            _sound, default, true, out _playbackChannel);

        if (r != FMOD.RESULT.OK)
        {
            UnityEngine.Debug.LogError($"[EnemyEcholocation] playSound={r}");
            return;
        }

        // Position 3D
        FMOD.VECTOR pos = new FMOD.VECTOR
        {
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z
        };
        FMOD.VECTOR vel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
        _playbackChannel.set3DAttributes(ref pos, ref vel);

        // Volume + pitch aleatoire pour varier les replays
        _playbackChannel.setVolume(playbackVolume);
        float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        _playbackChannel.setPitch(pitch);

        // Demarre la lecture
        _playbackChannel.setPaused(false);
        _playing = true;
    }

    private void StopPlayback()
    {
        if (_playing && _playbackChannel.hasHandle())
        {
            _playbackChannel.stop();
            _playing = false;
        }
    }
}
