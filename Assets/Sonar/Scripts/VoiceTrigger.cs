using System.Collections;
using UnityEngine;

/// <summary>
/// Detecte le volume du micro du joueur et declenche le sonar avec une portee dynamique.
/// Plus le joueur crie fort, plus l'onde va loin.
/// La touche E reste fonctionnelle dans Sonar (portee max).
/// Workaround bug Unity (60) "Error initializing output device" integre.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class VoiceTrigger : MonoBehaviour
{
    [Header("Parametres")]
    [SerializeField] private SO_SonarSettings settings;

    [Header("References")]
    [SerializeField] private Sonar sonar;

    [Header("Options")]
    [SerializeField] private bool voiceTriggerEnabled = true;

    // ---------------------------------------------------------------

    private AudioClip micClip;
    private string micName;
    private bool micReady = false;
    private int sampleRate = 16000;
    private AudioSource dummySource;
    private float[] volumeHistory;
    private int volumeHistoryIndex = 0;

    // ---------------------------------------------------------------

    private void Awake()
    {
        if (settings == null)
            Debug.LogError("[VoiceTrigger] SO_SonarSettings non assigne !");

        // WORKAROUND BUG UNITY (60) :
        // Un AudioSource doit jouer AVANT Microphone.Start()
        dummySource              = GetComponent<AudioSource>();
        dummySource.clip         = AudioClip.Create("silence", 44100, 1, 44100, false);
        dummySource.loop         = true;
        dummySource.volume       = 0f;
        dummySource.spatialBlend = 0f;
        dummySource.Play();

        volumeHistory = new float[settings != null ? settings.voiceSmoothFrames : 10];
    }

    private void Start()
    {
        StartCoroutine(InitMicrophoneCoroutine());
    }

    private IEnumerator InitMicrophoneCoroutine()
    {
        yield return null;
        yield return null;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[VoiceTrigger] Aucun microphone detecte !");
            yield break;
        }

        micName = Microphone.devices[0];
        Debug.Log($"[VoiceTrigger] Micro trouve : {micName}");

        Microphone.GetDeviceCaps(micName, out int minFreq, out int maxFreq);

        if (minFreq == 0 && maxFreq == 0)       sampleRate = 16000;
        else if (minFreq == maxFreq)             sampleRate = minFreq;
        else                                     sampleRate = Mathf.Clamp(16000, minFreq, maxFreq);

        micClip = Microphone.Start(micName, true, 1, sampleRate);

        if (micClip == null)
        {
            Debug.LogError("[VoiceTrigger] Microphone.Start() a retourne null !");
            yield break;
        }

        float timeout = 5f;
        float elapsed = 0f;
        while (Microphone.GetPosition(micName) <= 0)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                Debug.LogError("[VoiceTrigger] Timeout micro !");
                Microphone.End(micName);
                yield break;
            }
            yield return null;
        }

        micReady = true;
        Debug.Log("[VoiceTrigger] Micro pret !");
    }

    private void Update()
    {
        if (!voiceTriggerEnabled || !micReady || settings == null) return;

        float rawVolume    = GetMicVolume();
        float smoothVolume = GetSmoothedVolume(rawVolume);

        if (smoothVolume < settings.voiceThreshold) return;

        float normalizedVolume = settings.GetNormalizedVolume(smoothVolume);
        sonar.TriggerWaveWithVolume(normalizedVolume);
    }

    private float GetMicVolume()
    {
        if (micClip == null) return 0f;

        int sampleCount = Mathf.CeilToInt(sampleRate * settings.voiceSampleWindow);
        int micPosition = Microphone.GetPosition(micName);

        if (micPosition < sampleCount) return 0f;

        float[] samples = new float[sampleCount];
        micClip.GetData(samples, micPosition - sampleCount);

        float sum = 0f;
        foreach (float sample in samples)
            sum += sample * sample;

        return Mathf.Sqrt(sum / sampleCount);
    }

    private float GetSmoothedVolume(float _rawVolume)
    {
        volumeHistory[volumeHistoryIndex] = _rawVolume;
        volumeHistoryIndex = (volumeHistoryIndex + 1) % volumeHistory.Length;

        float sum = 0f;
        foreach (float volume in volumeHistory)
            sum += volume;

        return sum / volumeHistory.Length;
    }

    /// <summary>Active ou desactive la detection vocale depuis un autre script.</summary>
    public void SetVoiceTrigger(bool _enabled)
    {
        voiceTriggerEnabled = _enabled;
    }

    private void OnDestroy()
    {
        if (micReady)        Microphone.End(micName);
        if (dummySource != null) dummySource.Stop();
    }

    // ---------------------------------------------------------------
    // Debug visuel (Editor uniquement)

    private void OnGUI()
    {
#if UNITY_EDITOR
        if (settings == null) return;

        float raw    = micReady ? GetMicVolume() : 0f;
        float smooth = micReady ? GetSmoothedVolume(raw) : 0f;
        float norm   = settings.GetNormalizedVolume(smooth);

        string status = !micReady                          ? "Initialisation..."
                      : smooth < settings.voiceThreshold   ? "silence"
                      :                                      $">>> ONDE | Portee : {norm * 100f:F0}% <<<";

        GUI.Label(new Rect(10, 10, 600, 20),
            $"[VoiceTrigger] Brut: {raw:F4} | Lisse: {smooth:F4} | Seuil: {settings.voiceThreshold:F4} | {status}");

        float barWidth = Mathf.Clamp01(smooth / settings.voiceMax) * 300f;
        GUI.color = smooth >= settings.voiceThreshold ? Color.green : Color.gray;
        GUI.DrawTexture(new Rect(10, 30, barWidth, 12), Texture2D.whiteTexture);

        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(10f + (settings.voiceThreshold / settings.voiceMax) * 300f, 28, 2, 16), Texture2D.whiteTexture);
        GUI.color = Color.white;
#endif
    }
}
