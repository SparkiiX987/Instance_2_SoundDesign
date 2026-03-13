using System;
using UnityEngine;

namespace Utils.TimerSystem
{
    public class TimerSystem : MonoBehaviour
    {
        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public float Elapsed => Duration - Remaining;
        public bool IsRunning { get; private set; }
        public bool IsFinished => Remaining <= 0;
        public float Progress => Duration > 0 ? Mathf.Clamp01(Elapsed / Duration) : 1f;

        public event Action onTimerStart;
        public event Action onTimerTick;
        public event Action onTimerComplete;
        public event Action onTimerCanceled;

        private float _startTime;

        public void Initialize(float duration)
        {
            Duration = duration;
            Remaining = duration;
            IsRunning = false;
        }

        private void OnStart()
        {
            if (IsRunning) return;
            
            _startTime = Time.time;
            IsRunning = true;
            onTimerStart?.Invoke();
        }
        
        public static TimerSystem NewTimer(float _duration)
        {
            TimerSystem ts = new GameObject("TimerSystem").AddComponent<TimerSystem>();
            ts.Initialize(_duration);
            ts.OnStart();
            return ts;
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Resume()
        {
            if (IsFinished) return;
            
            _startTime = Time.time - Elapsed;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
            Remaining = 0;
            onTimerCanceled?.Invoke();
        }

        public void Reset()
        {
            IsRunning = false;
            Remaining = Duration;
            _startTime = 0;
        }

        public void Reset(float newDuration)
        {
            Duration = newDuration;
            Reset();
        }

        public void Update()
        {
            if (!IsRunning) return;

            float elapsed = Time.time - _startTime;
            Remaining = Mathf.Max(0, Duration - elapsed);

            onTimerTick?.Invoke();

            if (IsFinished)
            {
                IsRunning = false;
                onTimerComplete?.Invoke();
            }
        }

        public string GetFormattedTime(string format = "0.00")
        {
            return Remaining.ToString(format);
        }

        public string GetFormattedTimeWithLabel(string label = "Timer", string format = "0.00")
        {
            return IsFinished ? "Timer End" : $"{label}: {GetFormattedTime(format)}";
        }
    }
}