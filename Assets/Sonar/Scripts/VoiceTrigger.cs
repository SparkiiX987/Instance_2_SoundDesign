using System;
using System.Runtime.InteropServices;
using Player.Scripts;
using UnityEngine;

public class VoiceTrigger : PlayerAbility
{
    public static event Action<float[], int, int, float> OnSoundCaptured;
    public static event Action<float> OnSoundFired;

    private const string PREF_KEY = "VoiceEnabled";

    [Header("Activation")]
    [SerializeField] private bool isEnabled = true;

    [Header("Seuils de volume")]
    [Range(0f,1f)] [SerializeField] private float volumeThreshold = 0.02f;
    [Range(0f,1f)] [SerializeField] private float volumeMax = 0.3f;

    [Header("Lissage")]
    [Range(1,30)] [SerializeField] private int smoothFrames = 10;

    [Header("Charge")]
    [SerializeField] private float chargeMaxDuration = 1.5f;
    [SerializeField] private float chargeMinDuration = 0.1f;

    [Header("Detection casque")]
    [SerializeField] private float driverCheckInterval = 2f;

    [Header("References")]
    [SerializeField] private Sonar sonar;

    private FMOD.Sound _recordingSound;
    private bool _recording;
    private uint _lastRecPos;
    private uint _soundLengthSamples;
    private int _driverChannels;
    private int _driverRate;
    private int _activeDriverIndex = -1;
    private const int BUFFER_SEC = 2;

    private float[] _volumeHistory;
    private int _volumeHistoryIndex;
    private float _lastSmooth;
    private float _lastRaw;

    private bool _isCharging;
    private float _chargeTimer;
    private float _chargePeakVolume;

    private float _driverCheckTimer;
    private bool _isMuted = false;

   
    public override void Init(PlayerController _playerController)
    {
        base.Init(_playerController);

        
        isEnabled = PlayerPrefs.GetInt(PREF_KEY, 1) == 1;

        _volumeHistory = new float[smoothFrames];
        _driverCheckTimer = 0f;

        if (isEnabled)
            TryStartRecording();
    }

    private void Update()
    {
        if (!isEnabled) return;

        _driverCheckTimer += Time.deltaTime;
        if (_driverCheckTimer >= driverCheckInterval)
        {
            _driverCheckTimer = 0f;
            CheckDriverState();
        }

        if (!_recording || _isMuted) return;

        _lastRaw = ComputeRMSDelta();
        _lastSmooth = GetSmoothedVolume(_lastRaw);

        bool voiceActive = _lastSmooth >= volumeThreshold;

        if (voiceActive)
        {
            if (!_isCharging)
            {
                _isCharging = true;
                _chargeTimer = 0f;
                _chargePeakVolume = 0f;
            }

            _chargeTimer += Time.deltaTime;
            if (_lastSmooth > _chargePeakVolume)
                _chargePeakVolume = _lastSmooth;

            if (_chargeTimer >= chargeMaxDuration)
                Fire();
        }
        else
        {
            if (_isCharging && _chargeTimer >= chargeMinDuration)
                Fire();
            else
                _isCharging = false;
        }
    }

   
    private void Fire()
    {
        _isCharging = false;
        float normalizedVolume = Mathf.Clamp01(
            Mathf.InverseLerp(volumeThreshold, volumeMax, _chargePeakVolume));
        EmitPCMSnapshot(normalizedVolume);
        OnSoundFired?.Invoke(normalizedVolume);
    }

    private void EmitPCMSnapshot(float normalizedVolume)
    {
        if (OnSoundCaptured == null || _activeDriverIndex<0) return;

        FMODUnity.RuntimeManager.CoreSystem.getRecordPosition(_activeDriverIndex, out uint writePos);

        int sampleCount = (int)(_driverRate * Mathf.Lerp(0.5f,1.5f, normalizedVolume));
        sampleCount = Mathf.Min(sampleCount,(int)_soundLengthSamples);

        uint startPos = (writePos + _soundLengthSamples - (uint)sampleCount) % _soundLengthSamples;
        uint byteOffset = startPos * (uint)sizeof(float) * (uint)_driverChannels;
        uint byteCount = (uint)sampleCount * (uint)sizeof(float) * (uint)_driverChannels;

        FMOD.RESULT r = _recordingSound.@lock(byteOffset,byteCount,
            out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);

        if (r!=FMOD.RESULT.OK) return;

        int total = (int)((len1+len2)/sizeof(float));
        float[] pcm = new float[total];
        int offset = 0;

        if(ptr1!=IntPtr.Zero && len1>0)
        {
            int c = (int)(len1/sizeof(float));
            Marshal.Copy(ptr1,pcm,offset,c);
            offset+=c;
        }
        if(ptr2!=IntPtr.Zero && len2>0)
        {
            int c = (int)(len2/sizeof(float));
            Marshal.Copy(ptr2,pcm,offset,c);
        }

        _recordingSound.unlock(ptr1,ptr2,len1,len2);

        OnSoundCaptured?.Invoke(pcm,_driverRate,_driverChannels,normalizedVolume);
    }


