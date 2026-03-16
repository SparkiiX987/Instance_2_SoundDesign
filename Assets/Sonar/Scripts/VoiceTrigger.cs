using System.Runtime.InteropServices;
using Player.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Capture micro via FMOD Sound.lock + delta recPos.
/// Herite de PlayerAbility.
/// Gere le branchement/debranchement du casque en cours de jeu :
/// - Verifie les drivers FMOD toutes les <driverCheckInterval> secondes
/// - Si un nouveau micro est detecte → demarre automatiquement
/// - Si le micro actuel est deconnecte → arrete et attend le prochain
/// </summary>
public class VoiceTrigger : PlayerAbility
{
    /// <summary>
    /// Declenche au moment du Fire avec les bytes PCM du cri.
    /// EnemyVoiceCapture s'abonne pour capturer la voix du joueur.
    /// float[] = samples PCM, int = rate, int = channels, float = volume normalise
    /// </summary>
    public static event System.Action<float[], int, int, float> OnSoundCaptured;
    [Header("References")]
    [SerializeField] private Sonar sonar;

    [Header("Seuils de volume")]
    [Range(0f, 1f)] [SerializeField] private float volumeThreshold = 0.02f;
    [Range(0f, 1f)] [SerializeField] private float volumeMax       = 0.3f;

    [Header("Lissage")]
    [Range(1, 30)] [SerializeField] private int smoothFrames = 10;

    [Header("Charge")]
    [SerializeField] private float chargeMaxDuration = 1.5f;
    [SerializeField] private float chargeMinDuration = 0.1f;

    [Header("Detection casque")]
    [Tooltip("Intervalle en secondes entre chaque verification des drivers.")]
    [SerializeField] private float driverCheckInterval = 2f;

    // ── FMOD ─────────────────────────────────────────────────────────
    private FMOD.Sound _recordingSound;
    private bool       _recording;
    private uint       _lastRecPos;
    private uint       _soundLengthSamples;
    private int        _driverChannels;
    private int        _driverRate;
    private int        _activeDriverIndex = -1;  // -1 = aucun driver actif

    private const int BUFFER_SEC = 2;

    // ── Lissage ───────────────────────────────────────────────────────
    private float[] _volumeHistory;
    private int     _volumeHistoryIndex;
    private float   _lastSmooth;
    private float   _lastRaw;

    // ── Charge ────────────────────────────────────────────────────────
    private bool  _isCharging;
    private float _chargeTimer;
    private float _chargePeakVolume;

    // ── Detection driver ─────────────────────────────────────────────
    private float _driverCheckTimer;

    // ---------------------------------------------------------------

    public override void Init(PlayerController _playerController)
    {
        base.Init(_playerController);
        _volumeHistory    = new float[smoothFrames];
        _driverCheckTimer = 0f;
        TryStartRecording();
    }

    public override void Execute(InputAction.CallbackContext _context) { }

    // ---------------------------------------------------------------

    private void Update()
    {
        // Verifie periodiquement si un driver est branche/debranche
        _driverCheckTimer += Time.deltaTime;
        if (_driverCheckTimer >= driverCheckInterval)
        {
            _driverCheckTimer = 0f;
            CheckDriverState();
        }

        if (!_recording) { return; }

        _lastRaw    = ComputeRMSDelta();
        _lastSmooth = GetSmoothedVolume(_lastRaw);

        bool voiceActive = _lastSmooth >= volumeThreshold;

        if (voiceActive)
        {
            if (!_isCharging)
            {
                _isCharging       = true;
                _chargeTimer      = 0f;
                _chargePeakVolume = 0f;
            }

            _chargeTimer += Time.deltaTime;
            if (_lastSmooth > _chargePeakVolume) { _chargePeakVolume = _lastSmooth; }
            if (_chargeTimer >= chargeMaxDuration) { Fire(); }
        }
        else
        {
            if (_isCharging && _chargeTimer >= chargeMinDuration) { Fire(); }
            else { _isCharging = false; }
        }
    }

    // ── Gestion driver ───────────────────────────────────────────────

    /// <summary>
    /// Cherche le premier driver CONNECTED et demarre l'enregistrement dessus.
    /// Ignore les drivers loopback (haut-parleurs).
    /// </summary>
    private void TryStartRecording()
    {
        FMOD.System core = FMODUnity.RuntimeManager.CoreSystem;
        core.getRecordNumDrivers(out int numDrivers, out int _);

        if (numDrivers == 0)
        {
            UnityEngine.Debug.LogWarning("[VoiceTrigger] Aucun driver FMOD disponible.");
            return;
        }

        for (int i = 0; i < numDrivers; i++)
        {
            core.getRecordDriverInfo(
                i, out string name, 256,
                out System.Guid _, out int rate,
                out FMOD.SPEAKERMODE _, out int channels,
                out FMOD.DRIVER_STATE state);

            // Ignore les loopbacks (haut-parleurs)
            if (name.ToLower().Contains("loopback")) { continue; }

            // Prend le premier driver connecte
            bool connected = (state & FMOD.DRIVER_STATE.CONNECTED) != 0;
            if (!connected) { continue; }

            UnityEngine.Debug.Log($"[VoiceTrigger] Driver selectionne : [{i}] '{name}' {rate}Hz {channels}ch");
            StartRecordingOnDriver(i, rate, channels);
            return;
        }

        UnityEngine.Debug.LogWarning("[VoiceTrigger] Aucun micro connecte.");
    }

