using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Capture les cris du joueur en lisant directement le Sound FMOD
/// deja enregistre par VoiceTrigger — pas de second recordStart.
/// VoiceTrigger expose son Sound via un event statique.
/// </summary>
public class EnemyVoiceCapture : MonoBehaviour
{
    [Header("Capture")]
    [Tooltip("Duree capturee en secondes apres chaque cri.")]
    [SerializeField] private float captureDuration  = 1.5f;
    [Tooltip("Nombre max d'echantillons gardes.")]
    [SerializeField] private int   maxStoredSamples = 5;
    [Tooltip("Volume minimal du cri pour etre capture [0..1].")]
    [SerializeField] private float minVolumeToCapture = 0.2f;

    // Banque de sons prets a etre rejoues
    private List<FMOD.Sound> _storedSounds = new List<FMOD.Sound>();

    public bool HasSamples => _storedSounds.Count > 0;

    public event System.Action<FMOD.Sound> OnNewSampleCaptured;

    // ---------------------------------------------------------------

    private void OnEnable()
    {
        VoiceTrigger.OnSoundCaptured += OnPlayerCried;
    }

    private void OnDisable()
    {
        VoiceTrigger.OnSoundCaptured -= OnPlayerCried;
    }

    private void OnDestroy()
    {
        ReleaseAll();
    }

    // ---------------------------------------------------------------

    /// <summary>
    /// Reçoit les bytes PCM directement depuis VoiceTrigger.
    /// On cree un nouveau Sound FMOD a partir de ces bytes.
    /// </summary>
    private void OnPlayerCried(float[] _pcmData, int _rate, int _channels, float _normalizedVolume)
    {
        if (_normalizedVolume < minVolumeToCapture) { return; }
        if (_pcmData == null || _pcmData.Length == 0) { return; }

        FMOD.System core = FMODUnity.RuntimeManager.CoreSystem;

        // Cree un Sound FMOD depuis les bytes PCM du joueur
        FMOD.CREATESOUNDEXINFO ex = new FMOD.CREATESOUNDEXINFO();
        ex.cbsize           = Marshal.SizeOf(ex);
        ex.numchannels      = _channels;
        ex.defaultfrequency = _rate;
        ex.length           = (uint)(_pcmData.Length * sizeof(float));
        ex.format           = FMOD.SOUND_FORMAT.PCMFLOAT;

        FMOD.RESULT r = core.createSound(
            (string)null,
            FMOD.MODE.OPENUSER | FMOD.MODE.LOOP_OFF,
            ref ex,
            out FMOD.Sound newSound);

        if (r != FMOD.RESULT.OK)
        {
            UnityEngine.Debug.LogError($"[EnemyVoiceCapture] createSound={r}");
            return;
        }

        // Copie les bytes PCM dans le Sound
        r = newSound.@lock(0, ex.length,
            out System.IntPtr ptr1, out System.IntPtr ptr2,
            out uint len1, out uint len2);

        if (r != FMOD.RESULT.OK)
        {
            newSound.release();
            return;
        }

        int bytesToCopy = (int)Mathf.Min(len1, ex.length);
        Marshal.Copy(_pcmData, 0, ptr1, bytesToCopy / sizeof(float));
        newSound.unlock(ptr1, ptr2, len1, len2);

        // Stocke
        if (_storedSounds.Count >= maxStoredSamples)
        {
            _storedSounds[0].release();
            _storedSounds.RemoveAt(0);
        }

        _storedSounds.Add(newSound);
        UnityEngine.Debug.Log(
            $"[EnemyVoiceCapture] Sample capture ! vol={_normalizedVolume:F2} total={_storedSounds.Count}");

        OnNewSampleCaptured?.Invoke(newSound);
    }

    public FMOD.Sound GetRandomSample()
    {
        if (_storedSounds.Count == 0) { return default; }
        return _storedSounds[Random.Range(0, _storedSounds.Count)];
    }

    private void ReleaseAll()
    {
        foreach (FMOD.Sound s in _storedSounds)
        {
            if (s.hasHandle()) { s.release(); }
        }
        _storedSounds.Clear();
    }
}