    private float ComputeRMSDelta()
    {
        FMODUnity.RuntimeManager.CoreSystem.getRecordPosition(_activeDriverIndex,out uint writePos);

        uint delta = (writePos>=_lastRecPos) ? writePos-_lastRecPos : _soundLengthSamples-_lastRecPos+writePos;
        if(delta==0) return 0f;

        uint byteOffset = _lastRecPos*(uint)sizeof(float)*(uint)_driverChannels;
        uint byteCount = delta*(uint)sizeof(float)*(uint)_driverChannels;
        _lastRecPos = writePos;

        FMOD.RESULT r = _recordingSound.@lock(byteOffset,byteCount,
            out IntPtr ptr1,out IntPtr ptr2,out uint len1,out uint len2);
        if(r!=FMOD.RESULT.OK) return 0f;

        float rms=0f;
        int total=0;

        if(ptr1!=IntPtr.Zero && len1>0)
        {
            int count = (int)(len1/sizeof(float));
            float[] buf = new float[count];
            Marshal.Copy(ptr1,buf,0,count);
            for(int i=0;i<count;i++) rms+=buf[i]*buf[i];
            total+=count;
        }
        if(ptr2!=IntPtr.Zero && len2>0)
        {
            int count = (int)(len2/sizeof(float));
            float[] buf = new float[count];
            Marshal.Copy(ptr2,buf,0,count);
            for(int i=0;i<count;i++) rms+=buf[i]*buf[i];
            total+=count;
        }

        _recordingSound.unlock(ptr1,ptr2,len1,len2);

        return total>0 ? Mathf.Sqrt(rms/total) : 0f;
    }

    private float GetSmoothedVolume(float raw)
    {
        if(_volumeHistory==null) _volumeHistory = new float[smoothFrames];

        _volumeHistory[_volumeHistoryIndex]=raw;
        _volumeHistoryIndex=(_volumeHistoryIndex+1)%smoothFrames;

        float sum=0f;
        foreach(var v in _volumeHistory) sum+=v;
        return sum/smoothFrames;
    }


    private void CheckDriverState()
    {
        FMOD.System core = FMODUnity.RuntimeManager.CoreSystem;

        if(_recording && _activeDriverIndex>=0)
        {
            core.getRecordDriverInfo(_activeDriverIndex,out _,256,out Guid _,out int _,out FMOD.SPEAKERMODE _,out int _,out FMOD.DRIVER_STATE state);
            if((state & FMOD.DRIVER_STATE.CONNECTED)==0) StopRecording();
        }

        if(!_recording) TryStartRecording();
    }

    private void TryStartRecording()
    {
        FMOD.System core = FMODUnity.RuntimeManager.CoreSystem;
        core.getRecordNumDrivers(out int numDrivers,out int _);
        if(numDrivers==0) return;

        for(int i=0;i<numDrivers;i++)
        {
            core.getRecordDriverInfo(i,out string name,256,out Guid _,out int rate,out FMOD.SPEAKERMODE _,out int channels,out FMOD.DRIVER_STATE state);
            if(name.ToLower().Contains("loopback")) continue;

            if((state & FMOD.DRIVER_STATE.CONNECTED)!=0)
            {
                StartRecordingOnDriver(i,rate,channels);
                return;
            }
        }
    }

    private void StartRecordingOnDriver(int index,int rate,int channels)
    {
        StopRecording();

        FMOD.System core = FMODUnity.RuntimeManager.CoreSystem;
        _driverRate=rate;
        _driverChannels=channels;

        FMOD.CREATESOUNDEXINFO ex = new FMOD.CREATESOUNDEXINFO();
        ex.cbsize=Marshal.SizeOf(ex);
        ex.numchannels=channels;
        ex.defaultfrequency=rate;
        ex.length=(uint)(rate*sizeof(float)*channels*BUFFER_SEC);
        ex.format=FMOD.SOUND_FORMAT.PCMFLOAT;

        core.createSound((string)null,FMOD.MODE.LOOP_NORMAL|FMOD.MODE.OPENUSER,ref ex,out _recordingSound);
        _recordingSound.getLength(out _soundLengthSamples,FMOD.TIMEUNIT.PCM);
        core.recordStart(index,_recordingSound,true);

        _activeDriverIndex=index;
        _recording=true;
        _lastRecPos=0;
        Array.Clear(_volumeHistory,0,_volumeHistory.Length);
        _volumeHistoryIndex=0;
    }

    private void StopRecording()
    {
        if(_recording && _activeDriverIndex>=0)
            FMODUnity.RuntimeManager.CoreSystem.recordStop(_activeDriverIndex);

        if(_recordingSound.hasHandle())
            _recordingSound.release();

        _recording=false;
        _activeDriverIndex=-1;
        _isCharging=false;
    }


    public void SetMuted(bool muted)=>_isMuted=muted;
    public bool IsMuted=>_isMuted;

    public void SetVoiceEnabledFromPrefs()
    {
        isEnabled = PlayerPrefs.GetInt(PREF_KEY,1)==1;
        if(isEnabled) TryStartRecording(); else StopRecording();
    }

    public int GetActiveDriverIndex()=>_activeDriverIndex;
    public void SelectMicrophone(int index,int rate,int channels)=>StartRecordingOnDriver(index,rate,channels);

    private void OnDestroy()=>StopRecording();
}