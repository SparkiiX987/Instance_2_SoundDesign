using System.Collections;
using UnityEngine;

/// <summary>
/// Detecte le volume du micro du joueur et declenche le radar avec une portee dynamique.
/// Plus le joueur crie fort, plus l'onde va loin.
/// La touche E reste fonctionnelle dans RadarSystem (portee max).
///
/// Workaround bug Unity (60) "Error initializing output device" integre.
/// Prerequis : Edit > Project Settings > Player > cocher "Microphone Usage Description"
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class VoiceTrigger : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("Le RadarSystem a declencher quand le joueur parle.")]
    [SerializeField] private RadarSystem radarSystem;

    [Header("Seuils de volume")]
    [Tooltip("Volume minimum pour declencher l'onde (en dessous = silence ignore).")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeThreshold = 0.02f;

    [Tooltip("Volume maximum considere comme cri fort (au dessus = portee max).")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMax = 0.3f;

    [Tooltip("Lissage du volume sur N frames pour eviter les faux declenchements.")]
    [Range(1, 30)]
    [SerializeField] private int smoothFrames = 10;

    [Header("Options")]
    [Tooltip("Activer ou desactiver la detection vocale en jeu.")]
    [SerializeField] private bool voiceTriggerEnabled = true;

    [Tooltip("Duree d'analyse du micro en secondes (plus petit = plus reactif).")]
    [SerializeField] private float sampleWindow = 0.1f;

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
        // WORKAROUND BUG UNITY (60) :
        // Un AudioSource doit jouer AVANT Microphone.Start()
        dummySource              = GetComponent<AudioSource>();
        dummySource.clip         = AudioClip.Create("silence", 44100, 1, 44100, false);
        dummySource.loop         = true;
        dummySource.volume       = 0f;
        dummySource.spatialBlend = 0f;
        dummySource.Play();

        volumeHistory = new float[smoothFrames];
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

        Debug.Log($"[VoiceTrigger] {Microphone.devices.Length} micro(s) detecte(s) :");
        foreach (string device in Microphone.devices)
            Debug.Log($"  - {device}");

        micName = Microphone.devices[0];
        Debug.Log($"[VoiceTrigger] Utilisation de : {micName}");

        Microphone.GetDeviceCaps(micName, out int minFreq, out int maxFreq);
        Debug.Log($"[VoiceTrigger] Frequences : min={minFreq} Hz | max={maxFreq} Hz");

        if (minFreq == 0 && maxFreq == 0)
            sampleRate = 16000;
        else if (minFreq == maxFreq)
            sampleRate = minFreq;
        else
            sampleRate = Mathf.Clamp(16000, minFreq, maxFreq);

        Debug.Log($"[VoiceTrigger] Sample rate utilise : {sampleRate} Hz");

        micClip = Microphone.Start(micName, true, 1, sampleRate);

        if (micClip == null)
        {
            Debug.LogError("[VoiceTrigger] Microphone.Start() a retourne null ! Verifie les permissions Windows.");
            yield break;
        }

        float timeout = 5f;
        float elapsed = 0f;
        while (Microphone.GetPosition(micName) <= 0)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                Debug.LogError("[VoiceTrigger] Timeout : le micro ne demarre pas !");
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
        if (!voiceTriggerEnabled || !micReady) return;

        float rawVolume    = GetMicVolume();
        float smoothVolume = GetSmoothedVolume(rawVolume);

        if (smoothVolume < volumeThreshold) return;

        float normalizedVolume = Mathf.Clamp01(Mathf.InverseLerp(volumeThreshold, volumeMax, smoothVolume));
        radarSystem.TriggerWaveWithVolume(normalizedVolume);
    }

    private float GetMicVolume()
    {
        if (micClip == null) return 0f;

        int sampleCount = Mathf.CeilToInt(sampleRate * sampleWindow);
        int micPosition = Microphone.GetPosition(micName);

        if (micPosition < sampleCount) return 0f;

        float[] samples = new float[sampleCount];
        micClip.GetData(samples, micPosition - sampleCount);

        float sum = 0f;
        foreach (float s in samples)
            sum += s * s;

        return Mathf.Sqrt(sum / sampleCount);
    }

    private float GetSmoothedVolume(float _rawVolume)
    {
        volumeHistory[volumeHistoryIndex] = _rawVolume;
        volumeHistoryIndex = (volumeHistoryIndex + 1) % smoothFrames;

        float sum = 0f;
        foreach (float v in volumeHistory)
            sum += v;

        return sum / smoothFrames;
    }

    public void SetVoiceTrigger(bool _enabled) => voiceTriggerEnabled = _enabled;

    private void OnDestroy()
    {
        if (micReady)
            Microphone.End(micName);

        if (dummySource != null)
            dummySource.Stop();
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        float rawVolume    = micReady ? GetMicVolume() : 0f;
        float smoothVolume = micReady ? GetSmoothedVolume(rawVolume) : 0f;
        float normalized   = Mathf.Clamp01(Mathf.InverseLerp(volumeThreshold, volumeMax, smoothVolume));

        string status = !micReady
            ? "Initialisation..."
            : smoothVolume < volumeThreshold
                ? "silence"
                : $">>> ONDE | Portee : {normalized * 100f:F0}% <<<";

        GUI.Label(new Rect(10, 10, 600, 20),
            $"[VoiceTrigger] Brut: {rawVolume:F4} | Lisse: {smoothVolume:F4} | Seuil: {volumeThreshold:F4} | {status}");

        float barWidth = Mathf.Clamp01(smoothVolume / volumeMax) * 300f;
        GUI.color = smoothVolume >= volumeThreshold ? Color.green : Color.gray;
        GUI.DrawTexture(new Rect(10, 30, barWidth, 12), Texture2D.whiteTexture);

        GUI.color = Color.red;
        float thresholdX = 10f + (volumeThreshold / volumeMax) * 300f;
        GUI.DrawTexture(new Rect(thresholdX, 28, 2, 16), Texture2D.whiteTexture);

        GUI.color = Color.white;
#endif
    }
}