    /// <summary>
    /// Verifie si le driver actif est toujours connecte.
    /// Si deconnecte → arrete. Si aucun actif → cherche un nouveau.
    /// </summary>
    private void CheckDriverState()
    {
        FMOD.System core = FMODUnity.RuntimeManager.CoreSystem;

        if (_recording && _activeDriverIndex >= 0)
        {
            core.getRecordDriverInfo(
                _activeDriverIndex, out string _, 256,
                out System.Guid _, out int _,
                out FMOD.SPEAKERMODE _, out int _,
                out FMOD.DRIVER_STATE state);

            bool stillConnected = (state & FMOD.DRIVER_STATE.CONNECTED) != 0;
            if (!stillConnected)
            {
                UnityEngine.Debug.LogWarning(
                    $"[VoiceTrigger] Driver [{_activeDriverIndex}] deconnecte — arret du micro.");
                StopRecording();
            }
        }

        // Si plus de micro actif, cherche si un nouveau est branche
        if (!_recording)
        {
            TryStartRecording();
        }
    }

    private void StartRecordingOnDriver(int _driverIndex, int _rate, int _channels)
    {
        StopRecording();

        FMOD.System core = FMODUnity.RuntimeManager.CoreSystem;

        _driverRate     = _rate;
        _driverChannels = _channels;

        FMOD.CREATESOUNDEXINFO ex = new FMOD.CREATESOUNDEXINFO();
        ex.cbsize           = Marshal.SizeOf(ex);
        ex.numchannels      = _driverChannels;
        ex.defaultfrequency = _driverRate;
        ex.length           = (uint)(_driverRate * sizeof(float) * _driverChannels * BUFFER_SEC);
        ex.format           = FMOD.SOUND_FORMAT.PCMFLOAT;

        FMOD.RESULT r = core.createSound(
            (string)null,
            FMOD.MODE.LOOP_NORMAL | FMOD.MODE.OPENUSER,
            ref ex, out _recordingSound);

        if (r != FMOD.RESULT.OK)
        {
            UnityEngine.Debug.LogError($"[VoiceTrigger] createSound={r}");
            return;
        }

        _recordingSound.getLength(out _soundLengthSamples, FMOD.TIMEUNIT.PCM);

        r = core.recordStart(_driverIndex, _recordingSound, true);
        if (r != FMOD.RESULT.OK)
        {
            UnityEngine.Debug.LogError($"[VoiceTrigger] recordStart={r}");
            _recordingSound.release();
            return;
        }

        _activeDriverIndex = _driverIndex;
        _lastRecPos        = 0;
        _recording         = true;

        // Reset lissage pour eviter un pic au demarrage
        System.Array.Clear(_volumeHistory, 0, _volumeHistory.Length);
        _volumeHistoryIndex = 0;

        UnityEngine.Debug.Log($"[VoiceTrigger] Micro demarre sur driver [{_driverIndex}].");
    }

    private void StopRecording()
    {
        if (_recording && _activeDriverIndex >= 0)
        {
            FMODUnity.RuntimeManager.CoreSystem.recordStop(_activeDriverIndex);
        }
        if (_recordingSound.hasHandle())
        {
            _recordingSound.release();
            _recordingSound = default;
        }
        _recording         = false;
        _activeDriverIndex = -1;
        _isCharging        = false;
    }

    // ── Fire ─────────────────────────────────────────────────────────

    private void Fire()
    {
        _isCharging = false;
        float normalizedVolume = Mathf.Clamp01(
            Mathf.InverseLerp(volumeThreshold, volumeMax, _chargePeakVolume));

        // Capture un extrait PCM du buffer actuel pour EnemyVoiceCapture
        EmitPCMSnapshot(normalizedVolume);

        sonar.TriggerWaveWithVolume(normalizedVolume);
    }

