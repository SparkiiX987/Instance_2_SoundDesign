using System.Runtime.InteropServices;
using Player.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Capture micro via FMOD Sound.lock + delta recPos.
/// Herite de PlayerAbility.
/// Execute() est vide — le micro tourne en continu dans Update.
/// </summary>
public class VoiceTrigger : PlayerAbility
{
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

    // ── FMOD ─────────────────────────────────────────────────────────
    private FMOD.Sound _recordingSound;
    private bool       _recording;
    private uint       _lastRecPos;
    private uint       _soundLengthSamples;
    private int        _driverChannels;
    private int        _driverRate;

    private const int DRIVER_INDEX = 0;
    private const int BUFFER_SEC   = 2;

    // ── Lissage ───────────────────────────────────────────────────────
    private float[] _volumeHistory;
    private int     _volumeHistoryIndex;
    private float   _lastSmooth;
    private float   _lastRaw;

    // ── Charge ────────────────────────────────────────────────────────
    private bool  _isCharging;
    private float _chargeTimer;
    private float _chargePeakVolume;

    // ---------------------------------------------------------------

    public override void Init(PlayerController _playerController)
    {
        base.Init(_playerController);
        _volumeHistory = new float[smoothFrames];
        InitFMOD();
    }

    // Le micro tourne en continu dans Update — Execute non utilise
    public override void Execute(InputAction.CallbackContext _context) { }

    // ---------------------------------------------------------------

    private void InitFMOD()
    {
        FMOD.System core = FMODUnity.RuntimeManager.CoreSystem;

        core.getRecordNumDrivers(out int numDrivers, out int _);
        if (numDrivers == 0)
        {
            UnityEngine.Debug.LogWarning("[VoiceTrigger] Aucun driver FMOD.");
            return;
        }

        core.getRecordDriverInfo(
            DRIVER_INDEX, out string driverName, 256,
            out System.Guid _, out _driverRate,
            out FMOD.SPEAKERMODE _, out _driverChannels,
            out FMOD.DRIVER_STATE state);

        UnityEngine.Debug.Log($"[VoiceTrigger] Driver : '{driverName}' {_driverRate}Hz {_driverChannels}ch state={state}");

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

        if (r != FMOD.RESULT.OK) { UnityEngine.Debug.LogError($"[VoiceTrigger] createSound={r}"); return; }

        _recordingSound.getLength(out _soundLengthSamples, FMOD.TIMEUNIT.PCM);

        r = core.recordStart(DRIVER_INDEX, _recordingSound, true);
        if (r != FMOD.RESULT.OK) { _recordingSound.release(); return; }

        _recording = true;
        UnityEngine.Debug.Log("[VoiceTrigger] Micro FMOD pret !");
    }

    // ---------------------------------------------------------------

    private void Update()
    {
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
            if (_isCharging && _chargeTimer >= chargeMinDuration)
            {
                Fire();
            }
            else
            {
                _isCharging = false;
            }
        }
    }

    private void Fire()
    {
        _isCharging = false;

        float normalizedVolume = Mathf.Clamp01(
            Mathf.InverseLerp(volumeThreshold, volumeMax, _chargePeakVolume));

        sonar.TriggerWaveWithVolume(normalizedVolume);
    }

    // ---------------------------------------------------------------

    private float ComputeRMSDelta()
    {
        FMODUnity.RuntimeManager.CoreSystem.getRecordPosition(DRIVER_INDEX, out uint writePos);

        uint delta = (writePos >= _lastRecPos)
            ? writePos - _lastRecPos
            : _soundLengthSamples - _lastRecPos + writePos;

        if (delta == 0) { return 0f; }

        uint byteOffset = _lastRecPos * (uint)sizeof(float) * (uint)_driverChannels;
        uint byteCount  = delta       * (uint)sizeof(float) * (uint)_driverChannels;
        _lastRecPos = writePos;

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

    private void OnDestroy()
    {
        if (_recording) { FMODUnity.RuntimeManager.CoreSystem.recordStop(DRIVER_INDEX); }
        if (_recordingSound.hasHandle()) { _recordingSound.release(); }
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        float chargeRatio = _isCharging ? Mathf.Clamp01(_chargeTimer / chargeMaxDuration) : 0f;

        string status = !_recording                      ? "Non demarre"
            : _isCharging                                ? $"CHARGE {chargeRatio * 100f:F0}% | pic={_chargePeakVolume:F4}"
            : _lastSmooth < volumeThreshold              ? "silence"
            : "...";

        GUI.Label(new Rect(10, 10, 900, 20),
            $"[VoiceTrigger] Brut:{_lastRaw:F4} Lisse:{_lastSmooth:F4} Seuil:{volumeThreshold:F4} | {status}");

        GUI.color = _lastSmooth >= volumeThreshold ? Color.green : Color.gray;
        GUI.DrawTexture(new Rect(10, 30, Mathf.Clamp01(_lastSmooth / volumeMax) * 300f, 10), Texture2D.whiteTexture);

        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(10f + (volumeThreshold / volumeMax) * 300f, 28, 2, 14), Texture2D.whiteTexture);

        GUI.color = new Color(0f, 0.8f, 1f, 0.9f);
        GUI.DrawTexture(new Rect(10, 44, chargeRatio * 300f, 8), Texture2D.whiteTexture);

        GUI.color = Color.yellow;
        GUI.DrawTexture(new Rect(10, 54, Mathf.Clamp01(_chargePeakVolume / volumeMax) * 300f, 6), Texture2D.whiteTexture);

        GUI.color = Color.white;
#endif
    }
}