    /// <summary>
    /// Lit un extrait du buffer FMOD et le passe a OnSoundCaptured.
    /// </summary>
    private void EmitPCMSnapshot(float _normalizedVolume)
    {
        if (OnSoundCaptured == null) { return; }

        FMODUnity.RuntimeManager.CoreSystem
            .getRecordPosition(_activeDriverIndex, out uint writePos);

        // Lit les N derniers samples selon la duree du cri
        int sampleCount = (int)(_driverRate * Mathf.Lerp(0.5f, 1.5f, _normalizedVolume));
        sampleCount     = Mathf.Min(sampleCount, (int)_soundLengthSamples);

        uint startPos   = (writePos + _soundLengthSamples - (uint)sampleCount)
                          % _soundLengthSamples;
        uint byteOffset = startPos    * (uint)sizeof(float) * (uint)_driverChannels;
        uint byteCount  = (uint)sampleCount * (uint)sizeof(float) * (uint)_driverChannels;

        FMOD.RESULT r = _recordingSound.@lock(
            byteOffset, byteCount,
            out System.IntPtr ptr1, out System.IntPtr ptr2,
            out uint len1, out uint len2);

        if (r != FMOD.RESULT.OK) { return; }

        int total = (int)((len1 + len2) / sizeof(float));
        float[] pcm = new float[total];
        int offset  = 0;

        if (ptr1 != System.IntPtr.Zero && len1 > 0)
        {
            int c = (int)(len1 / sizeof(float));
            Marshal.Copy(ptr1, pcm, offset, c);
            offset += c;
        }
        if (ptr2 != System.IntPtr.Zero && len2 > 0)
        {
            int c = (int)(len2 / sizeof(float));
            Marshal.Copy(ptr2, pcm, offset, c);
        }

        _recordingSound.unlock(ptr1, ptr2, len1, len2);

        OnSoundCaptured?.Invoke(pcm, _driverRate, _driverChannels, _normalizedVolume);
    }

    // ── RMS ──────────────────────────────────────────────────────────

    private float ComputeRMSDelta()
    {
        FMODUnity.RuntimeManager.CoreSystem
            .getRecordPosition(_activeDriverIndex, out uint writePos);

        uint delta = (writePos >= _lastRecPos)
            ? writePos - _lastRecPos
            : _soundLengthSamples - _lastRecPos + writePos;

        if (delta == 0) { return 0f; }

        uint byteOffset = _lastRecPos * (uint)sizeof(float) * (uint)_driverChannels;
        uint byteCount  = delta       * (uint)sizeof(float) * (uint)_driverChannels;
        _lastRecPos     = writePos;

        FMOD.RESULT r = _recordingSound.@lock(
            byteOffset, byteCount,
            out System.IntPtr ptr1, out System.IntPtr ptr2,
            out uint len1, out uint len2);

        if (r != FMOD.RESULT.OK) { return 0f; }

        float rms   = 0f;
        int   total = 0;

        if (ptr1 != System.IntPtr.Zero && len1 > 0)
        {
            int count   = (int)(len1 / sizeof(float));
            float[] buf = new float[count];
            Marshal.Copy(ptr1, buf, 0, count);
            for (int i = 0; i < count; i++) { rms += buf[i] * buf[i]; }
            total += count;
        }

        if (ptr2 != System.IntPtr.Zero && len2 > 0)
        {
            int count   = (int)(len2 / sizeof(float));
            float[] buf = new float[count];
            Marshal.Copy(ptr2, buf, 0, count);
            for (int i = 0; i < count; i++) { rms += buf[i] * buf[i]; }
            total += count;
        }

        _recordingSound.unlock(ptr1, ptr2, len1, len2);
        return total > 0 ? Mathf.Sqrt(rms / total) : 0f;
    }

    private float GetSmoothedVolume(float _rawVolume)
    {
        _volumeHistory[_volumeHistoryIndex] = _rawVolume;
        _volumeHistoryIndex = (_volumeHistoryIndex + 1) % smoothFrames;
        float sum = 0f;
        foreach (float v in _volumeHistory) { sum += v; }
        return sum / smoothFrames;
    }

    private void OnDestroy() { StopRecording(); }

    // ── Debug GUI ────────────────────────────────────────────────────

    private void OnGUI()
    {
#if UNITY_EDITOR
        float chargeRatio = _isCharging
            ? Mathf.Clamp01(_chargeTimer / chargeMaxDuration) : 0f;

        string status = !_recording
            ? $"Micro non connecte (check dans {driverCheckInterval - _driverCheckTimer:F1}s)"
            : _isCharging
                ? $"CHARGE {chargeRatio * 100f:F0}% | pic={_chargePeakVolume:F4}"
                : _lastSmooth < volumeThreshold ? "silence" : "...";

        GUI.Label(new Rect(10, 10, 900, 20),
            $"[VoiceTrigger] Driver:[{_activeDriverIndex}] " +
            $"Brut:{_lastRaw:F4} Lisse:{_lastSmooth:F4} | {status}");

        GUI.color = _lastSmooth >= volumeThreshold ? Color.green : Color.gray;
        GUI.DrawTexture(new Rect(10, 30,
            Mathf.Clamp01(_lastSmooth / volumeMax) * 300f, 10),
            Texture2D.whiteTexture);

        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(
            10f + (volumeThreshold / volumeMax) * 300f, 28, 2, 14),
            Texture2D.whiteTexture);

        GUI.color = new Color(0f, 0.8f, 1f, 0.9f);
        GUI.DrawTexture(new Rect(10, 44, chargeRatio * 300f, 8),
            Texture2D.whiteTexture);

        GUI.color = Color.yellow;
        GUI.DrawTexture(new Rect(10, 54,
            Mathf.Clamp01(_chargePeakVolume / volumeMax) * 300f, 6),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
#endif
    }
}
